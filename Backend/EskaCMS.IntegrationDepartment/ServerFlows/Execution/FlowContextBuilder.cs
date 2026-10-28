using System.Globalization;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Helper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>The facts about an order the trigger and the runner decide on.</summary>
    public sealed class OrderFacts
    {
        public long OrderId { get; init; }
        public long SiteId { get; init; }
        public long CustomerId { get; init; }
        public long? CartId { get; init; }
        public long CultureId { get; init; }
        public OrderStatus Status { get; init; }
        public decimal OrderTotal { get; init; }
        public IReadOnlyList<long> ProductIds { get; init; } = Array.Empty<long>();

        /// <summary>The successful payment, if any.</summary>
        public string? PaymentMethod { get; init; }
        public decimal? PaidAmount { get; init; }
        public string? GatewayTransactionId { get; init; }

        /// <summary>
        /// A successful payment with a gateway transaction, by one of <paramref name="methods"/>
        /// ("*" = any gateway). The payment read here is the latest Succeeded row that is NOT one of
        /// DCMS's own coupon / points / refund / cash-on-delivery rows, so an order paid only that way
        /// is never verified. What is checked: Succeeded status, a non-empty transaction id, the method.
        /// </summary>
        public bool IsVerifiedPayment(IEnumerable<string> methods)
        {
            return PaymentMethod != null
                && !string.IsNullOrWhiteSpace(GatewayTransactionId)
                && methods.Any(m => m == FlowSettings.AnyPaymentMethod || string.Equals(m, PaymentMethod, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The payment method the order was placed with (Order.PaymentMethod, e.g. "SADAD").</summary>
        public string? OrderPaymentMethod { get; init; }

        /// <summary>
        /// How the order was paid: its successful payment's method, else the method it was placed
        /// with (an order settled outside DCMS, such as a SADAD bill, has no payment row).
        /// </summary>
        public string? PaidBy => !string.IsNullOrWhiteSpace(PaymentMethod) ? PaymentMethod : OrderPaymentMethod;

        /// <summary>Whether the order was paid by one of <paramref name="methods"/> (a flow's skipPaymentMethods).</summary>
        public bool IsPaidByOneOf(IEnumerable<string> methods) =>
            PaidBy is { } paidBy && methods.Any(m => string.Equals(m, paidBy.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>An order status change's run: its data, the order, and who and where its calls run as (culture 0: the site's default language).</summary>
    public sealed record StatusChangeContext(JObject Context, long SubjectId, long UserId, long? CartId, long CultureId);

    /// <summary>
    /// Builds the data a run reads, once, when the run is created: { order, cart, additionalDetails }.
    /// It is frozen on the run row, so a retry next week sends exactly what the first try sent, and
    /// nothing the customer changes in their cart afterwards can reach the insurer.
    ///
    ///  order: id, reference, status, currency, total, subTotal, discount, tax, createdOn, siteId,
    ///         cartId, customer { id, name, email, phone }, items [ { productId, quantity, price, additionalDetails } ],
    ///         payment { method, status, amount, transactionId, rrn, approvalCode, cardType }
    ///  cart: { id, items, total } (the same items, as the cart looked when it became the order)
    ///  additionalDetails: the first item's additional details (insurance journeys have one product)
    /// </summary>
    public sealed class FlowContextBuilder
    {
        private static readonly JsonSerializerSettings ReadSettings = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 64 };

        private readonly IRepository<Order> _orders;
        private readonly IRepository<OrderItem> _items;
        private readonly IRepository<Payment> _payments;
        private readonly IRepository<User> _users;
        private readonly IRepository<Cart> _carts;
        private readonly IRepository<UserOptions> _userOptions;
        private readonly IRepository<OrderHistory> _history;
        private readonly IRepository<UsersSites> _userSites;

        public FlowContextBuilder(IRepository<Order> orders, IRepository<OrderItem> items, IRepository<Payment> payments, IRepository<User> users,
            IRepository<Cart> carts, IRepository<UserOptions> userOptions, IRepository<OrderHistory> history, IRepository<UsersSites> userSites)
        {
            _orders = orders;
            _items = items;
            _payments = payments;
            _users = users;
            _carts = carts;
            _userOptions = userOptions;
            _history = history;
            _userSites = userSites;
        }

        /// <summary>The visitor's own active cart of the trigger's cartType (the newest), if any.</summary>
        public async Task<long?> FindPageCartIdAsync(long siteId, long userId, int cartType, CancellationToken ct = default)
        {
            var type = (CartTypes)cartType;
            return await _carts.Query().AsNoTracking()
                .Where(c => c.CustomerId == userId && c.IsActive && c.CartType == type && c.SiteId == siteId)
                .OrderByDescending(c => c.LatestUpdatedOn)
                .Select(c => (long?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// The data a run a page starts reads, frozen on the run:
        ///  { input, user, cart, additionalDetails, caller }.
        /// The cart is the one <see cref="FindPageCartIdAsync"/> found (the server finds it; the page
        /// never names a cart), read again only if it still belongs to the visitor. additionalDetails
        /// are its first item's, as for an order. caller is who and where the calls run as:
        /// { siteId, userId, cultureId, cartId }.
        /// </summary>
        public async Task<JObject> BuildPageAsync(long siteId, long userId, long cultureId, long? cartId, JObject input, CancellationToken ct = default)
        {
            var user = await UserTokenAsync(userId, withOptions: true, ct);

            var cart = cartId == null ? null : await _carts.Query().AsNoTracking()
                .Where(c => c.Id == cartId && c.CustomerId == userId && c.SiteId == siteId)
                .Select(c => new
                {
                    c.Id,
                    c.ReferenceId,
                    Items = c.Items.OrderBy(i => i.Id).Select(i => new { i.ProductId, i.Quantity, i.Price, i.AdditionalDetails }).ToList(),
                })
                .FirstOrDefaultAsync(ct);

            JObject? cartToken = null;
            JToken? additionalDetails = null;
            if (cart != null)
            {
                var items = new JArray(cart.Items.Select(i => new JObject
                {
                    ["productId"] = i.ProductId,
                    ["quantity"] = i.Quantity,
                    ["price"] = i.Price,
                    ["additionalDetails"] = ParseJson(i.AdditionalDetails),
                }));
                cartToken = new JObject { ["id"] = cart.Id, ["reference"] = cart.ReferenceId, ["items"] = items };
                additionalDetails = items.Select(i => i["additionalDetails"]).FirstOrDefault(d => d != null && d.Type != JTokenType.Null)?.DeepClone();
            }

            return new JObject
            {
                ["input"] = input,
                ["user"] = user,
                ["cart"] = cartToken,
                ["additionalDetails"] = additionalDetails,
                ["caller"] = new JObject
                {
                    ["siteId"] = siteId,
                    ["userId"] = userId,
                    ["cultureId"] = cultureId,
                    ["cartId"] = cart?.Id,
                },
            };
        }

        /// <summary>
        /// An order's status change (trigger "orderStatus"): the order as a payment flow reads it, plus
        /// statusChange { changeId, from, to, at }. Null when it is not of this site.
        /// </summary>
        public async Task<StatusChangeContext?> OrderStatusChangeAsync(long siteId, long changeId, CancellationToken ct = default)
        {
            var change = await _history.Query().AsNoTracking()
                .Where(h => h.Id == changeId && h.Order.SiteId == siteId)
                .Select(h => new { h.OrderId, h.OldStatus, h.NewStatus, h.CreatedOn })
                .FirstOrDefaultAsync(ct);
            // the order, its items and its payment are read once, for its facts and its data
            var read = change == null ? null : await ReadOrderAsync(change.OrderId, ct);
            if (read == null)
                return null;
            var (facts, context) = read.Value;

            context["statusChange"] = new JObject
            {
                ["changeId"] = changeId,
                ["from"] = change!.OldStatus?.ToString(),
                ["to"] = change.NewStatus.ToString(),
                ["at"] = change.CreatedOn.ToString("O", CultureInfo.InvariantCulture),
            };
            var cultureId = facts.CultureId > 0 ? facts.CultureId : await UserCultureIdAsync(siteId, facts.CustomerId, ct);
            return new StatusChangeContext(context, facts.OrderId, facts.CustomerId, facts.CartId, cultureId);
        }

        /// <summary>The language a user uses on the site: their own for the site, else their default (0 when neither is set).</summary>
        private async Task<long> UserCultureIdAsync(long siteId, long userId, CancellationToken ct)
        {
            var own = await _userSites.Query().AsNoTracking()
                .Where(s => s.UserId == userId && s.SiteId == siteId)
                .Select(s => s.CultureId)
                .FirstOrDefaultAsync(ct);
            if (own is > 0)
                return own.Value;
            return await _users.Query().AsNoTracking().Where(u => u.Id == userId).Select(u => u.DefaultCultureId).FirstOrDefaultAsync(ct) ?? 0;
        }

        /// <summary>
        /// The data a webhook run reads: { input, webhook { at, by, key?, address?, viaCloudflare? }, caller };
        /// it calls as the site. by: "signature" or "key"; key: the signing key's name and fingerprint.
        /// </summary>
        public static JObject WebhookContext(long siteId, JObject input, long cultureId, DateTimeOffset at, JObject? proof = null)
        {
            // how the call proved it was the other system's, and where it came from (for the run's record)
            var moment = Moment(at, null);
            foreach (var p in proof?.Properties() ?? Enumerable.Empty<JProperty>())
                moment[p.Name] = p.Value;
            return new JObject
            {
                ["input"] = input,
                ["webhook"] = moment,
                ["caller"] = SiteCaller(siteId, cultureId),
            };
        }

        /// <summary>The data a scheduled run reads: { schedule { at, startedBy? }, caller }; it calls as the site.</summary>
        public static JObject ScheduleContext(long siteId, long cultureId, DateTimeOffset at, long? startedBy) => new()
        {
            ["schedule"] = Moment(at, startedBy),
            ["caller"] = SiteCaller(siteId, cultureId),
        };

        /// <summary>When a run started, and the admin who started it with Try it.</summary>
        private static JObject Moment(DateTimeOffset at, long? startedBy)
        {
            var moment = new JObject { ["at"] = at.ToString("O", CultureInfo.InvariantCulture) };
            if (startedBy != null)
                moment["startedBy"] = startedBy.Value;
            return moment;
        }

        /// <summary>Who and where the calls run as: the site, as no one in particular, in a site language.</summary>
        private static JObject SiteCaller(long siteId, long cultureId) =>
            new() { ["siteId"] = siteId, ["userId"] = 0, ["cultureId"] = cultureId, ["cartId"] = null };

        /// <summary>A paid order's data as its payment run reads it: the order, plus the language its payment was made in.</summary>
        public async Task<JObject?> BuildPaidOrderAsync(OrderFacts facts, CancellationToken ct = default)
        {
            var context = await BuildAsync(facts.OrderId, ct);
            if (context != null)
                context["order"]!["cultureId"] = facts.CultureId;
            return context;
        }

        /// <summary>
        /// A user as flows read it, with the names the pages give the signed-in user (Logged In User,
        /// User Option, Current Username): id, username, fullName, email, mobilephone, userType
        /// (DCMS's user type, e.g. GuestClient), options by name.
        /// </summary>
        private async Task<JObject?> UserTokenAsync(long userId, bool withOptions, CancellationToken ct)
        {
            var user = await _users.Query().AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.UserName, u.FullName, u.Email, u.PhoneNumber, u.Type })
                .FirstOrDefaultAsync(ct);
            if (user == null)
                return null;

            var token = new JObject
            {
                ["id"] = user.Id,
                ["username"] = user.UserName,
                ["fullName"] = user.FullName,
                ["email"] = user.Email,
                ["mobilephone"] = user.PhoneNumber,
                // as the sign-in token names it (UserType claim)
                ["userType"] = user.Type.ToString(),
            };
            if (withOptions)
            {
                var options = await _userOptions.Query().AsNoTracking()
                    .Where(o => o.UserId == userId)
                    .Select(o => new { o.PropertyName, o.Value })
                    .ToListAsync(ct);
                var map = new JObject();
                foreach (var o in options.Where(o => !string.IsNullOrEmpty(o.PropertyName)))
                    map[o.PropertyName] = o.Value;
                token["options"] = map;
            }
            return token;
        }

        public async Task<OrderFacts?> GetFactsAsync(long orderId, CancellationToken ct = default)
        {
            var order = await _orders.Query().AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.Id, o.SiteId, o.CustomerId, o.CartId, o.OrderStatus, o.OrderTotal, o.PaymentMethod })
                .FirstOrDefaultAsync(ct);
            if (order == null)
                return null;

            var productIds = await _items.Query().AsNoTracking()
                .Where(i => i.Order.Id == orderId)
                .Select(i => i.ProductId)
                .ToListAsync(ct);

            var payment = await SucceededPaymentAsync(orderId, ct);

            return new OrderFacts
            {
                OrderId = order.Id,
                SiteId = order.SiteId,
                CustomerId = order.CustomerId,
                CartId = order.CartId,
                CultureId = ReadCultureId(payment?.PaymentReceipt),
                Status = order.OrderStatus,
                OrderTotal = order.OrderTotal,
                ProductIds = productIds.Distinct().ToList(),
                PaymentMethod = payment?.PaymentMethod,
                PaidAmount = payment?.Amount,
                GatewayTransactionId = payment?.GatewayTransactionId,
                OrderPaymentMethod = order.PaymentMethod,
            };
        }

        public async Task<JObject?> BuildAsync(long orderId, CancellationToken ct = default) => (await ReadOrderAsync(orderId, ct))?.Context;

        private sealed record ItemRow(long ProductId, int Quantity, decimal ProductPrice, decimal DiscountAmount, decimal TaxAmount, string? AdditionalDetails);

        /// <summary>An order's facts (as <see cref="GetFactsAsync"/> gives them) and its data (as <see cref="BuildAsync"/>), reading it once.</summary>
        private async Task<(OrderFacts Facts, JObject Context)?> ReadOrderAsync(long orderId, CancellationToken ct)
        {
            var order = await _orders.Query().AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null)
                return null;

            var items = await _items.Query().AsNoTracking()
                .Where(i => i.Order.Id == orderId)
                .OrderBy(i => i.Id)
                .Select(i => new ItemRow(i.ProductId, i.Quantity, i.ProductPrice, i.DiscountAmount, i.TaxAmount, i.AdditionalDetails))
                .ToListAsync(ct);

            var payment = await SucceededPaymentAsync(orderId, ct);
            var facts = new OrderFacts
            {
                OrderId = order.Id,
                SiteId = order.SiteId,
                CustomerId = order.CustomerId,
                CartId = order.CartId,
                CultureId = ReadCultureId(payment?.PaymentReceipt),
                Status = order.OrderStatus,
                OrderTotal = order.OrderTotal,
                ProductIds = items.Select(i => i.ProductId).Distinct().ToList(),
                PaymentMethod = payment?.PaymentMethod,
                PaidAmount = payment?.Amount,
                GatewayTransactionId = payment?.GatewayTransactionId,
                OrderPaymentMethod = order.PaymentMethod,
            };
            return (facts, await ContextOfAsync(order, items, payment, ct));
        }

        private async Task<JObject> ContextOfAsync(Order order, IReadOnlyList<ItemRow> items, Payment? payment, CancellationToken ct)
        {
            var customer = await _users.Query().AsNoTracking()
                .Where(u => u.Id == order.CustomerId)
                .Select(u => new { u.Id, u.FullName, u.Email, u.PhoneNumber })
                .FirstOrDefaultAsync(ct);

            var itemTokens = new JArray(items.Select(i => new JObject
            {
                ["productId"] = i.ProductId,
                ["quantity"] = i.Quantity,
                ["price"] = i.ProductPrice,
                ["discount"] = i.DiscountAmount,
                ["tax"] = i.TaxAmount,
                ["additionalDetails"] = ParseJson(i.AdditionalDetails),
            }));

            var orderToken = new JObject
            {
                ["id"] = order.Id,
                ["reference"] = order.OrderReferanceId,
                ["status"] = (int)order.OrderStatus,
                ["currency"] = order.CurrencyCode,
                ["total"] = order.OrderTotal,
                ["subTotal"] = order.SubTotal,
                ["discount"] = order.DiscountAmount,
                ["tax"] = order.TaxAmount,
                ["createdOn"] = order.CreatedOn.ToString("O", CultureInfo.InvariantCulture),
                ["siteId"] = order.SiteId,
                ["cartId"] = order.CartId,
                ["customer"] = customer == null ? null : new JObject
                {
                    ["id"] = customer.Id,
                    ["name"] = customer.FullName,
                    ["email"] = customer.Email,
                    ["phone"] = customer.PhoneNumber,
                },
                ["items"] = itemTokens,
                ["payment"] = PaymentToken(payment),
            };

            return new JObject
            {
                ["order"] = orderToken,
                // The signed-in user (Logged In User, User Option) in a payment flow: the order's customer
                ["user"] = await UserTokenAsync(order.CustomerId, withOptions: true, ct),
                ["cart"] = new JObject
                {
                    ["id"] = order.CartId,
                    ["items"] = itemTokens.DeepClone(),
                    ["total"] = order.OrderTotal,
                },
                ["additionalDetails"] = itemTokens
                    .Select(i => i["additionalDetails"])
                    .FirstOrDefault(d => d != null && d.Type != JTokenType.Null)?.DeepClone(),
            };
        }

        /// <summary>
        /// Payment rows DCMS writes itself, with no gateway behind them (the names come from DCMS's own
        /// EskaCommerce.Payments PaymentHelper): they are Succeeded and carry the ORDER id as their
        /// "transaction id", so they must never count as a verified payment, whatever the flow's
        /// paymentMethods say.
        /// </summary>
        public static readonly IReadOnlyList<string> InternalPaymentMethods = new[]
        {
            PaymentHelper.CouponAmount, PaymentHelper.RedeemAmount, PaymentHelper.RefundAmount, PaymentHelper.CashOnDelivery,
        };

        private Task<Payment?> SucceededPaymentAsync(long orderId, CancellationToken ct)
        {
            var internalMethods = InternalPaymentMethods.ToList();
            return _payments.Query().AsNoTracking()
                .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Succeeded && !internalMethods.Contains(p.PaymentMethod))
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct)!;
        }

        private static JToken? PaymentToken(Payment? payment)
        {
            if (payment == null)
                return null;

            var receipt = ParseJson(payment.PaymentReceipt) as JObject;
            var result = receipt?["Result"] as JObject;
            return new JObject
            {
                ["method"] = payment.PaymentMethod,
                ["status"] = (int)payment.Status,
                ["amount"] = payment.Amount,
                ["transactionId"] = payment.GatewayTransactionId,
                ["paidOn"] = payment.LatestUpdatedOn.ToString("O", CultureInfo.InvariantCulture),
                // Gateway details worth matching against the insurer's records; never card data.
                ["rrn"] = (string?)result?["Rrn"],
                ["approvalCode"] = (string?)result?["ApprovalCode"],
                ["cardType"] = (string?)result?["PaymentMethod"],
            };
        }

        private static long ReadCultureId(string? receipt)
        {
            return ParseJson(receipt) is JObject r && r["CultureId"]?.Type == JTokenType.Integer ? (long)r["CultureId"]! : 0;
        }

        /// <summary>Stored JSON as a token; text that is not JSON stays text; empty is null.</summary>
        private static JToken? ParseJson(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            var trimmed = text.TrimStart();
            if (trimmed[0] != '{' && trimmed[0] != '[')
                return text;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(text, ReadSettings);
            }
            catch (JsonException)
            {
                return text;
            }
        }
    }
}
