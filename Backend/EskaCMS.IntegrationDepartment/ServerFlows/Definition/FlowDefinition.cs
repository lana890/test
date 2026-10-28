using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>How a request may be repeated when its result is not clear.</summary>
    public enum FlowRequestClass
    {
        /// <summary>Only reads. Safe to repeat.</summary>
        Read = 1,

        /// <summary>Changes data, but sending it twice has the same effect as once (e.g. "set status to paid"). Safe to repeat.</summary>
        IdempotentWrite = 2,

        /// <summary>Sending it twice does it twice (e.g. "convert the quotation to a policy"). Never repeated blindly: a guard checks first.</summary>
        NonIdempotent = 3,
    }

    public enum FlowRequestKind
    {
        /// <summary>A data source (ApiMapper) run through the same invoker the pages use.</summary>
        DataSource = 1,

        /// <summary>A DCMS operation run in-process (see <see cref="ServerOperations"/>): notification, account.</summary>
        Local = 2,
    }

    /// <summary>A validated flow definition. Built only by <see cref="FlowDefinitionParser"/>; never changed after that.</summary>
    public sealed class FlowDefinition
    {
        public FlowSettings Settings { get; init; } = new();

        public IReadOnlyList<FlowGroup> Groups { get; init; } = Array.Empty<FlowGroup>();

        public IEnumerable<FlowRequest> Requests => Groups.SelectMany(g => g.Requests);

        public FlowRequest? FindRequest(string name) => Requests.FirstOrDefault(r => r.Name == name);

        /// <summary>
        /// The kept values the page can read, by their name: (request, output). A payment flow's
        /// reference is one of them: it is the value the customer is shown.
        /// </summary>
        public IReadOnlyList<FlowOutputRef> PublicOutputs
        {
            get
            {
                var list = Requests.SelectMany(r => r.Outputs.Values.Where(o => o.Public).Select(o => new FlowOutputRef(r.Name, o.Name))).ToList();
                if (Settings.ReferenceFrom is { } reference && !list.Contains(reference))
                    list.Add(reference);
                return list;
            }
        }

        private FlowOutputRef? _reference;

        /// <summary>
        /// The value that labels a run and tells the customer the work is done: orderReference, else
        /// the first value a page can read of a call that answers once (one that loops, or keeps every
        /// polling answer, has a list instead of one value).
        /// </summary>
        public FlowOutputRef? Reference => _reference ??= Settings.ReferenceFrom
            ?? PublicOutputs.FirstOrDefault(o => FindRequest(o.Request) is { Loop: null } r && r.Repeat?.CollectAll != true);
    }

    public sealed class FlowSettings
    {
        public const string OrderPaidTrigger = "orderPaid";

        /// <summary>Started by a page (a "Run on the server" request in a journey), for the signed-in visitor.</summary>
        public const string PageTrigger = "page";

        /// <summary>Started by the server itself at set times (every N minutes, or at times of day).</summary>
        public const string ScheduleTrigger = "schedule";

        /// <summary>Started by another system calling the flow's address with its key.</summary>
        public const string WebhookTrigger = "webhook";

        /// <summary>Started when an order moves to one of the flow's statuses (an admin, or DCMS itself, changed it).</summary>
        public const string OrderStatusTrigger = "orderStatus";

        /// <summary>paymentMethods entry that accepts the verified payment of any gateway.</summary>
        public const string AnyPaymentMethod = "*";

        public string Trigger { get; init; } = OrderPaidTrigger;

        /// <summary>The page trigger's settings (null for any other trigger).</summary>
        public PageTriggerSettings? Page { get; init; }

        /// <summary>The schedule trigger's settings (null for any other trigger).</summary>
        public ScheduleSettings? Schedule { get; init; }

        /// <summary>The webhook trigger's settings (null for any other trigger).</summary>
        public WebhookSettings? Webhook { get; init; }

        /// <summary>The order status trigger's settings (null for any other trigger); its products are <see cref="ProductIds"/>.</summary>
        public OrderStatusSettings? OrderStatus { get; init; }

        /// <summary>
        /// Whether somebody reads the run's answer (status, results, message): the page that started
        /// it, another system, or, after a payment, the payment result page ("Read the result").
        /// </summary>
        public bool AnswersACaller => Trigger is PageTrigger or WebhookTrigger or OrderPaidTrigger;

        /// <summary>How long the flow lets its caller wait for the result in all (0 for flows nobody reads).</summary>
        public int CallerWaitSec => Page?.WaitSec ?? Webhook?.WaitSec ?? (Trigger == OrderPaidTrigger ? PaymentPageWaitSec : 0);

        /// <summary>The longest a flow may let its caller wait in all (waitSec); one request is held at most ServerFlows:HoldSeconds.</summary>
        public const int MaxWaitSec = 300;

        /// <summary>
        /// After a payment: how long the payment result page waits for the result (trigger.waitSec).
        /// A payment flow that says nothing answers at once, as a page flow does: the page shows what
        /// there is and asks again when it wants to.
        /// </summary>
        public const int DefaultPaymentPageWaitSec = 0;

        public int PaymentPageWaitSec { get; init; } = DefaultPaymentPageWaitSec;

        /// <summary>Products the flow is for (after a payment, and an order's status); empty = any order.</summary>
        public IReadOnlyList<long> ProductIds { get; init; } = Array.Empty<long>();

        /// <summary>Payment methods whose successful payment counts as verified ("*" = any gateway). Other paid orders wait for an admin.</summary>
        public IReadOnlyList<string> PaymentMethods { get; init; } = new[] { AnyPaymentMethod };

        /// <summary>
        /// After a payment: payment methods that never start the flow (trigger.skipPaymentMethods), e.g.
        /// "SADAD" when the insurer issues the policy itself for a bill paid through SADAD. An order is
        /// paid by its successful payment's method, else by the method it was placed with.
        /// </summary>
        public IReadOnlyList<string> SkipPaymentMethods { get; init; } = Array.Empty<string>();

        /// <summary>Output that holds the amount the order should have cost (e.g. the quotation premium).</summary>
        public FlowOutputRef? PaymentAmountFrom { get; init; }

        /// <summary>"paidAmount" (the captured payment) or "orderTotal".</summary>
        public string PaymentCompareTo { get; init; } = "paidAmount";

        public decimal PaymentTolerance { get; init; } = 0.01m;

        /// <summary>
        /// After a payment: the output that is the run's reference (e.g. the policy number). It labels
        /// the run in the Admin, the payment page reads it like any value it can read, and once the
        /// run has it the customer is told the work is done, even if a later call stops for a person.
        /// </summary>
        public FlowOutputRef? ReferenceFrom { get; init; }

        /// <summary>Staff e-mails told when a run needs a person.</summary>
        public IReadOnlyList<string> AttentionEmails { get; init; } = Array.Empty<string>();

        /// <summary>
        /// The site's e-mail template those e-mails use (notify.onAttention.template, an Email
        /// templates id); null = the built-in text.
        /// </summary>
        public string? AttentionTemplateId { get; init; }

        public string TimeZoneId { get; init; } = "UTC";

        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

        /// <summary>
        /// Value names (any depth, any case) shown as "***" in the Admin's run screens and kept that
        /// way in the bodies and answers a run stores (settings.privacy.hide). The run itself still
        /// uses the real values.
        /// </summary>
        public IReadOnlyList<string> HiddenKeys { get; init; } = Array.Empty<string>();

        /// <summary>Finished runs are deleted after this many days (settings.privacy.keepRunsDays); null = the server's default.</summary>
        public int? KeepRunsDays { get; init; }
    }

    public sealed record FlowOutputRef(string Request, string Output)
    {
        public override string ToString() => $"{Request}.{Output}";
    }

    /// <summary>
    /// trigger { "type": "page", ... }: a page starts the flow for its visitor.
    ///  - who: "signedIn" (default) or "anyone" (visitors who are not signed in too);
    ///  - once: may it run again for the same "cart" (journey, default) / "user" / "input"
    ///    (onceInput's value, for this visitor) / "always";
    ///  - afterFailure: when the run an ask is about stopped by itself, answer "failed" ("keep",
    ///    default: a person handles it in the Admin) or start a new run ("newRun");
    ///  - inputs: rules for what the page sends. The inputs a flow accepts are the ones its
    ///    "From the page" mappings read; anything else the page sends is dropped;
    ///  - waitSec: how long a page waits for the result whenever it asks, to start or to read
    ///    (0 = it carries on at once). One request waits at most 30 s; the page asks again for the rest;
    ///  - cartType: which of the visitor's carts the flow reads (1 = the shop cart the journeys use).
    /// </summary>
    public sealed class PageTriggerSettings
    {
        public const string WhoSignedIn = "signedIn";
        public const string WhoAnyone = "anyone";

        public const string OnceAlways = "always";
        public const string OncePerCart = "cart";
        public const string OncePerUser = "user";
        public const string OncePerInput = "input";

        public const string AfterFailureKeep = "keep";
        public const string AfterFailureNewRun = "newRun";

        public string Who { get; init; } = WhoSignedIn;

        public string Once { get; init; } = OncePerCart;

        public string? OnceInput { get; init; }

        public string AfterFailure { get; init; } = AfterFailureKeep;

        /// <summary>Every input the flow accepts (the ones its mappings read plus declared ones), with its rules.</summary>
        public IReadOnlyDictionary<string, FlowInputRule> Inputs { get; init; } = new Dictionary<string, FlowInputRule>();

        public int WaitSec { get; init; }

        public int CartType { get; init; } = 1;
    }

    /// <summary>
    /// trigger { "type": "schedule", ... }: the server starts the flow by itself, in the flow's time
    /// zone, either every N minutes (counted from midnight: every 30 = :00 and :30) or at fixed
    /// times of day, on all days or only the listed ones. One run per time; a time that comes while
    /// the previous run is still working is skipped, and after a stop only the latest missed time runs.
    /// </summary>
    public sealed class ScheduleSettings
    {
        /// <summary>Every N minutes (5 to 1440), or null when <see cref="Times"/> is used.</summary>
        public int? EveryMinutes { get; init; }

        /// <summary>Times of day (minutes after midnight), sorted; empty when <see cref="EveryMinutes"/> is used.</summary>
        public IReadOnlyList<int> Times { get; init; } = Array.Empty<int>();

        /// <summary>The days it runs on; empty = every day.</summary>
        public IReadOnlyList<DayOfWeek> Days { get; init; } = Array.Empty<DayOfWeek>();
    }

    /// <summary>
    /// trigger { "type": "webhook", ... }: another system starts the flow with
    /// POST api/ServerFlows/hook/{siteId}/{flowName} and the key whose SHA-256 is keyHash (the key itself
    /// is shown once, when the Admin makes it). What it sends is read "From the page" (fromInput),
    /// checked by inputs. once: "always" (a new run each call) or "input" (one run per value of
    /// onceInput: calling again with it answers about that run). waitSec: how long the caller may
    /// wait for the result in all (0-300; one request is held at most ServerFlows:HoldSeconds, then
    /// the caller asks again with GET .../run/{runId}).
    /// </summary>
    public sealed class WebhookSettings
    {
        public const string OnceAlways = "always";
        public const string OncePerInput = "input";

        /// <summary>How the other system proves a call is its own: <see cref="WebhookAuth.KeyMode"/> or <see cref="WebhookAuth.SignatureMode"/>.</summary>
        public string Auth { get; init; } = WebhookAuth.KeyMode;

        public bool Signed => Auth == WebhookAuth.SignatureMode;

        /// <summary>Signed requests: the public keys that may sign them (the private keys stay with the other system).</summary>
        public IReadOnlyList<WebhookPublicKey> PublicKeys { get; init; } = Array.Empty<WebhookPublicKey>();

        /// <summary>Signed requests: the old key (KeyHash) is still taken through this day, in the flow's time zone; null: not at all.</summary>
        public DateOnly? KeyUntil { get; init; }

        /// <summary>The addresses (and ranges) calls are taken from; empty: any.</summary>
        public IReadOnlyList<AddressRule> AllowedAddresses { get; init; } = Array.Empty<AddressRule>();

        /// <summary>The key's SHA-256 (the key in a header; for a signed flow, its old key while keyUntil lasts).</summary>
        public string KeyHash { get; init; } = "";

        public string Once { get; init; } = OnceAlways;

        public string? OnceInput { get; init; }

        public IReadOnlyDictionary<string, FlowInputRule> Inputs { get; init; } = new Dictionary<string, FlowInputRule>();

        public int WaitSec { get; init; }
    }

    /// <summary>
    /// trigger { "type": "orderStatus", "toStatuses": [...], "productIds": [...] }: an order moving
    /// INTO one of toStatuses (DCMS's OrderStatus names; the paid ones belong to "orderPaid", which
    /// checks the payment) starts the flow, once per move, found by the worker's sweep (within about
    /// SweepSeconds), never for a move from before the flow was switched on. The run reads the
    /// order like a payment flow. Optionally only orders of productIds.
    /// </summary>
    public sealed class OrderStatusSettings
    {
        public IReadOnlyList<string> ToStatuses { get; init; } = Array.Empty<string>();
    }

    /// <summary>What one page input must look like.</summary>
    public sealed class FlowInputRule
    {
        public bool Required { get; init; }

        /// <summary>"any", "text", "number", "boolean".</summary>
        public string Type { get; init; } = "any";

        public int? MaxLength { get; init; }
    }

    /// <summary>The group's executeType. The server always runs the requests one after the other.</summary>
    public enum FlowGroupMode
    {
        Sequential = 1,

        /// <summary>The browser runs them at once; the server runs them in order (none may read another's result).</summary>
        Parallel = 2,

        /// <summary>One request, run once per item of <see cref="FlowGroup.Source"/> when that is a list, else once.</summary>
        Conditional = 3,
    }

    public sealed class FlowGroup
    {
        public string Name { get; init; } = "";

        public int Index { get; init; }

        public FlowGroupMode Mode { get; init; } = FlowGroupMode.Sequential;

        /// <summary>Conditional groups: the path (in the results) of the list the request runs over.</summary>
        public string? Source { get; init; }

        /// <summary>{ conditions, statement } or null.</summary>
        public JObject? ExecuteIf { get; init; }

        /// <summary>transformResult: reshapes the group's result once all its requests are done.</summary>
        public FlowTransform? Transform { get; init; }

        public IReadOnlyList<FlowRequest> Requests { get; init; } = Array.Empty<FlowRequest>();
    }

    /// <summary>A group's transformResult: valueOps, then the script (argument "result"), then the mapping.</summary>
    public sealed class FlowTransform
    {
        public JToken? ValueOps { get; init; }

        public string? Script { get; init; }

        /// <summary>{ newKey: "path in the result" }.</summary>
        public IReadOnlyDictionary<string, string>? Mapping { get; init; }
    }

    /// <summary>
    /// loopOn (and a conditional group's list): the request is sent once per item. Each item is a
    /// step of its own ("name[0]", "name[1]", ...), so a resumed run goes on from the next item.
    /// </summary>
    public sealed class FlowLoop
    {
        /// <summary>Path in the results of the list to go over.</summary>
        public string SourcePath { get; init; } = "";

        /// <summary>Extra name the current item is also readable under (currentItem always is).</summary>
        public string? ItemAlias { get; init; }

        /// <summary>Extra result key the list of answers is also kept under.</summary>
        public string? ResultKey { get; init; }

        /// <summary>A conditional group's loop: the list of answers is also the group's result.</summary>
        public bool IsGroupSource { get; init; }
    }

    /// <summary>repeatConfig: the request is sent again until its answer says it is done (polling).</summary>
    public sealed class FlowRepeat
    {
        /// <summary>fixedCount, untilCondition, untilValue, polling or exponentialBackoff.</summary>
        public string Strategy { get; init; } = "fixedCount";

        public int MaxAttempts { get; init; } = 1;

        public int DelayMs { get; init; } = 1000;

        public string? CustomDelayFn { get; init; }

        /// <summary>The "done" conditions on the answer (untilCondition, untilValue as conditions, polling, backoff).</summary>
        public JObject? Until { get; init; }

        /// <summary>untilValue written as { path, operator, value }.</summary>
        public string? UntilPath { get; init; }

        public string? UntilOperator { get; init; }

        public JToken? UntilValue { get; init; }

        public JObject? StopIf { get; init; }

        public int? MaxDurationMs { get; init; }

        public bool ContinueOnError { get; init; }

        public bool CollectAll { get; init; }

        public int InitialDelayMs { get; init; } = 1000;

        public bool Exponential { get; init; }

        public int MaxDelayMs { get; init; } = 30000;

        public double Multiplier { get; init; } = 2;

        public bool Jitter { get; init; }
    }

    /// <summary>What an optional request that failed for good answers instead (fallbackValueMapping / fallbackValue).</summary>
    public sealed class FlowFallback
    {
        public JArray? Mapping { get; init; }

        public JToken? Value { get; init; }
    }

    public sealed class FlowRequest
    {
        public string Name { get; init; } = "";

        public string GroupName { get; init; } = "";

        public FlowRequestKind Kind { get; init; }

        public long DataSourceId { get; init; }

        /// <summary>For Local requests: the operation (a <see cref="ServerOperations"/> constant).</summary>
        public string Operation { get; init; } = "";

        /// <summary>The mappings, always as an array (the object form is converted).</summary>
        public JArray DataMapping { get; init; } = new();

        /// <summary>
        /// A local endpoint whose mappings nest (inputs / children): the browser builds its body with
        /// buildLocalTreeMapping (no spread, mappings without a paramName skipped), and so does the server.
        /// </summary>
        public bool LocalTree { get; init; }

        public JToken? StaticData { get; init; }

        public bool MergeData { get; init; }

        public JObject? ExecuteIf { get; init; }

        /// <summary>Response checks of the request and of its group ({ conditions, statement } each): any match = the call failed.</summary>
        public IReadOnlyList<JObject> ResponseChecks { get; init; } = Array.Empty<JObject>();

        /// <summary>Path of the error message in a failed response, for the admin page.</summary>
        public string? ResponseMessagePath { get; init; }

        /// <summary>
        /// The call's "On error" message settings: what its caller reads when this call stops the
        /// run (null when the call says nothing; the site's general sentence is used then).
        /// </summary>
        public FlowErrorMessage? ErrorMessage { get; init; }

        /// <summary>transformResponse.valueOps, applied to the response before outputs are read.</summary>
        public JToken? TransformValueOps { get; init; }

        /// <summary>transformResponse.script (argument "response"), after the valueOps.</summary>
        public string? TransformScript { get; init; }

        /// <summary>transformResponse.pick / omit, after the script.</summary>
        public IReadOnlyList<string> Pick { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Omit { get; init; } = Array.Empty<string>();

        public FlowLoop? Loop { get; init; }

        /// <summary>
        /// A loop item that fails for good is given up and the loop goes on (the browser catches it
        /// as { error, item, index } when the request has no error settings of its own).
        /// </summary>
        public bool LoopItemFailuresContinue { get; init; }

        public FlowRepeat? Repeat { get; init; }

        public FlowFallback? Fallback { get; init; }

        public string? ReadResFrom { get; init; }

        public FlowRequestClass Class { get; init; }

        /// <summary>Stop waiting after this many seconds (1-120; data sources that are not nonIdempotent only).</summary>
        public int? TimeoutSec { get; init; }

        /// <summary>Retries when the call says nothing: a read or a safe write is tried 3 more times.</summary>
        public const int DefaultRetryMax = 3;

        /// <summary>The waits before those retries, in seconds: 10 s, 1 min, 5 min.</summary>
        public static readonly IReadOnlyList<int> DefaultRetryDelaysSec = new[] { 10, 60, 300 };

        public int RetryMax { get; init; }

        /// <summary>Delay before each retry; the last value repeats.</summary>
        public IReadOnlyList<int> RetryDelaysSec { get; init; } = new[] { 30 };

        public FlowGuard? Guard { get; init; }

        public IReadOnlyDictionary<string, FlowOutput> Outputs { get; init; } = new Dictionary<string, FlowOutput>();

        /// <summary>A failure stops the run (true) or is recorded and the run goes on (false).</summary>
        public bool Critical { get; init; } = true;

        /// <summary>Name of the token under ServerFlows:Tokens to send as the bearer token.</summary>
        public string? AuthToken { get; init; }

        /// <summary>Example response used by Simulate instead of calling.</summary>
        public JToken? SampleResponse { get; init; }

        /// <summary>
        /// Older flows only (server.refusedMessage): the sentence by language when this call's answer
        /// says it failed. New flows say it on the call's "On error" screen (<see cref="ErrorMessage"/>).
        /// </summary>
        public IReadOnlyDictionary<string, string> RefusedMessages { get; init; } = new Dictionary<string, string>();

        public int RetryDelayFor(int failedAttempts)
        {
            if (RetryDelaysSec.Count == 0)
                return 30;
            var i = Math.Clamp(failedAttempts - 1, 0, RetryDelaysSec.Count - 1);
            return RetryDelaysSec[i];
        }
    }

    /// <summary>
    /// A call's "On error" message settings, as the pages' request editor writes them: the sentence
    /// (Message), or the other side's own words at ResponseMessagePath; with Translate, looked up in
    /// the site's Translations (TranslateKey, TranslateKeyPrefix, MessageTemplate patterns with
    /// {placeholders}, TranslateParams read from the answer). <see cref="FlowErrorMessages"/> applies
    /// them the way the pages do.
    /// </summary>
    public sealed class FlowErrorMessage
    {
        public string? Message { get; init; }

        public string? ResponseMessagePath { get; init; }

        public bool Translate { get; init; }

        public string? TranslateKey { get; init; }

        public string? TranslateKeyPrefix { get; init; }

        /// <summary>Message patterns, one per line ("Quotation {id} not valid").</summary>
        public string? MessageTemplate { get; init; }

        /// <summary>{ placeholder: path in the answer (or the literal text) }.</summary>
        public IReadOnlyDictionary<string, string>? TranslateParams { get; init; }

        /// <summary>The Translations component the key belongs to; null = a site-wide key.</summary>
        public string? TranslateComponent { get; init; }
    }

    public sealed class FlowOutput
    {
        public string Name { get; init; } = "";

        public string Path { get; init; } = "";

        public JToken? ValueOps { get; init; }

        /// <summary>"The page can read this": given back to the page that started the run, under its name.</summary>
        public bool Public { get; init; }
    }

    /// <summary>
    /// The read that answers "did the unclear call go through?" for a NonIdempotent request.
    /// DoneIf / NotDoneIf read the guard's own response; when neither matches, a person decides.
    /// </summary>
    public sealed class FlowGuard
    {
        public long DataSourceId { get; init; }

        public JArray DataMapping { get; init; } = new();

        /// <summary>Seconds to wait before asking, so the unclear call has finished on the other side.</summary>
        public int SettleSec { get; init; } = 15;

        public JObject DoneIf { get; init; } = new();

        public JObject NotDoneIf { get; init; } = new();

        /// <summary>The request's outputs, read from the guard response when it says "done".</summary>
        public IReadOnlyDictionary<string, FlowOutput> Outputs { get; init; } = new Dictionary<string, FlowOutput>();

        public string? AuthToken { get; init; }

        public JToken? SampleResponse { get; init; }

        public int MaxTries { get; init; } = 3;
    }
}
