using System.Collections.Concurrent;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// What a caller gets back about a run: "running", "done" or "failed", the values the flow lets
    /// it read ("The page can read this"), for "running" and "failed" a sentence for the customer
    /// (the failed call's own error message when it has one, else the site's), and WaitSec: how long
    /// the flow lets a caller wait for the result in all, so a caller whose request came back
    /// "running" keeps asking until then. RunId is null while a payment's run is still to come.
    /// </summary>
    public sealed record PageRunAnswer(long? RunId, string Status, JObject Results, string? Message, int WaitSec);

    /// <summary>The answer, or why the caller cannot have one (HTTP status + a sentence).</summary>
    public sealed record PageRunResult(PageRunAnswer? Answer, int StatusCode, string? Error)
    {
        public static PageRunResult Ok(PageRunAnswer answer) => new(answer, 200, null);

        public static PageRunResult Refuse(int statusCode, string error) => new(null, statusCode, error);
    }

    /// <summary>
    /// Whose a run is: what it is about, its flow, the caller its context names ({ siteId, userId,
    /// cultureId, cartId }), for a page's run the visitor who started it (VisitorId: the caller, or
    /// for a run the retention cleared, the customer it is about or whose cart it is), and for a
    /// payment's run the order's customer.
    /// </summary>
    public sealed record RunOwner(string SubjectType, long SubjectId, string FlowName, JObject? Caller, long? OrderCustomerId = null, long? VisitorId = null);

    /// <summary>
    /// Answers a page or another system about a run it may read (<see cref="PageRunAnswer"/>). One
    /// request waits for the result at most HoldSeconds; it is woken by the runner the moment the run
    /// changes, and reads the database again every few seconds only for a run that another server
    /// process finishes.
    /// </summary>
    public sealed class ServerFlowAnswers
    {
        private static readonly TimeSpan RecheckEvery = TimeSpan.FromSeconds(3);
        private static readonly JsonSerializerSettings ReadSettings = new() { DateParseHandling = DateParseHandling.None };

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowSignals _signals;
        private readonly ServerFlowTexts _texts;
        private readonly IMemoryCache _cache;

        public ServerFlowAnswers(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog,
            ServerFlowSignals signals, ServerFlowTexts texts, IMemoryCache cache)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _signals = signals;
            _texts = texts;
            _cache = cache;
        }

        /// <summary>The longest one request is held open (ServerFlows:HoldSeconds, 5-90).</summary>
        public int HoldSeconds => Math.Clamp(_options.CurrentValue.HoldSeconds, 5, 90);

        /// <summary>
        /// The state of a run, for a caller <paramref name="mayRead"/> accepts (for anyone else the run
        /// does not exist), after waiting up to <paramref name="waitSec"/> for it to finish. Sentences
        /// are in the caller's language: <paramref name="cultureId"/>, else the one the run was started in.
        /// </summary>
        public async Task<PageRunResult> AnswerAsync(long siteId, long runId, Func<RunOwner, bool> mayRead, int waitSec, long cultureId, CancellationToken ct)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(waitSec, 0, HoldSeconds));

            var owner = await OwnerAsync(siteId, runId, ct);
            if (owner == null || !mayRead(owner))
                return PageRunResult.Refuse(404, (await _texts.ForAsync(siteId, cultureId, ct)).Get(FlowTexts.NoSuchRun));
            var culture = cultureId > 0 ? cultureId : (long?)owner.Caller?["cultureId"] ?? 0;

            while (true)
            {
                // Listening starts before the read, so a run that finishes in between still wakes us.
                using var wake = _signals.Arm(owner.SubjectId);

                using var scope = _scopes.CreateScope();
                var sp = scope.ServiceProvider;
                var run = await sp.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                    .Where(r => r.Id == runId && r.SiteId == siteId)
                    .Select(r => new { r.Id, r.FlowId, r.Status, r.Reference })
                    .FirstOrDefaultAsync(ct);
                if (run == null)
                    return PageRunResult.Refuse(404, (await _texts.ForAsync(siteId, culture, ct)).Get(FlowTexts.NoSuchRun));

                var status = run.Status switch
                {
                    ServerFlowRunStatus.Succeeded => "done",
                    // A payment's run that has its reference (e.g. the policy number) is done for the
                    // customer, even when a later call (the receipt, the account) stopped for a person.
                    ServerFlowRunStatus.NeedsAttention or ServerFlowRunStatus.Stopped when !string.IsNullOrEmpty(run.Reference) => "done",
                    ServerFlowRunStatus.NeedsAttention or ServerFlowRunStatus.Stopped => "failed",
                    // Waiting for an admin to start it (a payment the server could not verify) is
                    // still under way for the customer: nothing failed.
                    _ => "running",
                };

                if (status != "running" || DateTimeOffset.UtcNow >= deadline)
                {
                    var outputs = await sp.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                        .Where(r => r.Id == runId).Select(r => r.Outputs).FirstOrDefaultAsync(ct);
                    var definition = await _catalog.DefinitionAsync(run.FlowId, ct);
                    string? message = null;
                    if (status != "done")
                    {
                        var texts = await _texts.ForAsync(siteId, culture, ct);
                        message = status == "failed"
                            ? await FailedMessageAsync(sp, siteId, run.Id, definition, texts, ct)
                            : texts.Get(FlowTexts.Running);
                    }
                    return PageRunResult.Ok(new PageRunAnswer(run.Id, status, Results(definition, outputs), message, definition?.Settings.CallerWaitSec ?? 0));
                }

                // Woken by the runner the moment the run changes; the database is checked again
                // every few seconds only for a run finished by another server process.
                var left = deadline - DateTimeOffset.UtcNow;
                await _signals.WaitAsync(wake, left < RecheckEvery ? left : RecheckEvery, ct);
            }
        }

        /// <summary>
        /// Whose a run is. It never changes, and a page asks about the same run every few seconds, so it
        /// is kept a few minutes once read (the run's context, which names the caller, is read once).
        /// </summary>
        private async Task<RunOwner?> OwnerAsync(long siteId, long runId, CancellationToken ct)
        {
            var key = $"ServerFlows:owner:{siteId}:{runId}";
            if (_cache.TryGetValue(key, out RunOwner? known) && known != null)
                return known;

            using var scope = _scopes.CreateScope();
            var row = await scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                .Where(r => r.Id == runId && r.SiteId == siteId)
                .Select(r => new { r.SubjectType, r.SubjectId, r.FlowName, r.Context })
                .FirstOrDefaultAsync(ct);
            if (row == null)
                return null;
            long? customer = null;
            if (row.SubjectType == ServerFlowTriggers.OrderSubject)
                customer = await scope.ServiceProvider.GetRequiredService<IRepository<Order>>().Query().AsNoTracking()
                    .Where(o => o.Id == row.SubjectId).Select(o => (long?)o.CustomerId).FirstOrDefaultAsync(ct);
            var caller = CallerOf(row.Context);
            // A page's run names its visitor in its context. One the retention cleared has none any
            // more: its subject says whose it is (the customer, or the customer whose cart it is), so
            // the visitor still gets its answer (done or failed, without results).
            var visitor = (long?)caller?["userId"];
            if (visitor == null && row.SubjectType == ServerFlowPageRuns.UserSubject)
                visitor = row.SubjectId;
            else if (visitor == null && row.SubjectType == ServerFlowPageRuns.CartSubject)
                visitor = await scope.ServiceProvider.GetRequiredService<IRepository<Cart>>().Query().AsNoTracking()
                    .Where(c => c.Id == row.SubjectId).Select(c => (long?)c.CustomerId).FirstOrDefaultAsync(ct);
            var owner = new RunOwner(row.SubjectType, row.SubjectId, row.FlowName, caller, customer, visitor);
            _cache.Set(key, owner, new MemoryCacheEntryOptions { SlidingExpiration = OwnerKeep });
            return owner;
        }

        private static readonly TimeSpan OwnerKeep = TimeSpan.FromMinutes(5);

        /// <summary>The kept values marked "The page can read this", by their own names.</summary>
        private static JObject Results(FlowDefinition? definition, string? outputs)
        {
            var results = new JObject();
            if (definition == null || string.IsNullOrWhiteSpace(outputs))
                return results;
            if (JsonConvert.DeserializeObject<JToken>(outputs, ReadSettings) is not JObject kept)
                return results;
            foreach (var reference in definition.PublicOutputs)
            {
                var value = kept[reference.Request] is JObject request ? request[reference.Output] : null;
                if (value != null && value.Type != JTokenType.Null)
                    results[reference.Output] = value.DeepClone();
            }
            return results;
        }

        /// <summary>Who started a run and in which language: the context's caller { siteId, userId, cultureId, cartId }.</summary>
        public static JObject? CallerOf(string? context)
        {
            try
            {
                return (JsonConvert.DeserializeObject<JToken>(context ?? "", ReadSettings) as JObject)?["caller"] as JObject;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// The sentence for a run that stopped. When a call stopped it and has an error message of its
        /// own ("On error": the message, or the other side's words at its path, translated as the pages
        /// translate it), that; otherwise the site's sentence for a stopped run.
        /// </summary>
        private async Task<string> FailedMessageAsync(IServiceProvider sp, long siteId, long runId, FlowDefinition? definition, CallerTexts texts, CancellationToken ct)
        {
            if (definition != null && definition.Requests.Any(r => r.ErrorMessage != null || r.RefusedMessages.Count > 0))
            {
                var last = await sp.GetRequiredService<IRepository<ServerFlowStep>>().Query().AsNoTracking()
                    .Where(s => s.RunId == runId)
                    .OrderByDescending(s => s.Id)
                    .Select(s => new { s.RequestName, s.Status, s.Kind, s.Error, s.Response })
                    .FirstOrDefaultAsync(ct);
                // A loop item's step is named "request[3]".
                var name = last?.RequestName ?? "";
                var bracket = name.IndexOf('[');
                var request = last?.Status == ServerFlowStepStatus.Failed && last.Kind != "guard" && last.Kind != "manual"
                    ? definition.FindRequest(bracket > 0 ? name[..bracket] : name)
                    : null;

                // Older flows: server.refusedMessage, for an answer that said it failed.
                if (request != null && request.RefusedMessages.Count > 0 && last!.Error?.StartsWith(ServerFlowRunner.RefusalPrefix, StringComparison.Ordinal) == true)
                    return texts.Get(FlowTexts.Failed, request.RefusedMessages);

                if (request?.ErrorMessage is { } settings)
                {
                    var answer = ParseAnswer(last!.Response);
                    var message = FlowErrorMessages.Message(settings, answer);
                    if (settings.Translate && texts.Language != null)
                    {
                        // The pages look the key up with the message, else with the other side's own
                        // words (only to find the key and its values: they are not shown as they are).
                        var words = message ?? FlowErrorMessages.AnswerWords(answer);
                        if (words != null || !string.IsNullOrWhiteSpace(settings.TranslateKey))
                        {
                            var (lookup, values) = FlowErrorMessages.Translation(settings, words ?? "", answer);
                            var translated = await _texts.TranslateAsync(siteId, texts.Language.Id, lookup, settings.TranslateComponent,
                                FlowErrorMessages.IsAuthorKey(settings, lookup), ct);
                            if (translated != null)
                                return FlowErrorMessages.Fill(translated, values);
                        }
                    }
                    // not translated yet: the message as it is, as on the pages (never the other side's
                    // words the author did not choose: the site's general sentence then)
                    if (message != null)
                        return message;
                }
            }
            return texts.Get(FlowTexts.Failed);
        }

        /// <summary>A stored answer as JSON (text that is not JSON stays text).</summary>
        private static JToken? ParseAnswer(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(stored, ReadSettings);
            }
            catch (JsonException)
            {
                return new JValue(stored);
            }
        }
    }
}
