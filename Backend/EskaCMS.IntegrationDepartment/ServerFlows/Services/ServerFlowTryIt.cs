using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// What the Admin's "Try it" sends: whether to run for real or simulate, and what the flow's
    /// trigger needs: an order (after a payment, or an order's status), the visitor a page flow
    /// runs as, and what a page or another system sends.
    /// </summary>
    public sealed class TryBody
    {
        /// <summary>true: a real run, exactly as the trigger would start it (the version that is on only); false: simulate.</summary>
        public bool Run { get; set; }

        public long? OrderId { get; set; }

        /// <summary>A page flow: the visitor it runs as (the admin when not given; a user of the admin's site only).</summary>
        public long? UserId { get; set; }

        public JToken? Input { get; set; }
    }

    /// <summary>A simulation, or the real run "Try it" started (or found: a trigger never runs twice for the same thing).</summary>
    public sealed record TryResult(SimulationResult? Simulation, long? RunId, string? Message);

    /// <summary>
    /// "Try it", in one place for every trigger:
    ///  - Simulate (any version, any trigger): builds the data exactly as the trigger would and walks
    ///    the flow with each call's sample answer, calling nothing;
    ///  - Run for real (the version that is on), only where nothing else can start a run, through
    ///    the trigger's own starter and its once-rules: a paid order that has no run yet, an order's
    ///    status change, a scheduled flow now. A page flow is started by its page (or the page
    ///    editor's Test) and a webhook flow by its caller, so they are only simulated here.
    /// </summary>
    public sealed class ServerFlowTryIt
    {
        private readonly IRepository<ServerFlow> _flows;
        private readonly IRepository<UsersSites> _userSites;
        private readonly FlowContextBuilder _contexts;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowTexts _texts;
        private readonly ServerFlowTriggers _triggers;
        private readonly ServerFlowSchedules _schedules;
        private readonly ServerFlowOrderStatuses _orderStatuses;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;

        public ServerFlowTryIt(IRepository<ServerFlow> flows, IRepository<UsersSites> userSites, FlowContextBuilder contexts, ServerFlowCatalog catalog, ServerFlowTexts texts,
            ServerFlowTriggers triggers, ServerFlowSchedules schedules, ServerFlowOrderStatuses orderStatuses, IOptionsMonitor<ServerFlowOptions> options)
        {
            _flows = flows;
            _userSites = userSites;
            _contexts = contexts;
            _catalog = catalog;
            _texts = texts;
            _triggers = triggers;
            _schedules = schedules;
            _orderStatuses = orderStatuses;
            _options = options;
        }

        /// <summary>Whether "Run for real" exists for a trigger: only where nothing else can start a run.</summary>
        public static bool CanRun(string trigger) =>
            trigger is FlowSettings.OrderPaidTrigger or FlowSettings.OrderStatusTrigger or FlowSettings.ScheduleTrigger;

        public async Task<AdminResult<TryResult>> TryAsync(long siteId, long adminId, long flowId, TryBody body, CancellationToken ct = default)
        {
            var flow = await _flows.Query().AsNoTracking().FirstOrDefaultAsync(f => f.Id == flowId && f.SiteId == siteId, ct);
            if (flow == null)
                return AdminResult<TryResult>.Missing("Flow version");
            var parsed = _catalog.Parse(flow);
            if (parsed.Definition == null)
                return AdminResult<TryResult>.Fail("This version has errors; correct them first.", parsed.Issues);
            var definition = parsed.Definition;

            if (!body.Run)
            {
                var (context, why) = await ContextAsync(siteId, adminId, definition, body, ct);
                return context == null
                    ? AdminResult<TryResult>.Fail(why!)
                    : AdminResult<TryResult>.Success(new TryResult(FlowSimulator.Simulate(definition, context, DateTimeOffset.UtcNow), null, null));
            }

            var trigger = definition.Settings.Trigger;
            if (!CanRun(trigger))
                return AdminResult<TryResult>.Fail(trigger == FlowSettings.PageTrigger
                    ? "A page flow runs for real from its page (or the page editor's Test button). Simulate it here."
                    : "A webhook flow runs for real when the other system calls its address. Simulate it here.");
            if (!_options.CurrentValue.Enabled)
                return AdminResult<TryResult>.Fail("Server flows are switched off on this server (ServerFlows:Enabled), so nothing runs. Simulate instead.");
            var active = (await _catalog.GetActiveAsync(siteId, trigger, ct)).FirstOrDefault(f => f.Id == flowId);
            if (active == null)
                return AdminResult<TryResult>.Fail("Only the version that is on runs for real. Simulate this one, or switch it on first.");
            return await RunAsync(active, adminId, body, ct);
        }

        /// <summary>The data the trigger would give a run, for a simulation.</summary>
        private async Task<(JObject? Context, string? Why)> ContextAsync(long siteId, long adminId, FlowDefinition definition, TryBody body, CancellationToken ct)
        {
            var settings = definition.Settings;
            var cultureId = await _texts.DefaultCultureIdAsync(siteId, ct);
            switch (settings.Trigger)
            {
                case FlowSettings.PageTrigger:
                {
                    var (input, problem) = PageRunRules.CleanInput(settings.Page!, body.Input);
                    if (problem != null)
                        return (null, $"What the page sends: {problem}");
                    var userId = body.UserId ?? adminId;
                    // The visitor's own data (their details, options and cart) is read: only for a user of this site.
                    if (userId != adminId && !await _userSites.Query().AsNoTracking().AnyAsync(s => s.UserId == userId && s.SiteId == siteId, ct))
                        return (null, $"User {userId} is not a user of this site.");
                    var cartId = await _contexts.FindPageCartIdAsync(siteId, userId, settings.Page!.CartType, ct);
                    return (await _contexts.BuildPageAsync(siteId, userId, cultureId, cartId, input!, ct), null);
                }
                case FlowSettings.WebhookTrigger:
                {
                    var (input, problem) = PageRunRules.CleanInput(settings.Webhook!.Inputs, body.Input);
                    return problem != null
                        ? (null, $"What the other system sends: {problem}")
                        : (FlowContextBuilder.WebhookContext(siteId, input!, cultureId, DateTimeOffset.UtcNow), null);
                }
                case FlowSettings.ScheduleTrigger:
                    return (FlowContextBuilder.ScheduleContext(siteId, cultureId, DateTimeOffset.UtcNow, adminId), null);
                case FlowSettings.OrderStatusTrigger:
                {
                    var (changeId, why) = await ChangeIdAsync(siteId, settings.OrderStatus!, body, ct);
                    if (changeId == null)
                        return (null, why);
                    var (context, _, refused) = await _orderStatuses.ContextAsync(siteId, changeId.Value, adminId, ct);
                    return (context, refused);
                }
                default:
                {
                    if (body.OrderId is not long orderId)
                        return (null, "Give the number of a paid order.");
                    var facts = await _contexts.GetFactsAsync(orderId, ct);
                    if (facts == null || facts.SiteId != siteId)
                        return (null, $"This site has no order {orderId}.");
                    return (await _contexts.BuildPaidOrderAsync(facts, ct), null);
                }
            }
        }

        /// <summary>A real run, through the trigger's own starter.</summary>
        private async Task<AdminResult<TryResult>> RunAsync(ActiveServerFlow flow, long adminId, TryBody body, CancellationToken ct)
        {
            switch (flow.Definition.Settings.Trigger)
            {
                case FlowSettings.ScheduleTrigger:
                {
                    var (runId, refused) = await _schedules.RunNowAsync(flow, adminId, ct);
                    return runId == null ? AdminResult<TryResult>.Fail(refused!) : Started(runId.Value, $"Run {runId} started now, outside the schedule.");
                }
                case FlowSettings.OrderStatusTrigger:
                {
                    var (changeId, why) = await ChangeIdAsync(flow.SiteId, flow.Definition.Settings.OrderStatus!, body, ct);
                    if (changeId == null)
                        return AdminResult<TryResult>.Fail(why!);
                    var (runId, created, refused) = await _orderStatuses.StartAsync(flow, changeId.Value, adminId, ct);
                    return Answer(runId, created, refused, $"this status change of order {body.OrderId}");
                }
                default:
                {
                    if (body.OrderId is not long orderId)
                        return AdminResult<TryResult>.Fail("Give the number of a paid order.");
                    var (runId, created, refused) = await _triggers.StartForAdminAsync(flow, orderId, adminId, ct);
                    return Answer(runId, created, refused, $"order {orderId}");
                }
            }
        }

        /// <summary>An order status flow runs for the order's latest move into one of its statuses.</summary>
        private async Task<(long? ChangeId, string? Why)> ChangeIdAsync(long siteId, OrderStatusSettings settings, TryBody body, CancellationToken ct)
        {
            if (body.OrderId is not long orderId)
                return (null, "Give the number of an order that moved to one of the flow's statuses.");
            var changeId = await _orderStatuses.LatestChangeAsync(siteId, settings, orderId, ct);
            return changeId == null ? (null, $"Order {orderId} never moved to {string.Join(" or ", settings.ToStatuses)}.") : (changeId, null);
        }

        private static AdminResult<TryResult> Answer(long? runId, bool created, string? refused, string what) =>
            runId == null
                ? AdminResult<TryResult>.Fail(refused!)
                : Started(runId.Value, created ? $"Run {runId} started." : $"Run {runId} already exists for {what}; a trigger never starts twice for the same thing.");

        private static AdminResult<TryResult> Started(long runId, string message) =>
            AdminResult<TryResult>.Success(new TryResult(null, runId, message));
    }
}
