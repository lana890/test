using System.Collections.Concurrent;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// An active flow version, parsed. OnSince: since when the flow (by its name) has started the same
    /// way without a break, through the versions that replaced each other with the same trigger and
    /// trigger settings; what started it before that moment (a payment, an order's move, a scheduled
    /// time) is never run.
    /// </summary>
    public sealed record ActiveServerFlow(long Id, long SiteId, string Name, int Version, DateTimeOffset ActivatedOn, FlowDefinition Definition, DateTimeOffset OnSince);

    /// <summary>
    /// Reads flow versions once and keeps them parsed:
    ///  - by version id, for the runner (a saved version never changes, so it is parsed once);
    ///  - the active versions per site and trigger, for the trigger and the payment return page,
    ///    for 30 seconds (activating or archiving clears it at once in this process). A refresh reads
    ///    only the versions' ids and checksums, and a definition only when it is not parsed yet.
    /// An active version that no longer passes the checks starts nothing; it is said in the DCMS log
    /// and to the flow's attention e-mails, once per version in this process.
    /// </summary>
    public sealed class ServerFlowCatalog
    {
        private static readonly TimeSpan ActiveTtl = TimeSpan.FromSeconds(30);

        /// <summary>A version switched on this close to its predecessor's switch-off replaced it (the same save).</summary>
        private static readonly TimeSpan SameSwitch = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowAlerts _alerts;
        private readonly ILogger<ServerFlowCatalog> _logger;
        private readonly ConcurrentDictionary<long, (string Checksum, string TimeZone, FlowParseResult Parsed)> _versions = new();
        private readonly ConcurrentDictionary<(long SiteId, string Trigger), (DateTimeOffset LoadedAt, IReadOnlyList<ActiveServerFlow> Flows)> _active = new();
        private readonly ConcurrentDictionary<long, (DateTimeOffset ActivatedOn, DateTimeOffset OnSince)> _onSince = new();
        private readonly ConcurrentDictionary<long, string> _starts = new();
        private readonly ConcurrentDictionary<long, byte> _reported = new();

        public ServerFlowCatalog(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowAlerts alerts, ILogger<ServerFlowCatalog> logger)
        {
            _scopes = scopes;
            _options = options;
            _alerts = alerts;
            _logger = logger;
        }

        /// <summary>The parsed definition of a stored version (cached by id and checksum, and the server's default time zone).</summary>
        public FlowParseResult Parse(ServerFlow flow) =>
            Cached(flow.Id, flow.Checksum) ?? Store(flow.Id, flow.Checksum, flow.Definition);

        /// <summary>
        /// The definition of a version by its id, null when it does not exist or no longer passes the
        /// checks. A saved version never changes, so a parsed one is not read again.
        /// </summary>
        public async Task<FlowDefinition?> DefinitionAsync(long flowId, CancellationToken ct = default)
        {
            var zone = _options.CurrentValue.TimeZoneId;
            if (_versions.TryGetValue(flowId, out var cached) && cached.TimeZone == zone)
                return cached.Parsed.Definition;
            using var scope = _scopes.CreateScope();
            var row = await scope.ServiceProvider.GetRequiredService<IRepository<ServerFlow>>().Query().AsNoTracking()
                .Where(f => f.Id == flowId).Select(f => new { f.Checksum, f.Definition }).FirstOrDefaultAsync(ct);
            return row == null ? null : Store(flowId, row.Checksum, row.Definition).Definition;
        }

        private FlowParseResult? Cached(long id, string checksum) =>
            _versions.TryGetValue(id, out var cached) && cached.Checksum == checksum && cached.TimeZone == _options.CurrentValue.TimeZoneId ? cached.Parsed : null;

        private FlowParseResult Store(long id, string checksum, string definition)
        {
            var options = _options.CurrentValue;
            var parsed = FlowDefinitionParser.Parse(definition, options.TimeZoneId, options.MaxDefinitionLength);
            _versions[id] = (checksum, options.TimeZoneId, parsed);
            return parsed;
        }

        public async Task<IReadOnlyList<ActiveServerFlow>> GetActiveAsync(long siteId, string trigger, CancellationToken ct = default)
        {
            var key = (siteId, trigger);
            if (_active.TryGetValue(key, out var hit) && DateTimeOffset.UtcNow - hit.LoadedAt < ActiveTtl)
                return hit.Flows;

            using var scope = _scopes.CreateScope();
            var flows = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlow>>();
            var rows = await flows.Query().AsNoTracking()
                .Where(f => f.SiteId == siteId && f.Trigger == trigger && f.Status == ServerFlowStatus.Active)
                .Select(f => new { f.Id, f.SiteId, f.Name, f.Version, f.Checksum, f.ActivatedOn, f.CreatedOn })
                .ToListAsync(ct);

            var list = new List<ActiveServerFlow>();
            foreach (var row in rows)
            {
                var parsed = Cached(row.Id, row.Checksum);
                string? text = null;
                if (parsed == null)
                {
                    text = await flows.Query().AsNoTracking().Where(f => f.Id == row.Id).Select(f => f.Definition).FirstOrDefaultAsync(ct) ?? "";
                    parsed = Store(row.Id, row.Checksum, text);
                }
                var activatedOn = row.ActivatedOn ?? row.CreatedOn;
                if (parsed.Definition != null)
                    list.Add(new ActiveServerFlow(row.Id, row.SiteId, row.Name, row.Version, activatedOn, parsed.Definition,
                        await OnSinceAsync(flows, row.Id, row.SiteId, row.Name, activatedOn, ct)));
                else if (_reported.TryAdd(row.Id, 0))
                    // A version that no longer passes (an engine change) is skipped, never run half-checked; never silently.
                    await ReportBrokenAsync(row.SiteId, row.Name, row.Version, parsed,
                        text ?? await flows.Query().AsNoTracking().Where(f => f.Id == row.Id).Select(f => f.Definition).FirstOrDefaultAsync(ct));
            }

            _active[key] = (DateTimeOffset.UtcNow, list);
            return list;
        }

        /// <summary>
        /// Since when the flow has started the same way without a break: the version's own switch-on,
        /// or, when it replaced the version that was on at that moment (saving a new version, going
        /// back to an older one) and that version started exactly the same way (the same trigger with
        /// the same settings and time zone: the same payments, products, statuses, times), that
        /// version's, and so on back. Anything else counts from the version's own switch-on: a
        /// payment of a product only the new version takes, or a time only it has, is never reached
        /// back for. Going back to an older version counts from its switch-back (its earlier time on
        /// is not kept), so the moment found is never too early.
        /// </summary>
        private async Task<DateTimeOffset> OnSinceAsync(IRepository<ServerFlow> flows, long id, long siteId, string name, DateTimeOffset activatedOn, CancellationToken ct)
        {
            if (_onSince.TryGetValue(id, out var known) && known.ActivatedOn == activatedOn)
                return known.OnSince;

            var versions = await flows.Query().AsNoTracking()
                .Where(f => f.SiteId == siteId && f.Name == name && f.Id != id && f.ArchivedOn != null && f.ActivatedOn != null)
                .Select(f => new { f.Id, ActivatedOn = f.ActivatedOn!.Value, ArchivedOn = f.ArchivedOn!.Value })
                .ToListAsync(ct);
            var starts = await StartOfAsync(flows, id, ct);
            var since = activatedOn;
            var taken = new HashSet<long> { id };
            while (true)
            {
                var before = versions
                    .Where(v => !taken.Contains(v.Id) && v.ArchivedOn <= since + SameSwitch && v.ArchivedOn >= since - SameSwitch && v.ActivatedOn < since)
                    .OrderBy(v => v.ActivatedOn).FirstOrDefault();
                if (before == null || await StartOfAsync(flows, before.Id, ct) != starts)
                    break;
                taken.Add(before.Id);
                since = before.ActivatedOn;
            }
            _onSince[id] = (activatedOn, since);
            return since;
        }

        /// <summary>How a version starts (FlowDefinitionParser.StartOf), kept per version: a saved version never changes.</summary>
        private async Task<string> StartOfAsync(IRepository<ServerFlow> flows, long id, CancellationToken ct)
        {
            if (_starts.TryGetValue(id, out var known))
                return known;
            var text = await flows.Query().AsNoTracking().Where(f => f.Id == id).Select(f => f.Definition).FirstOrDefaultAsync(ct);
            return _starts[id] = FlowDefinitionParser.StartOf(text);
        }

        /// <summary>An active version that no longer passes the checks: the DCMS log and the flow's attention e-mails say so.</summary>
        private async Task ReportBrokenAsync(long siteId, string name, int version, FlowParseResult parsed, string? definition)
        {
            var first = parsed.Issues.FirstOrDefault(i => i.Severity == FlowIssueSeverity.Error);
            var reason = $"Flow \"{name}\" v{version} is on, but it does not pass the checks any more, so it starts no runs: {first?.Message} Save a corrected version.";
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IdcmsLogService>().AddError("ServerFlows", $"flow {name} v{version}", reason, null, siteId);
                var (emails, template) = FlowDefinitionParser.AttentionOf(definition);
                await _alerts.FlowBrokenAsync(siteId, name, version, reason, emails, template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server flows: {Reason}", reason);
            }
        }

        /// <summary>Sites and their active flows for a trigger, for the sweep.</summary>
        public async Task<IReadOnlyList<ActiveServerFlow>> GetAllActiveAsync(string trigger, CancellationToken ct = default)
        {
            using var scope = _scopes.CreateScope();
            var flows = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlow>>();
            var siteIds = await flows.Query().AsNoTracking()
                .Where(f => f.Trigger == trigger && f.Status == ServerFlowStatus.Active)
                .Select(f => f.SiteId)
                .Distinct()
                .ToListAsync(ct);

            var all = new List<ActiveServerFlow>();
            foreach (var siteId in siteIds)
                all.AddRange(await GetActiveAsync(siteId, trigger, ct));
            return all;
        }

        public void Invalidate(long siteId)
        {
            foreach (var key in _active.Keys.Where(k => k.SiteId == siteId).ToList())
                _active.TryRemove(key, out _);
        }
    }
}
