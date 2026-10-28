#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Models.DirectPay;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EskaCommerce.Payments.Services.DirectPay.Interfaces;

namespace EskaCommerce.Payments.Services.DirectPay
{
    /// <summary>
    /// DirectPay Redirect Payment (Integration Guide v2.1, MessageID 1).
    ///
    /// Start: the order is made from the cart exactly as PayFortTransaction makes it (status
    /// PendingPayment, a payment row Initiated), then the signed Redirect Payment fields are
    /// answered; the browser posts them to DirectPay's hosted page, so card data never reaches us.
    ///
    /// Result: DirectPay sends the outcome twice, from the customer's browser (ResponseBackURL)
    /// and server to server (Update Notification). Both run through HandleResultAsync, which checks
    /// the SecureHash, the merchant, the amount and the currency, and records the outcome once.
    /// The statuses are the ones Order/User/OrderDetails already reports to the pages: payment
    /// Succeeded (1) / Failed (5) / OnHold (3), order PaymentReceived (30) / PaymentFailed (35).
    /// </summary>
    public class DirectPayService : IDirectPayService
    {
        public const string ProviderId = "DirectPay";

        /// <summary>Configuration key of the Authentication Token (see DirectPaySettings.AuthenticationToken).</summary>
        public const string TokenConfigKey = "DirectPay:AuthenticationToken";

        public const string GenericStartError = "We couldn't open the payment page. Please try again.";

        private const string RedirectPaymentMessageId = "1";
        private const string SuccessCode = "00000";
        private const int MaxTransactionIdLength = 20;

        /// <summary>How long a queued completion may take before a later success message runs it again.</summary>
        private static readonly TimeSpan CompletionGrace = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Codes that do not settle the payment: SADAD bill pending, double submission (the guide
        /// says to inquire), timeout, gateway communication error, inquiry "payment pending".
        /// The Update Notification brings the final status later.
        /// </summary>
        private static readonly HashSet<string> PendingCodes = new(StringComparer.Ordinal)
        {
            "40006", "00054", "00031", "01111", "00072",
        };

        /// <summary>ISO 4217 numeric code and minor units, by the order's currency code.</summary>
        private static readonly Dictionary<string, (string Code, int MinorUnits)> Currencies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SAR"] = ("682", 2),
            ["AED"] = ("784", 2),
            ["USD"] = ("840", 2),
            ["QAR"] = ("634", 2),
            ["EGP"] = ("818", 2),
            ["JOD"] = ("400", 3),
            ["KWD"] = ("414", 3),
            ["BHD"] = ("048", 3),
            ["OMR"] = ("512", 3),
        };

        /// <summary>Response fields never written to the log: card holder data and card tokens.</summary>
        private static readonly string[] UnloggedFieldParts = { "Expiry", "HolderName", "Token", "SecurityCode", "CVV" };

