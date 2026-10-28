using System.Globalization;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>A payment gateway as the flow editor lists it.</summary>
    /// <param name="Id">What a flow names in paymentMethods (the provider id the payments carry).</param>
    /// <param name="Name">The provider's name in the Admin.</param>
    /// <param name="Enabled">Switched on for the site.</param>
    /// <param name="Used">Seen on at least one successful payment of the site.</param>
    public sealed record PaymentGatewayInfo(string Id, string Name, bool Enabled, bool Used);

    /// <summary>A payment method the site's orders were placed with (Order.PaymentMethod), e.g. "SADAD".</summary>
    /// <param name="Id">The method as the orders carry it: what a flow names in skipPaymentMethods.</param>
    /// <param name="Orders">How many of the site's orders were placed with it.</param>
    public sealed record OrderPaymentMethodInfo(string Id, int Orders);

    /// <summary>One coming time of a schedule: the exact moment and how it reads in the flow's time zone.</summary>
    public sealed record ScheduleTime(DateTimeOffset At, string Local);

    /// <summary>One of the site's e-mail templates (Email templates), for the e-mail a stopped run sends.</summary>
    public sealed record EmailTemplateInfo(string Id, string Subject, string? Culture);

    public sealed record OrderStatusInfo(string Name, int Value, string Label);

    /// <summary>
    /// Where other systems call this site's webhook flows: Path + "{flow name}" on the API's address
    /// (Path ends with the site id), with the key in the KeyHeader header. Open: the server lets the
    /// address through unsigned. Unwrapped: it answers as it is, not in DCMS's envelope.
    /// </summary>
    public sealed record WebhookAddressInfo(string Path, string KeyHeader, bool Open, bool Unwrapped, WebhookSigningInfo Signing);

    /// <summary>
    /// How a signed call is made (WebhookAuth): the headers, the first line of the text signed, how far
    /// the caller's clock may be from the server's (SignatureMaxAgeSeconds), and the editor's limits:
    /// keys per flow, days the old key may still be taken, address rules. ReadsCloudflare: a call
    /// through Cloudflare is known by its real address (CF-Connecting-IP).
    /// </summary>
    public sealed record WebhookSigningInfo(string TimestampHeader, string SignatureHeader, string Version, int MaxAgeSec,
        int MaxPublicKeys, int MaxKeyDays, int MaxAddressRules, bool ReadsCloudflare);

    /// <summary>
    /// The server's limits and defaults the flow editor shows (all from the ServerFlows settings and the
    /// server's rules). MinKeepRunsDays: finished runs are kept at least this long whatever a flow says
    /// (see ServerFlowRetention). RetryMax / RetryDelaysSec: the retries of a call that says nothing.
    /// PaymentPageWaitSec: how long a payment page waits when a payment flow says nothing.
    /// </summary>
    public sealed record ServerFlowLimits(int HoldSeconds, int MaxWaitSec, int PageStartsPerMinute, int WebhookCallsPerMinute,
        int KeepRunsDays, int MinKeepRunsDays, int LookbackHours, int SweepSeconds, int ScheduleCatchUpHours, int AlertQuietMinutes,
        int RetryMax, IReadOnlyList<int> RetryDelaysSec, int PaymentPageWaitSec);

    /// <summary>
    /// Everything the flow editor offers as a choice, read from the server and the site, so no list
    /// in the Admin is fixed: the default time zone, the site's languages, the order statuses an
    /// order status flow may name, the payment gateways, the payment methods orders were placed with,
    /// the site's e-mail templates, the webhook address, and the server's limits.
    /// </summary>
    public sealed record ServerFlowEditorSettings(string TimeZone, bool TimeZoneIsServers, IReadOnlyList<SiteLanguage> Languages,
        IReadOnlyList<OrderStatusInfo> OrderStatuses, IReadOnlyList<PaymentGatewayInfo> Gateways, IReadOnlyList<OrderPaymentMethodInfo> OrderPaymentMethods,
        IReadOnlyList<EmailTemplateInfo> EmailTemplates, WebhookAddressInfo Webhook, ServerFlowLimits Limits);

    /// <summary>The helpers of the flow editor: its settings and lists, and the schedule preview.</summary>
    public sealed class ServerFlowAdminTools
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _providers;
        private readonly IRepository<Payment> _payments;
        private readonly IRepository<Order> _orders;
        private readonly IRepositoryWithTypedId<EmailTemplates, string> _templates;
        private readonly ServerFlowTexts _texts;
        private readonly ServerFlowAnswers _answers;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly IConfiguration _configuration;

        public ServerFlowAdminTools(IRepositoryWithTypedId<PaymentProvider, string> providers, IRepository<Payment> payments, IRepository<Order> orders,
            IRepositoryWithTypedId<EmailTemplates, string> templates, ServerFlowTexts texts, ServerFlowAnswers answers,
            IOptionsMonitor<ServerFlowOptions> options, IConfiguration configuration)
        {
            _providers = providers;
            _payments = payments;
            _orders = orders;
            _templates = templates;
            _texts = texts;
            _answers = answers;
            _options = options;
            _configuration = configuration;
        }

        /// <summary>The flow editor's settings and lists for a site (see <see cref="ServerFlowEditorSettings"/>).</summary>
        public async Task<ServerFlowEditorSettings> SettingsAsync(long siteId, CancellationToken ct = default)
        {
            var options = _options.CurrentValue;
            var languages = await _texts.LanguagesAsync(siteId, ct);
            // the sentences callers get are in Translations as soon as an admin opens the flows
            await _texts.AddOwnKeysAsync(siteId, ct);

            var statuses = ServerFlowOrderStatuses.Offered
                .Select(s => new OrderStatusInfo(s.ToString(), (int)s, s.GetDisplayName()))
                .ToList();

            var (open, unwrapped) = ServerFlowWebhooks.AddressReadiness(_configuration);
            return new ServerFlowEditorSettings(
                options.TimeZoneId,
                string.IsNullOrWhiteSpace(options.DefaultTimeZone),
                languages,
                statuses,
                await PaymentGatewaysAsync(siteId),
                await OrderPaymentMethodsAsync(siteId, ct),
                await EmailTemplatesAsync(siteId, ct),
                new WebhookAddressInfo($"{ServerFlowWebhooks.PathPrefix}{siteId}/", ServerFlowWebhooks.KeyHeader, open, unwrapped,
                    new WebhookSigningInfo(WebhookAuth.TimestampHeader, WebhookAuth.SignatureHeader, WebhookAuth.Version,
                        WebhookAuth.ClampMaxAge(options.SignatureMaxAgeSeconds), WebhookAuth.MaxPublicKeys, WebhookAuth.MaxKeyDays,
                        WebhookAuth.MaxAddressRules, options.ReadCloudflareAddress)),
                new ServerFlowLimits(_answers.HoldSeconds, FlowSettings.MaxWaitSec, options.PageStartsPerMinute, options.WebhookCallsPerMinute,
                    options.KeepRunsDays, (int)Math.Ceiling(ServerFlowRetention.MinimumKeep(options).TotalDays), options.MissedTriggerLookbackHours,
                    options.SweepSeconds, options.ScheduleCatchUpHours, options.AlertQuietMinutes,
                    FlowRequest.DefaultRetryMax, FlowRequest.DefaultRetryDelaysSec, FlowSettings.DefaultPaymentPageWaitSec));
        }

        /// <summary>The site's active e-mail templates (Email templates), by subject.</summary>
        public async Task<IReadOnlyList<EmailTemplateInfo>> EmailTemplatesAsync(long siteId, CancellationToken ct = default)
        {
            var rows = await _templates.Query().AsNoTracking()
                .Where(t => t.SiteId == siteId && t.Status == Entities.Enum.GeneralEnums.EStatus.Active)
                .Select(t => new { t.Id, t.Subject, t.CultureId })
                .Take(500)
                .ToListAsync(ct);
            return rows
                .Select(t => new EmailTemplateInfo(t.Id, string.IsNullOrWhiteSpace(t.Subject) ? t.Id : t.Subject, t.CultureId))
                .OrderBy(t => t.Subject, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The site's payment providers, plus any other gateway its successful payments name (an
        /// older or removed provider). DCMS's own coupon / points / refund / cash-on-delivery rows
        /// are left out: they never count as a verified payment.
        /// </summary>
        public async Task<IReadOnlyList<PaymentGatewayInfo>> PaymentGatewaysAsync(long siteId)
        {
            var providers = await _providers.Query().AsNoTracking()
                .Where(p => p.SiteId == siteId)
                .Select(p => new { p.Id, p.Name, p.IsEnabled })
                .ToListAsync();
            var used = await _payments.Query().AsNoTracking()
                .Where(p => p.Order.SiteId == siteId && p.Status == PaymentStatus.Succeeded && p.PaymentMethod != null)
                .Select(p => p.PaymentMethod)
                .Distinct()
                .Take(100)
                .ToListAsync();

            var internalMethods = FlowContextBuilder.InternalPaymentMethods;
            bool IsInternal(string id) => internalMethods.Contains(id, StringComparer.OrdinalIgnoreCase);
            var usedSet = new HashSet<string>(used, StringComparer.OrdinalIgnoreCase);

            var list = providers
                .Where(p => !string.IsNullOrWhiteSpace(p.Id) && !IsInternal(p.Id))
                .Select(p => new PaymentGatewayInfo(p.Id, string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name, p.IsEnabled, usedSet.Contains(p.Id)))
                .ToList();
            foreach (var id in used.Where(u => !string.IsNullOrWhiteSpace(u) && !IsInternal(u) && !list.Any(g => string.Equals(g.Id, u, StringComparison.OrdinalIgnoreCase))))
                list.Add(new PaymentGatewayInfo(id, id, false, true));
            return list.OrderByDescending(g => g.Enabled).ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// The payment methods the site's orders were placed with, most used first: what "Never for
        /// orders paid by" offers. It includes methods settled outside DCMS (e.g. "SADAD"), whose
        /// orders have no payment row and so are not among the gateways.
        /// </summary>
        public async Task<IReadOnlyList<OrderPaymentMethodInfo>> OrderPaymentMethodsAsync(long siteId, CancellationToken ct = default)
        {
            var rows = await _orders.Query().AsNoTracking()
                .Where(o => o.SiteId == siteId && o.PaymentMethod != null && o.PaymentMethod != "")
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new { Id = g.Key, Orders = g.Count() })
                .OrderByDescending(x => x.Orders)
                .Take(50)
                .ToListAsync(ct);
            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Id))
                .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new OrderPaymentMethodInfo(g.First().Id.Trim(), g.Sum(x => x.Orders)))
                .OrderByDescending(m => m.Orders)
                .ToList();
        }

        /// <summary>The next times a schedule runs, in its time zone (or why it cannot run).</summary>
        public (IReadOnlyList<ScheduleTime> Next, IReadOnlyList<FlowIssue> Issues) SchedulePreview(JToken? trigger, string? timeZone, int count = 5)
        {
            var (schedule, zone, issues) = FlowDefinitionParser.ParseSchedule(trigger, timeZone, _options.CurrentValue.TimeZoneId);
            if (schedule == null)
                return (Array.Empty<ScheduleTime>(), issues);
            var next = ScheduleRules.Next(schedule, zone, DateTimeOffset.UtcNow, Math.Clamp(count, 1, 20))
                .Select(at => new ScheduleTime(at, TimeZoneInfo.ConvertTime(at, zone).ToString("ddd d MMM yyyy, HH:mm", CultureInfo.InvariantCulture)))
                .ToList();
            return (next, issues);
        }
    }
}
