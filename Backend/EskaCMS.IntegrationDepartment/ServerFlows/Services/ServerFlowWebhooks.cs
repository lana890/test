using System.Net;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// A call to a webhook flow's address as it reached the server: how it says whose it is (the
    /// key, or the time and signature of a signed request), the body's bytes exactly as sent (a
    /// signature covers them), and where it came from (the address the server saw, and the one
    /// Cloudflare names when the call came through Cloudflare).
    /// </summary>
    /// <param name="Path">The path as the request named it (from /api/ on), which a caller may have signed instead of the documented one (e.g. another letter case).</param>
    public sealed record WebhookRequest(string Method, string? Key, string? Timestamp, string? Signature, byte[] Body, IPAddress? Peer, string? CloudflareAddress,
        string? Path = null);

    /// <summary>
    /// Runs that another system starts by calling a flow's address:
    ///   POST {PathPrefix}{siteId}/{flow}                  the values it sends as a JSON body
    ///   GET  {PathPrefix}{siteId}/{flow}/run/{runId}?wait= the state of a run it started
    ///  - only the active version of a flow whose trigger is "webhook" answers;
    ///  - a flow may take calls from given addresses only (allowedIps; behind Cloudflare, the
    ///    caller's own address, CF-Connecting-IP, read only from Cloudflare's ranges);
    ///  - the caller proves the call is its own (see <see cref="WebhookAuth"/>): a signed request
    ///    (auth "signature": the time and the signature of method, path, time and body, checked with
    ///    the flow's public keys; refused once the time is MaxAge away from the server's), or the
    ///    flow's key in X-Flow-Key (auth "key"; the flow keeps the key's SHA-256, compared in constant
    ///    time; a signed flow still takes its old key through keyUntil);
    ///  - calls that prove it are limited per flow (ServerFlows:WebhookCallsPerMinute); every other
    ///    call (an unknown site or flow, a refused address, no or a wrong proof) per site, flow and
    ///    calling address (WebhookWrongKeysPerMinute), never from the flow's allowance;
    ///  - what is sent is checked against the flow's inputs; anything else is dropped;
    ///  - once "input": one run per value of onceInput, and calling again answers about that run; a
    ///    signed request sent again (a retry, or a copy within the time allowed) answers about the run
    ///    it started;
    ///  - the call waits for the result up to the flow's waitSec (at most HoldSeconds); ?wait= may
    ///    ask for less (the time the other system has), never for more;
    ///  - the run calls as the site (its "Server flows" account), in the language the call names in
    ///    its cultureId header, else the site's default one, which its sentences are in too;
    ///  - every call to an existing flow is kept in its "Recent calls" (<see cref="ServerFlowWebhookCalls"/>).
    /// </summary>
    public sealed class ServerFlowWebhooks
    {
        public const string WebhookSubject = "webhook";
        public const string KeyHeader = "X-Flow-Key";

        /// <summary>The start of every webhook address; the server's CypherSecurity must let it through unsigned.</summary>
        public const string PathPrefix = "/api/ServerFlows/hook/";

        private static readonly JsonSerializerSettings BodySettings = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 32 };
        private static readonly IReadOnlyList<AddressRule> DefaultCloudflare = WebhookAuth.RulesOf(WebhookAuth.CloudflareRanges);

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowAnswers _answers;
        private readonly ServerFlowTexts _texts;
        private readonly ServerFlowWebhookCalls _log;
        private readonly ILogger<ServerFlowWebhooks> _logger;
        /// <summary>Per flow (calls that prove they are the other system's), and per site, flow and address (every other call).</summary>
        private readonly PerMinuteLimit _calls = new();
        private readonly PerMinuteLimit _strangers = new();

        public ServerFlowWebhooks(IServiceScopeFactory scopes, IOptionsMonitor<ServerFlowOptions> options, ServerFlowCatalog catalog,
            ServerFlowQueue queue, ServerFlowAnswers answers, ServerFlowTexts texts, ServerFlowWebhookCalls log, ILogger<ServerFlowWebhooks> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _queue = queue;
            _answers = answers;
            _texts = texts;
            _log = log;
            _logger = logger;
        }

        /// <summary>
        /// Whether the server lets the webhook address through as another system calls it: without
        /// DCMS's request signature (CypherSecurity:ExecludedApis, or signing switched off), and with
        /// its answer as it is (CypherSecurity:ApiWrapperExecludedApis). The middleware matches an
        /// entry when the request path contains it.
        /// </summary>
        public static (bool Open, bool Unwrapped) AddressReadiness(IConfiguration configuration)
        {
            const string sample = PathPrefix + "1/flow";
            bool Listed(string section) => (configuration.GetSection(section).Get<string[]>() ?? Array.Empty<string>())
                .Any(entry => !string.IsNullOrWhiteSpace(entry) && sample.Contains(entry.Trim(), StringComparison.OrdinalIgnoreCase));
            var signing = bool.TryParse(configuration["CypherSecurity:IS_ACTIVE"], out var active) && active;
            return (!signing || Listed("CypherSecurity:ExecludedApis"), !signing || Listed("CypherSecurity:ApiWrapperExecludedApis"));
        }

        /// <summary>A new key for a webhook flow and the hash the flow keeps (the key itself is shown once and never stored).</summary>
        public static (string Key, string KeyHash) NewKey()
        {
            // 256 random bits, URL-safe (base64url without padding): 43 characters.
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return (key, PageRunRules.HashKey(key));
        }

        /// <summary>The ranges whose CF-Connecting-IP names the caller (none when ReadCloudflareAddress is off).</summary>
        private IReadOnlyList<AddressRule> CloudflareRules()
        {
            var options = _options.CurrentValue;
            if (!options.ReadCloudflareAddress)
                return Array.Empty<AddressRule>();
            return options.CloudflareRanges is { Length: > 0 } ranges ? WebhookAuth.RulesOf(ranges) : DefaultCloudflare;
        }

        /// <summary>A call: starts a run (or answers about the run of the same onceInput value, or of the same signed request), then waits up to waitSec.</summary>
        public async Task<PageRunResult> CallAsync(long siteId, string? flowName, WebhookRequest request, long cultureId, int? waitSec, CancellationToken ct = default)
        {
            var check = await CheckAsync(siteId, flowName, request, null, cultureId, ct);
            if (check.Flow == null)
                return check.Refused!;

            JToken? input = null;
            var text = Encoding.UTF8.GetString(request.Body).TrimStart('\uFEFF');
            if (text.Trim().Length > 0)
            {
                try
                {
                    input = JsonConvert.DeserializeObject<JToken>(text, BodySettings);
                }
                catch (JsonException)
                {
                    var refused = PageRunResult.Refuse(400, check.Texts!.Get(FlowTexts.BadJson));
                    Record(check, "input", refused, null, refused.Error);
                    return refused;
                }
            }

            var result = await StartAsync(check, input, waitSec, ct);
            Record(check, result.Answer != null ? "accepted" : "input", result, result.Answer?.RunId, result.Answer == null ? result.Error : null);
            return result;
        }

        /// <summary>The state of a run of this flow; waits up to the flow's waitSec, or less when the call asks for less.</summary>
        public async Task<PageRunResult> GetAsync(long siteId, string? flowName, WebhookRequest request, long cultureId, long runId, int? waitSec, CancellationToken ct = default)
        {
            var check = await CheckAsync(siteId, flowName, request, runId, cultureId, ct);
            if (check.Flow == null)
                return check.Refused!;
            var result = await AnswerAsync(check.Flow, runId, PageRunRules.WaitFor(waitSec, check.Flow.Definition.Settings.Webhook!.WaitSec), cultureId, ct);
            Record(check, result.Answer != null ? "accepted" : "input", result, result.Answer?.RunId ?? runId, result.Answer == null ? result.Error : null);
            return result;
        }

        /// <summary>A call that may use the flow: the flow, the caller's sentences, and how the call proved it is the other system's.</summary>
        private sealed record Checked(long SiteId, string Name, ActiveServerFlow? Flow, CallerTexts? Texts, PageRunResult? Refused,
            string Action, string? Address, bool ViaCloudflare, WebhookProof? Proof);

        /// <summary>How a call proved it is the other system's: "signature" (the key that signed it, and the request's identity) or "key".</summary>
        private sealed record WebhookProof(string By, WebhookPublicKey? Key, string? RequestId)
        {
            public string Describe => Key == null ? By : $"{By}: {(string.IsNullOrEmpty(Key.Name) ? Key.ShortFingerprint : Key.Name)}";
        }

        /// <summary>
        /// The active webhook flow with this name, if the call may use it. Nobody is signed in here.
        /// A call that is not the other system's (an unknown site or flow, an address the flow does not
        /// take, a missing or wrong proof) is held to a small allowance per site, flow and calling
        /// address, and never uses up the flow's own: a stranger can only use up their own. A call that
        /// proves it is the other system's uses the flow's allowance only. An unknown site or flow
        /// leaves nothing behind but its allowance's count.
        /// </summary>
        private async Task<Checked> CheckAsync(long siteId, string? flowName, WebhookRequest request, long? runId, long cultureId, CancellationToken ct)
        {
            var options = _options.CurrentValue;
            var now = DateTimeOffset.UtcNow;
            var name = (flowName ?? "").Trim();
            var action = runId == null ? "start" : "read";
            var address = WebhookAuth.CallerAddress(request.Peer, request.CloudflareAddress, CloudflareRules(), out var viaCloudflare);
            var addressText = address?.ToString();
            var stranger = $"{siteId}:{name}:{addressText ?? "unknown"}";
            var strangers = Math.Max(1, options.WebhookWrongKeysPerMinute);
            Checked Refused(PageRunResult refusal, CallerTexts? texts) => new(siteId, name, null, texts, refusal, action, addressText, viaCloudflare, null);

            // No such site: the built-in sentences (there are no site ones to read).
            if (!await _texts.SiteExistsAsync(siteId, ct))
                return Refused(_strangers.Take(stranger, strangers)
                    ? PageRunResult.Refuse(404, new CallerText(FlowTexts.NoSuchFlow, name).ToString())
                    : PageRunResult.Refuse(429, new CallerText(FlowTexts.TooMany).ToString()), null);

            var texts = await _texts.ForAsync(siteId, cultureId, ct);
            if (!options.Enabled)
                return Refused(PageRunResult.Refuse(503, texts.Get(FlowTexts.SwitchedOff)), texts);

            var flow = (await _catalog.GetActiveAsync(siteId, FlowSettings.WebhookTrigger, ct)).FirstOrDefault(f => f.Name == name);
            if (flow == null)
                return Refused(_strangers.Take(stranger, strangers)
                    ? PageRunResult.Refuse(404, texts.Get(new CallerText(FlowTexts.NoSuchFlow, name)))
                    : PageRunResult.Refuse(429, texts.Get(FlowTexts.TooMany)), texts);

            var hook = flow.Definition.Settings.Webhook!;

            // Refused, and kept in the flow's recent calls with the reason.
            Checked Refuse(int status, string outcome, string sentence, string? detail = null)
            {
                var refusal = _strangers.Take(stranger, strangers)
                    ? PageRunResult.Refuse(status, sentence)
                    : PageRunResult.Refuse(429, texts.Get(FlowTexts.TooMany));
                _log.Record(siteId, name, new WebhookCallRecord(now, addressText, viaCloudflare, action, refusal.StatusCode == 429 ? "tooMany" : outcome,
                    refusal.StatusCode, runId, null, detail ?? sentence));
                _logger.LogWarning("Server flows: a call to the webhook of {Flow} (site {SiteId}) from {Address} was refused: {Outcome}.", name, siteId, addressText ?? "unknown", outcome);
                return Refused(refusal, texts);
            }

            if (!WebhookAuth.AddressAllowed(hook.AllowedAddresses, address))
                return Refuse(403, "address", texts.Get(new CallerText(FlowTexts.AddressRefused, addressText ?? "unknown")));

            WebhookProof proof;
            if (hook.Signed && !string.IsNullOrWhiteSpace(request.Signature))
            {
                var maxAge = WebhookAuth.ClampMaxAge(options.SignatureMaxAgeSeconds);
                switch (WebhookAuth.CheckTime(request.Timestamp, now, maxAge, out var off))
                {
                    case WebhookAuth.TimeCheck.Missing:
                    case WebhookAuth.TimeCheck.NotANumber:
                        return Refuse(401, "time", texts.Get(FlowTexts.TimeMissing));
                    case WebhookAuth.TimeCheck.TooFar when WebhookAuth.LooksLikeMilliseconds(request.Timestamp):
                        return Refuse(401, "time", texts.Get(new CallerText(FlowTexts.TimeInMilliseconds, now.ToUnixTimeSeconds())),
                            "The request's time is in milliseconds; the time is signed and sent in seconds.");
                    case WebhookAuth.TimeCheck.TooFar:
                        return Refuse(401, "time", texts.Get(new CallerText(FlowTexts.TimeTooFar, request.Timestamp!.Trim(), off, now.ToUnixTimeSeconds(), maxAge)),
                            $"The request's time is {Math.Abs(off)} seconds {(off > 0 ? "ahead of" : "behind")} the server's (at most {maxAge} are allowed).");
                }
                // The documented path, else the path exactly as the request named it (another letter case reaches the same flow).
                var paths = new[] { WebhookAuth.PathOf(siteId, name, runId), request.Path }
                    .Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.Ordinal).ToList();
                string? signedText = null;
                WebhookPublicKey? key = null;
                foreach (var path in paths)
                {
                    var text = WebhookAuth.StringToSign(request.Timestamp!, request.Method, path!, request.Body);
                    if ((key = WebhookAuth.SignedBy(hook.PublicKeys, text, request.Signature)) != null)
                    {
                        signedText = text;
                        break;
                    }
                }
                if (key == null)
                    return Refuse(401, "signature", texts.Get(FlowTexts.BadSignature),
                        WebhookAuth.SignatureBytes(request.Signature) == null
                            ? "The signature is not base64."
                            : "No public key of the flow matches this signature. The server checked it against this text (its lines, joined here with \" | \"): "
                              + WebhookAuth.StringToSign(request.Timestamp!, request.Method, paths[0]!, request.Body).Replace("\n", " | "));
                proof = new WebhookProof("signature", key, WebhookAuth.RequestId(signedText!));
            }
            else if (!string.IsNullOrEmpty(request.Key))
            {
                // a signed flow takes its old key only through keyUntil (the day, in the flow's time zone)
                var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, flow.Definition.Settings.TimeZone).DateTime);
                var keyTaken = !hook.Signed || (hook.KeyUntil is { } until && today <= until && hook.KeyHash.Length > 0);
                if (!keyTaken)
                    return Refuse(401, "keyRetired", texts.Get(FlowTexts.SignedOnly),
                        hook.KeyUntil is { } last ? $"The old key was taken through {last:yyyy-MM-dd}." : "This flow takes signed requests only.");
                if (!PageRunRules.KeyMatches(request.Key, hook.KeyHash))
                    return Refuse(401, "key", texts.Get(FlowTexts.WrongKey));
                proof = new WebhookProof("key", null, null);
            }
            else
                return Refuse(401, hook.Signed ? "signature" : "key", texts.Get(hook.Signed ? FlowTexts.BadSignature : FlowTexts.WrongKey),
                    hook.Signed ? "The call had no X-Flow-Signature (and no key)." : "The call had no X-Flow-Key.");

            if (!_calls.Take($"{siteId}:{name}", options.WebhookCallsPerMinute))
            {
                var refusal = PageRunResult.Refuse(429, texts.Get(FlowTexts.TooMany));
                _log.Record(siteId, name, new WebhookCallRecord(now, addressText, viaCloudflare, action, "tooMany", 429, runId, proof.Describe, refusal.Error));
                return Refused(refusal, texts);
            }
            return new Checked(siteId, name, flow, texts, null, action, addressText, viaCloudflare, proof);
        }

        private void Record(Checked check, string outcome, PageRunResult result, long? runId, string? detail) =>
            _log.Record(check.SiteId, check.Name, new WebhookCallRecord(DateTimeOffset.UtcNow, check.Address, check.ViaCloudflare, check.Action, outcome,
                result.Answer != null ? 200 : result.StatusCode, runId, check.Proof?.Describe, detail));

        /// <summary>Starts a run of a webhook flow with what was sent (or answers about the run of the same value, or of the same signed request).</summary>
        private async Task<PageRunResult> StartAsync(Checked check, JToken? input, int? waitSec, CancellationToken ct)
        {
            var flow = check.Flow!;
            var texts = check.Texts!;
            var hook = flow.Definition.Settings.Webhook!;
            var (clean, problem) = PageRunRules.CleanInput(hook.Inputs, input);
            if (problem != null)
                return PageRunResult.Refuse(400, texts.Get(problem));

            var cultureId = texts.Language?.Id ?? 0;
            var onceValue = hook.Once == WebhookSettings.OncePerInput ? clean![hook.OnceInput!] : null;
            var requestId = check.Proof?.RequestId;
            var runKey = PageRunRules.WebhookRunKey(flow.SiteId, flow.Name, onceValue, requestId);
            var sameRunAgain = onceValue != null || requestId != null;
            var wait = PageRunRules.WaitFor(waitSec, hook.WaitSec);

            using var scope = _scopes.CreateScope();
            var runs = scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>();
            if (sameRunAgain && await FindRunIdAsync(runs, runKey, ct) is long existing)
                return await AnswerAsync(flow, existing, wait, cultureId, ct);

            var proof = new JObject { ["by"] = check.Proof?.By };
            if (check.Proof?.Key is { } key)
                proof["key"] = new JObject { ["name"] = key.Name, ["fingerprint"] = key.Fingerprint };
            if (check.Address != null)
                proof["address"] = check.Address;
            if (check.ViaCloudflare)
                proof["viaCloudflare"] = true;
            var context = FlowContextBuilder.WebhookContext(flow.SiteId, clean!, cultureId, DateTimeOffset.UtcNow, proof);
            var run = new ServerFlowRun
            {
                SiteId = flow.SiteId,
                FlowId = flow.Id,
                FlowName = flow.Name,
                SubjectType = WebhookSubject,
                SubjectId = flow.Id,
                RunKey = runKey,
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
                _logger.LogInformation("Server flow run {RunId} ({Flow}) started by its webhook ({Proof}).", run.Id, flow.Name, check.Proof?.Describe);
                _queue.Enqueue(run.Id);
            }
            catch (DbUpdateException) when (sameRunAgain)
            {
                // The same value (or the same signed request) was sent twice at once: answer about the run the other call made.
                using var retryScope = _scopes.CreateScope();
                var other = await FindRunIdAsync(retryScope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>(), runKey, ct);
                if (other == null)
                    throw;
                runId = other.Value;
            }
            return await AnswerAsync(flow, runId, wait, cultureId, ct);
        }

        /// <summary>A webhook run is read only through its own flow's address (and proof).</summary>
        private Task<PageRunResult> AnswerAsync(ActiveServerFlow flow, long runId, int waitSec, long cultureId, CancellationToken ct) =>
            _answers.AnswerAsync(flow.SiteId, runId, owner => owner.SubjectType == WebhookSubject && owner.FlowName == flow.Name, waitSec, cultureId, ct);

        private static Task<long?> FindRunIdAsync(IRepository<ServerFlowRun> runs, string runKey, CancellationToken ct) =>
            runs.Query().AsNoTracking().Where(r => r.RunKey == runKey).Select(r => (long?)r.Id).FirstOrDefaultAsync(ct);
    }
}