        /// <summary>
        /// One result at a time per order (striped, so nothing accumulates): the browser response and
        /// the notification can arrive together. In-process only; across processes, "paid" is claimed
        /// with a conditional update (Initiated to Succeeded), so only one message completes the order.
        /// </summary>
        private static readonly SemaphoreSlim[] OrderLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        private readonly IWorkContext _workContext;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IPaymentServices _paymentServices;
        private readonly IRepository<Payment> _payments;
        private readonly IRepository<Order> _orders;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _providers;
        private readonly IdcmsLogService _log;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public DirectPayService(
            IWorkContext workContext,
            ICartService cartService,
            IOrderService orderService,
            IPaymentServices paymentServices,
            IRepository<Payment> payments,
            IRepository<Order> orders,
            IRepositoryWithTypedId<PaymentProvider, string> providers,
            IdcmsLogService log,
            IHttpContextAccessor httpContextAccessor,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _workContext = workContext;
            _cartService = cartService;
            _orderService = orderService;
            _paymentServices = paymentServices;
            _payments = payments;
            _orders = orders;
            _providers = providers;
            _log = log;
            _httpContextAccessor = httpContextAccessor;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        #region Start

        public async Task<DirectPayStartResult> CreatePaymentAsync(DirectPayCreatePaymentRequest request, string requestHost)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var user = await _workContext.GetCurrentUser();
            if (user == null)
                throw new DirectPayException("Please sign in again and retry.", "No current user.");

            // Everything that can be checked is checked BEFORE the order exists, so a missing
            // setting or a bad return address never leaves an orphan order behind.
            var settings = await GetSettingsAsync(siteId);
            var returnUrl = CheckReturnUrl(request.ReturnUrl, requestHost, settings);
            var language = string.Equals(request.Language?.Trim(), "ar", StringComparison.OrdinalIgnoreCase) ? "Ar" : "En";

            var cart = await _cartService.GetActiveCart(user.Id, request.CartType, siteId, request.CartId);
            if (cart == null)
                throw new DirectPayException("Your cart is empty.", $"No active cart for user {user.Id} (CartId {request.CartId}).");

            var orderResult = await _orderService.CreateOrder(cart.Id, ProviderId, 0, siteId, OrderStatus.PendingPayment);
            if (!orderResult.Success)
                throw new DirectPayException(GenericStartError, $"CreateOrder for cart {cart.Id}: {orderResult.Error}");

            var order = orderResult.Value;
            string? transactionId = null;

            try
            {
                if (!Currencies.TryGetValue(order.CurrencyCode ?? string.Empty, out var currency))
                    throw new DirectPayException(GenericStartError, $"Currency '{order.CurrencyCode}' is not in the DirectPay currency list.");

                var minor = decimal.Round(order.OrderTotal * Pow10(currency.MinorUnits), 0, MidpointRounding.AwayFromZero);
                if (minor <= 0 || minor > 999_999_999m)
                    throw new DirectPayException(GenericStartError, $"Order total {order.OrderTotal} {order.CurrencyCode} cannot be paid.");

                var amount = minor.ToString("0", CultureInfo.InvariantCulture);
                transactionId = NewTransactionId(order.Id);

                await _paymentServices.CreatePayment(order, transactionId, ProviderId, null, PaymentStatus.Initiated);

                var fields = RequestFields(settings, transactionId, amount, currency.Code, language);
                fields[DirectPaySecureHash.RequestHashField] = DirectPaySecureHash.Compute(settings.AuthenticationToken!, fields);

                await SaveRecordAsync(order.Id, new DirectPayPaymentRecord
                {
                    TransactionId = transactionId,
                    Amount = amount,
                    CurrencyIsoCode = currency.Code,
                    ReturnUrl = returnUrl,
                    Language = language,
                    CultureId = _workContext.GetCurrentCultureId(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    RequestHash = fields[DirectPaySecureHash.RequestHashField],
                });

                await _log.AddInfo("DirectPay.CreatePayment",
                    $"order {order.Id} transaction {transactionId} amount {amount} currency {currency.Code}", null, siteId);

                return new DirectPayStartResult
                {
                    Url = settings.PaymentUrl!.Trim(),
                    Fields = fields,
                    TransactionId = transactionId,
                };
            }
            catch (Exception ex)
            {
                await RecordStartFailureAsync(order, transactionId, ex);
                if (ex is DirectPayException refusal)
                    throw new DirectPayException(refusal.Message, $"order {order.Id}: {refusal.Detail ?? refusal.Message}");
                throw new DirectPayException(GenericStartError, $"order {order.Id}: {ex}");
            }
        }

        /// <summary>The Redirect Payment request's fields, without the SecureHash.</summary>
        private static SortedDictionary<string, string> RequestFields(
            DirectPaySettings settings, string transactionId, string amount, string currencyCode, string language)
        {
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["MessageID"] = RedirectPaymentMessageId,
                ["TransactionID"] = transactionId,
                ["MerchantID"] = settings.MerchantId!.Trim(),
                ["Amount"] = amount,
                ["CurrencyISOCode"] = currencyCode,
                ["Language"] = language,
                ["Channel"] = string.IsNullOrWhiteSpace(settings.Channel) ? "0" : settings.Channel.Trim(),
                ["Quantity"] = "1",
                ["Version"] = string.IsNullOrWhiteSpace(settings.Version) ? "2.0" : settings.Version.Trim(),
                ["ResponseBackURL"] = settings.ResponseBackUrl!.Trim(),
            };
            if (!string.IsNullOrWhiteSpace(settings.ThemeId))
                fields["ThemeID"] = settings.ThemeId.Trim();
            return fields;
        }

