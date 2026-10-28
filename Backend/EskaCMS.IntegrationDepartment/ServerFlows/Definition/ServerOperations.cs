namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// The DCMS operations a flow can run in-process, as "local" requests. The endpoint is written
    /// the way a page writes a local call ("/Notification/send", "/api/Account/EnsureUser"); the
    /// server runs the operation itself instead of calling its own API.
    /// </summary>
    public static class ServerOperations
    {
        /// <summary>Sends a notification; the body is what pages send to /Notification/send.</summary>
        public const string Notify = "notification/send";

        /// <summary>
        /// Finds the customer's DCMS account, or creates it when there is none (never two accounts):
        /// the order's own account when it is the same person's, else by user name, else by a user
        /// option (e.g. nationalId); else Register with the given fields. Never by e-mail (families
        /// share one), and only customers' accounts. Answers { userId, userName, created, matchedBy }.
        /// </summary>
        public const string EnsureUser = "account/ensureuser";

        /// <summary>Adds or updates user options of one customer (DCMS Account/AddUpdateUserOptions). Answers { userId, added, updated, skipped }.</summary>
        public const string SetUserOptions = "account/addupdateuseroptions";

        /// <summary>
        /// Moves one order from a fixed status to a fixed status with DCMS's own status change (the
        /// Admin's: history, the customer's message, the order event). The order is found by its id,
        /// or by a value in its items' additional details (e.g. payment.sadadNo); a payment status
        /// also needs the amount, which must be the order's total. Moving an order that is already in
        /// the new status changes nothing and answers changed: false. Answers { orderId, from, to,
        /// changed, total }.
        /// </summary>
        public const string ChangeOrderStatus = "order/changestatus";

        private static readonly string[] All = { Notify, EnsureUser, SetUserOptions, ChangeOrderStatus };

        public static IReadOnlyCollection<string> Endpoints => All;

        /// <summary>The operation an endpoint names: case, leading "/" and "/api/" do not matter.</summary>
        public static bool TryResolve(string? endpoint, out string operation)
        {
            var e = (endpoint ?? "").Trim().TrimStart('/');
            if (e.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                e = e[4..];
            e = e.TrimEnd('/').ToLowerInvariant();
            operation = All.FirstOrDefault(o => o == e) ?? "";
            return operation.Length > 0;
        }

        /// <summary>
        /// All of them are safe to repeat: a notification sent twice is a nuisance, not a second
        /// policy; EnsureUser finds the account it created before; options are set, not added twice;
        /// an order already in the new status is left as it is.
        /// </summary>
        public static FlowRequestClass DefaultClass(string operation) => FlowRequestClass.IdempotentWrite;

        /// <summary>A notification that fails does not stop the run; the account operations do.</summary>
        public static bool DefaultCritical(string operation) => operation != Notify;
    }
}
