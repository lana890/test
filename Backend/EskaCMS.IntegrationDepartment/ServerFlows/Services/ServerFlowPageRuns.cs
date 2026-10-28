using System.Globalization;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// Runs that a page starts ("Run on the server" in a journey), for the visitor who asked:
    ///  - only an active flow whose trigger is "page" can be started, by its name;
    ///  - "signedIn" flows refuse visitors who are not signed in;
    ///  - "once" names the run an ask is about, always the visitor's own (their journey, themselves,
    ///    or them and an input's value). Asking again answers about that run without checking the
    ///    inputs again; with "afterFailure: newRun" a run that stopped by itself is replaced;
    ///  - the page sends inputs; only the ones the flow accepts are kept, checked against their rules;
    ///  - the run reads the visitor's own cart, which the server finds (the page never names one);
    ///  - every ask (start or read) waits for the result as long as the flow's waitSec, at most
    ///    HoldSeconds per request; a page may ask for less (the time it has left), never for more;
    ///    the answer says how long the page may go on asking;
    ///  - starts are limited per visitor and flow (ServerFlows:PageStartsPerMinute);
    ///  - only the visitor who started a run can read it;
    ///  - "Read the result" also reads a payment's run: the payment page names the flow and the
    ///    order (orderId, as the payment's return address gives it), and only the order's customer
    ///    gets an answer;
    ///  - every sentence is in the visitor's language (<see cref="ServerFlowTexts"/>).
    /// </summary>
    public sealed class ServerFlowPageRuns
    {
        public const string CartSubject = "cart";
        public const string UserSubject = "user";

        private const string ReplacedReason = "Replaced by a new run: the page asked again after this run stopped, and the flow then starts a new run (\"After a failure\").";

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowAnswers _answers;
        private readonly ServerFlowTexts _texts;
        private readonly ServerFlowTriggers _triggers;
        private readonly ILogger<ServerFlowPageRuns> _logger;
        private readonly PerMinuteLimit _starts = new();

        /// <summary>How often a payment page's ask looks again for a run that is still to come.</summary>
        private static readonly TimeSpan LookAgainEvery = TimeSpan.FromSeconds(2);

        public ServerFlowPageRuns(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog,
            ServerFlowQueue queue, ServerFlowAnswers answers, ServerFlowTexts texts, ServerFlowTriggers triggers, ILogger<ServerFlowPageRuns> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _queue = queue;
            _answers = answers;
            _texts = texts;
            _triggers = triggers;
            _logger = logger;
        }


        private sealed record RunRow(long Id, ServerFlowRunStatus Status);

        /// <summary>Starts the flow for the visitor, or answers about the run this ask is about; waits up to waitSec.</summary>
        public async Task<PageRunResult> StartAsync(long siteId, long userId, long cultureId, string? flowName, JToken? input, int? waitSec, CancellationToken ct = default)
        {
            if (await NoSuchSiteAsync(siteId, new CallerText(FlowTexts.NoSuchFlow, (flowName ?? "").Trim()), ct) is { } noSite)
                return noSite;
            var texts = await _texts.ForAsync(siteId, cultureId, ct);
            var (flow, refused) = await FindFlowAsync(siteId, flowName, texts, ct);
            if (flow == null)
                return refused!;
            var page = flow.Definition.Settings.Page!;

            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;
            if (await RefuseVisitorAsync(sp, page, userId, texts, ct) is { } notAllowed)
                return notAllowed;

            var contexts = sp.GetRequiredService<FlowContextBuilder>();
            var cartId = await contexts.FindPageCartIdAsync(siteId, userId, page.CartType, ct);
            var runs = sp.GetRequiredService<IRepository<ServerFlowRun>>();

            // The run this ask is about. Asking again answers about it before the inputs are checked,
            // so a later step may ask without sending them again.
            string? runKey = null;
            RunRow? failed = null;
            if (page.Once != PageTriggerSettings.OnceAlways)
            {
                var (key, keyRefused) = KeyOf(siteId, flow.Name, page, userId, cartId, input, texts, starting: true);
                if (key == null)
                    return keyRefused!;
                runKey = key;

                var existing = await FindRunAsync(runs, runKey, ct);
                if (existing != null)
                {
                    var replace = existing.Status == ServerFlowRunStatus.NeedsAttention && page.AfterFailure == PageTriggerSettings.AfterFailureNewRun;
                    if (!replace)
                        return await AnswerAsync(siteId, userId, existing.Id, PageRunRules.WaitFor(waitSec, page.WaitSec), cultureId, ct);
                    failed = existing;
                }
            }

            var (clean, problem) = PageRunRules.CleanInput(page, input);
            if (problem != null)
                return PageRunResult.Refuse(400, texts.Get(problem));

            if (!_starts.Take($"{siteId}:{userId}:{flow.Name}", _options.CurrentValue.PageStartsPerMinute))
                return PageRunResult.Refuse(429, texts.Get(FlowTexts.TooMany));

            // The run calls in the visitor's language (the site's default one when the page sent none).
            var context = await contexts.BuildPageAsync(siteId, userId, texts.Language?.Id ?? cultureId, cartId, clean!, ct);

            // If an admin's "Try again" (or the same ask, twice at once) moved the failed run on first,
            // it keeps the key and the insert below answers about it instead.
            if (failed != null)
                await RetireAsync(runs, failed.Id, runKey!, ct);

            var forCart = cartId != null && page.Once != PageTriggerSettings.OncePerUser;
            var run = new ServerFlowRun
            {
                SiteId = siteId,
                FlowId = flow.Id,
                FlowName = flow.Name,
                SubjectType = forCart ? CartSubject : UserSubject,
                SubjectId = forCart ? cartId!.Value : userId,
                RunKey = runKey ?? PageRunRules.NewRunKey(siteId, flow.Name),
                Status = ServerFlowRunStatus.Queued,
                Context = context.ToString(Formatting.None),
                Outputs = "{}",
            };
            runs.Add(run);

            long runId;
            try
            {
                await runs.SaveChangesAsync();
                runId = run.Id;
                _logger.LogInformation("Server flow run {RunId} ({Flow}) started by a page for user {UserId}{Replacing}.", run.Id, flow.Name, userId,
                    failed == null ? "" : $", replacing run {failed.Id}");
                _queue.Enqueue(run.Id);
            }
            catch (DbUpdateException) when (runKey != null)
            {
                // Another request made (or kept) the run with this key first: answer about that one.
                using var retryScope = _scopes.CreateScope();
                var other = await FindRunAsync(retryScope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>(), runKey, ct);
                if (other == null)
                    throw;
                runId = other.Id;
            }

            return await AnswerAsync(siteId, userId, runId, PageRunRules.WaitFor(waitSec, page.WaitSec), cultureId, ct);
        }

        /// <summary>
        /// The run this ask is about, without starting one ("Read the result" with no runId): the
        /// journey's, the visitor's, or the one for the input's value; for a payment's flow, the run of
        /// the order the page names. Waits up to waitSec.
        /// </summary>
        public async Task<PageRunResult> FindAsync(long siteId, long userId, long cultureId, string? flowName, JToken? input, int? waitSec, CancellationToken ct = default)
        {
            if (await NoSuchSiteAsync(siteId, new CallerText(FlowTexts.NoSuchFlow, (flowName ?? "").Trim()), ct) is { } noSite)
                return noSite;
            var texts = await _texts.ForAsync(siteId, cultureId, ct);
            if (_options.CurrentValue.Enabled
                && (await _catalog.GetActiveAsync(siteId, FlowSettings.OrderPaidTrigger, ct)).FirstOrDefault(f => f.Name == (flowName ?? "").Trim()) is { } payment)
                return await PaymentRunAsync(siteId, userId, cultureId, payment, input, waitSec, texts, ct);

            var (flow, refused) = await FindFlowAsync(siteId, flowName, texts, ct);
            if (flow == null)
                return refused!;
            var page = flow.Definition.Settings.Page!;
            if (page.Once == PageTriggerSettings.OnceAlways)
                return PageRunResult.Refuse(400, texts.Get(FlowTexts.NeedsRunId));

            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;
            if (await RefuseVisitorAsync(sp, page, userId, texts, ct) is { } notAllowed)
                return notAllowed;

            var cartId = page.Once == PageTriggerSettings.OncePerCart
                ? await sp.GetRequiredService<FlowContextBuilder>().FindPageCartIdAsync(siteId, userId, page.CartType, ct)
                : null;
            var (key, keyRefused) = KeyOf(siteId, flow.Name, page, userId, cartId, input, texts, starting: false);
            if (key == null)
                return keyRefused!;

            var run = await FindRunAsync(sp.GetRequiredService<IRepository<ServerFlowRun>>(), key, ct);
            if (run == null)
                return PageRunResult.Refuse(404, texts.Get(FlowTexts.NotStarted));
            return await AnswerAsync(siteId, userId, run.Id, PageRunRules.WaitFor(waitSec, page.WaitSec), cultureId, ct);
        }

        /// <summary>
        /// A payment page reads the run of its order: input orderId, the id the payment's return
        /// address carries (encrypted) or the plain number. Only the order's customer gets an answer.
        /// While the payment is still being confirmed, or the run is about to be made, the page is told
        /// "running" and waits as the flow says; an order this flow never runs for is "not started".
        /// </summary>
        private async Task<PageRunResult> PaymentRunAsync(long siteId, long userId, long cultureId, ActiveServerFlow flow, JToken? input, int? waitSec,
            CallerTexts texts, CancellationToken ct)
        {
            var orderId = OrderIdOf((input as JObject)?["orderId"]);
            if (orderId == null)
                return PageRunResult.Refuse(400, texts.Get(new CallerText(FlowTexts.InputRequired, "orderId")));

            var settings = flow.Definition.Settings;
            var wait = PageRunRules.WaitFor(waitSec, settings.PaymentPageWaitSec);
            var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Min(wait, _answers.HoldSeconds));
            var runKey = ServerFlowTriggers.RunKeyFor(orderId.Value, flow.Name);
            var caughtUp = false;
            while (true)
            {
                OrderFacts? facts;
                long? runId;
                using (var scope = _scopes.CreateScope())
                {
                    var sp = scope.ServiceProvider;
                    facts = await sp.GetRequiredService<FlowContextBuilder>().GetFactsAsync(orderId.Value, ct);
                    runId = facts == null ? null : (await FindRunAsync(sp.GetRequiredService<IRepository<ServerFlowRun>>(), runKey, ct))?.Id;
                }
                if (facts == null || facts.SiteId != siteId || facts.CustomerId != userId)
                    return PageRunResult.Refuse(404, texts.Get(FlowTexts.NoSuchRun));

                if (runId != null)
                {
                    var left = (int)Math.Ceiling(Math.Max(0, (deadline - DateTimeOffset.UtcNow).TotalSeconds));
                    return await AnswerAsync(siteId, userId, runId.Value, left, cultureId, ct);
                }

                // No run yet. An order of none of the flow's products never gets one.
                if (settings.ProductIds.Count > 0 && !settings.ProductIds.Intersect(facts.ProductIds).Any())
                    return PageRunResult.Refuse(404, texts.Get(FlowTexts.NotStarted));

                // Paid: the payment's trigger makes the run; make it now if it has not yet (its own rules).
                if (ServerFlowTriggers.RunnableStatuses.Contains(facts.Status))
                {
                    if (caughtUp)
                        return PageRunResult.Refuse(404, texts.Get(FlowTexts.NotStarted));
                    caughtUp = true;
                    await _triggers.CatchUpAsync(flow, orderId.Value, ct);
                    continue;
                }

                // Failed, cancelled or refunded: the flow will not run for it.
                if (!ServerFlowTriggers.AwaitingPaymentStatuses.Contains(facts.Status))
                    return PageRunResult.Refuse(404, texts.Get(FlowTexts.NotStarted));

                // The payment is still being confirmed: wait for it as long as this ask may.
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    return PageRunResult.Ok(new PageRunAnswer(null, "running", new JObject(), texts.Get(FlowTexts.Running), settings.CallerWaitSec));
                await Task.Delay(remaining < LookAgainEvery ? remaining : LookAgainEvery, ct);
            }
        }

        /// <summary>The order id a payment page sends: the encrypted id of the payment's return address, or the plain number.</summary>
        public static long? OrderIdOf(JToken? value)
        {
            var text = value?.Type is JTokenType.String or JTokenType.Integer ? ((string?)value)?.Trim() : null;
            if (string.IsNullOrEmpty(text) || text.Length > 512)
                return null;
            if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var plain))
                return plain > 0 ? plain : null;
            try
            {
                // A '+' that reached the page unencoded arrives as a space.
                var decrypted = EncryptDecryptHelper.Decrypt(text.Replace(' ', '+'));
                return long.TryParse(decrypted, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The state of a run this visitor may read; waits up to waitSec for it to finish (as long as
        /// the run's flow lets a page wait when the page does not say, never longer).
        /// </summary>
        public async Task<PageRunResult> GetAsync(long siteId, long userId, long cultureId, long runId, int? waitSec, CancellationToken ct = default)
        {
            if (await NoSuchSiteAsync(siteId, new CallerText(FlowTexts.NoSuchRun), ct) is { } noSite)
                return noSite;
            var flowWait = 0;
            // Switched off: nothing will change, so the answer comes at once.
            if (_options.CurrentValue.Enabled)
            {
                long? flowId;
                using (var scope = _scopes.CreateScope())
                    flowId = await scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                        .Where(r => r.Id == runId && r.SiteId == siteId).Select(r => (long?)r.FlowId).FirstOrDefaultAsync(ct);
                flowWait = flowId == null ? 0 : (await _catalog.DefinitionAsync(flowId.Value, ct))?.Settings.CallerWaitSec ?? 0;
            }
            return await AnswerAsync(siteId, userId, runId, PageRunRules.WaitFor(waitSec, flowWait), cultureId, ct);
        }

        /// <summary>
        /// The siteId header is the caller's to write: a site that does not exist is answered at once,
        /// with the built-in sentence, before anything of it is read or kept.
        /// </summary>
        private async Task<PageRunResult?> NoSuchSiteAsync(long siteId, CallerText refusal, CancellationToken ct) =>
            await _texts.SiteExistsAsync(siteId, ct) ? null : PageRunResult.Refuse(404, refusal.ToString());

        /// <summary>
        /// A page run is read only by the visitor who started it, a payment's run only by the order's
        /// customer: for anyone else it does not exist.
        /// </summary>
        private Task<PageRunResult> AnswerAsync(long siteId, long userId, long runId, int waitSec, long cultureId, CancellationToken ct) =>
            _answers.AnswerAsync(siteId, runId,
                owner => (owner.SubjectType is CartSubject or UserSubject && owner.VisitorId == userId)
                    || (owner.SubjectType == ServerFlowTriggers.OrderSubject && owner.OrderCustomerId == userId),
                waitSec, cultureId, ct);

        private async Task<(ActiveServerFlow? Flow, PageRunResult? Refused)> FindFlowAsync(long siteId, string? flowName, CallerTexts texts, CancellationToken ct)
        {
            if (!_options.CurrentValue.Enabled)
                return (null, PageRunResult.Refuse(503, texts.Get(FlowTexts.SwitchedOff)));

            var name = (flowName ?? "").Trim();
            var flow = (await _catalog.GetActiveAsync(siteId, FlowSettings.PageTrigger, ct)).FirstOrDefault(f => f.Name == name);
            return flow == null
                ? (null, PageRunResult.Refuse(404, texts.Get(new CallerText(FlowTexts.NoSuchFlow, name))))
                : (flow, null);
        }

        private static async Task<PageRunResult?> RefuseVisitorAsync(IServiceProvider sp, PageTriggerSettings page, long userId, CallerTexts texts, CancellationToken ct)
        {
            var userType = await sp.GetRequiredService<IRepository<User>>().Query().AsNoTracking()
                .Where(u => u.Id == userId).Select(u => (EUsersTypes?)u.Type).FirstOrDefaultAsync(ct);
            if (userType == null)
                return PageRunResult.Refuse(403, texts.Get(FlowTexts.SignIn));
            if (page.Who == PageTriggerSettings.WhoSignedIn && userType == EUsersTypes.GuestClient)
                return PageRunResult.Refuse(403, texts.Get(FlowTexts.SignInCustomer));
            return null;
        }

        /// <summary>The key of the run an ask is about, or why it cannot be named.</summary>
        private static (string? Key, PageRunResult? Refused) KeyOf(long siteId, string flowName, PageTriggerSettings page, long userId, long? cartId, JToken? input,
            CallerTexts texts, bool starting)
        {
            JToken? onceValue = null;
            if (page.Once == PageTriggerSettings.OncePerCart && cartId == null)
                return (null, starting
                    // Not 409: the website treats every 409 as "go to the login page named in the body".
                    ? PageRunResult.Refuse(422, texts.Get(FlowTexts.NoCart))
                    : PageRunResult.Refuse(404, texts.Get(FlowTexts.NotStarted)));
            if (page.Once == PageTriggerSettings.OncePerInput)
            {
                var name = page.OnceInput!;
                var (value, problem) = PageRunRules.CleanValue(name, page.Inputs[name], (input as JObject)?[name]);
                if (problem != null || value == null)
                    return (null, PageRunResult.Refuse(400, texts.Get(problem ?? new CallerText(FlowTexts.InputRequired, name))));
                onceValue = value;
            }
            return (PageRunRules.RunKey(siteId, flowName, page, userId, cartId, onceValue), null);
        }

        private static Task<RunRow?> FindRunAsync(IRepository<ServerFlowRun> runs, string runKey, CancellationToken ct) =>
            runs.Query().AsNoTracking().Where(r => r.RunKey == runKey).Select(r => new RunRow(r.Id, r.Status)).FirstOrDefaultAsync(ct);

        /// <summary>
        /// Takes the key from a run that stopped by itself and stops it for good, so a new run can
        /// have the key. Does nothing when the run moved on first (e.g. an admin's "Try again").
        /// </summary>
        private static async Task RetireAsync(IRepository<ServerFlowRun> runs, long runId, string runKey, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var replacedKey = PageRunRules.ReplacedKey(runId);
            await runs.Query()
                .Where(r => r.Id == runId && r.RunKey == runKey && r.Status == ServerFlowRunStatus.NeedsAttention)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.RunKey, replacedKey)
                    .SetProperty(r => r.Status, ServerFlowRunStatus.Stopped)
                    .SetProperty(r => r.Reason, ReplacedReason)
                    .SetProperty(r => r.CompletedOn, now)
                    .SetProperty(r => r.UpdatedOn, now), ct);
        }
    }
}
