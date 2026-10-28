using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// The rules of a page flow's trigger that need no database: which inputs are kept and how they
    /// are checked, and which run an ask is about (its run key).
    /// </summary>
    public static class PageRunRules
    {
        public const int MaxInputChars = 16 * 1024;

        /// <summary>The inputs the flow accepts, checked against their rules; anything else is dropped.</summary>
        public static (JObject? Clean, CallerText? Problem) CleanInput(PageTriggerSettings page, JToken? input) => CleanInput(page.Inputs, input);

        /// <summary>The inputs a flow accepts (a page's or a webhook's), checked against their rules; anything else is dropped.</summary>
        public static (JObject? Clean, CallerText? Problem) CleanInput(IReadOnlyDictionary<string, FlowInputRule> inputs, JToken? input)
        {
            if (input == null || input.Type == JTokenType.Null)
                input = new JObject();
            if (input is not JObject sent)
                return (null, new CallerText(FlowTexts.InputNotObject));
            if (sent.ToString(Formatting.None).Length > MaxInputChars)
                return (null, new CallerText(FlowTexts.InputTooLarge, MaxInputChars / 1024));

            var clean = new JObject();
            foreach (var (name, rule) in inputs)
            {
                var (value, problem) = CleanValue(name, rule, sent[name]);
                if (problem != null)
                    return (null, problem);
                if (value != null)
                    clean[name] = value;
            }
            return (clean, null);
        }

        /// <summary>One input as its rule turns it (null when it was not sent), or why it is refused.</summary>
        public static (JToken? Value, CallerText? Problem) CleanValue(string name, FlowInputRule rule, JToken? value)
        {
            var empty = value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.String && ((string)value!).Trim().Length == 0);
            if (empty)
                return rule.Required ? (null, new CallerText(FlowTexts.InputRequired, name)) : (null, null);
            // 1e400 is no number JSON can carry (nor a run key)
            if (HasNonFiniteNumber(value!))
                return (null, new CallerText(FlowTexts.InputNumber, name));

            switch (rule.Type)
            {
                case "text":
                    if (value!.Type is JTokenType.Object or JTokenType.Array)
                        return (null, new CallerText(FlowTexts.InputText, name));
                    value = new JValue(Js.ToJsString(value));
                    break;
                case "number":
                    var number = Js.ToNumber(value);
                    if (!double.IsFinite(number) || value!.Type is JTokenType.Object or JTokenType.Array or JTokenType.Boolean)
                        return (null, new CallerText(FlowTexts.InputNumber, name));
                    value = Js.FromNumber(number);
                    break;
                case "boolean":
                    if (value!.Type == JTokenType.Boolean)
                        break;
                    var text = Js.ToJsString(value).Trim().ToLowerInvariant();
                    if (text is not ("true" or "false"))
                        return (null, new CallerText(FlowTexts.InputBoolean, name));
                    value = new JValue(text == "true");
                    break;
            }

            if (rule.MaxLength is int max && value!.Type == JTokenType.String && ((string)value!).Length > max)
                return (null, new CallerText(FlowTexts.InputTooLong, name, max));
            return (value!.DeepClone(), null);
        }

        /// <summary>
        /// The key of the run an ask is about. Always the visitor's own: their journey (cart), the
        /// visitor (user), or the visitor and the value of onceInput (input), so two customers who send
        /// the same value each get their own run. Null when no key exists: every ask starts a new run
        /// ("always"), or the cart or value it is keyed on is missing.
        /// </summary>
        public static string? RunKey(long siteId, string flowName, PageTriggerSettings page, long userId, long? cartId, JToken? onceValue) =>
            page.Once switch
            {
                PageTriggerSettings.OncePerCart => cartId == null ? null : Key($"page:{siteId}:{flowName}:cart:{cartId}"),
                PageTriggerSettings.OncePerUser => Key($"page:{siteId}:{flowName}:user:{userId}"),
                PageTriggerSettings.OncePerInput => onceValue == null || onceValue.Type == JTokenType.Null ? null : Key($"page:{siteId}:{flowName}:input:{userId}:{Hash(onceValue)}"),
                _ => null,
            };

        /// <summary>The key of a run that answers one ask only ("always").</summary>
        public static string NewRunKey(long siteId, string flowName) => Key($"page:{siteId}:{flowName}:{Guid.NewGuid():N}");

        /// <summary>
        /// How long one ask waits for the result: what the caller asks for (the time it has left), never
        /// longer than the flow lets it wait; the flow's own time when it does not say.
        /// </summary>
        public static int WaitFor(int? asked, int flowWaitSec) => Math.Clamp(asked ?? flowWaitSec, 0, Math.Max(0, flowWaitSec));

        /// <summary>The key a stopped run keeps once a new run has taken its own ("afterFailure: newRun").</summary>
        public static string ReplacedKey(long runId) => Key($"replaced:{runId}");

        /// <summary>A webhook's run key: one run per value of onceInput, else a new run each call.</summary>
        /// <summary>
        /// A webhook run's key: per value of onceInput; else, for a signed request, per request (the same
        /// signed request sent again, by a retry or a copy within the time allowed, is the same run);
        /// else a new run every call.
        /// </summary>
        public static string WebhookRunKey(long siteId, string flowName, JToken? onceValue, string? requestId = null) =>
            onceValue != null && onceValue.Type != JTokenType.Null
                ? Key($"webhook:{siteId}:{flowName}:input:{Hash(onceValue)}")
                : !string.IsNullOrEmpty(requestId)
                    ? Key($"webhook:{siteId}:{flowName}:request:{requestId}")
                    : Key($"webhook:{siteId}:{flowName}:{Guid.NewGuid():N}");

        /// <summary>What a flow stores of its webhook key: SHA-256, as 64 lower-case hex digits.</summary>
        public static string HashKey(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

        /// <summary>Whether a key sent with a call is the flow's, compared in constant time.</summary>
        public static bool KeyMatches(string? key, string? keyHash)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(keyHash) || keyHash.Length != 64)
                return false;
            var sent = Encoding.ASCII.GetBytes(HashKey(key));
            var kept = Encoding.ASCII.GetBytes(keyHash.ToLowerInvariant());
            return CryptographicOperations.FixedTimeEquals(sent, kept);
        }

        /// <summary>A run key, written the same on every server whatever its culture.</summary>
        private static string Key(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

        private static string Hash(JToken value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(KeyText(value)))).ToLowerInvariant()[..32];

        /// <summary>
        /// A value as its run key holds it: text trimmed and in one Unicode form (NFC), so the same value
        /// typed or pasted another way is the same run; a number or true / false as JavaScript writes it;
        /// a list or an object as JSON with its keys in order (an object once read as "[object Object]",
        /// which made every object the same run). A plain value keys as it always did.
        /// </summary>
        internal static string KeyText(JToken value)
        {
            if (value is JContainer)
            {
                var text = new StringBuilder();
                WriteCanonical(value, text);
                return text.ToString();
            }
            return value.Type == JTokenType.String ? Text((string)value!) : Js.ToJsString(value);
        }

        private static string Text(string text) => text.Trim().Normalize(NormalizationForm.FormC);

        private static void WriteCanonical(JToken value, StringBuilder text)
        {
            switch (value)
            {
                case JObject o:
                    text.Append('{');
                    var first = true;
                    foreach (var p in o.Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        if (!first)
                            text.Append(',');
                        first = false;
                        text.Append(JsonConvert.ToString(p.Name)).Append(':');
                        WriteCanonical(p.Value, text);
                    }
                    text.Append('}');
                    break;
                case JArray a:
                    text.Append('[');
                    for (var i = 0; i < a.Count; i++)
                    {
                        if (i > 0)
                            text.Append(',');
                        WriteCanonical(a[i], text);
                    }
                    text.Append(']');
                    break;
                default:
                    text.Append(value.Type switch
                    {
                        JTokenType.String => JsonConvert.ToString(Text((string)value!)),
                        JTokenType.Integer or JTokenType.Float or JTokenType.Boolean => Js.ToJsString(value),
                        _ => "null",
                    });
                    break;
            }
        }

        private static bool HasNonFiniteNumber(JToken value) =>
            value is JContainer container
                ? container.DescendantsAndSelf().Any(t => t.Type == JTokenType.Float && t is JValue { Value: double d } && !double.IsFinite(d))
                : value.Type == JTokenType.Float && value is JValue { Value: double v } && !double.IsFinite(v);
    }
}