        /// <summary>
        /// The request's hash the browser was given: kept with the payment, or for a payment started
        /// before it was kept, made again from the same values.
        /// </summary>
        private static string RequestHashOf(DirectPayPaymentRecord? record, DirectPaySettings settings, string transactionId)
        {
            if (!string.IsNullOrEmpty(record?.RequestHash))
                return record.RequestHash;
            if (record == null)
                return string.Empty;
            return DirectPaySecureHash.Compute(settings.AuthenticationToken!,
                RequestFields(settings, transactionId, record.Amount, record.CurrencyIsoCode, record.Language));
        }

        /// <summary>
        /// Letters and digits only, at most 20 characters: the order id, "T", then random
        /// characters. Random rather than a timestamp: a result is matched to its order by this id,
        /// so it must not be guessable. Unique across environments that share the merchant, too.
        /// </summary>
        private static string NewTransactionId(long orderId)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var id = orderId.ToString(CultureInfo.InvariantCulture);
            var room = MaxTransactionIdLength - id.Length - 1;
            if (room < 8)
                throw new DirectPayException(GenericStartError, $"Order id {orderId} is too long for a DirectPay TransactionID.");
            return id + "T" + RandomNumberGenerator.GetString(alphabet, room);
        }

        private static bool TryParseOrderId(string? transactionId, out long orderId)
        {
            orderId = 0;
            if (string.IsNullOrWhiteSpace(transactionId) || transactionId.Length > MaxTransactionIdLength)
                return false;
            var cut = transactionId.IndexOf('T');
            return cut > 0
                && long.TryParse(transactionId.AsSpan(0, cut), NumberStyles.None, CultureInfo.InvariantCulture, out orderId)
                && orderId > 0;
        }

        private static decimal Pow10(int n)
        {
            decimal p = 1;
            for (var i = 0; i < n; i++) p *= 10;
            return p;
        }

