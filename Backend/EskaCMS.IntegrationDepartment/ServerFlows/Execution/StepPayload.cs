using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>
    /// What a step keeps of the body it sent and the answer it got, for the admin page: secrets
    /// masked, size capped. The values themselves (ids, names, amounts) stay readable on purpose -
    /// they are what an admin checks and corrects before sending a request again.
    /// </summary>
    public static class StepPayload
    {
        /// <summary>Kept per body or answer; a longer one is cut (and is then no longer valid JSON).</summary>
        public const int MaxLength = 256 * 1024;

        public const string Masked = "***";

        /// <summary>
        /// What makes a key a secret's, anywhere in it once its separators are dropped
        /// ("x-api-key", "CoreToken", "otpCode", "Set-Cookie")...
        /// </summary>
        private static readonly string[] SecretParts =
        {
            "password", "passwd", "passcode", "secret", "token", "cookie", "authorization", "apikey", "privatekey", "cardnumber", "otpcode", "pincode",
        };

        /// <summary>...and short ones that are a secret only as a word of their own ("pin", "cvv2", "jwtValue"; not "Spin" or "shipping").</summary>
        private static readonly HashSet<string> SecretWords = new(StringComparer.OrdinalIgnoreCase) { "pwd", "pin", "pan", "otp", "cvv", "cvc", "jwt" };

        private const int MaxCachedKeys = 4096;
        private static readonly ConcurrentDictionary<string, bool> SecretKeys = new(StringComparer.Ordinal);

        /// <summary>"Remote server returned (body)": the invoker's message for an answer that was not 2xx.</summary>
        private static readonly Regex RemoteError = new(
            @"Remote server returned \((?:\s*StatusCode:\s*\d+\s*Response:\s*)?(?<body>[\s\S]*)\)\s*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// The JSON text to keep, or null when there is nothing. Secrets are masked, and so are the
        /// flow's hidden values (settings.privacy.hide), whatever their type.
        /// </summary>
        public static string? Store(JToken? value, IReadOnlyCollection<string>? hidden = null)
        {
            if (value == null)
                return null;
            var copy = value.DeepClone();
            Mask(copy, hidden);
            var text = copy.ToString(Formatting.None);
            return text.Length <= MaxLength ? text : text[..MaxLength] + "...";
        }

        /// <summary>A copy for the Admin's screens with the secrets and the flow's hidden values masked.</summary>
        public static JToken? Hide(JToken? value, IReadOnlyCollection<string>? hidden)
        {
            if (value == null)
                return null;
            var copy = value.DeepClone();
            Mask(copy, hidden);
            return copy;
        }

        /// <summary>
        /// Puts the real value back where an edited body still holds a hidden one ("***") that the
        /// admin left as it was, when the original has a value at the same place.
        /// </summary>
        public static JToken Restore(JToken edited, JToken? original)
        {
            if (original == null)
                return edited;
            if (edited is JValue { Type: JTokenType.String } v && (string?)v == Masked && original.Type is not (JTokenType.Null or JTokenType.Undefined))
                return original.DeepClone();
            if (edited is JObject eo && original is JObject oo)
            {
                foreach (var p in eo.Properties().ToList())
                    p.Value = Restore(p.Value, oo[p.Name]);
            }
            else if (edited is JArray ea && original is JArray oa)
            {
                for (var i = 0; i < ea.Count && i < oa.Count; i++)
                    ea[i] = Restore(ea[i], oa[i]);
            }
            return edited;
        }

        /// <summary>
        /// A text cut to fit a column of <paramref name="max"/> characters, ending with "..." when it
        /// was cut (never longer than max, never half a character).
        /// </summary>
        public static string? Fit(string? text, int max)
        {
            if (text == null || text.Length <= max)
                return text;
            var keep = Math.Max(0, max - 3);
            if (keep > 0 && char.IsHighSurrogate(text[keep - 1]))
                keep--;
            return text[..keep] + "...";
        }

        /// <summary>
        /// An error text safe to keep, log and e-mail: when it is the invoker's "Remote server
        /// returned (...)", the other side's answer in it is masked as a kept answer is (secrets and
        /// the flow's hidden values). Any other text is returned as it is.
        /// </summary>
        public static string? SafeError(string? message, IReadOnlyCollection<string>? hidden)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            var match = RemoteError.Match(message);
            if (!match.Success || FromRemoteError(message) is not { } answer || answer.Type == JTokenType.String)
                return message;
            var body = match.Groups["body"];
            return message[..body.Index] + Store(answer, hidden) + message[(body.Index + body.Length)..];
        }

        /// <summary>
        /// The other side's answer inside an invoker error ("Remote server returned (...)"): as JSON
        /// when it is JSON, else as text. Null when the message is not one of those.
        /// </summary>
        public static JToken? FromRemoteError(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            var match = RemoteError.Match(message);
            if (!match.Success)
                return null;
            var body = match.Groups["body"].Value.Trim();
            if (body.Length == 0)
                return null;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(body, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            }
            catch (JsonException)
            {
                return new JValue(body);
            }
        }

        /// <summary>Reads a kept body back: JSON when it is JSON, else the text itself.</summary>
        public static JToken? Read(string? stored)
        {
            if (string.IsNullOrEmpty(stored))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<JToken>(stored, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            }
            catch (JsonException)
            {
                return new JValue(stored);
            }
        }

        private static void Mask(JToken token, IReadOnlyCollection<string>? hidden)
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties().ToList())
                {
                    var isHidden = hidden != null && hidden.Count > 0 && hidden.Contains(property.Name, StringComparer.OrdinalIgnoreCase);
                    if (isHidden && property.Value.Type is not (JTokenType.Null or JTokenType.Undefined))
                        property.Value = Masked;
                    // a secret's list too ("cookies": [...]); an object is looked into
                    else if (property.Value.Type is not (JTokenType.Null or JTokenType.Undefined or JTokenType.Object) && IsSecretKey(property.Name))
                        property.Value = Masked;
                    else
                        Mask(property.Value, hidden);
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                    Mask(item, hidden);
            }
        }

        /// <summary>Whether the values under this key are secrets (see <see cref="SecretParts"/> and <see cref="SecretWords"/>).</summary>
        internal static bool IsSecretKey(string key)
        {
            if (SecretKeys.TryGetValue(key, out var known))
                return known;
            var joined = new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            var secret = SecretParts.Any(p => joined.Contains(p, StringComparison.Ordinal)) || WordsOf(key).Any(SecretWords.Contains);
            if (SecretKeys.Count >= MaxCachedKeys)
                SecretKeys.Clear();
            SecretKeys[key] = secret;
            return secret;
        }

        /// <summary>The words of a key: "otpCode" → otp, Code; "APIKey" → API, Key; "cvv2" → cvv, 2; "card_pin" → card, pin.</summary>
        private static IEnumerable<string> WordsOf(string key)
        {
            var start = -1;
            for (var i = 0; i <= key.Length; i++)
            {
                var c = i < key.Length ? key[i] : ' ';
                if (!char.IsLetterOrDigit(c))
                {
                    if (start >= 0)
                        yield return key[start..i];
                    start = -1;
                    continue;
                }
                if (start >= 0)
                {
                    var p = key[i - 1];
                    var boundary = (char.IsLower(p) && char.IsUpper(c))
                        || (char.IsLetter(p) != char.IsLetter(c))
                        || (char.IsUpper(p) && char.IsUpper(c) && i + 1 < key.Length && char.IsLower(key[i + 1]));
                    if (boundary)
                    {
                        yield return key[start..i];
                        start = i;
                    }
                }
                else
                    start = i;
            }
        }
    }
}
