using System.Globalization;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using EskaCommerce.Orders.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>
    /// ServerOperations.ChangeOrderStatus: moves one order of the site from the flow's fixed status
    /// to its fixed new status with DCMS's own status change (IOrderService.OrderChangeStatus, what
    /// the Admin and the payment gateways use: the order's history, the customer's message, the
    /// order event, and for a paid status DCMS's paid-order handling).
    ///
    /// Body: { orderId | lookupPath + lookupValue, paymentMethod?, fromStatus, toStatus, amount?,
    /// tolerance?, note? }. The order is found by its id, or by a value in its items' additional
    /// details (e.g. lookupPath "payment.sadadNo"); it must be in fromStatus (and placed with
    /// paymentMethod, when given). A paid status needs amount, which must be the order's total
    /// within tolerance (default 0.01). An order already in toStatus is left as it is.
    ///
    /// Answers { orderId, from, to, total, changed }. A refusal (no such order, more than one,
    /// another status, another payment method, a wrong amount) is a ServerFlowOperationException:
    /// it is not retried, and the run stops with the reason.
    /// Runs on the call's own scope (FlowCaller.RunIsolated), so the site header is the run's site.
    /// </summary>
    public sealed class ServerFlowOrderOperations
    {
        /// <summary>Candidate items read for one lookup value; more than one matching order is refused anyway.</summary>
        private const int MaxCandidates = 50;

        private const int MaxValueLength = 200;
        private const int MaxNoteLength = 1000;
        private const decimal DefaultTolerance = 0.01m;

        private static readonly JsonSerializerSettings ReadSettings = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 64 };

        private readonly IRepository<Order> _orders;
        private readonly IRepository<OrderItem> _items;
        private readonly IOrderService _orderService;

        public ServerFlowOrderOperations(IRepository<Order> orders, IRepository<OrderItem> items, IOrderService orderService)
        {
            _orders = orders;
            _items = items;
            _orderService = orderService;
        }

        public async Task<JToken> ChangeStatusAsync(JToken body, long siteId)
        {
            if (body is not JObject b)
                throw new ServerFlowOperationException("The body of /Order/ChangeStatus must be an object.");

            var from = StatusOf(b["fromStatus"], "fromStatus");
            var to = StatusOf(b["toStatus"], "toStatus");
            if (from == to)
                throw new ServerFlowOperationException($"fromStatus and toStatus are both {to}: there is nothing to change.");
            var paymentMethod = TextOf(b["paymentMethod"]);

            var order = await FindAsync(b, siteId, paymentMethod);
            var answer = new JObject
            {
                ["orderId"] = order.Id,
                ["from"] = from.ToString(),
                ["to"] = to.ToString(),
                ["total"] = order.OrderTotal,
            };

            if (order.OrderStatus == to)
            {
                answer["changed"] = false;
                return answer;
            }
            if (order.OrderStatus != from)
                throw new ServerFlowOperationException($"Order {order.Id} is {order.OrderStatus}, not {from}: it was not changed.");
            if (paymentMethod != null && !string.Equals(order.PaymentMethod?.Trim(), paymentMethod, StringComparison.OrdinalIgnoreCase))
                throw new ServerFlowOperationException($"Order {order.Id} was placed with payment method \"{order.PaymentMethod}\", not \"{paymentMethod}\": it was not changed.");

            if (IsPaid(to) || HasValue(b["amount"]))
            {
                if (!TryAmount(b["amount"], out var amount))
                    throw new ServerFlowOperationException($"Moving order {order.Id} to {to} needs the amount paid (amount), a number.");
                var tolerance = TryAmount(b["tolerance"], out var t) && t >= 0 ? t : DefaultTolerance;
                if (Math.Abs(amount - order.OrderTotal) > tolerance)
                    throw new ServerFlowOperationException(string.Create(CultureInfo.InvariantCulture,
                        $"The amount paid ({amount}) is not order {order.Id}'s total ({order.OrderTotal}): it was not changed."));
            }

            var note = TextOf(b["note"]) ?? "";
            if (note.Length > MaxNoteLength)
                note = note[..MaxNoteLength];

            try
            {
                await _orderService.OrderChangeStatus(order.Id, to, note, siteId, true);
            }
            catch (Exception ex) when (ex.Message.StartsWith("The Order Is Already", StringComparison.OrdinalIgnoreCase))
            {
                // Another call moved it a moment ago (DCMS reads the order again and refuses the same status).
                answer["changed"] = false;
                return answer;
            }
            answer["changed"] = true;
            return answer;
        }

        private sealed record OrderRow(long Id, OrderStatus OrderStatus, decimal OrderTotal, string? PaymentMethod);

        /// <summary>The one order of the site the body names: by orderId, or by lookupPath = lookupValue in an item's additional details.</summary>
        private async Task<OrderRow> FindAsync(JObject b, long siteId, string? paymentMethod)
        {
            if (HasValue(b["orderId"]))
            {
                if (!long.TryParse(TextOf(b["orderId"]), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                    throw new ServerFlowOperationException("orderId must be an order number.");
                return await _orders.Query().AsNoTracking()
                    .Where(o => o.Id == id && o.SiteId == siteId)
                    .Select(o => new OrderRow(o.Id, o.OrderStatus, o.OrderTotal, o.PaymentMethod))
                    .FirstOrDefaultAsync()
                    ?? throw new ServerFlowOperationException($"This site has no order {id}.");
            }

            var lookupPath = TextOf(b["lookupPath"]);
            var value = TextOf(b["lookupValue"]);
            if (lookupPath == null || value == null)
                throw new ServerFlowOperationException("Say which order: orderId, or lookupPath and lookupValue (lookupValue was empty).");
            if (value.Length > MaxValueLength)
                throw new ServerFlowOperationException($"lookupValue is longer than {MaxValueLength} characters.");

            // The database narrows it down to the site's items whose details contain the value; the
            // value must then be exactly the one at lookupPath.
            var candidates = _items.Query().AsNoTracking()
                .Where(i => i.Order.SiteId == siteId && i.AdditionalDetails != null && i.AdditionalDetails.Contains(value));
            if (paymentMethod != null)
                candidates = candidates.Where(i => i.Order.PaymentMethod == paymentMethod);
            var rows = await candidates
                .OrderByDescending(i => i.Order.Id)
                .Select(i => new { OrderId = i.Order.Id, i.AdditionalDetails })
                .Take(MaxCandidates)
                .ToListAsync();
            var ids = rows.Where(r => ValueAt(r.AdditionalDetails, lookupPath) == value).Select(r => r.OrderId).Distinct().ToList();

            if (ids.Count == 0)
                throw new ServerFlowOperationException($"No order of this site{(paymentMethod != null ? $" placed with \"{paymentMethod}\"" : "")} has {lookupPath} = {value}.");
            if (ids.Count > 1)
                throw new ServerFlowOperationException($"{ids.Count} orders have {lookupPath} = {value} ({string.Join(", ", ids)}): none was changed. Say which one with orderId.");

            var orderId = ids[0];
            return await _orders.Query().AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new OrderRow(o.Id, o.OrderStatus, o.OrderTotal, o.PaymentMethod))
                .FirstAsync();
        }

        /// <summary>The value at <paramref name="path"/> in an item's additional details (JSON text, sometimes JSON inside a string), as text.</summary>
        internal static string? ValueAt(string? details, string path)
        {
            if (string.IsNullOrWhiteSpace(details))
                return null;
            try
            {
                var token = JsonConvert.DeserializeObject<JToken>(details, ReadSettings);
                if (token?.Type == JTokenType.String)
                    token = JsonConvert.DeserializeObject<JToken>((string)token!, ReadSettings);
                var v = JsonPath.Get(token, path);
                return v == null || v.Type is JTokenType.Null or JTokenType.Undefined or JTokenType.Object or JTokenType.Array
                    ? null
                    : Js.ToJsString(v).Trim();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsPaid(OrderStatus status) => status is OrderStatus.PaymentReceived or OrderStatus.PaidByCoupon;

        private static OrderStatus StatusOf(JToken? token, string field)
        {
            var text = TextOf(token);
            if (text != null && Enum.TryParse<OrderStatus>(text, true, out var status) && Enum.IsDefined(status))
                return status;
            throw new ServerFlowOperationException($"{field} \"{text}\" is not an order status. The statuses are: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");
        }

        private static bool HasValue(JToken? token) => TextOf(token) != null;

        private static string? TextOf(JToken? token)
        {
            if (token == null || token.Type is JTokenType.Null or JTokenType.Undefined or JTokenType.Object or JTokenType.Array)
                return null;
            var text = Js.ToJsString(token).Trim();
            return text.Length == 0 ? null : text;
        }

        private static bool TryAmount(JToken? token, out decimal amount)
        {
            amount = 0;
            var text = TextOf(token);
            return text != null && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