        private static string CheckReturnUrl(string? value, string requestHost, DirectPaySettings settings)
        {
            if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new DirectPayException(GenericStartError, $"ReturnUrl '{value}' is not an absolute http/https address.");

            var allowed = (settings.AllowedReturnHosts ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Append(requestHost);
            if (!allowed.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
                throw new DirectPayException(GenericStartError, $"ReturnUrl host '{uri.Host}' is not allowed (request host '{requestHost}', AllowedReturnHosts '{settings.AllowedReturnHosts}').");

            // The page itself, escaped: no user info, query or fragment (?OrderId= is appended on the way back).
            var clean = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
            if (clean.Length > 1000)
                throw new DirectPayException(GenericStartError, "ReturnUrl is longer than 1000 characters.");
            return clean;
        }

        private async Task RecordStartFailureAsync(Order order, string? transactionId, Exception ex)
        {
            try
            {
                var message = ex is DirectPayException refusal ? refusal.Detail ?? refusal.Message : ex.GetBaseException().Message;
                var exists = await _payments.Query().AsNoTracking()
                    .AnyAsync(p => p.OrderId == order.Id && p.PaymentMethod == ProviderId);
                if (exists)
                    await _paymentServices.UpdatePayment(order, transactionId, ProviderId, message, PaymentStatus.Failed);
                else
                    await _paymentServices.CreatePayment(order, transactionId, ProviderId, message, PaymentStatus.Failed);
            }
            catch
            {
                // The start already failed; the original exception is the one to report.
            }
        }

        #endregion

        #region Result

        public async Task<DirectPayOutcome> HandleResultAsync(IReadOnlyDictionary<string, string> fields, string source)
        {
            var module = $"DirectPay.{source}";
            var outcome = new DirectPayOutcome { Kind = DirectPayOutcomeKind.Unknown };
            var transactionId = Field(fields, "Response.TransactionID");
            var safeFields = ForLog(fields);

            try
            {
                if (!TryParseOrderId(transactionId, out var orderId))
                {
                    if (fields.Count > 0)
                        await _log.AddError(module, "Unknown transaction", $"TransactionID '{transactionId}'", safeFields, null);
                    return outcome;
                }

                var payment = await _payments.Query()
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PaymentMethod == ProviderId);
                if (payment == null || !string.Equals(payment.GatewayTransactionId, transactionId, StringComparison.Ordinal))
                {
                    await _log.AddError(module, "No DirectPay payment for this transaction", $"order {orderId} TransactionID '{transactionId}'", safeFields, null);
                    return outcome;
                }

                var record = ReadRecord(payment.PaymentReceipt);
                outcome.OrderId = orderId;
                outcome.ReturnUrl = record?.ReturnUrl;

                var order = await _orders.Query().AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    await _log.AddError(module, "Order not found", $"order {orderId}", safeFields, null);
                    outcome.Kind = DirectPayOutcomeKind.Error;
                    return outcome;
                }
                // The order services read the site, user and culture from these request headers
                // (a gateway post carries none of them); the same thing PayFort's callback does.
                SetWorkContextHeaders(_httpContextAccessor.HttpContext, order.SiteId, order.CustomerId, record?.CultureId ?? 0, order.CartId);

                // The hash covers values only, in name order, with no separators. The browser holds a
                // valid hash: its own request's. Posting the request's values under made-up Response.*
                // names, cut so they join into the same string, would verify. So a response must name
                // our merchant (DirectPay always sends it) and must not carry the request's hash.
                var settings = await GetSettingsAsync(order.SiteId);
                var merchantId = Field(fields, "Response.MerchantID");
                var receivedHash = Field(fields, DirectPaySecureHash.ResponseHashField).Trim();
                if (!DirectPaySecureHash.Verify(settings.AuthenticationToken!, fields)
                    || !string.Equals(merchantId, settings.MerchantId!.Trim(), StringComparison.Ordinal)
                    || string.Equals(receivedHash, RequestHashOf(record, settings, transactionId!), StringComparison.OrdinalIgnoreCase))
                {
                    await _log.AddError(module, "SecureHash or MerchantID does not match: nothing recorded",
                        $"order {orderId}", safeFields, order.SiteId);
                    outcome.Kind = DirectPayOutcomeKind.Rejected;
                    return outcome;
                }

                var gate = OrderLocks[(int)(orderId % OrderLocks.Length)];
                await gate.WaitAsync();
                try
                {
                    outcome.Kind = await SettleAsync(order, payment.Id, record, fields, source, safeFields);
                }
                finally
                {
                    gate.Release();
                }

                return outcome;
            }
            catch (Exception ex)
            {
                // Keep the return address: the customer still goes back to the page, which reads
                // the order's real status and keeps asking while it is not settled.
                var detail = ex is DirectPayException refusal ? refusal.Detail ?? refusal.Message : ex.ToString();
                await _log.AddError(module, ex.GetBaseException().Message, detail, safeFields, null);
                outcome.Kind = DirectPayOutcomeKind.Error;
                return outcome;
            }
        }

