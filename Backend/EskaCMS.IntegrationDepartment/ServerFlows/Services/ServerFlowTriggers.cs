using System.Collections.Concurrent;
using System.Globalization;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// Turns "an order was paid" into runs. One run per order and flow, ever (unique RunKey).
    ///
    /// A run starts by itself only when the order was paid by a verified payment: a successful
    /// payment with a gateway transaction, by one of the flow's trigger.paymentMethods. An order
    /// marked paid any other way (an admin changing its status, a coupon) gets a run that waits
    /// for an admin to start it - nothing is issued for an order nobody actually paid.
    /// </summary>
    public sealed class ServerFlowTriggers
    {
        public const string OrderSubject = "order";

        /// <summary>The statuses that mean "just paid": a change INTO one of them triggers.</summary>
        public static readonly IReadOnlyCollection<OrderStatus> PaidStatuses = new[] { OrderStatus.PaymentReceived, OrderStatus.PaidByCoupon };

        /// <summary>Paid, and not cancelled or refunded since: a run may still be created.</summary>
        private static readonly OrderStatus[] Runnable = { OrderStatus.PaymentReceived, OrderStatus.PaidByCoupon, OrderStatus.Invoiced, OrderStatus.Complete };

        public static IReadOnlyCollection<OrderStatus> RunnableStatuses => Runnable;

        /// <summary>Not paid yet, and not failed or cancelled: a payment may still come.</summary>
        public static readonly IReadOnlyCollection<OrderStatus> AwaitingPaymentStatuses = new[] { OrderStatus.New, OrderStatus.OnHold, OrderStatus.PendingPayment, OrderStatus.SadadPayment };

        /// <summary>The most orders kept to be tried again by the next sweeps.</summary>
        private const int MaxRetries = 1000;

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ILogger<ServerFlowTriggers> _logger;
        private readonly OrderHistoryCursor _paidMoves = new();
        private readonly ConcurrentDictionary<long, (long SiteId, DateTimeOffset PaidAt)> _retry = new();

        public ServerFlowTriggers(
            IServiceScopeFactory scopes,
            IOptionsMonitor<ServerFlowOptions> options,
            ServerFlowCatalog catalog,
            ServerFlowQueue queue,
            ILogger<ServerFlowTriggers> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _queue = queue;
            _logger = logger;
        }

        public static string RunKeyFor(long orderId, string flowName) => string.Create(CultureInfo.InvariantCulture, $"{OrderSubject}:{orderId}:{flowName}");

        /// <summary>Creates the runs a paid order needs and queues the ones that may start. Returns their ids.</summary>
        public Task<IReadOnlyList<long>> OrderPaidAsync(long orderId, CancellationToken ct = default) => OrderPaidAsync(orderId, null, ct);

        /// <summary>
        /// The same for an order paid at <paramref name="paidAt"/> (the sweep, a while after the
        /// payment): only the flows that were on by then.
        /// </summary>
        public async Task<IReadOnlyList<long>> OrderPaidAsync(long orderId, DateTimeOffset? paidAt, CancellationToken ct = default)
        {
            if (!_options.CurrentValue.Enabled)
                return Array.Empty<long>();

            OrderFacts? facts;
            using (var scope = _scopes.CreateScope())
                facts = await scope.ServiceProvider.GetRequiredService<FlowContextBuilder>().GetFactsAsync(orderId, ct);
            if (facts == null || !RunnableStatuses.Contains(facts.Status))
                return Array.Empty<long>();

            var flows = (await _catalog.GetActiveAsync(facts.SiteId, FlowSettings.OrderPaidTrigger, ct))
                .Where(f => paidAt == null || f.OnSince <= paidAt)
                .Where(f => f.Definition.Settings.ProductIds.Count == 0 || f.Definition.Settings.ProductIds.Intersect(facts.ProductIds).Any())
                .ToList();
            if (flows.Count == 0)
                return Array.Empty<long>();

            var created = new List<long>();
            foreach (var flow in flows)
            {
                var id = await CreateRunAsync(flow, facts, ct);
                if (id.HasValue)
                    created.Add(id.Value);
            }
            return created;
        }

        private async Task<long?> CreateRunAsync(ActiveServerFlow flow, OrderFacts facts, CancellationToken ct)
        {
            // A payment method the flow skips (e.g. a SADAD bill the insurer settles itself) never makes a run.
            if (facts.IsPaidByOneOf(flow.Definition.Settings.SkipPaymentMethods))
            {
                _logger.LogDebug("Server flows: order {OrderId} was paid by {Method}, which {Flow} skips.", facts.OrderId, facts.PaidBy, flow.Name);
                return null;
            }

            var runKey = RunKeyFor(facts.OrderId, flow.Name);

            // Its own scope: a duplicate insert must not leave a failed entity in a shared context.
            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;
            var runs = sp.GetRequiredService<IRepository<ServerFlowRun>>();
            if (await runs.Query().AsNoTracking().AnyAsync(r => r.RunKey == runKey, ct))
                return null;

            var context = await sp.GetRequiredService<FlowContextBuilder>().BuildPaidOrderAsync(facts, ct);
            if (context == null)
                return null;

            var verified = facts.IsVerifiedPayment(flow.Definition.Settings.PaymentMethods);
            var run = new ServerFlowRun
            {
                SiteId = facts.SiteId,
                FlowId = flow.Id,
                FlowName = flow.Name,
                SubjectType = OrderSubject,
                SubjectId = facts.OrderId,
                RunKey = runKey,
                Status = verified ? ServerFlowRunStatus.Queued : ServerFlowRunStatus.AwaitingApproval,
                Reason = verified ? null : StepPayload.Fit(facts.PaymentMethod == null
                    ? "The order is marked paid but has no successful payment. Check it, then start the run."
                    : $"Paid by \"{facts.PaymentMethod}\", which is not one of this flow's verified payment methods ({string.Join(", ", flow.Definition.Settings.PaymentMethods)}). Check the payment, then start the run.",
                    ServerFlowRun.ReasonLength),
                Context = context.ToString(Formatting.None),
                Outputs = "{}",
            };

            runs.Add(run);
            try
            {
                await runs.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Created at the same moment by the sweep or another server: fine. Anything else is a real error.
                if (await RunIdAsync(runKey, ct) != null)
                    return null;
                throw;
            }

            _logger.LogInformation("Server flow run {RunId} ({Flow}) created for order {OrderId}: {Status}.", run.Id, flow.Name, facts.OrderId, run.Status);
            if (run.Status == ServerFlowRunStatus.Queued)
                _queue.Enqueue(run.Id);
            return run.Id;
        }

        /// <summary>
        /// The Admin's "Try it" for one payment flow and a paid order: the run the payment would have
        /// created (a payment the server could not verify included: an admin starting it is the
        /// approval), or the run the order already has. Returns the run id, whether it is new, or why not.
        /// </summary>
        public async Task<(long? RunId, bool Created, string? Refused)> StartForAdminAsync(ActiveServerFlow flow, long orderId, long adminId, CancellationToken ct = default)
        {
            OrderFacts? facts;
            using (var scope = _scopes.CreateScope())
                facts = await scope.ServiceProvider.GetRequiredService<FlowContextBuilder>().GetFactsAsync(orderId, ct);
            if (facts == null || facts.SiteId != flow.SiteId)
                return (null, false, $"This site has no order {orderId}.");
            if (!RunnableStatuses.Contains(facts.Status))
                return (null, false, $"Order {orderId} is {facts.Status}: only a paid order that was not cancelled or refunded gets this flow.");
            var products = flow.Definition.Settings.ProductIds;
            if (products.Count > 0 && !products.Intersect(facts.ProductIds).Any())
                return (null, false, $"Order {orderId} has none of this flow's products.");
            if (facts.IsPaidByOneOf(flow.Definition.Settings.SkipPaymentMethods))
                return (null, false, $"Order {orderId} was paid by \"{facts.PaidBy}\", which this flow skips (Never for these payment methods).");

            var runKey = RunKeyFor(orderId, flow.Name);
            if (await RunIdAsync(runKey, ct) is long existing)
                return (existing, false, null);
            var created = await CreateRunAsync(flow, facts, ct) ?? await RunIdAsync(runKey, ct);
            if (created == null)
                return (null, false, $"Order {orderId} could not be read.");

            using (var scope = _scopes.CreateScope())
            {
                var runs = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>();
                var run = await runs.Query().FirstOrDefaultAsync(r => r.Id == created.Value, ct);
                if (run != null && run.Status == ServerFlowRunStatus.AwaitingApproval)
                {
                    run.Status = ServerFlowRunStatus.Queued;
                    run.Reason = "Started by an admin (Try it).";
                    run.LastActionById = adminId;
                    run.UpdatedOn = DateTimeOffset.UtcNow;
                    await runs.SaveChangesAsync();
                    _queue.Enqueue(run.Id);
                }
            }
            return (created, true, null);
        }

        /// <summary>
        /// The run this flow should have for a paid order, made now when the trigger has not made it
        /// yet: the payment page asks a moment after the payment, and the sweep would make it within a
        /// minute anyway. The sweep's own rules apply: the order is paid, has one of the flow's
        /// products, and was paid since the flow has been on and within the look-back, so a page can
        /// never start a flow for an older order. Returns the run's id, or null.
        /// </summary>
        public async Task<long?> CatchUpAsync(ActiveServerFlow flow, long orderId, CancellationToken ct = default)
        {
            if (!_options.CurrentValue.Enabled)
                return null;
            var runKey = RunKeyFor(orderId, flow.Name);
            if (await RunIdAsync(runKey, ct) is long existing)
                return existing;

            OrderFacts? facts;
            DateTimeOffset? paidAt;
            using (var scope = _scopes.CreateScope())
            {
                var sp = scope.ServiceProvider;
                facts = await sp.GetRequiredService<FlowContextBuilder>().GetFactsAsync(orderId, ct);
                paidAt = await sp.GetRequiredService<IRepository<OrderHistory>>().Query().AsNoTracking()
                    .Where(h => h.OrderId == orderId && (h.NewStatus == OrderStatus.PaymentReceived || h.NewStatus == OrderStatus.PaidByCoupon))
                    .MaxAsync(h => (DateTimeOffset?)h.CreatedOn, ct);
            }
            if (facts == null || facts.SiteId != flow.SiteId || !RunnableStatuses.Contains(facts.Status))
                return null;
            var products = flow.Definition.Settings.ProductIds;
            if (products.Count > 0 && !products.Intersect(facts.ProductIds).Any())
                return null;
            var lookback = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, _options.CurrentValue.MissedTriggerLookbackHours));
            if (paidAt == null || paidAt < Floor(flow, lookback))
                return null;

            var created = await CreateRunAsync(flow, facts, ct) ?? await RunIdAsync(runKey, ct);
            if (created != null)
                _logger.LogInformation("Server flow run {RunId} ({Flow}) for order {OrderId} made when its payment page asked.", created, flow.Name, orderId);
            return created;
        }

        private async Task<long?> RunIdAsync(string runKey, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>()
                .Query().AsNoTracking().Where(r => r.RunKey == runKey).Select(r => (long?)r.Id).FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// Paid orders that have no run although an active flow applies (the process stopped
        /// between the payment and the run, or the event handler failed). Only orders paid since the
        /// flow has been on, and within MissedTriggerLookbackHours, so switching a flow on never
        /// reaches back to old orders. The history is read from where this process's last sweep got
        /// to (<see cref="OrderHistoryCursor"/>); an order whose runs could not be made is tried again
        /// by the next sweeps while it is within the look-back.
        /// </summary>
        public async Task SweepMissedAsync(CancellationToken ct)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
                return;

            var flows = await _catalog.GetAllActiveAsync(FlowSettings.OrderPaidTrigger, ct);
            if (flows.Count == 0)
                return;

            var lookback = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, options.MissedTriggerLookbackHours));
            var bySite = flows.GroupBy(f => f.SiteId).ToDictionary(g => g.Key, g => g.ToList());
            using var scope = _scopes.CreateScope();
            var runs = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>();

            // Orders an earlier sweep could not make runs for, while they are within the look-back.
            var retries = new Dictionary<long, (long SiteId, DateTimeOffset PaidAt)>();
            foreach (var (orderId, retry) in _retry)
            {
                if (retry.PaidAt < lookback)
                    _retry.TryRemove(orderId, out _);
                else
                    retries[orderId] = retry;
            }
            await MakeMissedRunsAsync(runs, bySite, retries, ct);

            // The new payments, read back no further than the earliest any flow takes one from.
            await _paidMoves.ReadAsync(scope.ServiceProvider.GetRequiredService<IRepository<OrderHistory>>(), PaidStatuses, flows.Min(f => Floor(f, lookback)),
                changes =>
                {
                    var due = new Dictionary<long, (long SiteId, DateTimeOffset PaidAt)>();
                    foreach (var change in changes)
                        if (bySite.TryGetValue(change.SiteId, out var siteFlows) && siteFlows.Any(f => change.CreatedOn >= Floor(f, lookback)))
                            due[change.OrderId] = (change.SiteId, change.CreatedOn);
                    return MakeMissedRunsAsync(runs, bySite, due, ct);
                }, ct);
        }

        /// <summary>
        /// The runs paid orders should have and lack. An order whose every flow has its run already
        /// needs nothing more (the run keys are read in one go); one whose runs could not be made is
        /// kept for the next sweeps.
        /// </summary>
        private async Task MakeMissedRunsAsync(IRepository<ServerFlowRun> runs, IReadOnlyDictionary<long, List<ActiveServerFlow>> bySite,
            Dictionary<long, (long SiteId, DateTimeOffset PaidAt)> due, CancellationToken ct)
        {
            if (due.Count == 0)
                return;
            var keys = due.SelectMany(d => (bySite.TryGetValue(d.Value.SiteId, out var f) ? f : new List<ActiveServerFlow>()).Select(x => RunKeyFor(d.Key, x.Name))).ToList();
            var existing = keys.Count == 0 ? new HashSet<string>() : (await runs.Query().AsNoTracking()
                .Where(r => keys.Contains(r.RunKey)).Select(r => r.RunKey).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);

            foreach (var (orderId, (siteId, paidAt)) in due)
            {
                if (bySite.TryGetValue(siteId, out var siteFlows) && siteFlows.All(f => existing.Contains(RunKeyFor(orderId, f.Name))))
                {
                    _retry.TryRemove(orderId, out _);
                    continue;
                }
                try
                {
                    var created = await OrderPaidAsync(orderId, paidAt, ct);
                    _retry.TryRemove(orderId, out _);
                    if (created.Count > 0)
                        _logger.LogWarning("Server flows: order {OrderId} had missed its trigger; {Count} run(s) created by the sweep.", orderId, created.Count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Server flows: could not create runs for order {OrderId}; the next sweep tries again.", orderId);
                    if (_retry.Count < MaxRetries)
                        _retry[orderId] = (siteId, paidAt);
                }
            }
        }

        /// <summary>The earliest payment a flow runs for: since the flow has been on, within the look-back.</summary>
        private static DateTimeOffset Floor(ActiveServerFlow flow, DateTimeOffset lookback) => flow.OnSince > lookback ? flow.OnSince : lookback;
    }
}
