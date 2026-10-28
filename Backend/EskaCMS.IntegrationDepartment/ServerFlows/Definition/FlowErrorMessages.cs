using System.Text;
using System.Text.RegularExpressions;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// A call's "On error" message, worked out the way the pages work it out
    /// (dynamic-api-caller.service.ts resolveErrorMessage, resolveTranslation and their helpers), so
    /// one setting means the same thing on a page and in a flow:
    ///  - the other side's own words at responseMessagePath, else the configured message. The pages
    ///    then fall back to whatever message the answer carries; a flow gives the site's general
    ///    sentence instead, so a customer never reads an API's raw error the author did not choose;
    ///  - with "Translate the message": the key to look up (a matching message pattern, the
    ///    translation key, the key prefix + the message, or the message itself) and the values for
    ///    its {placeholders}.
    /// </summary>
    public static class FlowErrorMessages
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);
        private static readonly Regex Placeholder = new(@"\{([A-Za-z0-9_]+)\}", RegexOptions.CultureInvariant, RegexTimeout);
        private static readonly JsonSerializerSettings ReadSettings = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 64 };

        /// <summary>Where answers usually say what went wrong (the pages' envelope fields and DCMS's own).</summary>
        private static readonly string[] UsualMessagePaths =
            { "errorMessage", "ErrorMessage", "ResponseMessage", "message", "Message", "Status.Reason", "title", "detail", "error", "description" };

        /// <summary>The message before any translation, or null when the call says nothing that applies.</summary>
        public static string? Message(FlowErrorMessage settings, JToken? answer) =>
            AtPath(answer, settings.ResponseMessagePath)
            ?? (string.IsNullOrWhiteSpace(settings.Message) ? null : settings.Message!.Trim());

        /// <summary>
        /// The other side's own words in an answer: at <paramref name="path"/> when it says something
        /// there, else in the usual message fields. Null when the answer has none.
        /// </summary>
        public static string? AnswerWords(JToken? answer, string? path = null) =>
            AtPath(answer, path) ?? UsualMessagePaths.Select(p => AtPath(answer, p)).FirstOrDefault(m => m != null);

        /// <summary>
        /// Whether a Translations key is one the flow's author wrote (the key, a message pattern, the
        /// message, or the prefix with the message), not one made of what the other side answered.
        /// </summary>
        public static bool IsAuthorKey(FlowErrorMessage settings, string lookup)
        {
            var message = settings.Message?.Trim();
            return lookup == settings.TranslateKey?.Trim()
                || (!string.IsNullOrEmpty(message) && (lookup == message || lookup == settings.TranslateKeyPrefix + message))
                || (settings.MessageTemplate?.Split('\n').Any(line => line.Trim().Length > 0 && line.Trim() == lookup) ?? false);
        }

        /// <summary>The first sentence found at <paramref name="path"/> in the answer (or the items of a list answer).</summary>
        private static string? AtPath(JToken? answer, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            foreach (var body in Candidates(answer))
            {
                var value = JsonPath.Get(body, path);
                if (IsMessage(value))
                    return Js.ToJsString(value).Trim();
            }
            return null;
        }

        /// <summary>The Translations key for <paramref name="message"/> and the values its {placeholders} take.</summary>
        public static (string Lookup, IReadOnlyDictionary<string, string>? Values) Translation(FlowErrorMessage settings, string message, JToken? answer)
        {
            var key = settings.TranslateKey?.Trim();
            if (MatchPattern(settings.MessageTemplate, message) is { } matched)
                return (string.IsNullOrEmpty(key) ? matched.Key : key, matched.Values);

            if (settings.TranslateKey != null && settings.TranslateKey.Contains('{') && ParamsFrom(settings.TranslateKey, message) is { } inline)
                return (settings.TranslateKey, inline);

            var lookup = !string.IsNullOrEmpty(key) ? key
                : !string.IsNullOrEmpty(settings.TranslateKeyPrefix) ? settings.TranslateKeyPrefix + message
                : message;
            return (lookup, settings.TranslateParams == null ? null : ParamValues(settings.TranslateParams, answer));
        }

        /// <summary>{name} → value, in a translated sentence.</summary>
        public static string Fill(string template, IReadOnlyDictionary<string, string>? values)
        {
            if (values == null || !template.Contains('{'))
                return template;
            var text = template;
            foreach (var (name, value) in values)
                text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
            return text;
        }

        /// <summary>
        /// translateParams as the request editor writes it: an object, a JSON object as text, or
        /// "name: path" lines. Null when it is none of them.
        /// </summary>
        public static IReadOnlyDictionary<string, string>? ParseParams(JToken? raw)
        {
            if (raw is JObject obj)
                return obj.Properties().ToDictionary(p => p.Name, p => Js.ToJsString(p.Value), StringComparer.Ordinal);
            if (raw?.Type != JTokenType.String)
                return null;
            var input = ((string?)raw ?? "").Trim();
            if (input.Length == 0)
                return null;
            try
            {
                if (JsonConvert.DeserializeObject<JToken>(input, ReadSettings) is JObject parsed)
                    return parsed.Properties().ToDictionary(p => p.Name, p => Js.ToJsString(p.Value), StringComparer.Ordinal);
            }
            catch (JsonException)
            {
                // "name: path" lines
            }
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in input.Split('\n'))
            {
                var trimmed = line.Trim();
                var sep = trimmed.IndexOf(':');
                if (sep <= 0)
                    continue;
                var name = trimmed[..sep].Trim();
                if (name.Length > 0)
                    result[name] = trimmed[(sep + 1)..].Trim();
            }
            return result.Count > 0 ? result : null;
        }

        /// <summary>The first message pattern (one per line) the message matches: the pattern is the key.</summary>
        private static (string Key, IReadOnlyDictionary<string, string>? Values)? MatchPattern(string? templates, string message)
        {
            if (string.IsNullOrWhiteSpace(templates))
                return null;
            foreach (var line in templates.Split('\n'))
            {
                var pattern = line.Trim();
                if (pattern.Length == 0)
                    continue;
                if (ParamsFrom(pattern, message) is { } values)
                    return (pattern, values);
                // a pattern without {placeholders} matches by its exact text
                if (!pattern.Contains('{') && pattern == message.Trim())
                    return (pattern, null);
            }
            return null;
        }

        /// <summary>
        /// The {placeholder} values a sentence carries, by matching it against a pattern
        /// ("Quotation {id} not valid" and "Quotation 77 not valid" give { id: "77" }); null when it does not match.
        /// </summary>
        public static IReadOnlyDictionary<string, string>? ParamsFrom(string template, string message)
        {
            var names = new List<string>();
            var pattern = new StringBuilder();
            var last = 0;
            foreach (Match m in Placeholder.Matches(template))
            {
                pattern.Append(Regex.Escape(template[last..m.Index])).Append("(.+?)");
                names.Add(m.Groups[1].Value);
                last = m.Index + m.Length;
            }
            if (names.Count == 0)
                return null;
            pattern.Append(Regex.Escape(template[last..]));
            try
            {
                // surrounding spaces and a full stop at the end do not matter
                var match = Regex.Match(message.Trim(), $@"^\s*{pattern}\.?\s*$", RegexOptions.CultureInvariant, RegexTimeout);
                if (!match.Success)
                    return null;
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var i = 0; i < names.Count; i++)
                    values[names[i]] = match.Groups[i + 1].Value;
                return values;
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }

        /// <summary>Each value read from the answer by its path; a path that reads nothing is the text itself.</summary>
        private static IReadOnlyDictionary<string, string> ParamValues(IReadOnlyDictionary<string, string> paths, JToken? answer)
        {
            var bodies = Candidates(answer).ToList();
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, pathOrText) in paths)
            {
                JToken? value = null;
                foreach (var body in bodies)
                {
                    var v = JsonPath.Get(body, pathOrText);
                    if (!Js.IsNil(v))
                    {
                        value = v;
                        break;
                    }
                }
                values[name] = Js.IsNil(value) ? pathOrText : Js.ToJsString(value);
            }
            return values;
        }

        /// <summary>The answer, read as JSON when it is JSON text, and the items of a list answer.</summary>
        private static IEnumerable<JToken> Candidates(JToken? answer)
        {
            if (answer == null || answer.Type == JTokenType.Null)
                yield break;
            var body = Coerce(answer);
            yield return body;
            if (body is JArray items)
                foreach (var item in items.Where(i => i.Type != JTokenType.Null))
                    yield return item;
        }

        private static JToken Coerce(JToken body)
        {
            if (body.Type != JTokenType.String)
                return body;
            var text = (body is JValue { Value: string s } ? s : "").Trim();
            if (text.StartsWith('(') && text.EndsWith(')'))
                text = text[1..^1].Trim();
            if (text.Length == 0 || (text[0] != '{' && text[0] != '['))
                return body;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(text, ReadSettings) ?? body;
            }
            catch (JsonException)
            {
                return body;
            }
        }

        /// <summary>A value worth showing: not missing, not an object or a list, not blank.</summary>
        private static bool IsMessage(JToken? value) =>
            !Js.IsNil(value) && value!.Type is not (JTokenType.Object or JTokenType.Array) && Js.ToJsString(value).Trim().Length > 0;
    }
}