        private async Task<DirectPayOutcomeKind> SettleAsync(
            Order order, long paymentId, DirectPayPaymentRecord? record,
            IReadOnlyDictionary<string, string> fields, string source, string safeFields)
        {
            var module = $"DirectPay.{source}";
            var code = Field(fields, "Response.StatusCode");
            var description = Field(fields, "Response.StatusDescription");
            var transactionId = Field(fields, "Response.TransactionID");
            var result = new DirectPayResultRecord
            {
                StatusCode = code,
                StatusDescription = description,
                GatewayName = Field(fields, "Response.GatewayName"),
                GatewayStatusCode = Field(fields, "Response.GatewayStatusCode"),
                GatewayStatusDescription = Field(fields, "Response.GatewayStatusDescription"),
                PaymentMethod = Field(fields, "Response.PaymentMethod"),
                Rrn = Field(fields, "Response.RRN"),
                ApprovalCode = Field(fields, "Response.ApprovalCode"),
                MaskedCardNumber = MaskCardNumber(Field(fields, "Response.CardNumber")),
                Amount = Field(fields, "Response.Amount"),
                CurrencyIsoCode = Field(fields, "Response.CurrencyISOCode"),
                Source = source,
                ReceivedAt = DateTimeOffset.UtcNow,
            };

            // Fresh from the database: the other DirectPay message may have settled it meanwhile.
            var paymentStatus = await _payments.Query().AsNoTracking()
                .Where(p => p.Id == paymentId).Select(p => p.Status).FirstAsync();
            var orderStatus = await _orders.Query().AsNoTracking()
                .Where(o => o.Id == order.Id).Select(o => o.OrderStatus).FirstAsync();

            if (paymentStatus == PaymentStatus.Succeeded || orderStatus == OrderStatus.PaymentReceived)
            {
                // Paid, but the order never moved (the completion failed, or the app pool recycled
                // under it): a later success message finishes it. Not while a completion may still
                // be running.
                var paidAt = await _payments.Query().AsNoTracking()
                    .Where(p => p.Id == paymentId).Select(p => p.LatestUpdatedOn).FirstAsync();
                if (code == SuccessCode && paymentStatus == PaymentStatus.Succeeded && orderStatus == OrderStatus.PendingPayment
                    && paidAt < DateTimeOffset.UtcNow - CompletionGrace)
                {
                    // Claimed too: of two late messages, only one completes it again.
                    var reclaimed = await _payments.Query()
                        .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Succeeded && p.LatestUpdatedOn == paidAt)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.LatestUpdatedOn, DateTimeOffset.UtcNow));
                    if (reclaimed > 0)
                    {
                        await _log.AddError(module, "Paid but the order is still waiting for payment: completing it again",
                            $"order {order.Id} paid at {paidAt:o}", null, order.SiteId);
                        QueueCompletion(order.Id, order.SiteId, order.CustomerId, record?.CultureId ?? 0, order.CartId);
                        return DirectPayOutcomeKind.Paid;
                    }
                }

                await _log.AddInfo(module, $"order {order.Id} already paid; {code} ignored", null, order.SiteId);
                return DirectPayOutcomeKind.AlreadySettled;
            }

            if (paymentStatus != PaymentStatus.Initiated || orderStatus != OrderStatus.PendingPayment)
            {
                if (code == SuccessCode && paymentStatus != PaymentStatus.OnHold)
                {
                    // Money was taken for an order that is no longer waiting for it: refused by an
                    // earlier message, or cancelled by the system after an hour unpaid. Never
                    // complete or refuse that automatically: hold it for a person to refund or complete.
                    await _paymentServices.UpdatePayment(order, transactionId, ProviderId,
                        $"Paid after the order became {orderStatus} (payment was {paymentStatus}): refund or complete it",
                        PaymentStatus.OnHold);
                    await SaveResultAsync(paymentId, record, transactionId, result);
                    await _log.AddError(module, "Paid for an order that is no longer waiting for payment: payment put on hold",
                        $"order {order.Id} order status {orderStatus} payment status {paymentStatus}", safeFields, order.SiteId);
                    return DirectPayOutcomeKind.Pending;
                }

                await _log.AddInfo(module, $"order {order.Id} already settled ({paymentStatus}/{orderStatus}); {code} ignored", null, order.SiteId);
                return DirectPayOutcomeKind.AlreadySettled;
            }

            if (code == SuccessCode)
            {
                var amountMatches = record != null
                    && SameNumber(result.Amount, record.Amount)
                    && SameNumber(result.CurrencyIsoCode, record.CurrencyIsoCode);

                if (!amountMatches)
                {
                    // DirectPay says paid, but not the amount or currency this order asked for.
                    // Never mark that paid or refused automatically: hold it for a person to look at.
                    await _paymentServices.UpdatePayment(order, transactionId, ProviderId,
                        $"Amount or currency mismatch: received {result.Amount} {result.CurrencyIsoCode}, expected {record?.Amount} {record?.CurrencyIsoCode}",
                        PaymentStatus.OnHold);
                    await SaveResultAsync(paymentId, record, transactionId, result);
                    await _log.AddError(module, "Paid amount or currency does not match the order: payment put on hold",
                        $"order {order.Id}", safeFields, order.SiteId);
                    return DirectPayOutcomeKind.Pending;
                }

                // Claimed in the database, so a second process handling the other message cannot
                // mark it paid again. The result is written before the completion moves the order
                // (the server flows read its RRN and card type then), and the completion is queued
                // even if writing it fails, so a paid order is never left waiting.
                var claimed = await _payments.Query()
                    .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Initiated)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Status, PaymentStatus.Succeeded)
                        .SetProperty(p => p.LatestUpdatedOn, DateTimeOffset.UtcNow));
                if (claimed == 0)
                {
                    await _log.AddInfo(module, $"order {order.Id} settled by the other message meanwhile; {code} ignored", null, order.SiteId);
                    return DirectPayOutcomeKind.AlreadySettled;
                }

                try
                {
                    await _paymentServices.UpdatePayment(order, transactionId, ProviderId, description, PaymentStatus.Succeeded);
                    await SaveResultAsync(paymentId, record, transactionId, result);
                }
                finally
                {
                    QueueCompletion(order.Id, order.SiteId, order.CustomerId, record!.CultureId, order.CartId);
                }
                await _log.AddInfo(module, $"order {order.Id} paid ({result.PaymentMethod} RRN {result.Rrn}) - completion queued", null, order.SiteId);
                return DirectPayOutcomeKind.Paid;
            }

            if (PendingCodes.Contains(code))
            {
                await SaveResultAsync(paymentId, record, transactionId, result);
                await _log.AddInfo(module, $"order {order.Id} not settled yet: {code} {description}", safeFields, order.SiteId);
                return DirectPayOutcomeKind.Pending;
            }

            var reason = string.IsNullOrWhiteSpace(description) ? $"DirectPay status {code}" : $"{code} {description}";
            await _paymentServices.UpdatePayment(order, transactionId, ProviderId, reason, PaymentStatus.Failed);
            await SaveResultAsync(paymentId, record, transactionId, result);
            await _orderService.OrderChangeStatus(order.Id, OrderStatus.PaymentFailed, reason, order.SiteId);
            await _log.AddInfo(module, $"order {order.Id} failed: {reason}", null, order.SiteId);
            return DirectPayOutcomeKind.Failed;
        }

        private static string Field(IReadOnlyDictionary<string, string> fields, string key)
        {
            return fields.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }

        private static bool SameNumber(string? a, string? b)
        {
            return decimal.TryParse(a, NumberStyles.Number, CultureInfo.InvariantCulture, out var x)
                && decimal.TryParse(b, NumberStyles.Number, CultureInfo.InvariantCulture, out var y)
                && x == y;
        }

        /// <summary>DirectPay sends the number masked already; never keep more than the first 6 and last 4 digits.</summary>
        private static string MaskCardNumber(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Count(char.IsDigit) <= 10 || trimmed.Length <= 10)
                return trimmed;
            return trimmed[..6] + new string('*', trimmed.Length - 10) + trimmed[^4..];
        }

        /// <summary>The received fields as JSON for the log, without card holder data or card tokens.</summary>
        private static string ForLog(IReadOnlyDictionary<string, string> fields)
        {
            var safe = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                if (UnloggedFieldParts.Any(part => field.Key.Contains(part, StringComparison.OrdinalIgnoreCase)))
                    continue;
                safe[field.Key] = field.Key == "Response.CardNumber" ? MaskCardNumber(field.Value ?? string.Empty) : field.Value ?? string.Empty;
            }
            return JsonConvert.SerializeObject(safe);
        }

        /// <summary>
        /// Order status, cart and receipt, on a detached scope so it survives the browser's
        /// redirect (or DirectPay closing the notification) - the same pattern as PayFort's
        /// QueueOrderCompletion. Only ids cross the scope boundary.
        ///
        /// The status goes first: "paid" is what starts the server flows (e.g. issuing the policy),
        /// which the return page reads. The receipt e-mail is slowest, so last.
        /// </summary>
        private void QueueCompletion(long orderId, long siteId, long customerId, long cultureId, long? cartId)
        {
            // The task must not inherit this request's execution context: IHttpContextAccessor is
            // AsyncLocal, and setting the synthetic context in an inherited flow would clear the
            // current request's HttpContext while the request is still running.
            using var noFlow = ExecutionContext.IsFlowSuppressed() ? default(AsyncFlowControl?) : ExecutionContext.SuppressFlow();
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sp = scope.ServiceProvider;

                    // WorkContext caches IHttpContextAccessor.HttpContext when it is created, so the
                    // synthetic context has to be in place before anything resolves it.
                    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var synthetic = new DefaultHttpContext { RequestServices = sp };
                    SetWorkContextHeaders(synthetic, siteId, customerId, cultureId, cartId);
                    accessor.HttpContext = synthetic;

                    var log = sp.GetRequiredService<IdcmsLogService>();
                    var orders = sp.GetRequiredService<IOrderService>();

                    var orderRows = sp.GetRequiredService<IRepository<Order>>();
                    Task<OrderStatus> StatusNow() => orderRows.Query().AsNoTracking()
                        .Where(o => o.Id == orderId).Select(o => o.OrderStatus).FirstAsync();

                    var before = await StatusNow();
                    try { await orders.OrderChangeStatus(orderId, OrderStatus.PaymentReceived, "", siteId); }
                    catch (Exception ex)
                    {
                        await log.AddError("DirectPay.Complete.OrderChangeStatus", ex.Message, ex.StackTrace, null, siteId);
                        // The status is saved before the order's event handlers run, so a handler
                        // failing still leaves the order moved: this completion then finishes it.
                        // Not moved, or moved by an earlier completion: the cart and the receipt
                        // belong to the completion that moved it, so they are not done twice.
                        if (before == OrderStatus.PaymentReceived || await StatusNow() != OrderStatus.PaymentReceived)
                        {
                            accessor.HttpContext = null;
                            return;
                        }
                    }

                    try
                    {
                        if (cartId.HasValue)
                            await orders.DeActiveCart(cartId.Value);
                    }
                    catch (Exception ex) { await log.AddError("DirectPay.Complete.DeActiveCart", ex.Message, ex.StackTrace, null, siteId); }

                    try { await SendReceiptAsync(orders, orderId); }
                    catch (Exception ex) { await log.AddError("DirectPay.Complete.SendReceipt", ex.Message, ex.StackTrace, null, siteId); }

                    await log.AddInfo("DirectPay.Complete", $"order {orderId} completed", null, siteId);
                    accessor.HttpContext = null;
                }
                catch (Exception ex)
                {
                    // Nothing may escape a detached task; the failure is still worth a log line.
                    try
                    {
                        using var logScope = _scopeFactory.CreateScope();
                        await logScope.ServiceProvider.GetRequiredService<IdcmsLogService>()
                            .AddError("DirectPay.Complete", ex.Message, ex.ToString(), null, siteId);
                    }
                    catch
                    {
                        // The log itself is unavailable.
                    }
                }
            });
        }

        /// <summary>
        /// Kept in its own method: SendReceipt gained its second parameter on 2026-09-27, and against
        /// an older EskaCommerce.Orders.dll the call fails when its method is compiled. Here that is
        /// a logged error, and the order status and cart steps still run.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task SendReceiptAsync(IOrderService orders, long orderId)
        {
            return orders.SendReceipt(orderId);
        }

        private static void SetWorkContextHeaders(HttpContext? context, long siteId, long customerId, long cultureId, long? cartId)
        {
            if (context == null)
                return;
            var headers = context.Request.Headers;
            headers["siteId"] = siteId.ToString(CultureInfo.InvariantCulture);
            headers["userId"] = customerId.ToString(CultureInfo.InvariantCulture);
            if (cultureId > 0)
                headers["cultureId"] = cultureId.ToString(CultureInfo.InvariantCulture);
            if (cartId.HasValue)
                headers["CartId"] = cartId.Value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Settings and the payment record

        private async Task<DirectPaySettings> GetSettingsAsync(long siteId)
        {
            var row = await _providers.Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ProviderId && x.SiteId == siteId);
            if (row == null)
                throw Unavailable($"No Payments_PaymentProvider row with Id '{ProviderId}' for site {siteId}.");

            JObject json;
            try
            {
                json = JObject.Parse(string.IsNullOrWhiteSpace(row.AdditionalSettings) ? "{}" : row.AdditionalSettings);
            }
            catch (JsonException)
            {
                throw Unavailable($"Payments_PaymentProvider '{ProviderId}' AdditionalSettings is not valid JSON.");
            }

            // The token must never sit in the provider row (see DirectPaySettings.AuthenticationToken):
            // refuse to run until it is removed, rather than use a token that may have leaked.
            if (json.Properties().Any(p => string.Equals(p.Name, nameof(DirectPaySettings.AuthenticationToken), StringComparison.OrdinalIgnoreCase)
                                           && !string.IsNullOrWhiteSpace(p.Value?.ToString())))
                throw Unavailable($"Payments_PaymentProvider '{ProviderId}' AdditionalSettings contains an AuthenticationToken. "
                    + $"Remove it (api/payments-providers shows AdditionalSettings to any signed-in visitor) and set {TokenConfigKey} in the server configuration instead.");

            var settings = json.ToObject<DirectPaySettings>() ?? new DirectPaySettings();
            settings.AuthenticationToken = _configuration[TokenConfigKey]?.Trim();

            if (string.IsNullOrEmpty(settings.AuthenticationToken))
                throw Unavailable($"{TokenConfigKey} is not set (server appsettings.json, or the DirectPay__AuthenticationToken environment variable).");

            if (string.IsNullOrWhiteSpace(settings.MerchantId)
                || !IsAbsoluteHttpUrl(settings.PaymentUrl)
                || !IsAbsoluteHttpUrl(settings.ResponseBackUrl))
                throw Unavailable($"Payments_PaymentProvider '{ProviderId}' AdditionalSettings needs MerchantId, PaymentUrl and ResponseBackUrl (absolute http/https addresses).");

            return settings;
        }

        private static DirectPayException Unavailable(string detail)
        {
            return new DirectPayException("DirectPay is not available right now.", detail);
        }

        private static bool IsAbsoluteHttpUrl(string? value)
        {
            return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        private static DirectPayPaymentRecord? ReadRecord(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                var record = JsonConvert.DeserializeObject<DirectPayPaymentRecord>(json);
                return record != null && record.Gateway == ProviderId ? record : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task SaveRecordAsync(long orderId, DirectPayPaymentRecord record)
        {
            var payment = await _payments.Query()
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PaymentMethod == ProviderId);
            if (payment == null)
                throw new InvalidOperationException($"No DirectPay payment row for order {orderId}.");
            payment.PaymentReceipt = JsonConvert.SerializeObject(record);
            await _payments.SaveChangesAsync();
        }

        private async Task SaveResultAsync(long paymentId, DirectPayPaymentRecord? record, string transactionId, DirectPayResultRecord result)
        {
            var payment = await _payments.Query().FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null)
                return;
            record ??= new DirectPayPaymentRecord { TransactionId = transactionId };
            record.Result = result;
            payment.PaymentReceipt = JsonConvert.SerializeObject(record);
            payment.LatestUpdatedOn = DateTimeOffset.UtcNow;
            await _payments.SaveChangesAsync();
        }

        #endregion
    }
}
