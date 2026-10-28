using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// E-mails the staff listed in settings.notify.onAttention when a run stops for a person (or a
    /// flow that is on stops passing the checks), through DCMS's notifications. The text is the
    /// site's e-mail template the flow names (Email templates, filled the way DCMS fills a template:
    /// [FlowName], [RunId], [Subject], [Reason], [QuietMinutes]), else a built-in English text.
    /// At most one e-mail per flow every AlertQuietMinutes, so an insurer outage does not flood the inbox;
    /// every run is still on the runs page and in the log. The quiet time counts from an e-mail that
    /// went out: one that could not be sent does not keep the next one back. The e-mail carries no
    /// customer data: the run and order numbers, and the reason.
    /// </summary>
    public sealed class ServerFlowAlerts
    {
        private readonly FlowCaller _caller;
        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ILogger<ServerFlowAlerts> _logger;
        private readonly ConcurrentDictionary<(long SiteId, string Flow), DateTimeOffset> _lastSent = new();

        /// <summary>The flows an e-mail is being sent for right now: one at a time per flow.</summary>
        private readonly ConcurrentDictionary<(long SiteId, string Flow), byte> _sending = new();

        public ServerFlowAlerts(FlowCaller caller, IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ILogger<ServerFlowAlerts> logger)
        {
            _caller = caller;
            _scopes = scopes;
            _options = options;
            _logger = logger;
        }

        private TimeSpan Quiet => TimeSpan.FromMinutes(Math.Clamp(_options.CurrentValue.AlertQuietMinutes, 0, 1440));

        public Task NeedsAttentionAsync(ServerFlowRun run, string reason, IReadOnlyList<string> emails, string? templateId) =>
            SendAsync(run.SiteId, run.FlowName ?? "", $"{run.SubjectType} {run.SubjectId}", run.Id.ToString(CultureInfo.InvariantCulture),
                "A server flow stopped and needs a person.", "Open the server flow runs page to retry it or record the result.", reason, emails, templateId);

        /// <summary>A flow that is on but no longer passes the checks: it starts no runs until a corrected version is saved.</summary>
        public Task FlowBrokenAsync(long siteId, string flowName, int version, string reason, IReadOnlyList<string> emails, string? templateId) =>
            SendAsync(siteId, flowName, $"version {version}", "", "A server flow that is on no longer passes the checks, so it starts no runs.",
                "Open the flow in the Admin and save a corrected version.", reason, emails, templateId);

        private async Task SendAsync(long siteId, string flowName, string subject, string runId, string headline, string action, string reason,
            IReadOnlyList<string> emails, string? templateId)
        {
            if (emails.Count == 0)
                return;

            var key = (siteId, flowName);
            var quiet = Quiet;
            bool Quieted() => _lastSent.TryGetValue(key, out var last) && DateTimeOffset.UtcNow - last < quiet;
            // Checked again once this flow's slot is taken: an e-mail sent meanwhile counts.
            if (Quieted() || !_sending.TryAdd(key, 0))
                return;
            try
            {
                if (Quieted())
                    return;

                var message = new JObject
                {
                    ["NotificationType"] = 3, // Email
                    ["SendVia"] = 2,          // by e-mail address
                    ["SendValue"] = new JArray(emails),
                    ["isHTML"] = true,
                };

                if (templateId != null && await TemplateExistsAsync(siteId, templateId))
                {
                    // DCMS fills the template itself: its subject, its body, these values.
                    var values = new Dictionary<string, string>
                    {
                        ["[FlowName]"] = flowName,
                        ["[RunId]"] = runId,
                        ["[Subject]"] = subject,
                        ["[Reason]"] = reason,
                        ["[QuietMinutes]"] = quiet.TotalMinutes.ToString("0", CultureInfo.InvariantCulture),
                    };
                    message["EmailOptions"] = new JObject
                    {
                        ["TemplateId"] = templateId,
                        ["Params"] = new JArray(values.Select(v => new JObject { ["Param"] = v.Key, ["Value"] = Html(v.Value) })),
                    };
                }
                else
                {
                    if (templateId != null)
                        _logger.LogWarning("Server flow alert for {Flow}: the e-mail template \"{Template}\" is not on site {SiteId}; the built-in text is sent.", flowName, templateId, siteId);
                    message["MessageTitle"] = $"Server flow needs attention: {flowName} ({subject})";
                    message["MessageBody"] =
                        $"<p>{Html(headline)}</p>" +
                        $"<p><b>Flow:</b> {Html(flowName)}{(runId.Length == 0 ? "" : $"<br><b>Run:</b> {Html(runId)}")}<br><b>For:</b> {Html(subject)}</p>" +
                        $"<p><b>Why:</b> {Html(reason)}</p>" +
                        $"<p>{Html(action)}" +
                        (quiet > TimeSpan.Zero ? $" Other alerts of this flow in the next {quiet.TotalMinutes:0} minutes are not e-mailed separately." : "") + "</p>";
                }

                try
                {
                    var result = await _caller.SendNotificationAsync(message, new FlowIdentity(siteId, 0, 0, null));
                    if (result.Status == FlowCallStatus.Ok)
                        _lastSent[key] = DateTimeOffset.UtcNow;
                    else
                        _logger.LogWarning("Server flow alert for {Flow} ({Subject}) was not sent: {Error}", flowName, subject, result.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Server flow alert for {Flow} ({Subject}) was not sent.", flowName, subject);
                }
            }
            finally
            {
                _sending.TryRemove(key, out _);
            }

            // Forget old entries, so the map stays as small as the number of flows.
            var now = DateTimeOffset.UtcNow;
            foreach (var stale in _lastSent.Where(p => now - p.Value > quiet).Select(p => p.Key).ToList())
                _lastSent.TryRemove(stale, out _);
        }

        /// <summary>Whether the site has this e-mail template (an active one).</summary>
        private async Task<bool> TemplateExistsAsync(long siteId, string templateId)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<EmailTemplates, string>>().Query().AsNoTracking()
                    .AnyAsync(t => t.Id == templateId && t.SiteId == siteId && t.Status == EStatus.Active);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Server flow alert: could not read e-mail template \"{Template}\".", templateId);
                return false;
            }
        }

        private static string Html(string? text) => WebUtility.HtmlEncode(text ?? "");
    }
}
