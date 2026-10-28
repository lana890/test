using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    public sealed class FlowParseResult
    {
        public FlowDefinition? Definition { get; init; }

        public IReadOnlyList<FlowIssue> Issues { get; init; } = Array.Empty<FlowIssue>();

        public bool IsValid => Definition != null;
    }

    /// <summary>
    /// Reads and checks a server flow: an apiGroupList (the dynamic data provider format) plus a
    /// "settings" object, limited to what the server can run safely ("server profile").
    ///
    /// What the request editor writes is read as the pages read it. The rules exist because the
    /// browser runtime would run something the server must not or cannot (browser-only values such as
    /// form controls, storage or the query string, files, form data), or because a call must be safe
    /// to resume. Each issue says what to use instead. The same parser runs when a flow is saved, when
    /// it is activated and when a run loads it, so a rule that a stored flow may break is a warning,
    /// unless such a flow could never have run: errors are for what cannot run as written.
    /// </summary>
    public static class FlowDefinitionParser
    {
        /// <summary>typeOfValue values a server mapping may use.</summary>
        public static readonly IReadOnlyCollection<string> MappingTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "static", "categories", "null", "currentDate", "fromOrder", "fromCart", "fromAdditionalDetailsCart",
            "previousResults", "objectOfObject", "arrayOfValues",
            "undefined", // the request editor's row for an unmapped input: the value is left out
            "currentItem", "currentItemKey", "currentIndex", // inside a loop
            "fromInput", // what the page sent (flows a page starts)
            // the signed-in visitor (a page flow) or the order's customer, read as the pages read
            // the signed-in user: Logged In User (a path such as username), User Option (an option's
            // name), Current Username
            "loggedInUser", "userOption", "currentUsername",
            "fromUser", // the older name for the same user (its own field names); still read
        };

        /// <summary>The input a "From the page" path reads: its first part ("claim.no" reads "claim").</summary>
        private static string InputName(string? path) => (path ?? "").Trim().Split('.', '[')[0].Trim();

        private static readonly HashSet<string> LiteralSources = new(StringComparer.Ordinal)
        {
            "static", "number", "true", "false", "null", "undefined",
        };

        /// <summary>getDataFrom values a condition may use in executeIf / conditional mappings.</summary>
        public static readonly IReadOnlyCollection<string> FlowConditionSources = new HashSet<string>(LiteralSources.Concat(new[]
        {
            "apiRes", "previousApi", "fromOrder", "fromCart", "fromAdditionalDetailsCart", "arrayData", "fromInput",
            // the signed-in user, as page conditions read it: User info (a path), User options (a name)
            "userInfo", "userOptions",
            "fromUser", // the older name; still read
        }), StringComparer.Ordinal);

        /// <summary>getDataFrom values a response check or a guard decision may use: the response itself and literals.</summary>
        public static readonly IReadOnlyCollection<string> ResponseConditionSources = new HashSet<string>(LiteralSources.Concat(new[]
        {
            "apiRes", "previousApi", "arrayData",
        }), StringComparer.Ordinal);

        /// <summary>Keys of the condition data; a request or group with one of these names would hide them.</summary>
        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "order", "cartData", "additionalDetails", "additionalDetailsCart", "InputAdditionalDetails", "allSessionsData",
            // a loop's current item and position
            "currentItem", "currentIndex",
        };

        private static readonly Regex NamePattern = new("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        private static readonly JsonSerializerSettings ReadSettings = new()
        {
            // Dates stay the text they were written as, like JSON.parse in the browser.
            DateParseHandling = DateParseHandling.None,
            MaxDepth = 64,
        };

        public static FlowParseResult Parse(string? json, string defaultTimeZone, int maxLength = 256 * 1024)
        {
            var issues = new List<FlowIssue>();
            if (string.IsNullOrWhiteSpace(json))
            {
                issues.Add(FlowIssue.Error("", "EMPTY", "The flow is empty."));
                return new FlowParseResult { Issues = issues };
            }

            if (json.Length > maxLength)
            {
                issues.Add(FlowIssue.Error("", "TOO_LARGE", $"The flow is {json.Length} characters; the limit is {maxLength}."));
                return new FlowParseResult { Issues = issues };
            }

            JToken? root;
            try
            {
                root = JsonConvert.DeserializeObject<JToken>(json, ReadSettings);
            }
            catch (JsonException ex)
            {
                issues.Add(FlowIssue.Error("", "BAD_JSON", $"Not valid JSON: {ex.Message}"));
                return new FlowParseResult { Issues = issues };
            }

            return Parse(root, defaultTimeZone, issues);
        }

        public static FlowParseResult Parse(JToken? root, string defaultTimeZone, List<FlowIssue>? issues = null)
        {
            issues ??= new List<FlowIssue>();
            if (root is not JObject obj)
            {
                issues.Add(FlowIssue.Error("", "NOT_OBJECT", "The flow must be an object: { \"settings\": { ... }, \"apiGroupList\": [ ... ] }."));
                return new FlowParseResult { Issues = issues };
            }

            foreach (var p in obj.Properties())
            {
                if (p.Name != "settings" && p.Name != "apiGroupList")
                    issues.Add(FlowIssue.Warning(p.Name, "UNKNOWN_KEY", $"\"{p.Name}\" is not used; only \"settings\" and \"apiGroupList\" are read."));
            }

            var ctx = new ParseContext(issues);
            IReadOnlyList<FlowGroup> groups;
            FlowSettings? settings;
            try
            {
                groups = ParseGroups(obj["apiGroupList"], ctx);
                settings = ParseSettings(obj["settings"], defaultTimeZone, ctx);

                if (settings != null)
                {
                    CheckSettingsReferences(settings, groups, ctx);
                    CheckWebhookStrength(settings, groups, ctx);
                }

                CheckExtraNames(groups, ctx);
                CheckPreviousResultReferences(groups, ctx);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException)
            {
                // A value where an object was expected (Newtonsoft throws when indexing into a
                // plain value): report it instead of failing the request.
                issues.Add(FlowIssue.Error("", "BAD_SHAPE", $"Part of the flow has the wrong shape (an object, list or value where another was expected): {ex.Message}"));
                return new FlowParseResult { Issues = issues };
            }

            var valid = !issues.Any(i => i.Severity == FlowIssueSeverity.Error) && settings != null;
            return new FlowParseResult
            {
                Issues = issues,
                Definition = valid ? new FlowDefinition { Settings = settings!, Groups = groups } : null,
            };
        }

        #region Settings

        private static FlowSettings? ParseSettings(JToken? token, string defaultTimeZone, ParseContext ctx)
        {
            const string path = "settings";
            if (token is not JObject s)
            {
                ctx.Error(path, "NO_SETTINGS", "\"settings\" is required, at least { \"trigger\": { \"type\": \"orderPaid\" } }.");
                return null;
            }

            var known = new HashSet<string>(StringComparer.Ordinal) { "trigger", "paymentCheck", "outputs", "notify", "timeZone", "privacy" };
            foreach (var p in s.Properties().Where(p => !known.Contains(p.Name)))
                ctx.Warning($"{path}.{p.Name}", "UNKNOWN_KEY", $"\"{p.Name}\" is not a setting and is ignored.");

            // trigger: "orderPaid" | { type: "orderPaid", productIds, paymentMethods, waitSec } | { type: "page", who, once, ... }
            //          | { type: "schedule", everyMinutes | times, days } | { type: "webhook", keyHash, once, onceInput, inputs, waitSec }
            //          | { type: "orderStatus", toStatuses, productIds }
            var trigger = s["trigger"];
            string? triggerType = trigger?.Type == JTokenType.String ? (string?)trigger : (string?)(trigger as JObject)?["type"];
            if (triggerType is not (FlowSettings.OrderPaidTrigger or FlowSettings.PageTrigger or FlowSettings.ScheduleTrigger or FlowSettings.WebhookTrigger or FlowSettings.OrderStatusTrigger))
                ctx.Error($"{path}.trigger", "BAD_TRIGGER", "trigger.type must be \"orderPaid\" (after a payment), \"page\" (a page starts it), \"schedule\" (at set times), \"webhook\" (another system calls it) or \"orderStatus\" (an order's status changes).");
            var isPayment = triggerType is FlowSettings.OrderPaidTrigger or null;
            var isPage = triggerType == FlowSettings.PageTrigger;
            var isSchedule = triggerType == FlowSettings.ScheduleTrigger;
            var isWebhook = triggerType == FlowSettings.WebhookTrigger;
            var isOrderStatus = triggerType == FlowSettings.OrderStatusTrigger;
            var startsWhat = isPage ? "a page starts this one"
                : isSchedule ? "this one runs on a schedule"
                : isWebhook ? "another system starts this one"
                : "an order's status starts this one";

            if (trigger is JObject to)
            {
                var knownTrigger = isPage
                    ? new HashSet<string>(StringComparer.Ordinal) { "type", "who", "once", "onceInput", "afterFailure", "inputs", "waitSec", "cartType" }
                    : isSchedule
                        ? new HashSet<string>(StringComparer.Ordinal) { "type", "everyMinutes", "times", "days" }
                    : isWebhook
                        ? new HashSet<string>(StringComparer.Ordinal) { "type", "auth", "publicKeys", "keyHash", "keyUntil", "allowedIps", "once", "onceInput", "inputs", "waitSec" }
                    : isOrderStatus
                        ? new HashSet<string>(StringComparer.Ordinal) { "type", "toStatuses", "productIds" }
                        : new HashSet<string>(StringComparer.Ordinal) { "type", "productIds", "paymentMethods", "skipPaymentMethods", "waitSec" };
                foreach (var p in to.Properties().Where(p => !knownTrigger.Contains(p.Name)))
                {
                    if (!isPayment && p.Name is "productIds" or "paymentMethods" or "skipPaymentMethods")
                        ctx.Error($"{path}.trigger.{p.Name}", "PAYMENT_ONLY", $"\"{p.Name}\" belongs to flows that start after a payment; {startsWhat}.");
                    else
                        ctx.Warning($"{path}.trigger.{p.Name}", "UNKNOWN_KEY", $"\"{p.Name}\" is not a setting of this trigger and is ignored.");
                }
            }
            if (!isPayment && s["paymentCheck"] != null)
                ctx.Error($"{path}.paymentCheck", "PAYMENT_ONLY", $"The amount check belongs to flows that start after a payment; {startsWhat}.");
            if (isPage && s["outputs"]?["orderReference"] != null)
                ctx.Error($"{path}.outputs.orderReference", "PAYMENT_ONLY", "orderReference belongs to flows that start after a payment. Tick \"The page can read this\" on the values the page should get back instead.");
            else if (!isPayment && s["outputs"]?["orderReference"] != null)
                ctx.Error($"{path}.outputs.orderReference", "PAYMENT_ONLY", $"orderReference belongs to flows that start after a payment; {startsWhat}.");
            var page = isPage ? ParsePageTrigger(trigger as JObject, $"{path}.trigger", ctx) : null;
            var schedule = isSchedule ? ParseScheduleTrigger(trigger as JObject, $"{path}.trigger", ctx) : null;
            var webhook = isWebhook ? ParseWebhookTrigger(trigger as JObject, $"{path}.trigger", ctx) : null;
            var orderStatus = isOrderStatus ? ParseOrderStatusTrigger(trigger as JObject, $"{path}.trigger", ctx) : null;
            CheckSourcesForTrigger(triggerType, ctx);

            var productIds = new List<long>();
            if (trigger is JObject t && t["productIds"] is { } ids)
            {
                if (ids is not JArray arr)
                    ctx.Error($"{path}.trigger.productIds", "BAD_PRODUCTS", "productIds must be an array of product ids.");
                else
                    foreach (var (id, i) in arr.Select((x, i) => (x, i)))
                    {
                        if (TryPositiveLong(id, out var pid)) productIds.Add(pid);
                        else ctx.Error($"{path}.trigger.productIds[{i}]", "BAD_PRODUCT", "A product id must be a positive whole number.");
                    }
            }

            // No gateway is assumed: a payment flow that names none takes a verified payment of any gateway.
            var methods = new List<string> { FlowSettings.AnyPaymentMethod };
            if (isPayment && (trigger as JObject)?["paymentMethods"] == null)
                ctx.Warning($"{path}.trigger.paymentMethods", "METHODS_DEFAULT", "No gateways are named, so a verified payment of any gateway starts the flow. Name the gateways to start it only for those.");
            if (trigger is JObject t2 && t2["paymentMethods"] is { } pm)
            {
                if (pm is not JArray arr || arr.Count == 0 || arr.Any(x => x.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)x)))
                    ctx.Error($"{path}.trigger.paymentMethods", "BAD_METHODS", "paymentMethods must be a non-empty array of the site's payment gateway ids, or [\"*\"] for any gateway.");
                else
                {
                    methods = arr.Select(x => ((string)x!).Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (methods.Contains(FlowSettings.AnyPaymentMethod) && methods.Count > 1)
                        ctx.Warning($"{path}.trigger.paymentMethods", "ANY_METHOD", "\"*\" already accepts every gateway; the other names add nothing.");
                }
            }

            // Payment methods that never start the flow (e.g. a bill another system settles itself).
            var skipMethods = new List<string>();
            if (isPayment && trigger is JObject t3 && t3["skipPaymentMethods"] is { } sm && sm.Type != JTokenType.Null)
            {
                if (sm is not JArray arr || arr.Any(x => x.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)x)))
                    ctx.Error($"{path}.trigger.skipPaymentMethods", "BAD_SKIP_METHODS", "skipPaymentMethods must be an array of payment method names, e.g. [\"SADAD\"].");
                else
                {
                    skipMethods = arr.Select(x => ((string)x!).Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (skipMethods.Contains(FlowSettings.AnyPaymentMethod))
                    {
                        ctx.Error($"{path}.trigger.skipPaymentMethods", "SKIP_ALL", "\"*\" would skip every payment, so the flow would never start. Name the payment methods to skip.");
                        skipMethods.Remove(FlowSettings.AnyPaymentMethod);
                    }
                    foreach (var both in skipMethods.Where(m => methods.Contains(m, StringComparer.OrdinalIgnoreCase)))
                        ctx.Error($"{path}.trigger.skipPaymentMethods", "SKIP_AND_START", $"\"{both}\" is both a payment that starts the flow (paymentMethods) and one it skips. Keep it in one list.");
                }
            }

            // How long the payment result page waits for the result ("Read the result").
            var paymentPageWait = FlowSettings.DefaultPaymentPageWaitSec;
            if (isPayment && (trigger as JObject)?["waitSec"] != null)
                paymentPageWait = ParseWaitSec(trigger as JObject, $"{path}.trigger", "the payment page shows what there is at once", ctx);

            FlowOutputRef? amountFrom = null;
            var compareTo = "paidAmount";
            var tolerance = 0.01m;
            if (s["paymentCheck"] is { } pc)
            {
                if (pc is not JObject pco)
                    ctx.Error($"{path}.paymentCheck", "BAD_PAYMENT_CHECK", "paymentCheck must be an object { amountFrom, compareTo, tolerance }.");
                else
                {
                    amountFrom = ParseOutputRef(pco["amountFrom"], $"{path}.paymentCheck.amountFrom", ctx, required: true);
                    if (pco["compareTo"] is { } ct)
                    {
                        compareTo = (string?)ct ?? "";
                        if (compareTo != "paidAmount" && compareTo != "orderTotal")
                            ctx.Error($"{path}.paymentCheck.compareTo", "BAD_COMPARE_TO", "compareTo must be \"paidAmount\" or \"orderTotal\".");
                    }
                    if (pco["tolerance"] is { } tol)
                    {
                        if (tol.Type is not (JTokenType.Integer or JTokenType.Float) || (decimal)tol < 0)
                            ctx.Error($"{path}.paymentCheck.tolerance", "BAD_TOLERANCE", "tolerance must be a number of at least 0.");
                        else
                            tolerance = (decimal)tol;
                    }
                }
            }

            FlowOutputRef? reference = null;
            if (s["outputs"] is { } outs)
            {
                if (outs is not JObject oo)
                    ctx.Error($"{path}.outputs", "BAD_OUTPUTS", "settings.outputs must be an object { \"orderReference\": \"request.output\" }.");
                else
                {
                    reference = oo["orderReference"] is { } r ? ParseOutputRef(r, $"{path}.outputs.orderReference", ctx, required: true) : null;
                    foreach (var p in oo.Properties().Where(p => p.Name != "orderReference"))
                        ctx.Warning($"{path}.outputs.{p.Name}", "UNKNOWN_KEY", $"\"{p.Name}\" is not used; settings.outputs only has \"orderReference\".");
                }
            }

            var emails = new List<string>();
            string? attentionTemplate = null;
            if (s["notify"] is { } notify)
            {
                var onAttention = (notify as JObject)?["onAttention"] as JObject;
                var list = onAttention?["emails"];
                if (onAttention?["template"] is { } tpl && tpl.Type != JTokenType.Null)
                {
                    attentionTemplate = tpl.Type == JTokenType.String ? ((string?)tpl)?.Trim() : null;
                    if (string.IsNullOrEmpty(attentionTemplate) || attentionTemplate.Length > 450)
                    {
                        ctx.Error($"{path}.notify.onAttention.template", "BAD_TEMPLATE", "template is the id of one of the site's e-mail templates (Email templates); leave it out for the built-in text.");
                        attentionTemplate = null;
                    }
                }
                foreach (var extra in onAttention?.Properties().Where(x => x.Name is not ("emails" or "template")) ?? Enumerable.Empty<JProperty>())
                    ctx.Warning($"{path}.notify.onAttention.{extra.Name}", "UNKNOWN_KEY", $"\"{extra.Name}\" is not used; onAttention has \"emails\" and \"template\".");
                if (notify is not JObject || (list != null && list is not JArray))
                    ctx.Error($"{path}.notify", "BAD_NOTIFY", "notify must look like { \"onAttention\": { \"emails\": [\"ops@example.com\"], \"template\": \"...\" } }.");
                else if (list is JArray ea)
                    foreach (var (e, i) in ea.Select((x, i) => (x, i)))
                    {
                        var address = e.Type == JTokenType.String ? ((string?)e)?.Trim() : null;
                        if (address != null && MailAddress.TryCreate(address, out var parsed) && parsed.Address == address)
                            emails.Add(address);
                        else
                            ctx.Error($"{path}.notify.onAttention.emails[{i}]", "BAD_EMAIL", "Not a valid e-mail address.");
                    }
            }

            var zoneId = (string?)s["timeZone"] ?? defaultTimeZone;
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var zone))
            {
                ctx.Error($"{path}.timeZone", "BAD_TIME_ZONE", $"Unknown time zone \"{zoneId}\" (use an IANA name such as \"Asia/Riyadh\").");
                zone = TimeZoneInfo.Utc;
            }

            var (hidden, keepDays) = ParsePrivacy(s["privacy"], $"{path}.privacy", ctx);

            return new FlowSettings
            {
                Trigger = triggerType ?? FlowSettings.OrderPaidTrigger,
                Page = page,
                Schedule = schedule,
                Webhook = webhook,
                OrderStatus = orderStatus,
                ProductIds = productIds,
                PaymentMethods = methods,
                SkipPaymentMethods = skipMethods,
                PaymentAmountFrom = amountFrom,
                PaymentCompareTo = compareTo,
                PaymentTolerance = tolerance,
                ReferenceFrom = reference,
                AttentionEmails = emails,
                AttentionTemplateId = attentionTemplate,
                PaymentPageWaitSec = paymentPageWait,
                TimeZoneId = zoneId,
                TimeZone = zone,
                HiddenKeys = hidden,
                KeepRunsDays = keepDays,
            };
        }

        private static readonly Regex HiddenNamePattern = new(@"^[A-Za-z_][A-Za-z0-9_\-]{0,99}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>privacy: { "hide": ["nationalId", "Mobile"], "keepRunsDays": 90 }.</summary>
        private static (IReadOnlyList<string> Hidden, int? KeepDays) ParsePrivacy(JToken? token, string path, ParseContext ctx)
        {
            var hidden = new List<string>();
            int? keepDays = null;
            if (token == null || token.Type == JTokenType.Null)
                return (hidden, keepDays);
            if (token is not JObject p)
            {
                ctx.Error(path, "BAD_PRIVACY", "privacy must look like { \"hide\": [\"nationalId\"], \"keepRunsDays\": 90 }.");
                return (hidden, keepDays);
            }
            foreach (var extra in p.Properties().Where(x => x.Name is not ("hide" or "keepRunsDays")))
                ctx.Warning($"{path}.{extra.Name}", "UNKNOWN_KEY", $"\"{extra.Name}\" is not a privacy setting and is ignored.");

            if (p["hide"] is { } h)
            {
                if (h is not JArray arr)
                    ctx.Error($"{path}.hide", "BAD_HIDE", "hide must be a list of value names, e.g. [\"nationalId\", \"Mobile\"].");
                else
                    foreach (var (x, i) in arr.Select((x, i) => (x, i)))
                    {
                        var name = x.Type == JTokenType.String ? ((string?)x)?.Trim() : null;
                        if (name != null && HiddenNamePattern.IsMatch(name))
                        {
                            if (!hidden.Contains(name, StringComparer.OrdinalIgnoreCase))
                                hidden.Add(name);
                        }
                        else
                            ctx.Error($"{path}.hide[{i}]", "BAD_HIDE", "A value name is letters, digits, _ and -, as it appears in the data (e.g. \"nationalId\"), not a path.");
                    }
            }
            if (p["keepRunsDays"] is { } kd && kd.Type != JTokenType.Null)
            {
                if (TryIntInRange(kd, 1, 3650, out var d))
                    keepDays = d;
                else
                    ctx.Error($"{path}.keepRunsDays", "BAD_KEEP_DAYS", "keepRunsDays must be a whole number of days from 1 to 3650; leave it out to keep runs as long as the server's default.");
            }
            return (hidden, keepDays);
        }

        private static readonly HashSet<string> InputTypes = new(StringComparer.Ordinal) { "any", "text", "number", "boolean" };

        /// <summary>
        /// The page trigger. The inputs it accepts are the ones the flow reads "From the page"
        /// (fromInput mappings and conditions) plus any declared under inputs, which add rules.
        /// </summary>
        private static PageTriggerSettings ParsePageTrigger(JObject? t, string path, ParseContext ctx)
        {
            var who = (string?)t?["who"] ?? PageTriggerSettings.WhoSignedIn;
            if (who is not (PageTriggerSettings.WhoSignedIn or PageTriggerSettings.WhoAnyone))
            {
                ctx.Error($"{path}.who", "BAD_WHO", "who must be \"signedIn\" (signed-in customers) or \"anyone\" (also visitors who are not signed in).");
                who = PageTriggerSettings.WhoSignedIn;
            }

            var once = (string?)t?["once"] ?? PageTriggerSettings.OncePerCart;
            if (once is not (PageTriggerSettings.OnceAlways or PageTriggerSettings.OncePerCart or PageTriggerSettings.OncePerUser or PageTriggerSettings.OncePerInput))
            {
                ctx.Error($"{path}.once", "BAD_ONCE", "once must be \"always\" (every time), \"cart\" (once per journey), \"user\" (once per customer) or \"input\" (once per value of onceInput).");
                once = PageTriggerSettings.OncePerCart;
            }

            var afterFailure = (string?)t?["afterFailure"] ?? PageTriggerSettings.AfterFailureKeep;
            if (afterFailure is not (PageTriggerSettings.AfterFailureKeep or PageTriggerSettings.AfterFailureNewRun))
            {
                ctx.Error($"{path}.afterFailure", "BAD_AFTER_FAILURE", "afterFailure must be \"keep\" (the page is told the run failed; a person handles it in the Admin) or \"newRun\" (the next time the page asks, a new run starts).");
                afterFailure = PageTriggerSettings.AfterFailureKeep;
            }
            else if (afterFailure == PageTriggerSettings.AfterFailureNewRun && once == PageTriggerSettings.OnceAlways)
                ctx.Warning($"{path}.afterFailure", "AFTER_FAILURE_UNUSED", "Every ask starts a new run already, so \"after a failure\" changes nothing.");

            var inputs = ParseInputs(t, path, ctx);

            var onceInput = ((string?)t?["onceInput"])?.Trim();
            if (once == PageTriggerSettings.OncePerInput)
            {
                if (string.IsNullOrEmpty(onceInput) || !inputs.ContainsKey(onceInput))
                    ctx.Error($"{path}.onceInput", "BAD_ONCE_INPUT", "\"Once per value of an input\" needs onceInput: the name of an input the flow reads from the page.");
                else if (!inputs[onceInput].Required)
                    inputs[onceInput] = new FlowInputRule { Required = true, Type = inputs[onceInput].Type, MaxLength = inputs[onceInput].MaxLength };
            }

            var waitSec = ParseWaitSec(t, path, "carry on at once", ctx);

            var cartType = 1;
            if (t?["cartType"] is { } cartTypeToken && !TryIntInRange(cartTypeToken, 1, 3, out cartType))
            {
                ctx.Error($"{path}.cartType", "BAD_CART_TYPE", "cartType must be 1 (shop), 2 (plan) or 3 (custom).");
                cartType = 1;
            }

            return new PageTriggerSettings
            {
                Who = who,
                Once = once,
                OnceInput = once == PageTriggerSettings.OncePerInput ? onceInput : null,
                AfterFailure = afterFailure,
                Inputs = inputs,
                WaitSec = waitSec,
                CartType = cartType,
            };
        }

        /// <summary>
        /// The inputs a page or another system may send: every name the flow reads "From the page"
        /// (fromInput mappings and conditions), then the rules declared under inputs.
        /// </summary>
        private static Dictionary<string, FlowInputRule> ParseInputs(JObject? t, string path, ParseContext ctx)
        {
            var inputs = new Dictionary<string, FlowInputRule>(StringComparer.Ordinal);
            foreach (var used in ctx.InputUses.Select(u => u.Name).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal))
                inputs[used] = new FlowInputRule();
            if (t?["inputs"] is not { } declared)
                return inputs;
            if (declared is not JObject io)
            {
                ctx.Error($"{path}.inputs", "BAD_INPUTS", "inputs must be an object: { \"claimNo\": { \"required\": true, \"type\": \"text\", \"maxLength\": 50 } }.");
                return inputs;
            }
            foreach (var p in io.Properties())
            {
                var ip = $"{path}.inputs.{p.Name}";
                if (!NamePattern.IsMatch(p.Name))
                {
                    ctx.Error(ip, "BAD_INPUT_NAME", "An input name must start with a letter and use only letters, digits and _.");
                    continue;
                }
                var rule = p.Value as JObject ?? new JObject();
                var type = (string?)rule["type"] ?? "any";
                if (!InputTypes.Contains(type))
                {
                    ctx.Error($"{ip}.type", "BAD_INPUT_TYPE", "type must be \"any\", \"text\", \"number\" or \"boolean\".");
                    type = "any";
                }
                int? maxLength = null;
                if (rule["maxLength"] is { } ml)
                {
                    if (TryIntInRange(ml, 1, 100_000, out var m)) maxLength = m;
                    else ctx.Error($"{ip}.maxLength", "BAD_MAX_LENGTH", "maxLength must be a whole number from 1 to 100000.");
                }
                if (!inputs.ContainsKey(p.Name))
                    ctx.Warning(ip, "INPUT_UNUSED", $"No call reads \"{p.Name}\" from the page; it is accepted but nothing uses it.");
                inputs[p.Name] = new FlowInputRule { Required = IsTrue(rule["required"]), Type = type, MaxLength = maxLength };
            }
            return inputs;
        }

        /// <summary>waitSec: how long the caller (a page or another system) may wait for the result in all.</summary>
        private static int ParseWaitSec(JObject? t, string path, string zeroMeans, ParseContext ctx)
        {
            if (t?["waitSec"] is not { } ws)
                return 0;
            if (TryIntInRange(ws, 0, FlowSettings.MaxWaitSec, out var waitSec))
                return waitSec;
            ctx.Error($"{path}.waitSec", "BAD_WAIT", $"waitSec must be a whole number of seconds from 0 ({zeroMeans}) to {FlowSettings.MaxWaitSec}.");
            return 0;
        }

        private static readonly Regex KeyHashPattern = new("^[0-9a-f]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// The webhook: how the other system proves a call is its own (signed requests with its public
        /// keys, or a key in a header), the addresses it calls from, what it may send, and whether a
        /// value identifies the job.
        /// </summary>
        private static WebhookSettings ParseWebhookTrigger(JObject? t, string path, ParseContext ctx)
        {
            var auth = ((string?)t?["auth"])?.Trim() ?? WebhookAuth.KeyMode;
            if (auth is not (WebhookAuth.KeyMode or WebhookAuth.SignatureMode))
            {
                ctx.Error($"{path}.auth", "BAD_AUTH", "auth must be \"signature\" (the other system signs each request with its private key) or \"key\" (it sends a key in a header).");
                auth = WebhookAuth.KeyMode;
            }
            var signed = auth == WebhookAuth.SignatureMode;

            var keyHash = ((string?)t?["keyHash"])?.Trim().ToLowerInvariant() ?? "";
            if (!signed && !KeyHashPattern.IsMatch(keyHash))
                ctx.Error($"{path}.keyHash", "NO_KEY", "The webhook has no key yet. In \"When it runs\", press \"Make a new key\" (the server keeps only its hash).");
            else if (signed && keyHash.Length > 0 && !KeyHashPattern.IsMatch(keyHash))
                ctx.Error($"{path}.keyHash", "BAD_KEY", "keyHash is not a key the Admin made (64 hexadecimal digits). Remove it, or make a new key.");

            var publicKeys = ParsePublicKeys(t?["publicKeys"], $"{path}.publicKeys", ctx);
            if (signed && publicKeys.Count == 0 && t?["publicKeys"] is null or { Type: JTokenType.Null } or JArray { Count: 0 })
                ctx.Error($"{path}.publicKeys", "NO_PUBLIC_KEY", "Signed requests need the other system's public key. In \"When it runs\", make a key pair for them or add their public key.");
            else if (!signed && publicKeys.Count > 0)
                ctx.Warning($"{path}.publicKeys", "PUBLIC_KEYS_UNUSED", "These public keys are not used while the other system sends a key in the header. Choose signed requests to use them.");

            DateOnly? keyUntil = null;
            if (t?["keyUntil"] is { Type: not JTokenType.Null } until)
            {
                if (until.Type == JTokenType.String && DateOnly.TryParseExact(((string?)until)?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                    keyUntil = day;
                else
                    ctx.Error($"{path}.keyUntil", "BAD_DATE", "keyUntil is the last day the old key is still taken, written yyyy-MM-dd (e.g. 2026-11-30).");
            }
            if (keyUntil != null && !signed)
                ctx.Warning($"{path}.keyUntil", "KEY_UNTIL_UNUSED", "keyUntil only matters for signed requests: it is the last day the old key is still taken while the other system moves over.");
            else if (keyUntil != null && keyHash.Length == 0)
                ctx.Warning($"{path}.keyUntil", "KEY_UNTIL_UNUSED", "There is no old key, so keyUntil changes nothing.");
            else if (signed && keyHash.Length > 0 && keyUntil == null)
                ctx.Warning($"{path}.keyHash", "OLD_KEY_OFF", "The old key is no longer taken: only signed requests are. Remove the old key, or set the last day it is still taken (keyUntil).");

            var allowed = ParseAddressRules(t?["allowedIps"], $"{path}.allowedIps", ctx);

            var inputs = ParseInputs(t, path, ctx);

            var once = (string?)t?["once"] ?? WebhookSettings.OnceAlways;
            if (once is not (WebhookSettings.OnceAlways or WebhookSettings.OncePerInput))
            {
                ctx.Error($"{path}.once", "BAD_ONCE", "once must be \"always\" (a new run each call) or \"input\" (one run per value of onceInput).");
                once = WebhookSettings.OnceAlways;
            }
            var onceInput = ((string?)t?["onceInput"])?.Trim();
            if (once == WebhookSettings.OncePerInput)
            {
                if (string.IsNullOrEmpty(onceInput) || !inputs.ContainsKey(onceInput))
                    ctx.Error($"{path}.onceInput", "BAD_ONCE_INPUT", "\"Once per value\" needs onceInput: the name of a value the flow reads from the call (e.g. its own reference).");
                else if (!inputs[onceInput].Required)
                    inputs[onceInput] = new FlowInputRule { Required = true, Type = inputs[onceInput].Type, MaxLength = inputs[onceInput].MaxLength };
            }

            var waitSec = ParseWaitSec(t, path, "answer at once", ctx);

            return new WebhookSettings
            {
                Auth = auth,
                PublicKeys = publicKeys,
                KeyUntil = keyUntil,
                AllowedAddresses = allowed,
                KeyHash = keyHash,
                Once = once,
                OnceInput = once == WebhookSettings.OncePerInput ? onceInput : null,
                Inputs = inputs,
                WaitSec = waitSec,
            };
        }

        /// <summary>
        /// A flow another system starts with a key in a header, that changes something (a write, a
        /// DCMS operation): anyone who gets the key can make it do so, for as long as the key lasts.
        /// Signed requests cannot be copied or changed.
        /// </summary>
        private static void CheckWebhookStrength(FlowSettings settings, IReadOnlyList<FlowGroup> groups, ParseContext ctx)
        {
            if (settings.Webhook is not { Signed: false })
                return;
            var writes = groups.SelectMany(g => g.Requests).Where(r => r.Class != FlowRequestClass.Read).Select(r => r.Name).ToList();
            if (writes.Count > 0)
                ctx.Warning("settings.trigger.auth", "KEY_FOR_WRITES",
                    $"Anyone who gets this flow's key can make it change data ({string.Join(", ", writes.Select(w => $"\"{w}\""))}) until a new key is made. " +
                    "Choose signed requests: the other system signs each request with its private key, nothing secret is sent, and a request cannot be changed or used again later.");
        }

        /// <summary>
        /// publicKeys: [{ "name": "Finance production", "key": "MFkwEwYH...", "addedOn": "2026-10-22" }]:
        /// the keys that may sign the other system's requests (at most three: the one in use and its
        /// replacement while it is rolled out). A key may be written as the Admin writes it (base64 of
        /// the DER key) or as PEM / a certificate / a JWK.
        /// </summary>
        private static IReadOnlyList<WebhookPublicKey> ParsePublicKeys(JToken? token, string path, ParseContext ctx)
        {
            var keys = new List<WebhookPublicKey>();
            if (token == null || token.Type == JTokenType.Null)
                return keys;
            if (token is not JArray list)
            {
                ctx.Error(path, "BAD_PUBLIC_KEYS", "publicKeys must be a list: [{ \"name\": \"Finance production\", \"key\": \"MFkwEwYH...\" }].");
                return keys;
            }
            if (list.Count > WebhookAuth.MaxPublicKeys)
                ctx.Error(path, "TOO_MANY_KEYS", $"At most {WebhookAuth.MaxPublicKeys} public keys: the one in use and its replacement. Remove the ones no longer used.");
            foreach (var (item, i) in list.Select((x, i) => (x, i)))
            {
                var ip = $"{path}[{i}]";
                var entry = item as JObject;
                var text = item.Type == JTokenType.String ? (string?)item : entry?["key"]?.Type == JTokenType.String ? (string?)entry["key"] : null;
                var parsed = WebhookAuth.ParsePublicKey(text, out var problem);
                if (parsed == null)
                {
                    ctx.Error($"{ip}.key", "BAD_PUBLIC_KEY", problem ?? "This is not a public key.");
                    continue;
                }
                if (keys.Any(k => k.Fingerprint == parsed.Fingerprint))
                {
                    ctx.Error(ip, "DUPLICATE_PUBLIC_KEY", $"The key {WebhookAuth.Short(parsed.Fingerprint)} is listed twice.");
                    continue;
                }
                var name = ((string?)entry?["name"])?.Trim() ?? "";
                if (name.Length > 60)
                    ctx.Error($"{ip}.name", "BAD_KEY_NAME", "A key's name is at most 60 characters.");
                var addedOn = ((string?)entry?["addedOn"])?.Trim();
                foreach (var extra in entry?.Properties().Where(p => p.Name is not ("name" or "key" or "addedOn")) ?? Enumerable.Empty<JProperty>())
                    ctx.Warning($"{ip}.{extra.Name}", "UNKNOWN_KEY", $"\"{extra.Name}\" is not used; a public key has \"name\", \"key\" and \"addedOn\".");
                keys.Add(new WebhookPublicKey { Name = name, Spki = parsed.Spki, Type = parsed.Type, Fingerprint = parsed.Fingerprint, AddedOn = addedOn });
            }
            return keys.Take(WebhookAuth.MaxPublicKeys).ToList();
        }

        /// <summary>allowedIps: ["10.20.30.40", "10.20.31.0/24"]: the only addresses calls are taken from (empty: any).</summary>
        private static IReadOnlyList<AddressRule> ParseAddressRules(JToken? token, string path, ParseContext ctx)
        {
            var rules = new List<AddressRule>();
            if (token == null || token.Type == JTokenType.Null)
                return rules;
            if (token is not JArray list || list.Any(x => x.Type != JTokenType.String))
            {
                ctx.Error(path, "BAD_ADDRESSES", "allowedIps must be a list of addresses or ranges, e.g. [\"10.20.30.40\", \"10.20.31.0/24\"].");
                return rules;
            }
            if (list.Count > WebhookAuth.MaxAddressRules)
                ctx.Error(path, "TOO_MANY_ADDRESSES", $"At most {WebhookAuth.MaxAddressRules} addresses or ranges.");
            foreach (var (item, i) in list.Select((x, i) => (x, i)))
            {
                if (!WebhookAuth.TryParseAddressRule((string?)item, out var rule, out var problem))
                    ctx.Error($"{path}[{i}]", "BAD_ADDRESS", problem ?? "Not an address.");
                else if (!rules.Any(r => r.Text == rule.Text))
                {
                    rules.Add(rule);
                    if (IPAddress.IsLoopback(rule.Network.BaseAddress))
                        ctx.Warning($"{path}[{i}]", "ADDRESS_IS_SERVER",
                            $"{rule.Text} is the server itself. Another system's calls come from it only when the proxy in front of the server does not pass on the caller's address; then every caller looks the same, and this lets them all in.");
                }
            }
            return rules;
        }

        /// <summary>
        /// An order's status changes: the statuses it moves to that start the flow (DCMS's status names;
        /// the server checks them, as it knows them). The products are productIds, as after a payment.
        /// </summary>
        private static OrderStatusSettings ParseOrderStatusTrigger(JObject? t, string path, ParseContext ctx)
        {
            var statuses = new List<string>();
            if (t?["toStatuses"] is { } ts)
            {
                if (ts is not JArray arr || arr.Any(x => x.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)x)))
                    ctx.Error($"{path}.toStatuses", "BAD_STATUSES", "toStatuses must be a list of order status names, e.g. [\"Shipping\", \"Complete\"].");
                else
                    statuses = arr.Select(x => ((string)x!).Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            if (statuses.Count == 0)
                ctx.Error($"{path}.toStatuses", "NO_STATUSES", "Choose the statuses an order moves to that start the flow.");
            return new OrderStatusSettings { ToStatuses = statuses };
        }

        /// <summary>
        /// Only a schedule trigger and its time zone, for showing the next times while the rest of
        /// the flow is still being written (the full check needs the whole flow).
        /// </summary>
        public static (ScheduleSettings? Schedule, TimeZoneInfo Zone, IReadOnlyList<FlowIssue> Issues) ParseSchedule(JToken? trigger, string? timeZone, string defaultTimeZone)
        {
            var issues = new List<FlowIssue>();
            var ctx = new ParseContext(issues);
            var zoneId = string.IsNullOrWhiteSpace(timeZone) ? defaultTimeZone : timeZone.Trim();
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var zone))
            {
                ctx.Error("settings.timeZone", "BAD_TIME_ZONE", $"Unknown time zone \"{zoneId}\" (use an IANA name such as \"Asia/Riyadh\").");
                zone = TimeZoneInfo.Utc;
            }
            var schedule = ParseScheduleTrigger(trigger as JObject, "settings.trigger", ctx);
            return (issues.Any(i => i.Severity == FlowIssueSeverity.Error) ? null : schedule, zone, issues);
        }

        private static readonly Dictionary<string, DayOfWeek> DayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sun"] = DayOfWeek.Sunday, ["mon"] = DayOfWeek.Monday, ["tue"] = DayOfWeek.Tuesday, ["wed"] = DayOfWeek.Wednesday,
            ["thu"] = DayOfWeek.Thursday, ["fri"] = DayOfWeek.Friday, ["sat"] = DayOfWeek.Saturday,
        };

        /// <summary>The schedule: everyMinutes (5-1440) or times ["HH:mm", ...], on days ["sun", ...] (none = every day).</summary>
        private static ScheduleSettings ParseScheduleTrigger(JObject? t, string path, ParseContext ctx)
        {
            int? every = null;
            if (t?["everyMinutes"] is { } em)
            {
                if (TryIntInRange(em, 5, 1440, out var m))
                {
                    every = m;
                    if (1440 % m != 0)
                        ctx.Warning($"{path}.everyMinutes", "UNEVEN_EVERY", $"Every {m} minutes does not divide a day evenly: the count starts again at midnight, so the last gap of the day is shorter.");
                }
                else
                    ctx.Error($"{path}.everyMinutes", "BAD_EVERY", "everyMinutes must be a whole number from 5 to 1440 (once a day).");
            }

            var times = new SortedSet<int>();
            if (t?["times"] is { } tt)
            {
                if (tt is not JArray arr || arr.Count == 0)
                    ctx.Error($"{path}.times", "BAD_TIMES", "times must be a list of times of day, e.g. [\"08:00\", \"20:30\"].");
                else if (arr.Count > 48)
                    ctx.Error($"{path}.times", "TOO_MANY_TIMES", "At most 48 times a day; use everyMinutes for more.");
                else
                    foreach (var (x, i) in arr.Select((x, i) => (x, i)))
                    {
                        if (TryTimeOfDay(x, out var minute)) times.Add(minute);
                        else ctx.Error($"{path}.times[{i}]", "BAD_TIME", "Write a time as HH:mm, from 00:00 to 23:59.");
                    }
            }

            if (t?["everyMinutes"] == null && t?["times"] == null)
                ctx.Error(path, "NO_SCHEDULE", "Say when it runs: everyMinutes (e.g. 60) or times (e.g. [\"08:00\"]).");
            else if (t?["everyMinutes"] != null && t?["times"] != null)
                ctx.Error(path, "TWO_SCHEDULES", "Use everyMinutes or times, not both.");

            var days = new HashSet<DayOfWeek>();
            if (t?["days"] is { } dd)
            {
                if (dd is not JArray darr)
                    ctx.Error($"{path}.days", "BAD_DAYS", "days must be a list such as [\"sun\", \"mon\"]; leave it out for every day.");
                else
                    foreach (var (d, i) in darr.Select((d, i) => (d, i)))
                    {
                        if (d.Type == JTokenType.String && DayNames.TryGetValue(((string)d!).Trim(), out var day)) days.Add(day);
                        else ctx.Error($"{path}.days[{i}]", "BAD_DAY", "A day is one of \"sun\", \"mon\", \"tue\", \"wed\", \"thu\", \"fri\", \"sat\".");
                    }
                if (dd is JArray { Count: 0 })
                    ctx.Error($"{path}.days", "NO_DAYS", "No day is chosen, so it would never run. Leave days out for every day.");
            }

            return new ScheduleSettings
            {
                EveryMinutes = every,
                Times = every == null ? times.ToList() : Array.Empty<int>(),
                Days = days.Count is 0 or 7 ? Array.Empty<DayOfWeek>() : days.OrderBy(d => d).ToList(),
            };
        }

        private static bool TryTimeOfDay(JToken x, out int minute)
        {
            minute = 0;
            var text = x.Type == JTokenType.String ? ((string?)x)?.Trim() : null;
            if (text == null)
                return false;
            var parts = text.Split(':');
            if (parts.Length != 2 || parts[0].Length is < 1 or > 2 || parts[1].Length != 2
                || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var h)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m)
                || h > 23 || m > 59)
                return false;
            minute = h * 60 + m;
            return true;
        }

        /// <summary>
        /// What each start has to read from: a page has no order; an order (paid, or its status
        /// changed) has no page input; a schedule has none of them; a webhook only what it sends.
        /// </summary>
        private static void CheckSourcesForTrigger(string? trigger, ParseContext ctx)
        {
            void NoOrder(string why) { foreach (var use in ctx.OrderUses) ctx.Error(use, "NO_ORDER", why); }
            void NoInput(string why) { foreach (var use in ctx.InputUses) ctx.Error(use.Path, "NO_PAGE_INPUT", why); }
            void NoCart(string why) { foreach (var use in ctx.CartUses) ctx.Error(use, "NO_CART", why); }
            void NoUser(string why) { foreach (var use in ctx.UserUses) ctx.Error(use, "NO_USER", why); }

            switch (trigger)
            {
                case FlowSettings.PageTrigger:
                    NoOrder("There is no order when a page starts the flow. Read the value from the cart, from the page (\"From the page\") or from the signed-in user.");
                    foreach (var use in ctx.AccountInputUses)
                        ctx.Error(use, "ACCOUNT_FROM_PAGE", "A visitor must not choose whose account this changes: read the account from the signed-in user (Logged In User, Current Username, User Option), not \"From the page\".");
                    foreach (var use in ctx.OrderOperationUses)
                        ctx.Error(use, "ORDER_FROM_PAGE", "A page must not change an order's status: any visitor can start a page's flow. Let the system that knows (another system calling the flow), a payment, an order's status or a set time start it.");
                    break;
                case FlowSettings.ScheduleTrigger:
                    const string read = "Use fixed values, today's date and what earlier calls answered (Previous Results).";
                    NoOrder($"There is no order when the flow runs on a schedule. {read}");
                    NoInput($"Nothing is sent by a page when the flow runs on a schedule. {read}");
                    NoCart($"There is no cart when the flow runs on a schedule. {read}");
                    NoUser($"There is no signed-in user when the flow runs on a schedule. {read}");
                    break;
                case FlowSettings.WebhookTrigger:
                    const string sent = "Read what the other system sends (\"From the page\"), fixed values, today's date and what earlier calls answered.";
                    NoOrder($"There is no order when another system starts the flow. {sent}");
                    NoCart($"There is no cart when another system starts the flow. {sent}");
                    NoUser($"There is no signed-in user when another system starts the flow. {sent}");
                    break;
                default:
                    // after a payment, and an order's status: the order, its cart and its customer
                    NoInput("Only a flow that a page or another system starts has sent values. For an order, read the value from the order or the cart.");
                    break;
            }
        }

        private static FlowOutputRef? ParseOutputRef(JToken? token, string path, ParseContext ctx, bool required)
        {
            var text = token?.Type == JTokenType.String ? ((string?)token)?.Trim() : null;
            if (string.IsNullOrEmpty(text))
            {
                if (required)
                    ctx.Error(path, "BAD_OUTPUT_REF", "Write it as \"requestName.outputName\".");
                return null;
            }

            var parts = text.Split('.');
            if (parts.Length != 2 || !NamePattern.IsMatch(parts[0]) || !NamePattern.IsMatch(parts[1]))
            {
                ctx.Error(path, "BAD_OUTPUT_REF", $"\"{text}\" must be \"requestName.outputName\" (one dot).");
                return null;
            }

            return new FlowOutputRef(parts[0], parts[1]);
        }

        private static void CheckSettingsReferences(FlowSettings settings, IReadOnlyList<FlowGroup> groups, ParseContext ctx)
        {
            void Check(FlowOutputRef? r, string path)
            {
                if (r == null)
                    return;
                var request = groups.SelectMany(g => g.Requests).FirstOrDefault(x => x.Name == r.Request);
                if (request == null)
                    ctx.Error(path, "UNKNOWN_REQUEST", $"There is no request named \"{r.Request}\".");
                else if (!request.Outputs.ContainsKey(r.Output))
                    ctx.Error(path, "UNKNOWN_OUTPUT", $"Request \"{r.Request}\" has no output \"{r.Output}\" (declare it in its server.outputs).");
            }

            Check(settings.PaymentAmountFrom, "settings.paymentCheck.amountFrom");
            Check(settings.ReferenceFrom, "settings.outputs.orderReference");

            // The reference is one value; a call that loops (or keeps every polling answer) has a list of them.
            if (settings.ReferenceFrom is { } referenceFrom && groups.SelectMany(g => g.Requests).FirstOrDefault(x => x.Name == referenceFrom.Request) is { } referenced && AnswersAList(referenced))
                ctx.Error("settings.outputs.orderReference", "REFERENCE_NOT_ONE",
                    $"\"{referenced.Name}\" answers a list (one answer per item, or every polling answer), so it has no single \"{referenceFrom.Output}\" to show as the reference. Take it from a call that answers once.");
            foreach (var listed in groups.SelectMany(g => g.Requests).Where(r => AnswersAList(r) && r.Outputs.Values.Any(o => o.Public)))
                ctx.Warning("apiGroupList", "PUBLIC_FROM_LIST",
                    $"\"{listed.Name}\" answers a list (one answer per item, or every polling answer), so the page cannot read its values by name. Give the page values from a call that answers once.");

            // What the page can read back: one value per name.
            var results = groups.SelectMany(g => g.Requests)
                .SelectMany(r => r.Outputs.Values.Where(o => o.Public).Select(o => (Request: r.Name, Output: o.Name)))
                .ToList();
            foreach (var clash in results.GroupBy(x => x.Output, StringComparer.Ordinal).Where(x => x.Count() > 1))
                ctx.Error("apiGroupList", "DUPLICATE_RESULT",
                    $"\"{clash.Key}\" is given to the page by {string.Join(" and ", clash.Select(x => $"\"{x.Request}\""))}; the page reads results by name, so each name may come from one call only.");
            var startedBy = settings.Trigger switch
            {
                FlowSettings.ScheduleTrigger => "runs on a schedule",
                FlowSettings.OrderStatusTrigger => "starts when an order's status changes",
                _ => "starts after a payment",
            };
            // A page, a webhook's caller and a payment page get the results and the message; nobody else does.
            if (results.Count > 0 && !settings.AnswersACaller)
                ctx.Warning("apiGroupList", "RESULTS_UNUSED", $"\"The page can read this\" is only used by flows a page, another system or a payment page reads; this one {startedBy}.");
            if (!settings.AnswersACaller)
                foreach (var r in groups.SelectMany(g => g.Requests).Where(r => r.RefusedMessages.Count > 0 || r.ErrorMessage != null))
                    ctx.Warning("apiGroupList", "MESSAGE_UNUSED", $"\"{r.Name}\" has an error message, but nobody reads this flow's answer (it {startedBy}); the run page shows what the other side answered.");

            // A new run after a failure sends every call again, including the ones the failed run
            // had already sent: only calls that are safe to send twice may be in such a flow.
            if (settings.Page is { AfterFailure: PageTriggerSettings.AfterFailureNewRun, Once: not PageTriggerSettings.OnceAlways })
                foreach (var r in groups.SelectMany(g => g.Requests).Where(r => r.Class == FlowRequestClass.NonIdempotent))
                    ctx.Error("settings.trigger.afterFailure", "AFTER_FAILURE_NOT_SAFE",
                        $"\"{r.Name}\" must never be sent twice, and a new run would send it again. Keep \"Tell the page it failed\" and handle failed runs in the Admin.");

            // The amount check runs right after its request. If that request may be skipped, or a
            // call that changes something runs before it, the check protects nothing.
            if (settings.PaymentAmountFrom is { } amount)
            {
                const string ap = "settings.paymentCheck.amountFrom";
                var group = groups.FirstOrDefault(g => g.Requests.Any(r => r.Name == amount.Request));
                var request = group?.Requests.First(r => r.Name == amount.Request);
                if (request != null)
                {
                    if (AnswersAList(request))
                        ctx.Error(ap, "AMOUNT_LOOP", $"\"{request.Name}\" gives the amount to check, so it must answer once, not once per item of a list (or with every polling answer).");
                    if (!request.Critical)
                        ctx.Error(ap, "AMOUNT_NOT_CRITICAL", $"\"{request.Name}\" gives the amount to check, so it must stop the run when it fails (remove \"Keep going on error\" / server.critical:false).");
                    if (request.ExecuteIf != null || group!.ExecuteIf != null)
                        ctx.Error(ap, "AMOUNT_CONDITIONAL", $"\"{request.Name}\" gives the amount to check, so it must always run (remove its condition and its group's).");
                    var before = groups.SelectMany(g => g.Requests).TakeWhile(r => r.Name != request.Name)
                        .FirstOrDefault(r => r.Class != FlowRequestClass.Read);
                    if (before != null)
                        ctx.Error(ap, "AMOUNT_TOO_LATE", $"\"{before.Name}\" changes data before the amount is checked; put \"{request.Name}\" (and the check) before any call that is not a read.");
                }
            }
        }

        #endregion

        #region Groups and requests

        private static IReadOnlyList<FlowGroup> ParseGroups(JToken? token, ParseContext ctx)
        {
            if (token is not JArray list || list.Count == 0)
            {
                ctx.Error("apiGroupList", "NO_GROUPS", "\"apiGroupList\" must be a non-empty array of groups.");
                return Array.Empty<FlowGroup>();
            }

            // Names first, so a request may not take a group's name (or the other way round) whatever the order.
            foreach (var (g, gi) in list.Select((x, i) => (x, i)))
            {
                var gName = (string?)g["groupName"] ?? (string?)g["name"];
                if (!string.IsNullOrEmpty(gName))
                    ctx.GroupNames.Add(gName);
            }

            var groups = new List<FlowGroup>();
            var seenGroups = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (g, gi) in list.Select((x, i) => (x, i)))
            {
                var path = $"apiGroupList[{gi}]";
                if (g is not JObject go)
                {
                    ctx.Error(path, "BAD_GROUP", "A group must be an object.");
                    continue;
                }

                var name = (string?)go["groupName"] ?? (string?)go["name"];
                if (!CheckName(name, $"{path}.groupName", "group", ctx))
                    name = $"group{gi + 1}";
                else if (!seenGroups.Add(name!))
                    ctx.Error($"{path}.groupName", "DUPLICATE_GROUP", $"Two groups are named \"{name}\".");
                else
                    CheckNameLength(name!, $"{path}.groupName", "group", loops: false, ctx);

                // The browser's normalizeExecuteType: "condition" is "conditional"; anything else sequential.
                var executeType = (string?)go["executeType"];
                var mode = executeType switch
                {
                    null or "" or "sequential" => FlowGroupMode.Sequential,
                    "parallel" => FlowGroupMode.Parallel,
                    "conditional" or "condition" => FlowGroupMode.Conditional,
                    _ => FlowGroupMode.Sequential,
                };
                if (executeType is not (null or "" or "sequential" or "parallel" or "conditional" or "condition"))
                    ctx.Warning($"{path}.executeType", "UNKNOWN_EXECUTE_TYPE", $"\"{executeType}\" is not an execute type; the group runs sequentially (as in the browser).");
                if (mode == FlowGroupMode.Parallel)
                    ctx.Warning($"{path}.executeType", "PARALLEL_IN_ORDER",
                        "The server runs the requests of a parallel group one after the other (each step stays safe to resume). The results are the same: none of them may read another's result.");

                var source = mode == FlowGroupMode.Conditional ? ((string?)go["source"])?.Trim() : null;

                // Group repeat (useStepRepeat): the group's repeatConfig, used by every request in it.
                JObject? groupRepeat = null;
                var groupUsesRepeat = IsTrue(go["useStepRepeat"]);
                if (go["repeatConfig"] is JObject grc && !(grc["enabled"]?.Type == JTokenType.Boolean && !(bool)grc["enabled"]!))
                    groupRepeat = grc;

                var transform = ParseGroupTransform(go["transformResult"], $"{path}.transformResult", ctx);

                // The group's condition and its transform read what the groups before it answered.
                ctx.CurrentRequest = null;
                ctx.CurrentGroup = name;
                ctx.CurrentLoop = null;
                var executeIf = ReadExecuteIf(go, $"{path}", ConditionScope.Flow, ctx);
                var groupChecks = ReadResponseChecks(go["errorHandling"], $"{path}.errorHandling", ctx);
                WarnGroupOnError(go["errorHandling"], path, ctx);
                if (!string.IsNullOrEmpty(source))
                    ctx.AddReference($"{path}.source", source!, "the group's list", strict: false);

                var requests = new List<FlowRequest>();
                if (go["apis"] is not JArray apis || apis.Count == 0)
                    ctx.Error($"{path}.apis", "NO_REQUESTS", "A group needs at least one request in \"apis\".");
                else
                {
                    var used = apis;
                    if (mode == FlowGroupMode.Conditional && apis.Count > 1)
                    {
                        ctx.Warning($"{path}.apis", "CONDITIONAL_ONE_REQUEST", "A conditional group runs only its first request (as in the browser); the others are ignored.");
                        used = new JArray(apis[0]);
                    }
                    var groupInfo = new GroupInfo(name!, mode, source, groupUsesRepeat ? groupRepeat : null, groupRepeat, groupChecks);
                    foreach (var (a, ai) in used.Select((x, i) => (x, i)))
                    {
                        var request = ParseRequest(a, $"{path}.apis[{ai}]", groupInfo, ctx);
                        if (request != null)
                            requests.Add(request);
                    }
                }

                groups.Add(new FlowGroup { Name = name!, Index = gi, Mode = mode, Source = string.IsNullOrEmpty(source) ? null : source, ExecuteIf = executeIf, Transform = transform, Requests = requests });
            }

            return groups;
        }

        /// <summary>The longest the server waits for one call, in seconds.</summary>
        private const int MaxTimeoutSec = 120;

        /// <summary>The most times a call is tried again, and the longest wait before it, in seconds.</summary>
        private const int MaxRetries = 10;
        private const int MaxRetryDelaySec = 3600;

        /// <summary>What a request takes from its group.</summary>
        private sealed record GroupInfo(string Name, FlowGroupMode Mode, string? Source, JObject? ForcedRepeat, JObject? GroupRepeat, IReadOnlyList<JObject> Checks);

        private static FlowRequest? ParseRequest(JToken token, string path, GroupInfo group, ParseContext ctx)
        {
            var groupName = group.Name;
            var groupChecks = group.Checks;
            if (token is not JObject a)
            {
                ctx.Error(path, "BAD_REQUEST", "A request must be an object.");
                return null;
            }

            // The result key is apiName, as the browser keys it (normalizeApiConfig); the request
            // editor edits only apiName and leaves "name" as it was ("api"). A flow written by hand
            // may give only "name".
            var apiName = (string?)a["apiName"];
            var name = !string.IsNullOrEmpty(apiName) ? apiName : (string?)a["name"];
            var namePath = !string.IsNullOrEmpty(apiName) ? $"{path}.apiName" : $"{path}.name";
            if (!CheckName(name, namePath, "request", ctx))
                return null;
            if (!ctx.RequestNames.Add(name!))
                ctx.Error(namePath, "DUPLICATE_REQUEST", $"Two requests are named \"{name}\"; names must be unique across the whole flow (each one is a result key).");
            if (ctx.GroupNames.Contains(name!))
                ctx.Error(namePath, "NAME_CLASH", $"\"{name}\" is also a group name; the request's result would overwrite the group's.");
            ctx.CurrentRequest = name;
            ctx.CurrentGroup = groupName;

            // What is called
            var type = (string?)a["type"];
            var kind = FlowRequestKind.DataSource;
            long dataSourceId = 0;
            var operation = "";
            if (type == "dataSource" || (type == null && IsTrue(a["isDataSourceApi"])))
            {
                if (!TryPositiveLong(a["dataSourceId"], out dataSourceId))
                    ctx.Error($"{path}.dataSourceId", "NO_DATA_SOURCE", "A dataSource request needs \"dataSourceId\": a positive whole number.");
            }
            else if (type == "local")
            {
                if (ServerOperations.TryResolve((string?)a["endpoint"], out operation))
                    kind = FlowRequestKind.Local;
                else
                    ctx.Error($"{path}.endpoint", "LOCAL_NOT_ALLOWED", $"The server runs only these local endpoints: {string.Join(", ", ServerOperations.Endpoints.Select(e => "/" + e))} (got \"{(string?)a["endpoint"]}\"). Make other calls data sources.");
            }
            else
            {
                ctx.Error($"{path}.type", "BAD_TYPE", $"Request type \"{type}\" does not run on the server; use \"dataSource\" (or \"local\" with /Notification/send).");
            }

            // Things the server cannot do. A file answer as the browser decides it: responseType
            // "blob", or the older useFileDataSource flag when no responseType is set.
            var responseType = a["responseType"];
            if ((responseType?.Type == JTokenType.String && (string?)responseType == "blob")
                || ((responseType == null || responseType.Type == JTokenType.Null) && IsTrue(a["useFileDataSource"])))
                ctx.Error($"{path}.responseType", "NO_FILES", "File (blob) responses are not supported on the server.");
            if (IsTrue(a["sendAsFormData"]))
                ctx.Error($"{path}.sendAsFormData", "NO_FORM_DATA", "Form-data requests are not supported on the server.");
            if (a["pathParams"] is JObject { Count: > 0 })
                ctx.Warning($"{path}.pathParams", "IGNORED", "pathParams are not used for data sources and are ignored.");

            // Repeat (polling): the group's repeatConfig when the group or the request says
            // useStepRepeat, else the request's own (isRepeatable), as getEffectiveApiRepeatConfig.
            FlowRepeat? repeat = null;
            var repeatSource = group.ForcedRepeat ?? (IsTrue(a["useStepRepeat"]) ? group.GroupRepeat : null);
            var repeatPath = repeatSource != null ? $"{path} (the group's repeatConfig)" : $"{path}.repeatConfig";
            if (repeatSource == null && IsTrue(a["isRepeatable"]) && a["repeatConfig"] is JObject own
                && !(own["enabled"]?.Type == JTokenType.Boolean && !(bool)own["enabled"]!))
                repeatSource = own;
            if (repeatSource != null)
                repeat = ParseRepeat(repeatSource, repeatPath, ctx);

            // loopOn (sequential groups only, as in the browser) and a conditional group's list
            FlowLoop? loop = null;
            if (group.Mode == FlowGroupMode.Conditional)
            {
                if (!string.IsNullOrEmpty(group.Source))
                    loop = new FlowLoop { SourcePath = group.Source!, IsGroupSource = true };
            }
            else if (IsTrue(a["useLoopOn"]))
            {
                if (group.Mode == FlowGroupMode.Parallel)
                    ctx.Warning($"{path}.loopOn", "LOOP_IGNORED", "loopOn works only in a sequential group (the browser ignores it in a parallel one); it is ignored.");
                else
                    loop = ParseLoop(a["loopOn"], $"{path}.loopOn", ctx);
            }
            ctx.CurrentLoop = loop;
            CheckNameLength(name!, namePath, "request", loop != null, ctx);

            // Response shaping (transformApiResponse): valueOps, script, pick, omit, then readResFrom
            JToken? transformOps = null;
            string? transformScript = null;
            IReadOnlyList<string> pick = Array.Empty<string>(), omit = Array.Empty<string>();
            var trf = a["transformResponse"];
            if (IsTrue(trf?["enabled"]))
            {
                if (trf?["valueOps"] is { } ops && HasItems(ops))
                {
                    CheckValueOps(ops, $"{path}.transformResponse.valueOps", ctx);
                    transformOps = ops;
                }
                var script = (string?)trf?["script"];
                if (!string.IsNullOrWhiteSpace(script))
                {
                    CheckScript("response", script!, $"{path}.transformResponse.script", ctx);
                    transformScript = script;
                }
                pick = ListOf(trf?["pick"]) ?? ListOf(trf?["pickStr"]) ?? Array.Empty<string>();
                omit = ListOf(trf?["omit"]) ?? ListOf(trf?["omitStr"]) ?? Array.Empty<string>();
                if (!string.IsNullOrWhiteSpace((string?)trf?["opsRef"]))
                    ctx.Error($"{path}.transformResponse.opsRef", "NO_OPS_REF", "opsRef (the page transform library) does not exist on the server; write the valueOps inline.");
            }

            var readResFrom = (string?)a["readResFrom"];

            // Mappings
            var mappings = NormalizeMappings(a["dataMapping"], $"{path}.dataMapping", ctx);
            CheckMappings(mappings, $"{path}.dataMapping", ctx, buildsObject: true);
            if (kind == FlowRequestKind.Local && operation is ServerOperations.EnsureUser or ServerOperations.SetUserOptions)
                CheckAccountBody(mappings, $"{path}.dataMapping", ctx);
            if (kind == FlowRequestKind.Local && operation == ServerOperations.ChangeOrderStatus)
            {
                CheckOrderStatusBody(mappings, $"{path}.dataMapping", ctx);
                ctx.OrderOperationUses.Add($"{path}.endpoint");
            }

            JToken? staticData = a["staticData"];
            if ((staticData == null || staticData.Type == JTokenType.Null) && a["staticDataJson"] is { Type: JTokenType.String } sj && !string.IsNullOrWhiteSpace((string?)sj))
            {
                try { staticData = JsonConvert.DeserializeObject<JToken>((string)sj!, ReadSettings); }
                catch (JsonException) { ctx.Error($"{path}.staticDataJson", "BAD_JSON", "staticDataJson is not valid JSON."); }
            }
            if (staticData?.Type == JTokenType.Null)
                staticData = null;

            var executeIf = ReadExecuteIf(a, path, ConditionScope.Flow, ctx);
            var ownChecks = ReadResponseChecks(a["errorHandling"], $"{path}.errorHandling", ctx);
            var eh = MergedErrorHandling(a["errorHandling"]);
            var messagePath = (string?)eh?["responseMessagePath"];
            WarnBrowserOnlyHandling(eh, a["successHandling"], path, ctx);
            var fallback = ParseFallback(eh, $"{path}.errorHandling", ctx);

            // The server block
            var server = a["server"] as JObject;
            if (a["server"] != null && server == null)
                ctx.Error($"{path}.server", "BAD_SERVER", "\"server\" must be an object.");
            server ??= new JObject();
            var sp = $"{path}.server";

            var knownServer = new HashSet<string>(StringComparer.Ordinal) { "class", "timeoutSec", "retry", "guard", "outputs", "critical", "authToken", "sampleResponse", "refusedMessage" };
            foreach (var p in server.Properties().Where(p => !knownServer.Contains(p.Name)))
                ctx.Warning($"{sp}.{p.Name}", "UNKNOWN_KEY", $"\"{p.Name}\" is not a server setting and is ignored.");

            var cls = FlowRequestClass.Read;
            var clsText = (string?)server["class"];
            if (kind == FlowRequestKind.Local)
            {
                // The DCMS operations are safe to repeat (see ServerOperations.DefaultClass).
                cls = clsText == "nonIdempotent" ? FlowRequestClass.NonIdempotent : ServerOperations.DefaultClass(operation);
            }
            else
            {
                switch (clsText)
                {
                    case "read": cls = FlowRequestClass.Read; break;
                    case "idempotentWrite": cls = FlowRequestClass.IdempotentWrite; break;
                    case "nonIdempotent": cls = FlowRequestClass.NonIdempotent; break;
                    default:
                        // No default on purpose: a forgotten class on "convert to policy" must not
                        // make it repeatable.
                        ctx.Error($"{sp}.class", "NO_CLASS", "Say what the call does: \"read\", \"idempotentWrite\" (safe to send twice) or \"nonIdempotent\" (sending twice does it twice, e.g. issuing a policy).");
                        break;
                }
            }

            // The call's own settings, on the screens of the request editor the pages use: "Timeout (ms)"
            // on Request, the retries and "Keep going on error" on On error. They mean the same thing on
            // the server. server.timeoutSec / retry / critical are the older spellings: still read, so a
            // saved version runs as it did.
            int? timeoutSec = null;
            const string waitsToTheEnd = "A call that must never be sent twice is always waited for to the end (giving up early would leave it running while the flow moves on), so the server ignores its Timeout.";
            const string runsItself = "The server runs this DCMS operation itself and always waits for it to finish, so its Timeout is ignored.";
            if (TryPositiveInt(a["timeout"], out var timeoutMs))
            {
                if (kind == FlowRequestKind.Local)
                    ctx.Warning($"{path}.timeout", "TIMEOUT_IGNORED", runsItself);
                else if (cls == FlowRequestClass.NonIdempotent)
                    ctx.Warning($"{path}.timeout", "NO_TIMEOUT", waitsToTheEnd);
                else
                {
                    if (timeoutMs > MaxTimeoutSec * 1000)
                        ctx.Warning($"{path}.timeout", "TIMEOUT_CAPPED", $"On the server a call waits at most {MaxTimeoutSec * 1000} ms (2 minutes): this one gives up after that.");
                    timeoutSec = Math.Clamp((timeoutMs + 999) / 1000, 1, MaxTimeoutSec);
                }
            }
            else if (server["timeoutSec"] is { } to)
            {
                ctx.Warning($"{sp}.timeoutSec", "OLD_SERVER_SETTING", "server.timeoutSec is the older spelling of the call's \"Timeout (ms)\". It still works; its place is now the Request screen of the call.");
                if (!TryPositiveInt(to, out var t))
                    ctx.Error($"{sp}.timeoutSec", "BAD_TIMEOUT", $"timeoutSec must be a whole number of seconds from 1 to {MaxTimeoutSec}.");
                else if (kind == FlowRequestKind.Local)
                    ctx.Warning($"{sp}.timeoutSec", "TIMEOUT_IGNORED", runsItself);
                else if (cls == FlowRequestClass.NonIdempotent)
                    ctx.Warning($"{sp}.timeoutSec", "NO_TIMEOUT", waitsToTheEnd);
                else
                {
                    if (t > MaxTimeoutSec)
                        ctx.Warning($"{sp}.timeoutSec", "TIMEOUT_CAPPED", $"On the server a call waits at most {MaxTimeoutSec} s (2 minutes): this one gives up after that.");
                    timeoutSec = Math.Min(t, MaxTimeoutSec);
                }
            }

            // Not set: the server tries a read or a safe write 3 more times, after 10 s, 1 min and 5 min.
            var retryMax = cls == FlowRequestClass.NonIdempotent ? 0 : FlowRequest.DefaultRetryMax;
            IReadOnlyList<int> delays = FlowRequest.DefaultRetryDelaysSec;

            // "Retry attempts" and "Retry Delay (ms)" on the call's On error screen. The browser counts
            // the wait in milliseconds; the server counts seconds. 0 is the pages' filler value and
            // means "not set" in the browser too, so only a positive count is used; a count without a
            // delay keeps the server's waits.
            if (server["retry"] == null && eh != null && TryPositiveInt(eh["retryAttempts"], out var browserMax))
            {
                if (cls == FlowRequestClass.NonIdempotent)
                    ctx.Warning($"{path}.errorHandling.retryAttempts", "RETRY_IGNORED",
                        "A call that must never be sent twice is never sent again by the server, so its Retry attempts are ignored. Add a guard: it checks whether an unclear call went through.");
                else
                {
                    if (browserMax > MaxRetries)
                        ctx.Warning($"{path}.errorHandling.retryAttempts", "RETRY_CAPPED", $"The server tries a call again at most {MaxRetries} times; this one is tried again {MaxRetries} times.");
                    retryMax = Math.Min(browserMax, MaxRetries);
                    var delayToken = eh["retryDelay"];
                    if (TryPositiveInt(delayToken, out var ms))
                    {
                        if (ms > MaxRetryDelaySec * 1000)
                            ctx.Warning($"{path}.errorHandling.retryDelay", "RETRY_CAPPED", $"The server waits at most {MaxRetryDelaySec * 1000} ms (1 hour) before trying again; this call waits that long.");
                        delays = new[] { Math.Clamp(ms / 1000, 1, MaxRetryDelaySec) };
                    }
                    else if (delayToken != null && delayToken.Type != JTokenType.Null)
                        ctx.Warning($"{path}.errorHandling.retryDelay", "RETRY_DELAY_IGNORED", "Retry Delay (ms) is not a wait of at least 1 ms, so the server waits its own times before trying again: 10 s, 1 min, 5 min.");
                }
            }

            if (server["retry"] is { } retry)
            {
                ctx.Warning($"{sp}.retry", "OLD_SERVER_SETTING", "server.retry is the older spelling of the call's \"Retry attempts\" and \"Retry Delay (ms)\". It still works; its place is now the On error screen of the call.");
                if (cls == FlowRequestClass.NonIdempotent)
                    ctx.Error($"{sp}.retry", "NO_RETRY", "A nonIdempotent call is never re-sent blindly. Add a server.guard: it checks whether the call went through and re-sends at most once.");
                else if (retry is not JObject ro)
                    ctx.Error($"{sp}.retry", "BAD_RETRY", "retry must be { \"max\": 3, \"delaySec\": [10, 60, 300] }.");
                else
                {
                    if (ro["max"] is { } mx)
                    {
                        if (TryIntInRange(mx, 0, 10, out var m)) retryMax = m;
                        else ctx.Error($"{sp}.retry.max", "BAD_RETRY", "retry.max must be a whole number from 0 to 10.");
                    }
                    if (ro["delaySec"] is { } ds)
                    {
                        var parsed = new List<int>();
                        var items = ds is JArray da ? da.ToList() : new List<JToken> { ds };
                        foreach (var d in items)
                        {
                            if (TryIntInRange(d, 1, 3600, out var v)) parsed.Add(v);
                            else ctx.Error($"{sp}.retry.delaySec", "BAD_RETRY", "Each delay must be a whole number of seconds from 1 to 3600.");
                        }
                        if (parsed.Count > 0) delays = parsed;
                    }
                }
            }

            var outputs = ParseOutputs(server["outputs"], $"{sp}.outputs", ctx);

            FlowGuard? guard = null;
            if (server["guard"] is { } g)
            {
                if (cls != FlowRequestClass.NonIdempotent)
                    ctx.Warning($"{sp}.guard", "GUARD_UNUSED", "A guard is only used by nonIdempotent calls; this call is simply retried.");
                guard = ParseGuard(g, $"{sp}.guard", outputs, ctx);
            }
            else if (cls == FlowRequestClass.NonIdempotent && kind == FlowRequestKind.DataSource)
            {
                ctx.Warning($"{sp}.guard", "NO_GUARD", "Without a guard, any unclear result of this call (timeout, error, crash) stops the run until a person checks it.");
            }

            var critical = kind == FlowRequestKind.DataSource || ServerOperations.DefaultCritical(operation);

            // "Keep going on error" on the call's On error screen: this call failed, carry on with the
            // next one. server.critical (the older spelling) wins when both are set, so a saved
            // version runs as it did.
            if (IsTrue(eh?["continueOnError"]))
                critical = false;

            // In a parallel group the browser catches a failed call that has no error settings of its
            // own ({ error, apiName }) and the group goes on; with settings, a failure ends the group.
            if (group.Mode == FlowGroupMode.Parallel && !MeaningfulErrorHandling(eh))
                critical = false;

            if (server["critical"] is { } cr)
            {
                ctx.Warning($"{sp}.critical", "OLD_SERVER_SETTING", "server.critical is the older spelling of the call's \"Keep going on error\" (critical false = ticked). It still works; its place is now the On error screen of the call.");
                if (cr.Type != JTokenType.Boolean) ctx.Error($"{sp}.critical", "BAD_CRITICAL", "critical must be true or false.");
                else critical = (bool)cr;
            }

            if (fallback != null && critical)
                ctx.Warning($"{path}.errorHandling.fallbackValueMapping", "FALLBACK_UNUSED",
                    "Fallback values are used only when this call may fail without stopping the run: tick \"Keep going on error\" on the call's On error screen.");

            if (cls == FlowRequestClass.NonIdempotent && loop != null)
                ctx.Error($"{path}.loopOn", "NO_LOOP_ONCE", "A call that must never be sent twice cannot loop over a list on the server (each item would need its own check). Split it, or make it idempotentWrite if sending it twice is harmless.");
            if (cls == FlowRequestClass.NonIdempotent && repeat != null)
                ctx.Error(repeatPath, "NO_REPEAT_ONCE", "A call that must never be sent twice cannot be repeated (polling). Poll with a separate read request instead.");
            ctx.CurrentLoop = null;

            var authToken = (string?)server["authToken"];
            if (authToken != null && !NamePattern.IsMatch(authToken))
                ctx.Error($"{sp}.authToken", "BAD_TOKEN_NAME", "authToken is the NAME of a token under ServerFlows:Tokens in the server configuration (letters, digits, _), never the token itself.");

            if (kind == FlowRequestKind.Local && operation == ServerOperations.Notify && outputs.Count > 0)
                ctx.Warning($"{sp}.outputs", "NOTIFY_OUTPUTS", "A notification answers a plain message; its outputs are usually empty.");

            var refused = ParseLanguageTexts(server["refusedMessage"], $"{sp}.refusedMessage", "BAD_REFUSED_MESSAGE", ctx);
            if (refused.Count > 0)
                ctx.Warning($"{sp}.refusedMessage", "OLD_SERVER_SETTING", "server.refusedMessage is the older way to say what the customer reads. It still works; its place is now the call's \"Error message\" (On error screen), with \"Translate the message\" for the site's languages.");

            var errorMessage = ParseErrorMessage(eh);

            return new FlowRequest
            {
                Name = name!,
                GroupName = groupName,
                Kind = kind,
                DataSourceId = dataSourceId,
                Operation = operation,
                DataMapping = mappings,
                LocalTree = kind == FlowRequestKind.Local
                    && mappings.Any(m => m["children"] is JArray { Count: > 0 } || m["inputs"] is JArray { Count: > 0 }),
                StaticData = staticData,
                MergeData = IsTrue(a["mergeData"]),
                ExecuteIf = executeIf,
                ResponseChecks = ownChecks.Concat(groupChecks).ToList(),
                ResponseMessagePath = string.IsNullOrWhiteSpace(messagePath) ? null : messagePath,
                ErrorMessage = errorMessage,
                TransformValueOps = transformOps,
                TransformScript = transformScript,
                Pick = pick,
                Omit = omit,
                Loop = loop,
                LoopItemFailuresContinue = loop != null && !MeaningfulErrorHandling(eh) && !(server["critical"]?.Type == JTokenType.Boolean && (bool)server["critical"]!),
                Repeat = repeat,
                Fallback = critical ? null : fallback,
                ReadResFrom = string.IsNullOrWhiteSpace(readResFrom) ? null : readResFrom,
                Class = cls,
                TimeoutSec = timeoutSec,
                RetryMax = retryMax,
                RetryDelaysSec = delays,
                Guard = guard,
                Outputs = outputs,
                Critical = critical,
                AuthToken = authToken,
                SampleResponse = server["sampleResponse"],
                RefusedMessages = refused,
            };
        }

        private const int MaxCustomerText = 300;

        /// <summary>
        /// A sentence for the customer (server.refusedMessage): one for every language, or
        /// { "en": "...", "ar": "..." } ("*" = every language). It reaches the customer as it is, so
        /// it is plain text of a reasonable length.
        /// </summary>
        private static IReadOnlyDictionary<string, string> ParseLanguageTexts(JToken? token, string path, string code, ParseContext ctx)
        {
            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            if (token == null || token.Type == JTokenType.Null)
                return texts;

            void Add(string language, JToken value, string at)
            {
                var text = value.Type == JTokenType.String ? ((string?)value)?.Trim() : null;
                if (string.IsNullOrEmpty(text))
                    ctx.Error(at, code, "Write the sentence the customer sees, or remove it.");
                else if (text.Length > MaxCustomerText)
                    ctx.Error(at, code, $"At most {MaxCustomerText} characters: it is shown in a toast.");
                else if (text.Contains('<'))
                    ctx.Error(at, code, "Plain text only (no HTML).");
                else
                    texts[language] = text;
            }

            if (token is JObject byLanguage)
            {
                foreach (var p in byLanguage.Properties())
                {
                    var language = p.Name.Trim().ToLowerInvariant();
                    if (language != FlowTexts.AnyLanguage && (language.Length is < 2 or > 3 || !language.All(char.IsAsciiLetterLower)))
                        ctx.Error($"{path}.{p.Name}", "BAD_LANGUAGE", "Name the language with its 2-letter code, e.g. \"en\" or \"ar\" (or \"*\" for every language).");
                    else
                        Add(language, p.Value, $"{path}.{p.Name}");
                }
            }
            else
                Add(FlowTexts.AnyLanguage, token, path);
            return texts;
        }

        private static FlowGuard? ParseGuard(JToken token, string path, IReadOnlyDictionary<string, FlowOutput> requestOutputs, ParseContext ctx)
        {
            if (token is not JObject g)
            {
                ctx.Error(path, "BAD_GUARD", "guard must be an object { dataSourceId, dataMapping, settleSec, doneIf, notDoneIf, outputs }.");
                return null;
            }

            if (!TryPositiveLong(g["dataSourceId"], out var ds))
                ctx.Error($"{path}.dataSourceId", "NO_DATA_SOURCE", "The guard needs \"dataSourceId\": a data source that READS the current state (e.g. get the quotation / policy).");

            var mappings = NormalizeMappings(g["dataMapping"], $"{path}.dataMapping", ctx);
            CheckMappings(mappings, $"{path}.dataMapping", ctx, buildsObject: true);

            var settle = 15;
            if (g["settleSec"] is { } st && !TryIntInRange(st, 0, 600, out settle))
            {
                ctx.Error($"{path}.settleSec", "BAD_SETTLE", "settleSec must be a whole number from 0 to 600.");
                settle = 15;
            }

            var maxTries = 3;
            if (g["maxTries"] is { } mt && !TryIntInRange(mt, 1, 10, out maxTries))
            {
                ctx.Error($"{path}.maxTries", "BAD_TRIES", "maxTries must be a whole number from 1 to 10.");
                maxTries = 3;
            }

            var doneIf = ReadConditionalList(g["doneIf"], $"{path}.doneIf", ConditionScope.Response, ctx);
            var notDoneIf = ReadConditionalList(g["notDoneIf"], $"{path}.notDoneIf", ConditionScope.Response, ctx);
            if (doneIf == null)
                ctx.Error($"{path}.doneIf", "NO_DONE_IF", "doneIf is required: the condition on the guard's response that means \"it went through\".");
            if (notDoneIf == null)
                ctx.Error($"{path}.notDoneIf", "NO_NOT_DONE_IF", "notDoneIf is required: the condition that means \"it did not go through\". When neither matches, a person decides.");

            var outputs = ParseOutputs(g["outputs"], $"{path}.outputs", ctx);
            foreach (var missing in requestOutputs.Keys.Where(k => !outputs.ContainsKey(k)))
                ctx.Error($"{path}.outputs", "GUARD_OUTPUTS", $"guard.outputs must also give \"{missing}\": when the guard says the call went through, the flow continues with the guard's values.");

            var authToken = (string?)g["authToken"];
            if (authToken != null && !NamePattern.IsMatch(authToken))
                ctx.Error($"{path}.authToken", "BAD_TOKEN_NAME", "authToken is the NAME of a token under ServerFlows:Tokens.");

            return new FlowGuard
            {
                DataSourceId = ds,
                DataMapping = mappings,
                SettleSec = settle,
                MaxTries = maxTries,
                DoneIf = doneIf ?? new JObject(),
                NotDoneIf = notDoneIf ?? new JObject(),
                Outputs = outputs,
                AuthToken = authToken,
                SampleResponse = g["sampleResponse"],
            };
        }

        #region Scripts, loops, repeats, fallbacks, group transforms

        /// <summary>A page transform script must compile here as it does in the browser (new Function(arg, "return " + script)).</summary>
        private static void CheckScript(string argName, string script, string path, ParseContext ctx)
        {
            try
            {
                JsScript.Check(argName, script);
            }
            catch (FlowScriptException ex)
            {
                ctx.Error(path, "BAD_SCRIPT", ex.Message);
            }
        }

        /// <summary>The browser's parseList: an array of non-blank strings, or a comma-separated string.</summary>
        private static IReadOnlyList<string>? ListOf(JToken? token)
        {
            if (token is JArray arr)
                return arr.Where(v => v.Type == JTokenType.String && ((string)v!).Trim().Length > 0).Select(v => ((string)v!).Trim()).ToList();
            if (token?.Type == JTokenType.String)
            {
                var list = ((string)token!).Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                return list.Count > 0 ? list : null;
            }
            return null;
        }

        /// <summary>The browser's parseJsonValueOrRaw: JSON text is parsed, other text kept, blank = none.</summary>
        private static JToken? JsonValueOrRaw(JToken? token)
        {
            if (token == null || token.Type is JTokenType.Null or JTokenType.Undefined)
                return null;
            if (token.Type != JTokenType.String)
                return token.DeepClone();
            var text = ((string)token!).Trim();
            if (text.Length == 0)
                return null;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(text, ReadSettings) ?? JValue.CreateNull();
            }
            catch (JsonException)
            {
                return new JValue(text);
            }
        }

        /// <summary>normalizeErrorHandlingConfig's "hasMeaningful": settings that make the browser handle the error itself.</summary>
        private static bool MeaningfulErrorHandling(JObject? eh)
        {
            if (eh == null)
                return false;
            var checks = eh["responseErrorChecksConfig"];
            var onError = eh["onErrorConfig"];
            return IsTrue(eh["continueOnError"])
                || TryPositiveInt(eh["retryAttempts"], out _)
                || JsonValueOrRaw(eh["fallbackValue"]) != null
                || HasItems(eh["fallbackValueMapping"])
                || !string.IsNullOrEmpty((string?)eh["message"])
                || !string.IsNullOrEmpty((string?)eh["responseMessagePath"])
                || IsTrue(eh["showErrorToast"])
                || HasItems(checks?["conditionalList"]?["conditions"]) || HasItems(checks?["conditions"])
                || IsTrue(eh["recallApi"])
                || (onError is JObject oe && !(oe["enabled"]?.Type == JTokenType.Boolean && !(bool)oe["enabled"]!)
                    && (HasItems(oe["callbackApiConfig"]?["apiGroupList"]) || IsTrue(oe["retryOriginalOnSuccess"])))
                || HasItems(eh["onErrorActions"]?["actionList"]);
        }

        /// <summary>fallbackValueMapping (built like a body) or fallbackValue: what a call that failed for good answers instead.</summary>
        private static FlowFallback? ParseFallback(JObject? eh, string path, ParseContext ctx)
        {
            if (eh == null)
                return null;
            JArray? mapping = null;
            if (eh["fallbackValueMapping"] is JArray { Count: > 0 } fm)
            {
                mapping = NormalizeMappings(fm, $"{path}.fallbackValueMapping", ctx);
                var loop = ctx.CurrentLoop;
                ctx.CurrentLoop = null; // the browser builds it without the loop's item
                CheckMappings(mapping, $"{path}.fallbackValueMapping", ctx, buildsObject: true);
                ctx.CurrentLoop = loop;
            }
            var value = JsonValueOrRaw(eh["fallbackValue"]);
            return mapping == null && value == null ? null : new FlowFallback { Mapping = mapping, Value = value };
        }

        private static FlowLoop? ParseLoop(JToken? token, string path, ParseContext ctx)
        {
            if (token is not JObject lo)
            {
                ctx.Error(path, "BAD_LOOP", "loopOn must be { \"sourcePath\": \"request.listOutput\", \"itemAlias\": \"item\", \"resultKey\": \"\" }.");
                return null;
            }

            var source = ((string?)lo["sourcePath"])?.Trim();
            if (string.IsNullOrEmpty(source))
            {
                ctx.Error($"{path}.sourcePath", "NO_LOOP_SOURCE", "loopOn needs sourcePath: where the list to go over is in the earlier results.");
                return null;
            }
            ctx.AddReference($"{path}.sourcePath", source!, "the loop's list", strict: false);

            string? Name(string key, string what)
            {
                var value = ((string?)lo[key])?.Trim();
                if (string.IsNullOrEmpty(value))
                    return null;
                if (!NamePattern.IsMatch(value))
                {
                    ctx.Error($"{path}.{key}", "BAD_NAME", $"The {what} must start with a letter and use only letters, digits and _.");
                    return null;
                }
                ctx.ExtraNames.Add(($"{path}.{key}", value, what));
                return value;
            }

            // The loop editor fills the item name with "currentItem", the name the item always has:
            // that is no extra name.
            var alias = ((string?)lo["itemAlias"])?.Trim() == "currentItem" ? null : Name("itemAlias", "loop item name");
            var resultKey = Name("resultKey", "loop result key");
            if ((string?)lo["processing"] == "parallel")
                ctx.Warning($"{path}.processing", "LOOP_IN_ORDER", "The server goes over the items one after the other (the results are the same).");

            return new FlowLoop { SourcePath = source!, ItemAlias = alias, ResultKey = resultKey };
        }

        private static HashSet<string> LoopScope(FlowLoop? loop)
        {
            var scope = new HashSet<string>(StringComparer.Ordinal);
            if (loop == null)
                return scope;
            scope.Add("currentItem");
            scope.Add("currentIndex");
            if (loop.ItemAlias != null)
                scope.Add(loop.ItemAlias);
            return scope;
        }

        private static readonly string[] RepeatStrategies = { "fixedCount", "untilCondition", "untilValue", "polling", "exponentialBackoff" };

        /// <summary>The browser's normalizeApiRepeatConfig plus each strategy's defaults (applyRepeatConfig).</summary>
        private static FlowRepeat? ParseRepeat(JObject r, string path, ParseContext ctx)
        {
            static int? Num(JToken? t)
            {
                if (t == null) return null;
                if (t.Type is JTokenType.Integer or JTokenType.Float) return (int)Math.Clamp((double)t, int.MinValue, int.MaxValue);
                if (t.Type == JTokenType.String)
                {
                    var text = ((string)t!).Trim();
                    var digits = new string(text.TakeWhile((c, i) => char.IsDigit(c) || (i == 0 && c == '-')).ToArray());
                    return int.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) && n != 0 ? n : null;
                }
                return null;
            }
            static bool? Flag(JToken? t) => t?.Type == JTokenType.Boolean ? (bool)t : null;

            var strategy = (string?)r["strategy"] ?? "fixedCount";
            if (!RepeatStrategies.Contains(strategy))
            {
                ctx.Warning($"{path}.strategy", "UNKNOWN_REPEAT", $"Unknown repeat strategy \"{strategy}\": the call is sent once (as in the browser).");
                return null;
            }

            var delayMs = Num(r["delayMs"]) ?? 1000;
            var maxRaw = Num(r["maxAttempts"]);
            var repeatCount = r["repeatCount"]?.Type is JTokenType.Integer or JTokenType.Float ? (int)(double)r["repeatCount"]! : (maxRaw ?? 1);
            JObject? Conditions(JToken? cfg, string at) =>
                cfg?["conditionalList"]?["conditions"] is JArray { Count: > 0 } ? ReadConditionalList(cfg["conditionalList"], at, ConditionScope.Response, ctx) : null;

            JObject? until = null;
            string? untilPath = null, untilOperator = null;
            JToken? untilValue = null;
            var maxAttempts = 10;
            int initialDelay = delayMs, maxDelay = 30000;
            var exponential = false;
            var multiplier = 2.0;
            var jitter = false;
            switch (strategy)
            {
                case "fixedCount":
                    maxAttempts = repeatCount;
                    break;
                case "untilCondition":
                    maxAttempts = maxRaw ?? 10;
                    until = Conditions(r["untilConditionConfig"], $"{path}.untilConditionConfig.conditionalList");
                    if (until == null)
                        ctx.Warning($"{path}.untilConditionConfig", "NO_UNTIL", "untilCondition has no condition, so it never counts as done and repeats to the maximum (as in the browser).");
                    break;
                case "untilValue":
                    maxAttempts = maxRaw ?? 10;
                    until = Conditions(r["untilValueConfig"], $"{path}.untilValueConfig.conditionalList");
                    if (until == null)
                    {
                        var uv = r["untilValue"] as JObject;
                        untilPath = (string?)r["untilValuePath"] ?? (string?)uv?["path"];
                        untilOperator = (string?)r["untilValueOperator"] ?? (string?)uv?["operator"] ?? "===";
                        untilValue = r["untilValueValue"] ?? uv?["value"];
                        if (r["untilValuePath"] == null && r["untilValueOperator"] == null && r["untilValueValue"] == null && uv == null)
                        {
                            ctx.Warning(path, "NO_UNTIL", "untilValue has nothing to compare: the call is sent once (as in the browser).");
                            return null;
                        }
                        if (untilOperator is not ("===" or "!==" or ">" or "<" or ">=" or "<=" or "exists" or "notExists" or "includes"))
                            ctx.Error($"{path}.untilValueOperator", "BAD_OPERATOR", "untilValue's operator must be one of ===, !==, >, <, >=, <=, exists, notExists, includes.");
                    }
                    break;
                case "polling":
                {
                    var pc = r["pollingConfig"] as JObject;
                    until = Conditions(pc, $"{path}.pollingConfig.conditionalList");
                    if (until == null)
                    {
                        ctx.Warning($"{path}.pollingConfig", "NO_UNTIL", "Polling needs a condition (pollingConfig): without one the call is sent once (as in the browser).");
                        return null;
                    }
                    maxAttempts = maxRaw ?? 20;
                    initialDelay = Num(r["initialDelayMs"]) ?? Num(pc?["initialDelayMs"]) ?? delayMs;
                    exponential = Flag(r["pollingUseExponentialBackoff"]) ?? Flag(pc?["useExponentialBackoff"]) ?? false;
                    maxDelay = Num(r["pollingMaxDelayMs"]) ?? Num(pc?["maxDelayMs"]) ?? 30000;
                    break;
                }
                case "exponentialBackoff":
                {
                    var nb = r["exponentialBackoffConfig"] as JObject;
                    maxAttempts = maxRaw ?? 10;
                    until = Conditions(nb, $"{path}.exponentialBackoffConfig.conditionalList");
                    initialDelay = Num(r["exponentialInitialDelayMs"]) ?? Num(nb?["initialDelayMs"]) ?? delayMs;
                    var m = r["exponentialMultiplier"] ?? nb?["multiplier"];
                    multiplier = m?.Type is JTokenType.Integer or JTokenType.Float ? (double)m! : double.TryParse((string?)m, NumberStyles.Float, CultureInfo.InvariantCulture, out var mm) && mm > 0 ? mm : 2;
                    maxDelay = Num(r["exponentialMaxDelayMs"]) ?? Num(nb?["maxDelayMs"]) ?? 60000;
                    jitter = Flag(r["exponentialAddJitter"]) ?? Flag(nb?["addJitter"]) ?? true;
                    break;
                }
            }

            if (maxAttempts < 1)
                maxAttempts = 1;
            if (maxAttempts > 100)
            {
                ctx.Warning(path, "REPEAT_CAPPED", $"{maxAttempts} attempts is more than the server allows; it stops at 100.");
                maxAttempts = 100;
            }

            var customDelay = (string?)r["customDelayFn"];
            if (!string.IsNullOrWhiteSpace(customDelay))
                CheckScript("ctx", customDelay!, $"{path}.customDelayFn", ctx);
            if (!string.IsNullOrWhiteSpace((string?)r["onEachAttempt"]))
                ctx.Warning($"{path}.onEachAttempt", "BROWSER_ONLY", "onEachAttempt only reports to the page and is ignored on the server.");

            return new FlowRepeat
            {
                Strategy = strategy,
                MaxAttempts = maxAttempts,
                DelayMs = Math.Max(0, delayMs),
                CustomDelayFn = string.IsNullOrWhiteSpace(customDelay) ? null : customDelay,
                Until = until,
                UntilPath = untilPath,
                UntilOperator = untilOperator,
                UntilValue = untilValue?.DeepClone(),
                StopIf = Conditions(r["stopIfConfig"], $"{path}.stopIfConfig.conditionalList"),
                MaxDurationMs = Num(r["maxDurationMs"]) is int md && md > 0 ? md : null,
                ContinueOnError = IsTrue(r["continueOnError"]),
                CollectAll = IsTrue(r["collectAllResponses"]),
                InitialDelayMs = Math.Max(0, initialDelay),
                Exponential = exponential,
                MaxDelayMs = Math.Max(0, maxDelay),
                Multiplier = multiplier,
                Jitter = jitter,
            };
        }

        /// <summary>transformResult: valueOps, script ("result"), mapping ({ newKey: path } from mapping or mappingJson).</summary>
        private static FlowTransform? ParseGroupTransform(JToken? token, string path, ParseContext ctx)
        {
            if (token is not JObject tr || !IsTrue(tr["enabled"]))
                return null;

            JToken? ops = null;
            if (tr["valueOps"] is { } vo && HasItems(vo))
            {
                CheckValueOps(vo, $"{path}.valueOps", ctx);
                ops = vo;
            }
            if (!string.IsNullOrWhiteSpace((string?)tr["opsRef"]))
                ctx.Error($"{path}.opsRef", "NO_OPS_REF", "opsRef (the page transform library) does not exist on the server; write the valueOps inline.");

            var script = (string?)tr["script"];
            if (!string.IsNullOrWhiteSpace(script))
                CheckScript("result", script!, $"{path}.script", ctx);
            else
                script = null;

            Dictionary<string, string>? mapping = null;
            void FromObject(JObject o)
            {
                mapping = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var p in o.Properties())
                    mapping[p.Name] = p.Value.Type == JTokenType.String ? (string)p.Value! : p.Value.ToString(Formatting.None);
            }
            if (tr["mapping"] is JObject mo && mo.Count > 0)
                FromObject(mo);
            else if (tr["mappingJson"] is JObject mj && mj.Count > 0)
                FromObject(mj);
            else if (tr["mappingJson"]?.Type == JTokenType.String && ((string)tr["mappingJson"]!).Trim().Length > 0)
            {
                var raw = ((string)tr["mappingJson"]!).Trim();
                JObject? parsed = null;
                try { parsed = JsonConvert.DeserializeObject<JToken>(raw, ReadSettings) as JObject; }
                catch (JsonException) { parsed = null; }
                if (parsed != null)
                    FromObject(parsed);
                else
                {
                    // "newKey: path" per line, as the browser falls back to
                    mapping = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var line in raw.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
                    {
                        var sep = line.IndexOf(':');
                        if (sep > 0 && line[..sep].Trim().Length > 0)
                            mapping[line[..sep].Trim()] = line[(sep + 1)..].Trim();
                    }
                    if (mapping.Count == 0)
                        mapping = null;
                }
            }

            if (ops == null && script == null && mapping == null)
            {
                ctx.Warning(path, "EMPTY_GROUP_TRANSFORM", "transformResult is enabled but empty; it is ignored.");
                return null;
            }
            return new FlowTransform { ValueOps = ops, Script = script, Mapping = mapping };
        }

        /// <summary>Loop item names and result keys share the results with the requests and groups.</summary>
        private static void CheckExtraNames(IReadOnlyList<FlowGroup> groups, ParseContext ctx)
        {
            var taken = new HashSet<string>(groups.Select(g => g.Name).Concat(groups.SelectMany(g => g.Requests).Select(r => r.Name)), StringComparer.Ordinal);
            var resultKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (path, name, what) in ctx.ExtraNames)
            {
                if (taken.Contains(name) || ReservedNames.Contains(name))
                    ctx.Error(path, "NAME_CLASH", $"The {what} \"{name}\" is also the name of a request, a group or the run's data; pick another one.");
                else if (what == "loop result key" && !resultKeys.Add(name))
                    ctx.Error(path, "NAME_CLASH", $"Two loops keep their results under \"{name}\".");
            }
        }

        #endregion

        private static IReadOnlyDictionary<string, FlowOutput> ParseOutputs(JToken? token, string path, ParseContext ctx)
        {
            var outputs = new Dictionary<string, FlowOutput>(StringComparer.Ordinal);
            if (token == null || token.Type == JTokenType.Null)
                return outputs;
            if (token is not JObject o)
            {
                ctx.Error(path, "BAD_OUTPUTS", "outputs must be an object: { \"policyNo\": \"data.policyNumber\" } or { \"premium\": { \"path\": \"data.total\", \"valueOps\": [\"number\"] } }.");
                return outputs;
            }

            foreach (var p in o.Properties())
            {
                if (!NamePattern.IsMatch(p.Name))
                {
                    ctx.Error($"{path}.{p.Name}", "BAD_OUTPUT_NAME", "An output name must start with a letter and use only letters, digits and _.");
                    continue;
                }

                string? outPath;
                JToken? ops = null;
                var isPublic = false;
                if (p.Value.Type == JTokenType.String)
                    outPath = (string?)p.Value;
                else if (p.Value is JObject po)
                {
                    outPath = (string?)po["path"];
                    ops = po["valueOps"];
                    if (ops != null)
                        CheckValueOps(ops, $"{path}.{p.Name}.valueOps", ctx);
                    isPublic = IsTrue(po["public"]);
                }
                else
                {
                    ctx.Error($"{path}.{p.Name}", "BAD_OUTPUT", "An output is a path into the response, or { \"path\", \"valueOps\", \"public\" }.");
                    continue;
                }

                outputs[p.Name] = new FlowOutput { Name = p.Name, Path = outPath ?? "", ValueOps = ops, Public = isPublic };
            }

            return outputs;
        }

        #endregion

        #region Mappings

        /// <summary>Array form as-is; object form ({ key: "path" } or { key: { ... } }) converted the way the browser does.</summary>
        private static JArray NormalizeMappings(JToken? token, string path, ParseContext ctx)
        {
            if (token == null || token.Type == JTokenType.Null)
                return new JArray();
            if (token is JArray arr)
                return (JArray)arr.DeepClone();
            if (token is JObject obj)
            {
                var list = new JArray();
                foreach (var p in obj.Properties())
                {
                    if (p.Value.Type == JTokenType.String)
                        list.Add(new JObject { ["paramName"] = p.Name, ["typeOfValue"] = "previousResults", ["path"] = p.Value.DeepClone() });
                    else if (p.Value is JObject cfg)
                    {
                        var copy = (JObject)cfg.DeepClone();
                        if (copy["paramName"] == null)
                            copy["paramName"] = p.Name;
                        list.Add(copy);
                    }
                    else
                        ctx.Error($"{path}.{p.Name}", "BAD_MAPPING", "A mapping must be an object or a previousResults path.");
                }
                return list;
            }

            ctx.Error(path, "BAD_MAPPINGS", "dataMapping must be an array of mappings.");
            return new JArray();
        }

        /// <summary>
        /// <paramref name="buildsObject"/>: the mappings become the keys of an object (a request body,
        /// objectOfObject), so each needs a paramName and names may not repeat. arrayOfValues inputs
        /// are list items: neither applies.
        /// </summary>
        private static void CheckMappings(JArray mappings, string path, ParseContext ctx, bool buildsObject)
        {
            var byParam = new Dictionary<string, List<bool>>(StringComparer.Ordinal);
            foreach (var (m, i) in mappings.Select((x, i) => (x, i)))
            {
                var mp = $"{path}[{i}]";
                if (m is not JObject mo)
                {
                    ctx.Error(mp, "BAD_MAPPING", "A mapping must be an object.");
                    continue;
                }

                CheckMapping(mo, mp, ctx);
                if (!buildsObject)
                    continue;

                if (mo["paramName"] == null && !IsTrue(mo["spreadValue"]))
                    ctx.Error($"{mp}.paramName", "NO_PARAM_NAME", "Give the mapping a paramName (the key it is sent as), or \"\" to send its value as the whole body.");

                var param = (string?)mo["paramName"] ?? "";
                var conditional = IsTrue(mo["hasCondition"]) && mo["conditionalList"]?["conditions"] is JArray { Count: > 0 };
                if (!byParam.TryGetValue(param, out var flags))
                    byParam[param] = flags = new List<bool>();
                flags.Add(conditional);
            }

            foreach (var (param, flags) in byParam)
            {
                if (flags.Count > 1 && flags.Any(f => !f))
                    ctx.Error(path, "DUPLICATE_PARAM", $"\"{param}\" is mapped {flags.Count} times. Only an ordered list of conditional mappings (hasCondition with conditions on each) may share a name.");
            }
        }

        private static void CheckMapping(JObject m, string path, ParseContext ctx)
        {
            var type = (string?)m["typeOfValue"];
            if (type == null || !MappingTypes.Contains(type))
            {
                ctx.Error($"{path}.typeOfValue", "BAD_VALUE_SOURCE",
                    $"typeOfValue \"{type}\" is not available on the server. Use one of: {string.Join(", ", MappingTypes)}. (There is no form, page, query string or browser storage on the server: put those values in the cart before payment and read them with fromAdditionalDetailsCart.)");
            }

            if (m["getDataFrom"] != null && m["typeOfValue"] == null)
                ctx.Error($"{path}.getDataFrom", "WRONG_KEY", "Mappings use \"typeOfValue\", not \"getDataFrom\".");

            if (!string.IsNullOrWhiteSpace((string?)m["transform"]))
                CheckScript("value", (string)m["transform"]!, $"{path}.transform", ctx);
            if (!string.IsNullOrWhiteSpace((string?)m["opsRef"]))
                ctx.Error($"{path}.opsRef", "NO_OPS_REF", "opsRef (the page transform library) does not exist on the server; write the valueOps inline.");
            if (IsTrue(m["useTransformPrevResults"]))
                ctx.Error($"{path}.useTransformPrevResults", "NO_TRANSFORM_PREV", "transformPrevResults feeds a page-only value (transformedData) and has no meaning on the server. Read the value with previousResults and reshape it with \"transform\" or valueOps.");

            if (m["valueOps"] is { } ops && HasItems(ops))
                CheckValueOps(ops, $"{path}.valueOps", ctx);

            if (IsTrue(m["hasCondition"]))
                ReadConditionalList(m["conditionalList"], $"{path}.conditionalList", ConditionScope.Flow, ctx);

            switch (type)
            {
                case "currentDate":
                    if (!FlowDateFormat.IsSupportedCurrentDateFormat((string?)m["formatDate"], (string?)m["formatDateOther"], out var dateError))
                        ctx.Error($"{path}.formatDate", "BAD_DATE_FORMAT", dateError ?? "This date format is not supported on the server.");
                    break;
                case "previousResults":
                    ctx.AddReference($"{path}.path", (string?)m["path"] ?? "", "a previousResults mapping", strict: true);
                    break;
                case "fromInput":
                    ctx.InputUses.Add(($"{path}.path", InputName((string?)m["path"])));
                    break;
                case "fromOrder":
                    ctx.OrderUses.Add($"{path}.path");
                    break;
                case "fromCart":
                case "fromAdditionalDetailsCart":
                    ctx.CartUses.Add($"{path}.path");
                    break;
                case "loggedInUser":
                case "userOption":
                case "currentUsername":
                    ctx.UserUses.Add($"{path}.path");
                    break;
                case "fromUser":
                    ctx.UserUses.Add($"{path}.path");
                    ctx.Warning($"{path}.typeOfValue", "OLD_SOURCE", "\"From the signed-in user (server flows)\" is the older name. It still works; read the user as pages do instead, with Logged In User (e.g. username, fullName, email, mobilephone), User Option (an option's name) or Current Username.");
                    break;
                case "currentItem":
                case "currentItemKey":
                case "currentIndex":
                    if (ctx.CurrentLoop == null)
                        ctx.Error($"{path}.typeOfValue", "NOT_IN_LOOP", $"\"{type}\" reads the item of a loop, but this request does not loop over a list (loopOn, or a conditional group with a source list).");
                    break;
                case "objectOfObject":
                case "arrayOfValues":
                    if (m["inputs"] is JArray inputs)
                        CheckMappings(inputs, $"{path}.inputs", ctx, buildsObject: type == "objectOfObject");
                    else
                        ctx.Error($"{path}.inputs", "NO_INPUTS", $"{type} needs \"inputs\": the list of mappings that build it.");
                    break;
            }
        }

        /// <summary>The body fields of the account operations that say whose account it is.</summary>
        private static readonly HashSet<string> AccountIdentityFields = new(StringComparer.Ordinal) { "userName", "userId", "preferUserId", "lookupValue", "nationalId" };

        /// <summary>
        /// The account operations act on a person's DCMS account, so a visitor must not choose whose
        /// account it is, nor the role a new one gets: roleId is a fixed value of the flow, and in a
        /// flow a page starts the identity is never read "From the page" (checked with the trigger;
        /// another system calling a webhook is trusted by its key and may send it).
        /// </summary>
        private static void CheckAccountBody(JArray mappings, string path, ParseContext ctx)
        {
            foreach (var (m, i) in mappings.Select((x, i) => (x as JObject, i)))
            {
                if (m == null)
                    continue;
                var param = (string?)m["paramName"];
                if (param == "roleId" && (string?)m["typeOfValue"] is not ("static" or "categories"))
                    ctx.Error($"{path}[{i}].typeOfValue", "ROLE_NOT_FIXED", "The role a new account gets must be a fixed value of the flow (Static): never read from the page, the cart or an answer.");
                // a value given as the whole body (no paramName, or spread) can carry any field
                var wholeBody = string.IsNullOrEmpty(param) || IsTrue(m["spreadValue"]);
                if ((wholeBody || AccountIdentityFields.Contains(param!) || param == "roleId") && ReadsInput(m))
                    ctx.AccountInputUses.Add($"{path}[{i}]");
            }
        }

        /// <summary>The body fields of /Order/ChangeStatus that must be fixed values of the flow.</summary>
        private static readonly HashSet<string> OrderFixedFields = new(StringComparer.Ordinal) { "fromStatus", "toStatus", "paymentMethod", "lookupPath" };

        /// <summary>The body fields /Order/ChangeStatus reads.</summary>
        private static readonly HashSet<string> OrderFields = new(StringComparer.Ordinal)
            { "orderId", "lookupPath", "lookupValue", "paymentMethod", "fromStatus", "toStatus", "amount", "tolerance", "note" };

        /// <summary>
        /// /Order/ChangeStatus moves orders, so what decides which orders move and to what is a fixed
        /// value of the flow: the statuses, the payment method and where the order is looked up. What
        /// the caller sends only picks the one order (its id, or e.g. its SADAD bill number) and says
        /// the amount, which the server compares with the order's total. The statuses' names and the
        /// amount a paid status needs are checked by the server, which knows DCMS's statuses.
        /// </summary>
        private static void CheckOrderStatusBody(JArray mappings, string path, ParseContext ctx)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (m, i) in mappings.Select((x, i) => (x as JObject, i)))
            {
                if (m == null)
                    continue;
                var param = (string?)m["paramName"];
                if (string.IsNullOrEmpty(param) || IsTrue(m["spreadValue"]))
                {
                    ctx.Error($"{path}[{i}]", "ORDER_BODY_WHOLE", "Give /Order/ChangeStatus its fields one by one (orderId, or lookupPath and lookupValue; fromStatus, toStatus, amount, note), not as one object: the statuses must be fixed values of the flow.");
                    continue;
                }
                names.Add(param!);
                if (!OrderFields.Contains(param!))
                    ctx.Warning($"{path}[{i}].paramName", "ORDER_FIELD_UNKNOWN", $"/Order/ChangeStatus does not read \"{param}\". It reads: {string.Join(", ", OrderFields)}.");
                if (OrderFixedFields.Contains(param!) && (string?)m["typeOfValue"] != "static")
                    ctx.Error($"{path}[{i}].typeOfValue", "ORDER_FIELD_NOT_FIXED", $"\"{param}\" must be a fixed value of the flow (Static): what a caller or an answer sends never decides which orders move, or to what.");
            }
            if (!names.Contains("fromStatus") || !names.Contains("toStatus"))
                ctx.Error(path, "ORDER_STATUS_NEEDED", "/Order/ChangeStatus needs fromStatus (the only status an order is moved from) and toStatus, both fixed values, e.g. \"SadadPayment\" and \"PaymentReceived\".");
            var byId = names.Contains("orderId");
            var byLookup = names.Contains("lookupPath") || names.Contains("lookupValue");
            if (!byId && !(names.Contains("lookupPath") && names.Contains("lookupValue")))
                ctx.Error(path, "ORDER_LOOKUP_NEEDED", "Say which order: orderId, or lookupPath (where the value is in the order's details, e.g. payment.sadadNo) and lookupValue.");
            else if (byId && byLookup)
                ctx.Error(path, "ORDER_LOOKUP_TWO", "Say which order one way: orderId, or lookupPath and lookupValue, not both.");
        }

        /// <summary>The fixed (Static) value a request's mapping gives a body field, as text; null when it has none.</summary>
        public static string? FixedValueOf(FlowRequest request, string field) =>
            request.DataMapping.OfType<JObject>()
                .Where(m => (string?)m["paramName"] == field && (string?)m["typeOfValue"] == "static")
                .Select(m => m["valueToSend"] is { Type: not JTokenType.Null } v ? ((string?)v)?.Trim() : null)
                .FirstOrDefault();

        /// <summary>Whether a request's body has this field at all.</summary>
        public static bool HasField(FlowRequest request, string field) =>
            request.DataMapping.OfType<JObject>().Any(m => (string?)m["paramName"] == field);

        /// <summary>Whether a mapping (or one it is built from) reads "From the page".</summary>
        private static bool ReadsInput(JObject mapping) =>
            (string?)mapping["typeOfValue"] == "fromInput"
            || new[] { mapping["inputs"], mapping["children"] }.OfType<JArray>().SelectMany(x => x.OfType<JObject>()).Any(ReadsInput);

        /// <summary>
        /// previousResults only holds the declared outputs of earlier requests (that is all a resumed
        /// run still has), so "request.output" must name an earlier request and one of its outputs.
        /// </summary>
        private static void CheckPreviousResultReferences(IReadOnlyList<FlowGroup> groups, ParseContext ctx)
        {
            var requests = groups.SelectMany(g => g.Requests).ToDictionary(r => r.Name, StringComparer.Ordinal);
            var groupsByName = groups.ToDictionary(g => g.Name, StringComparer.Ordinal);
            var byResultKey = groups.SelectMany(g => g.Requests).Where(r => r.Loop?.ResultKey != null)
                .GroupBy(r => r.Loop!.ResultKey!, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            foreach (var reference in ctx.References)
            {
                var (path, refPath, earlier, what) = (reference.Path, reference.RefPath, reference.Earlier, reference.What);
                // a.b[0].c reads a, b, 0, c: the engine's own path rules
                var segments = JsonPath.ParsePath(refPath);
                var head = segments.Length > 0 ? segments[0] : "";

                // A mapping that does not hold (an older flow's condition, a loop's list) is said, not refused.
                void Issue(string code, string message)
                {
                    if (reference.Strict)
                        ctx.Error(path, code, message);
                    else
                        ctx.Warning(path, code, message);
                }

                // the loop's current item (currentItem, currentIndex, the alias); a condition also
                // reads the run's own data (the cart, the order, the input, the user)
                if (reference.Scope.Contains(head) || (!reference.Strict && ConditionDataNames.Contains(head)))
                    continue;

                FlowRequest? target = null;
                var rest = segments.Skip(1);
                if (requests.TryGetValue(head, out var r))
                    target = r;
                else if (byResultKey.TryGetValue(head, out var keyed))
                    target = keyed;
                else if (groupsByName.TryGetValue(head, out var g))
                {
                    // A group whose result is reshaped (transformResult) or is the list of a
                    // conditional group's answers: any path in it, once all its requests ran.
                    if (g.Transform != null || g.Requests.Any(x => x.Loop?.IsGroupSource == true))
                    {
                        var later = g.Requests.FirstOrDefault(x => !earlier.Contains(x.Name));
                        if (later != null)
                            Issue("FORWARD_REFERENCE", $"\"{later.Name}\" (group \"{g.Name}\") runs later; {what} can only read requests that ran before it.");
                        continue;
                    }
                    if (segments.Length > 1 && requests.TryGetValue(segments[1], out var gr) && gr.GroupName == g.Name)
                    {
                        target = gr;
                        rest = segments.Skip(2);
                    }
                }

                if (target == null)
                {
                    Issue("UNKNOWN_REQUEST", $"\"{refPath}\" ({what}) must start with an earlier request's name.");
                    continue;
                }
                if (!earlier.Contains(target.Name))
                {
                    Issue("FORWARD_REFERENCE", $"\"{target.Name}\" runs later; {what} can only read requests that ran before it.");
                    continue;
                }
                if (reference.Group != null && target.GroupName == reference.Group && groupsByName.TryGetValue(reference.Group, out var own) && own.Mode == FlowGroupMode.Parallel)
                {
                    Issue("PARALLEL_SIBLING", $"\"{target.Name}\" is in the same parallel group: in the browser both run at the same time, so its result is not there yet.");
                    continue;
                }

                // A request that loops (or collects every polling answer) gives a list: request.0.output
                var lists = AnswersAList(target);
                var output = (lists ? rest.SkipWhile(s => s.All(char.IsDigit)) : rest).FirstOrDefault();
                if (output == null && (lists || !reference.Strict))
                    continue; // the whole list, or (a condition) the request's values as a whole
                var available = target.Outputs.Keys.Concat(target.Guard?.Outputs.Keys ?? Enumerable.Empty<string>()).Concat(GivenUpKeys(target, lists));
                if (output == null || !available.Contains(output, StringComparer.Ordinal))
                    Issue("UNKNOWN_OUTPUT",
                        $"\"{refPath}\": request \"{target.Name}\" has no output \"{output}\". On the server, earlier results hold only the values a request declares in server.outputs ({(target.Outputs.Count == 0 ? "none yet" : string.Join(", ", target.Outputs.Keys))}).");
            }
        }

        /// <summary>
        /// What a call that failed for good keeps when the run goes on without it and it has no
        /// fallback, as the browser keeps it: { error, name }, a loop item { error, item, index }.
        /// </summary>
        private static IEnumerable<string> GivenUpKeys(FlowRequest request, bool listItem)
        {
            if (request.Fallback != null)
                return Enumerable.Empty<string>();
            if (listItem && request.LoopItemFailuresContinue)
                return new[] { "error", "item", "index" };
            return request.Critical ? Enumerable.Empty<string>() : new[] { "error", "name" };
        }

        /// <summary>A request whose result is a list: it loops, or it keeps every polling answer.</summary>
        private static bool AnswersAList(FlowRequest request) => request.Loop != null || request.Repeat?.CollectAll == true;

        /// <summary>The run's own data a condition reads besides the results (FlowRunData.ConditionData).</summary>
        private static readonly HashSet<string> ConditionDataNames = new(StringComparer.Ordinal) { "cartData", "additionalDetails", "order", "input", "user" };

        #endregion

        #region Conditions

        private enum ConditionScope
        {
            /// <summary>executeIf and conditional mappings: order, cart, earlier outputs, literals.</summary>
            Flow,

            /// <summary>Response checks and guard decisions: the response and literals.</summary>
            Response,
        }

        /// <summary>The browser's normalizeExecuteIf: executeIf.conditionalList, else the builder's conditionalList.</summary>
        private static JObject? ReadExecuteIf(JObject owner, string path, ConditionScope scope, ParseContext ctx)
        {
            var fromExecuteIf = owner["executeIf"]?["conditionalList"] as JObject;
            if (fromExecuteIf?["conditions"] is JArray { Count: > 0 })
                return ReadConditionalList(fromExecuteIf, $"{path}.executeIf.conditionalList", scope, ctx);

            if (owner["conditionalList"] is JObject cl
                && (cl["conditions"] is JArray { Count: > 0 } || !string.IsNullOrWhiteSpace((string?)cl["statement"])))
                return ReadConditionalList(cl, $"{path}.conditionalList", scope, ctx);

            return null;
        }

        private static IReadOnlyList<JObject> ReadResponseChecks(JToken? errorHandling, string path, ParseContext ctx)
        {
            var eh = MergedErrorHandling(errorHandling);
            var cfg = eh?["responseErrorChecksConfig"];
            if (cfg == null || cfg.Type == JTokenType.Null)
                return Array.Empty<JObject>();

            var list = cfg["conditionalList"] as JObject ?? cfg as JObject;
            if (list?["conditions"] is not JArray { Count: > 0 })
                return Array.Empty<JObject>();

            // A blank statement is always true: every answer would count as a refusal.
            var read = ReadConditionalList(list, $"{path}.responseErrorChecksConfig", ConditionScope.Response, ctx,
                blankStatement: "statement is blank, so the check always matches and every answer counts as failed. Write e.g. \"C1\" or \"C1 or C2\".");
            return read == null ? Array.Empty<JObject>() : new[] { read };
        }

        /// <summary>errorHandling with its inner errorHandling merged over it, as the browser reads it.</summary>
        private static JObject? MergedErrorHandling(JToken? errorHandling)
        {
            if (errorHandling is not JObject outer)
                return null;
            if (outer["errorHandling"] is not JObject inner)
                return outer;
            var merged = (JObject)outer.DeepClone();
            foreach (var p in inner.Properties())
                merged[p.Name] = p.Value.DeepClone();
            return merged;
        }

        /// <summary>
        /// The call's On error message, as the request editor writes it: what the caller reads when this
        /// call stops the run. Null when the call says nothing (the site's general sentence is used).
        /// </summary>
        private static FlowErrorMessage? ParseErrorMessage(JObject? eh)
        {
            if (eh == null)
                return null;
            var message = ((string?)eh["message"])?.Trim();
            var messagePath = ((string?)eh["responseMessagePath"])?.Trim();
            string? Text(string key) => string.IsNullOrWhiteSpace((string?)eh[key]) ? null : (string?)eh[key];
            // A message given only as a translation ("Translate the message" with its key or patterns)
            // is a message too, as on the pages.
            var translatedOnly = IsTrue(eh["translateMessage"]) && (Text("translateKey") ?? Text("messageTemplate") ?? Text("translateKeyPrefix")) != null;
            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(messagePath) && !translatedOnly)
                return null;
            return new FlowErrorMessage
            {
                Message = string.IsNullOrEmpty(message) ? null : message,
                ResponseMessagePath = string.IsNullOrEmpty(messagePath) ? null : messagePath,
                Translate = IsTrue(eh["translateMessage"]),
                TranslateKey = Text("translateKey"),
                TranslateKeyPrefix = Text("translateKeyPrefix"),
                MessageTemplate = Text("messageTemplate"),
                TranslateParams = FlowErrorMessages.ParseParams(eh["translateParams"]),
                TranslateComponent = Text("translateComponentName"),
            };
        }

        /// <summary>
        /// A group's own "On error" settings apply to the whole group in the browser (try the group
        /// again, keep going, its fallback, its message). The server keeps every call a step of its own
        /// and handles errors call by call, so only the group's response checks are used (by each of its
        /// calls); the author is told about the rest.
        /// </summary>
        private static void WarnGroupOnError(JToken? errorHandling, string path, ParseContext ctx)
        {
            var eh = MergedErrorHandling(errorHandling);
            if (eh == null)
                return;
            var unused = new List<string>();
            if (TryPositiveInt(eh["retryAttempts"], out _))
                unused.Add("Retry attempts");
            if (IsTrue(eh["continueOnError"]))
                unused.Add("Keep going on error");
            if (HasItems(eh["fallbackValueMapping"]) || JsonValueOrRaw(eh["fallbackValue"]) != null)
                unused.Add("fallback values");
            if (!string.IsNullOrWhiteSpace((string?)eh["message"]) || !string.IsNullOrWhiteSpace((string?)eh["responseMessagePath"]))
                unused.Add("the error message");
            if (IsTrue(eh["recallApi"]) || IsTrue(eh["onErrorConfig"]?["enabled"]) || HasItems(eh["onErrorActions"]?["actionList"]))
                unused.Add("on-error actions");
            if (unused.Count > 0)
                ctx.Warning($"{path}.errorHandling", "GROUP_ON_ERROR_NOT_ON_SERVER",
                    $"The group's own On error settings ({string.Join(", ", unused)}) are not used on the server: it handles errors call by call. Set them on each call's On error screen; the group's response checks are used.");
        }

        /// <summary>
        /// The request editor is the pages' own one, so it offers error and success settings written for
        /// a browser. Most mean the same on the server and are used (timeout, retries, "Keep going on
        /// error", the error message and its translation); the rest need somebody watching a screen, so
        /// they are ignored. Either way the author is told here, with the flow's other issues, instead of guessing.
        /// </summary>
        private static void WarnBrowserOnlyHandling(JObject? errorHandling, JToken? successHandling, string path, ParseContext ctx)
        {
            if (errorHandling != null)
            {
                if (IsTrue(errorHandling["showErrorToast"]))
                    ctx.Warning($"{path}.errorHandling.showErrorToast", "BROWSER_ONLY",
                        "There is no toast on the server: the error message goes to the page or system that reads the run's answer, which shows it its own way.");

                if (HasItems(errorHandling["onErrorActions"]?["actionList"]))
                    ctx.Warning($"{path}.errorHandling.onErrorActions", "BROWSER_ONLY",
                        "On-error page actions (toasts, navigation, store) need a page and are ignored on the server.");

                if (IsTrue(errorHandling["onErrorConfig"]?["enabled"]) || IsTrue(errorHandling["recallApi"]))
                    ctx.Warning($"{path}.errorHandling.onErrorConfig", "NOT_YET_ON_SERVER",
                        "\"Call another request on error\" does not run on the server yet. Use the call's Retry attempts and Retry Delay (On error screen), a guard (for a call that must never be sent twice), or another request with its own condition.");
            }

            var sh = successHandling as JObject;
            if (sh != null && (IsTrue(sh["showToast"]) || IsTrue(sh["resultsReport"]?["enabled"])))
                ctx.Warning($"{path}.successHandling", "BROWSER_ONLY",
                    "The success toast and the results report are browser settings and are ignored.");
        }

        /// <summary>
        /// A { conditions, statement } list, checked. <paramref name="blankStatement"/>: where a blank
        /// statement can only be a mistake, the error to give (else it is a warning, as in the browser).
        /// </summary>
        private static JObject? ReadConditionalList(JToken? token, string path, ConditionScope scope, ParseContext ctx, string? blankStatement = null)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;
            if (token is not JObject list)
            {
                ctx.Error(path, "BAD_CONDITIONS", "A condition list must be { \"conditions\": [ ... ], \"statement\": \"C1\" }.");
                return null;
            }

            var conditions = list["conditions"] as JArray;
            if (conditions == null || conditions.Count == 0)
            {
                if (list["conditions"] != null && list["conditions"]!.Type != JTokenType.Array)
                    ctx.Error($"{path}.conditions", "BAD_CONDITIONS", "conditions must be an array.");
                return (JObject)list.DeepClone();
            }

            var ids = new HashSet<int>();
            foreach (var (c, i) in conditions.Select((x, i) => (x, i)))
            {
                var cp = $"{path}.conditions[{i}]";
                if (c is not JObject co)
                {
                    ctx.Error(cp, "BAD_CONDITION", "A condition must be an object.");
                    continue;
                }

                if (!TryPositiveInt(co["id"], out var id))
                    ctx.Error($"{cp}.id", "BAD_CONDITION_ID", "Each condition needs a numeric \"id\" (1, 2, ...), used as C1, C2 in the statement.");
                else if (!ids.Add(id))
                    ctx.Error($"{cp}.id", "DUPLICATE_CONDITION_ID", $"Two conditions have id {id}.");

                var op = (string?)co["operator"];
                if (op == null || !ConditionOperators.Names.Contains(op))
                    ctx.Error($"{cp}.operator", "BAD_OPERATOR", $"Unknown operator \"{op}\". Use one of: {string.Join(", ", ConditionOperators.Names)}.");

                if (co["left"] != null || co["right"] != null)
                    ctx.Error(cp, "WRONG_KEYS", "Conditions use \"leftCon\" / \"rightCon\", not \"left\" / \"right\".");

                CheckSide(co["leftCon"], $"{cp}.leftCon", scope, ctx, required: true);
                CheckSide(co["rightCon"], $"{cp}.rightCon", scope, ctx, required: false);

                if (IsTrue(co["leftCon"]?["getDataObj"]?["useLeftSearchArray"]))
                {
                    var fn = (string?)co["leftCon"]?["getDataObj"]?["arrayFunction"];
                    if (fn != "some" && fn != "every" && fn != "notInArray")
                        ctx.Error($"{cp}.leftCon.getDataObj.arrayFunction", "BAD_ARRAY_FUNCTION", "arrayFunction must be \"some\", \"every\" or \"notInArray\".");
                }
            }

            var statementToken = list["statement"];
            if (statementToken == null || statementToken.Type == JTokenType.Null)
            {
                ctx.Error($"{path}.statement", "NO_STATEMENT", "statement is missing, so the list is always false. Write e.g. \"C1\" or \"C1 and C2\".");
            }
            else
            {
                var statement = (string?)statementToken ?? "";
                if (statement.Trim().Length == 0 && blankStatement != null)
                    ctx.Error($"{path}.statement", "BLANK_STATEMENT", blankStatement);
                else if (statement.Trim().Length == 0)
                    ctx.Warning($"{path}.statement", "BLANK_STATEMENT", "statement is blank, so the list is always TRUE whatever the conditions say.");
                else if (!ConditionStatement.TryCompile(statement, out var compiled, out var error))
                    ctx.Error($"{path}.statement", "BAD_STATEMENT", $"{error} Use C<id>, parentheses and lowercase and / or / not (\"&&\" and \"||\" do not work).");
                else
                    foreach (var missing in compiled!.ReferencedIds.Where(r => !ids.Contains(r)))
                        ctx.Error($"{path}.statement", "UNKNOWN_CONDITION", $"The statement uses C{missing}, but no condition has id {missing}, so it is always false.");
            }

            return (JObject)list.DeepClone();
        }

        private static void CheckSide(JToken? side, string path, ConditionScope scope, ParseContext ctx, bool required)
        {
            var getDataObj = side?["getDataObj"];
            // The condition editor writes an empty right side (getDataFrom null) for one-sided
            // operators such as IsEmpty: that is the same as no right side.
            if (!required && getDataObj is JObject empty && string.IsNullOrEmpty((string?)empty["getDataFrom"]) && empty["key"]?.Type is null or JTokenType.Null && empty["value"]?.Type is null or JTokenType.Null)
                return;
            if (getDataObj == null || getDataObj.Type == JTokenType.Null)
            {
                if (required)
                    ctx.Error(path, "NO_SIDE", "The left side needs getDataObj: { \"getDataFrom\": ..., \"key\": ... }.");
                return;
            }

            var from = (string?)getDataObj["getDataFrom"];
            if (from == "fromInput")
                ctx.InputUses.Add(($"{path}.getDataObj.key", InputName((string?)getDataObj["key"])));
            else if (from == "fromOrder")
                ctx.OrderUses.Add($"{path}.getDataObj.key");
            else if (from is "fromCart" or "fromAdditionalDetailsCart")
                ctx.CartUses.Add($"{path}.getDataObj.key");
            else if (from is "userInfo" or "userOptions")
                ctx.UserUses.Add($"{path}.getDataObj.key");
            else if (from == "fromUser")
            {
                ctx.UserUses.Add($"{path}.getDataObj.key");
                ctx.Warning($"{path}.getDataObj.getDataFrom", "OLD_SOURCE", "\"fromUser\" is the older name. It still works; read the user as page conditions do instead, with User info (e.g. username, userType) or User options (an option's name).");
            }
            // In a flow's own conditions apiRes / previousApi read the earlier results: the same
            // outputs a previousResults mapping reads (a response check reads the response instead).
            if (scope == ConditionScope.Flow && from is "apiRes" or "previousApi" && !string.IsNullOrWhiteSpace((string?)getDataObj["key"]))
                ctx.AddReference($"{path}.getDataObj.key", ((string)getDataObj["key"]!).Trim(), "the condition", strict: false);

            var allowed = scope == ConditionScope.Flow ? FlowConditionSources : ResponseConditionSources;
            if (from == null || !allowed.Contains(from))
                ctx.Error($"{path}.getDataObj.getDataFrom", "BAD_CONDITION_SOURCE",
                    $"getDataFrom \"{from}\" is not available here. Use one of: {string.Join(", ", allowed)}."
                    + (scope == ConditionScope.Response ? " (A response check reads the response itself: apiRes with a path inside it.)" : ""));

            if (IsTrue(getDataObj["useTransformResponse"]) && !string.IsNullOrWhiteSpace((string?)getDataObj["transformResponseScript"]))
                CheckScript("value", (string)getDataObj["transformResponseScript"]!, $"{path}.getDataObj.transformResponseScript", ctx);
            if (!string.IsNullOrWhiteSpace((string?)getDataObj["opsRef"]))
                ctx.Error($"{path}.getDataObj.opsRef", "NO_OPS_REF", "opsRef does not exist on the server; write the valueOps inline.");
            if (getDataObj["valueOps"] is { } ops && HasItems(ops))
                CheckValueOps(ops, $"{path}.getDataObj.valueOps", ctx);

            if (from == "arrayData" && getDataObj["arrayItems"] is JArray items)
                foreach (var (item, i) in items.Select((x, i) => (x, i)))
                    CheckSide(item, $"{path}.getDataObj.arrayItems[{i}]", scope, ctx, required: true);
        }

        #endregion

        #region Helpers

        private static void CheckValueOps(JToken ops, string path, ParseContext ctx)
        {
            foreach (var problem in ValueOps.Validate(ops))
                ctx.Error(path, "BAD_VALUE_OPS", problem);
        }

        private static bool CheckName(string? name, string path, string what, ParseContext ctx)
        {
            if (string.IsNullOrEmpty(name) || !NamePattern.IsMatch(name))
            {
                ctx.Error(path, "BAD_NAME", $"Every {what} needs a name that starts with a letter and uses only letters, digits and _ (it is the result key).");
                return false;
            }
            if (ReservedNames.Contains(name))
            {
                ctx.Error(path, "RESERVED_NAME", $"\"{name}\" is reserved (it is part of the data conditions read); pick another {what} name.");
                return false;
            }
            // Conditions read the signed-in user and the page's input under these names, so a result
            // kept under one of them is hidden from conditions (mappings still read it). Older flows
            // may use them, so it is said, not refused.
            if (name is "user" or "input")
                ctx.Warning(path, "RESERVED_NAME",
                    $"A condition that reads \"{name}\" gets {(name == "user" ? "the signed-in user" : "what the page sent")}, not this {what}'s result. Pick another {what} name so conditions can read it.");
            return true;
        }

        /// <summary>The longest name a step row keeps, and the most a request that loops may use ("name[12]" must still fit).</summary>
        private const int MaxStoredName = 100;
        private const int MaxName = 80;

        private static void CheckNameLength(string name, string path, string what, bool loops, ParseContext ctx)
        {
            if (name.Length <= MaxName)
                return;
            if (name.Length > MaxStoredName)
                ctx.Error(path, "NAME_TOO_LONG", $"\"{name[..20]}...\" is {name.Length} characters long; the server keeps names of at most {MaxStoredName}. Keep it to {MaxName}.");
            else if (loops)
                ctx.Error(path, "NAME_TOO_LONG", $"This {what} goes over a list and keeps each item as \"{name[..20]}...[1]\", which must fit in {MaxStoredName} characters: keep the name to {MaxName} (it is {name.Length}).");
            else
                ctx.Warning(path, "NAME_TOO_LONG", $"The {what} name is {name.Length} characters long. It works, but keep it to {MaxName}: a call that goes over a list must, and long names are hard to read in the run screens.");
        }

        private static bool IsTrue(JToken? token) => token?.Type == JTokenType.Boolean && (bool)token;

        private static bool HasItems(JToken? token) => token is JArray { Count: > 0 };

        private static bool TryPositiveLong(JToken? token, out long value)
        {
            value = 0;
            return token?.Type switch
            {
                JTokenType.Integer => (value = (long)token) > 0,
                JTokenType.String => long.TryParse((string?)token, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0,
                _ => false,
            };
        }

        private static bool TryPositiveInt(JToken? token, out int value)
        {
            value = 0;
            return TryPositiveLong(token, out var l) && l <= int.MaxValue && (value = (int)l) > 0;
        }

        private static bool TryIntInRange(JToken? token, int min, int max, out int value)
        {
            value = 0;
            if (token?.Type != JTokenType.Integer)
                return false;
            var l = (long)token;
            if (l < min || l > max)
                return false;
            value = (int)l;
            return true;
        }

        /// <summary>
        /// A path into the earlier results, checked once every request is known: a previousResults
        /// mapping (Strict: it must hold), a condition's key, a loop's list (said when they do not
        /// hold, as older flows may have them).
        /// </summary>
        private sealed record ResultReference(string Path, string RefPath, HashSet<string> Earlier, string? Group, HashSet<string> Scope, string What, bool Strict);

        private sealed class ParseContext
        {
            private readonly List<FlowIssue> _issues;

            public ParseContext(List<FlowIssue> issues) => _issues = issues;

            public HashSet<string> GroupNames { get; } = new(StringComparer.Ordinal);

            /// <summary>Request names seen so far, in order (so "earlier" can be checked).</summary>
            public HashSet<string> RequestNames { get; } = new(StringComparer.Ordinal);

            /// <summary>The request being read; its own mappings cannot read its result.</summary>
            public string? CurrentRequest { get; set; }

            /// <summary>The group of the request being read.</summary>
            public string? CurrentGroup { get; set; }

            /// <summary>The loop of the request being read: its mappings may read the current item.</summary>
            public FlowLoop? CurrentLoop { get; set; }

            public List<ResultReference> References { get; } = new();

            /// <summary>A path the request (or group) being read takes from the earlier results.</summary>
            public void AddReference(string path, string refPath, string what, bool strict) =>
                References.Add(new ResultReference(path, refPath, RequestNames.Where(n => n != CurrentRequest).ToHashSet(StringComparer.Ordinal),
                    CurrentGroup, LoopScope(CurrentLoop), what, strict));

            /// <summary>Loop item aliases and result keys, checked against the other names at the end.</summary>
            public List<(string Path, string Name, string What)> ExtraNames { get; } = new();

            /// <summary>Where the flow reads "From the page" (the input name is the path's first part).</summary>
            public List<(string Path, string Name)> InputUses { get; } = new();

            /// <summary>Where the flow reads "From the order".</summary>
            public List<string> OrderUses { get; } = new();

            /// <summary>Where the flow reads the cart ("From Cart", "From Additional Details Cart").</summary>
            public List<string> CartUses { get; } = new();

            /// <summary>Where the flow reads "From the signed-in user".</summary>
            public List<string> UserUses { get; } = new();

            /// <summary>Where an account operation reads whose account it is (or its role) "From the page".</summary>
            public List<string> AccountInputUses { get; } = new();

            /// <summary>Where the flow changes an order's status (never in a flow a page starts).</summary>
            public List<string> OrderOperationUses { get; } = new();

            public void Error(string path, string code, string message) => _issues.Add(FlowIssue.Error(path, code, message));

            public void Warning(string path, string code, string message) => _issues.Add(FlowIssue.Warning(path, code, message));
        }

        #endregion

        /// <summary>
        /// The attention e-mails and template of a flow that may not pass the checks any more (read
        /// as far as they can be), so the staff can still be told that it stopped starting runs.
        /// </summary>
        public static (IReadOnlyList<string> Emails, string? Template) AttentionOf(string? json)
        {
            try
            {
                var onAttention = JsonConvert.DeserializeObject<JToken>(json ?? "", ReadSettings)?["settings"]?["notify"]?["onAttention"] as JObject;
                var emails = (onAttention?["emails"] as JArray ?? new JArray())
                    .Where(e => e.Type == JTokenType.String)
                    .Select(e => ((string)e!).Trim())
                    .Where(e => MailAddress.TryCreate(e, out var parsed) && parsed.Address == e)
                    .ToList();
                var template = onAttention?["template"]?.Type == JTokenType.String ? ((string?)onAttention["template"])?.Trim() : null;
                return (emails, string.IsNullOrEmpty(template) || template.Length > 450 ? null : template);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
            {
                return (Array.Empty<string>(), null);
            }
        }

        /// <summary>
        /// How a flow starts, as one text: its trigger with all its settings (products, gateways,
        /// statuses, times, days...) and its time zone, written the same however the JSON was laid out.
        /// Two versions start the same way exactly when these are equal.
        /// </summary>
        public static string StartOf(string? json)
        {
            try
            {
                var settings = JsonConvert.DeserializeObject<JToken>(json ?? "", ReadSettings)?["settings"];
                var trigger = settings?["trigger"];
                var start = new JObject
                {
                    ["trigger"] = trigger?.Type == JTokenType.String ? new JObject { ["type"] = trigger.DeepClone() } : trigger?.DeepClone(),
                    ["timeZone"] = settings?["timeZone"]?.DeepClone(),
                };
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(PageRunRules.KeyText(start))));
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
            {
                // unreadable: like no other version
                return $"unreadable:{Guid.NewGuid():N}";
            }
        }

        /// <summary>The token names a definition uses, so the service can warn when one is not configured.</summary>
        public static IReadOnlyCollection<string> TokenNamesOf(FlowDefinition definition)
        {
            return definition.Requests
                .SelectMany(r => new[] { r.AuthToken, r.Guard?.AuthToken })
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
    }
}
