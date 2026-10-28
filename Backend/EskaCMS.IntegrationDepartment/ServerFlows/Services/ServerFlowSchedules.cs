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
    /// Starts the flows that run on a schedule. The worker's sweep calls it every SweepSeconds in
    /// every process that does the work (RunWorker); the run key (flow name + time) makes each time
    /// run once, whichever process gets there first, and also across a version change.
    ///  - Only the latest time that is due runs: after a stop, missed times are not all run at once
    ///    (and none older than ScheduleCatchUpHours, or from before the flow was switched on).
    ///  - While a run of the flow is still working, the next time waits for it.
    ///  - The calls run as the site, in its default language.
    /// </summary>
    public sealed class ServerFlowSchedules
    {
        public const string ScheduleSubject = "schedule";

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowTexts _texts;
        private readonly ILogger<ServerFlowSchedules> _logger;

        public ServerFlowSchedules(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog,
            ServerFlowQueue queue, ServerFlowTexts texts, ILogger<ServerFlowSchedules> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _queue = queue;
            _texts = texts;
            _logger = logger;
        }

        /// <summary>Creates the run of every scheduled flow whose time has come.</summary>
        public async Task SweepAsync(CancellationToken ct)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
                return;

            var now = DateTimeOffset.UtcNow;
            var catchUp = TimeSpan.FromHours(Math.Clamp(options.ScheduleCatchUpHours, 1, 168));
            foreach (var flow in await _catalog.GetAllActiveAsync(FlowSettings.ScheduleTrigger, ct))
            {
                var settings = flow.Definition.Settings;
                if (settings.Schedule == null)
                    continue;
                // Since the flow has been on (a version that replaced the one before keeps its times).
                var due = ScheduleRules.LatestDue(settings.Schedule, settings.TimeZone, now, flow.OnSince, catchUp);
                if (due == null)
                    continue;
                try
                {
                    await CreateAsync(flow, ScheduleRules.RunKey(flow.SiteId, flow.Name, due.Value), due.Value, null, ct,
                        ScheduleRules.OlderRunKey(flow.SiteId, flow.Name, due.Value));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Server flows: could not start the scheduled run of {Flow} for {At}.", flow.Name, due.Value);
                }
            }
        }

        /// <summary>
        /// The Admin's "Try it" for a scheduled flow: one run of the version that is on, now, outside
        /// the schedule. Refused while a run of the flow is still working. Returns the run id, or why not.
        /// </summary>
        public async Task<(long? RunId, string? Refused)> RunNowAsync(ActiveServerFlow flow, long adminUserId, CancellationToken ct = default)
        {
            var (id, busy) = await CreateAsync(flow, ScheduleRules.ManualRunKey(flow.SiteId, flow.Name), DateTimeOffset.UtcNow, adminUserId, ct);
            return busy ? (null, "A run of this flow is still working. Wait for it to finish, or stop it in Runs.") : (id, null);
        }

        /// <summary><paramref name="olderKey"/>: the key this time had before the update, when it was written another way (ScheduleRules.OlderRunKey).</summary>
        private async Task<(long? RunId, bool Busy)> CreateAsync(ActiveServerFlow flow, string runKey, DateTimeOffset at, long? startedBy, CancellationToken ct, string? olderKey = null)
        {
            using var scope = _scopes.CreateScope();
            var runs = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>();
            if (await runs.Query().AsNoTracking().AnyAsync(r => r.RunKey == runKey || (olderKey != null && r.RunKey == olderKey), ct))
                return (null, false);

            // The previous run still working: this time waits for it (the next sweep looks again).
            var siteId = flow.SiteId;
            var name = flow.Name;
            var busy = await runs.Query().AsNoTracking().AnyAsync(r => r.SiteId == siteId && r.FlowName == name && r.SubjectType == ScheduleSubject
                && (r.Status == ServerFlowRunStatus.Queued || r.Status == ServerFlowRunStatus.Running || r.Status == ServerFlowRunStatus.Waiting), ct);
            if (busy)
                return (null, true);

            var context = FlowContextBuilder.ScheduleContext(siteId, await _texts.DefaultCultureIdAsync(siteId, ct), at, startedBy);

            var run = new ServerFlowRun
            {
                SiteId = siteId,
                FlowId = flow.Id,
                FlowName = name,
                SubjectType = ScheduleSubject,
                SubjectId = flow.Id,
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
                // Another process created this time's run at the same moment: fine.
                using var check = _scopes.CreateScope();
                if (await check.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking().AnyAsync(r => r.RunKey == runKey, ct))
                    return (null, false);
                throw;
            }

            _logger.LogInformation("Server flow run {RunId} ({Flow}) started by its schedule ({At}){Manual}.", run.Id, name, at, startedBy == null ? "" : $", outside the schedule (Try it, user {startedBy})");
            _queue.Enqueue(run.Id);
            return (run.Id, false);
        }
    }
}
