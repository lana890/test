using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// Clears finished runs (done or stopped) once they are older than their flow keeps them:
    /// settings.privacy.keepRunsDays of the flow's version that is on (else its newest version),
    /// else ServerFlows:KeepRunsDays (0 = keep). A run that waits for a person, or is still going,
    /// is never cleared. Called from the worker's sweep; does its work at most once an hour.
    ///
    /// What a run held (its data, outputs, edited bodies, last error) and its steps are deleted; its
    /// row stays, with its run key, status, subject and dates. The run key is what tells every
    /// trigger "this already ran" (a payment, an order's move, a page's once-value, a webhook's), so
    /// the row is never deleted: deleting it would let the same thing start again. A run that an
    /// admin started again between the read and the clearing is left alone. A page or another
    /// system that asks about a cleared run gets it as finished (done, or failed with the site's
    /// sentence) without results; the page's visitor is told by the run's subject (the customer, or
    /// whose cart it is).
    /// <see cref="MinimumKeep"/> is the sweeps' look-back plus a day.
    /// </summary>
    public sealed class ServerFlowRetention
    {
        private static readonly TimeSpan Every = TimeSpan.FromHours(1);
        private const int Batch = 500;

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ILogger<ServerFlowRetention> _logger;
        private DateTimeOffset _lastSweep = DateTimeOffset.MinValue;

        public ServerFlowRetention(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog, ILogger<ServerFlowRetention> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _logger = logger;
        }

        /// <summary>The shortest a finished run is kept, whatever a flow says: the sweeps' look-back, plus a day.</summary>
        public static TimeSpan MinimumKeep(ServerFlowOptions options) =>
            TimeSpan.FromHours(Math.Max(Math.Max(1, options.MissedTriggerLookbackHours), Math.Clamp(options.ScheduleCatchUpHours, 1, 168))) + TimeSpan.FromDays(1);

        public async Task SweepAsync(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastSweep < Every)
                return;
            _lastSweep = now;

            var options = _options.CurrentValue;
            var minimum = MinimumKeep(options);
            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;
            var flowRows = sp.GetRequiredService<IRepository<ServerFlow>>();
            var runs = sp.GetRequiredService<IRepository<ServerFlowRun>>();
            var steps = sp.GetRequiredService<IRepository<ServerFlowStep>>();

            // Only flows with finished runs older than the minimum, not cleared yet, can have anything to clear.
            var oldest = now - minimum;
            var names = await runs.Query().AsNoTracking()
                .Where(r => (r.Status == ServerFlowRunStatus.Succeeded || r.Status == ServerFlowRunStatus.Stopped) && (r.CompletedOn ?? r.UpdatedOn) < oldest && r.Context != null)
                .Select(r => new { r.SiteId, r.FlowName })
                .Distinct()
                .ToListAsync(ct);

            foreach (var flow in names)
            {
                var days = await KeepDaysAsync(flowRows, flow.SiteId, flow.FlowName, options.KeepRunsDays, ct);
                if (days <= 0)
                    continue;

                var keep = TimeSpan.FromDays(days) > minimum ? TimeSpan.FromDays(days) : minimum;
                var cutoff = now - keep;
                var (siteId, name) = (flow.SiteId, flow.FlowName);
                var cleared = 0;
                while (!ct.IsCancellationRequested)
                {
                    var ids = await runs.Query().AsNoTracking()
                        .Where(r => r.SiteId == siteId && r.FlowName == name
                            && (r.Status == ServerFlowRunStatus.Succeeded || r.Status == ServerFlowRunStatus.Stopped)
                            && (r.CompletedOn ?? r.UpdatedOn) < cutoff && r.Context != null)
                        .OrderBy(r => r.Id)
                        .Select(r => r.Id)
                        .Take(Batch)
                        .ToListAsync(ct);
                    if (ids.Count == 0)
                        break;
                    // The row, only while it is still finished, and the steps of the rows cleared: in one
                    // transaction, so a cleared run never keeps its steps (later sweeps skip it).
                    var batch = 0;
                    await runs.ExecuteInTransactionAsync(async () =>
                    {
                        batch = await runs.Query()
                            .Where(r => ids.Contains(r.Id) && (r.Status == ServerFlowRunStatus.Succeeded || r.Status == ServerFlowRunStatus.Stopped)
                                && (r.CompletedOn ?? r.UpdatedOn) < cutoff && r.Context != null)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(r => r.Context, (string?)null)
                                .SetProperty(r => r.Outputs, (string?)null)
                                .SetProperty(r => r.RequestOverrides, (string?)null)
                                .SetProperty(r => r.LastError, (string?)null)
                                .SetProperty(r => r.CurrentStep, (string?)null), ct);
                        await steps.Query().Where(s => ids.Contains(s.RunId) && s.Run.Context == null).ExecuteDeleteAsync(ct);
                    });
                    cleared += batch;
                    if (ids.Count < Batch)
                        break;
                }
                if (cleared > 0)
                    _logger.LogInformation("Server flows: cleared {Count} finished run(s) of {Flow} older than {Days:0.#} days (their rows stay, so nothing starts twice).", cleared, name, keep.TotalDays);
            }
        }

        /// <summary>Whether a run was cleared by the retention (only its row is left): it can no longer be run or looked into.</summary>
        public static bool IsCleared(ServerFlowRun run) => run.Context == null;

        /// <summary>The days a flow keeps finished runs: its version that is on, else its newest version, else the server's default.</summary>
        private async Task<int> KeepDaysAsync(IRepository<ServerFlow> flows, long siteId, string name, int serverDays, CancellationToken ct)
        {
            var current = await flows.Query().AsNoTracking()
                .Where(f => f.SiteId == siteId && f.Name == name)
                .OrderByDescending(f => f.Status == ServerFlowStatus.Active)
                .ThenByDescending(f => f.Version)
                .FirstOrDefaultAsync(ct);
            return current == null ? serverDays : _catalog.Parse(current).Definition?.Settings.KeepRunsDays ?? serverDays;
        }
    }
}
