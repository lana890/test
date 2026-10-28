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
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// Starts the flows whose trigger is an order's status ("orderStatus"). The worker's sweep calls
    /// it every SweepSeconds in every process that does the work (RunWorker); each move of an order
    /// INTO one of a flow's statuses (a row of the order's status history) gets one run of that flow
    /// (the run key), whichever process gets there first. Only moves since the flow has been on and
    /// within MissedTriggerLookbackHours, so switching a flow on never reaches back to old orders. The
    /// paid statuses are not offered: "After a payment" starts on them, and checks the payment.
    /// </summary>
    public sealed class ServerFlowOrderStatuses
    {
        public const string Subject = "orderStatus";

        /// <summary>The most moves kept to be tried again by the next sweeps.</summary>
        private const int MaxRetries = 1000;

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowTexts _texts;
        private readonly ILogger<ServerFlowOrderStatuses> _logger;
        private readonly OrderHistoryCursor _moves = new();
        private readonly ConcurrentDictionary<(long SiteId, string Flow, long ChangeId), DateTimeOffset> _retry = new();

        public ServerFlowOrderStatuses(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog,
            ServerFlowQueue queue, ServerFlowTexts texts, ILogger<ServerFlowOrderStatuses> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _queue = queue;
            _texts = texts;
            _logger = logger;
        }

        /// <summary>The run key of one status change, for one flow (written the same whatever the server's culture).</summary>
        public static string RunKeyFor(long siteId, string flowName, long changeId) =>
            string.Create(CultureInfo.InvariantCulture, $"orderStatus:{siteId}:{flowName}:{changeId}");

        /// <summary>The statuses an order status flow may name: every DCMS order status but the paid ones.</summary>
        public static IReadOnlyList<OrderStatus> Offered { get; } =
            Enum.GetValues<OrderStatus>().Where(s => !ServerFlowTriggers.PaidStatuses.Contains(s)).ToList();

        /// <summary>The flow's statuses as DCMS's; names it does not know, or paid ones, are left out (the Admin's check reports them).</summary>
        public static IReadOnlyList<OrderStatus> StatusesOf(OrderStatusSettings settings) =>
            settings.ToStatuses
                .Select(n => Enum.TryParse<OrderStatus>(n, ignoreCase: true, out var s) && Enum.IsDefined(s) ? (OrderStatus?)s : null)
                .Where(s => s != null && Offered.Contains(s.Value))
                .Select(s => s!.Value).Distinct().ToList();

        /// <summary>
        /// Creates the runs of every order status flow for the moves since this process's last sweep
        /// (<see cref="OrderHistoryCursor"/>). A move whose run could not be made is tried again by the
        /// next sweeps while it is within the look-back.
        /// </summary>
        public async Task SweepAsync(CancellationToken ct)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
                return;

            var flows = (await _catalog.GetAllActiveAsync(FlowSettings.OrderStatusTrigger, ct))
                .Where(f => f.Definition.Settings.OrderStatus != null)
                .Select(f => (Flow: f, Statuses: StatusesOf(f.Definition.Settings.OrderStatus!)))
                .Where(f => f.Statuses.Count > 0)
                .ToList();
            if (flows.Count == 0)
                return;

            var lookback = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, options.MissedTriggerLookbackHours));
            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;

            // Moves an earlier sweep could not start a run for, while they are within the look-back.
            var retries = new List<(ActiveServerFlow Flow, OrderChange Change)>();
            foreach (var ((siteId, name, changeId), at) in _retry)
            {
                var flow = flows.Select(f => f.Flow).FirstOrDefault(f => f.SiteId == siteId && f.Name == name);
                if (at < lookback || flow == null)
                    _retry.TryRemove((siteId, name, changeId), out _);
                else
                    retries.Add((flow, new OrderChange(changeId, 0, siteId, null, default, at)));
            }
            await StartDueAsync(sp, retries, ct);

            // The new moves, read back no further than the earliest any flow takes one from: each
            // flow's own are into one of its statuses, since it has been on, of one of its products.
            await _moves.ReadAsync(sp.GetRequiredService<IRepository<OrderHistory>>(), flows.SelectMany(f => f.Statuses).Distinct().ToList(),
                flows.Min(f => f.Flow.OnSince > lookback ? f.Flow.OnSince : lookback),
                changes =>
                {
                    var due = new List<(ActiveServerFlow Flow, OrderChange Change)>();
                    foreach (var (flow, statuses) in flows)
                    {
                        var since = flow.OnSince > lookback ? flow.OnSince : lookback;
                        due.AddRange(changes
                            .Where(c => c.SiteId == flow.SiteId && statuses.Contains(c.NewStatus) && c.OldStatus != c.NewStatus && c.CreatedOn >= since)
                            .Select(c => (flow, c)));
                    }
                    return StartDueAsync(sp, due, ct);
                }, ct);
        }

        /// <summary>
        /// Starts the runs of these moves that have none yet: of a flow's products only (read in one
        /// query), skipping those whose run key exists (read in one go). One that could not be started
        /// is kept for the next sweeps.
        /// </summary>
        private async Task StartDueAsync(IServiceProvider sp, List<(ActiveServerFlow Flow, OrderChange Change)> due, CancellationToken ct)
        {
            if (due.Count == 0)
                return;
            var productOrders = await OrdersWithProductsAsync(sp, due, ct);
            due = due.Where(d => d.Flow.Definition.Settings.ProductIds.Count == 0 || productOrders.Contains((d.Flow.Id, d.Change.OrderId)) || d.Change.OrderId == 0).ToList();
            // A move that has its run already needs nothing more (the run key tells).
            var keys = due.Select(d => RunKeyFor(d.Flow.SiteId, d.Flow.Name, d.Change.Id)).ToList();
            var existing = keys.Count == 0 ? new HashSet<string>() : (await sp.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                .Where(r => keys.Contains(r.RunKey)).Select(r => r.RunKey).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);

            foreach (var (flow, change) in due)
            {
                var retryKey = (flow.SiteId, flow.Name, change.Id);
                if (existing.Contains(RunKeyFor(flow.SiteId, flow.Name, change.Id)))
                {
                    _retry.TryRemove(retryKey, out _);
                    continue;
                }
                try
                {
                    await StartAsync(flow, change.Id, null, ct);
                    _retry.TryRemove(retryKey, out _);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Server flows: could not start the run of {Flow} for status change {ChangeId}; the next sweep tries again.", flow.Name, change.Id);
                    if (_retry.Count < MaxRetries)
                        _retry[retryKey] = change.CreatedOn;
                }
            }
        }

        /// <summary>The (flow, order) pairs among <paramref name="due"/> where the order has one of the flow's products, read in one query.</summary>
        private static async Task<HashSet<(long FlowId, long OrderId)>> OrdersWithProductsAsync(IServiceProvider sp, IReadOnlyList<(ActiveServerFlow Flow, OrderChange Change)> due, CancellationToken ct)
        {
            var orderIds = due.Where(d => d.Flow.Definition.Settings.ProductIds.Count > 0 && d.Change.OrderId > 0).Select(d => d.Change.OrderId).Distinct().ToList();
            if (orderIds.Count == 0)
                return new HashSet<(long, long)>();
            var items = await sp.GetRequiredService<IRepository<OrderItem>>().Query().AsNoTracking()
                .Where(i => orderIds.Contains(i.Order.Id))
                .Select(i => new { OrderId = i.Order.Id, i.ProductId })
                .ToListAsync(ct);
            var products = items.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.Select(i => i.ProductId).ToHashSet());
            return due.Where(d => products.TryGetValue(d.Change.OrderId, out var p) && d.Flow.Definition.Settings.ProductIds.Any(p.Contains))
                .Select(d => (d.Flow.Id, d.Change.OrderId)).ToHashSet();
        }

        /// <summary>
        /// The data a run for one status change reads, exactly as the sweep builds it; also what
        /// "Try it" simulates with. Null with the reason when the site has no such change.
        /// </summary>
        public async Task<(JObject? Context, long OrderId, string? Refused)> ContextAsync(long siteId, long changeId, long? startedBy, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var built = await scope.ServiceProvider.GetRequiredService<FlowContextBuilder>().OrderStatusChangeAsync(siteId, changeId, ct);
            if (built == null)
                return (null, 0, $"This site has no order status change {changeId}.");

            if (startedBy != null)
                built.Context["statusChange"]!["startedBy"] = startedBy.Value;
            // The calls run as the order's customer, in their language (else the site's default).
            built.Context["caller"] = new JObject
            {
                ["siteId"] = siteId,
                ["userId"] = built.UserId,
                ["cultureId"] = (await _texts.LanguageAsync(siteId, built.CultureId, ct))?.Id ?? 0,
                ["cartId"] = built.CartId,
            };
            return (built.Context, built.SubjectId, null);
        }

        /// <summary>
        /// Starts the flow for one status change: also the Admin's "Try it", which starts it exactly
        /// as the sweep would (once). Returns the run id (the existing one when it already ran),
        /// whether it is new, or why it cannot.
        /// </summary>
        public async Task<(long? RunId, bool Created, string? Refused)> StartAsync(ActiveServerFlow flow, long changeId, long? startedBy, CancellationToken ct)
        {
            var runKey = RunKeyFor(flow.SiteId, flow.Name, changeId);
            using var scope = _scopes.CreateScope();
            var runs = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>();
            if (await RunIdAsync(runs, runKey, ct) is long existing)
                return (existing, false, null);

            var (context, orderId, refused) = await ContextAsync(flow.SiteId, changeId, startedBy, ct);
            if (context == null)
                return (null, false, refused);

            var run = new ServerFlowRun
            {
                SiteId = flow.SiteId,
                FlowId = flow.Id,
                FlowName = flow.Name,
                SubjectType = Subject,
                SubjectId = orderId,
                RunKey = runKey,
                Status = ServerFlowRunStatus.Queued,
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
                // Created at the same moment by another process: fine. Anything else is a real error.
                using var check = _scopes.CreateScope();
                if (await RunIdAsync(check.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>(), runKey, ct) is long other)
                    return (other, false, null);
                throw;
            }

            _logger.LogInformation("Server flow run {RunId} ({Flow}) started by order {OrderId}'s status change {ChangeId}{Who}.", run.Id, flow.Name, orderId, changeId,
                startedBy == null ? "" : $" (Try it, user {startedBy})");
            _queue.Enqueue(run.Id);
            return (run.Id, true, null);
        }

        /// <summary>What "Try it" runs a flow for: the order's latest move into one of the flow's statuses (null when it has none).</summary>
        public async Task<long?> LatestChangeAsync(long siteId, OrderStatusSettings settings, long orderId, CancellationToken ct)
        {
            var statuses = StatusesOf(settings);
            using var scope = _scopes.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IRepository<OrderHistory>>().Query().AsNoTracking()
                .Where(h => h.OrderId == orderId && h.Order.SiteId == siteId && statuses.Contains(h.NewStatus))
                .OrderByDescending(h => h.Id)
                .Select(h => (long?)h.Id)
                .FirstOrDefaultAsync(ct);
        }

        private static Task<long?> RunIdAsync(IRepository<ServerFlowRun> runs, string runKey, CancellationToken ct) =>
            runs.Query().AsNoTracking().Where(r => r.RunKey == runKey).Select(r => (long?)r.Id).FirstOrDefaultAsync(ct);
    }
}
