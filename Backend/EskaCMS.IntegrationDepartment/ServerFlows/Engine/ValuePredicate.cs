using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// value-ops.ts <c>testPredicate</c> — the small declarative test behind
    /// <c>filter</c>, <c>find</c>, <c>switch</c> and <c>when</c> — and <c>readPath</c>.
    /// </summary>
    internal static class ValuePredicate
    {
        /// <summary>
        /// value-ops.ts <c>readPath</c>: dot segments only (no brackets), and a string met
        /// before a segment is JSON-parsed first (unparseable → undefined). Different from
        /// <see cref="JsonPath.Get"/>, which parses after each step and only <c>{…}</c>/<c>[…]</c>.
        /// A falsy path returns the source.
        /// </summary>
        internal static JToken? ReadPath(JToken? source, JToken? path)
        {
            if (!Js.Truthy(path)) return source;
            JToken? current = source;
            foreach (string key in Js.ToJsString(path).Split('.'))
            {
                if (Js.IsNil(current)) return null;
                if (Js.TypeOf(current) == JsType.String)
                {
                    if (!JsJson.TryParse(Js.StringValue(current!), out JToken? parsed)) return null;
                    // JSON.parse("null") gives null, and null[key] is a TypeError in JS.
                    if (Js.TypeOf(parsed) == JsType.Null) throw new InvalidOperationException("Cannot read properties of null (reading '" + key + "')");
                    current = parsed;
                }
                current = JsObjects.Get(current, key);
            }
            return current;
        }

        /// <summary>testPredicate(item, predicate).</summary>
        internal static bool Test(JToken? item, JToken? predicate, TimeZoneInfo zone)
        {
            if (!Js.Truthy(predicate)) return !Js.IsEmpty(item);
            JsType type = Js.TypeOf(predicate);
            if (type == JsType.String || type == JsType.Number || type == JsType.Boolean)
            {
                // `'equals' in "text"` is a TypeError in JS.
                throw new InvalidOperationException("Cannot use 'in' operator on a " + type.ToString().ToLowerInvariant() + " predicate");
            }
            if (!(predicate is JObject p))
            {
                // An array or date predicate has none of the keys: only the fallback test applies.
                return !Js.IsEmpty(item);
            }

            JToken? subject = ReadPath(item, JsObjects.Get(p, "path"));

            if (p.ContainsKey("equals")) return Js.StrictEquals(subject, JsObjects.Get(p, "equals"));
            if (p.ContainsKey("notEquals")) return !Js.StrictEquals(subject, JsObjects.Get(p, "notEquals"));
            if (p.ContainsKey("greaterThan")) return Js.ToNumber(subject) > Js.ToNumber(JsObjects.Get(p, "greaterThan"));
            if (p.ContainsKey("lessThan")) return Js.ToNumber(subject) < Js.ToNumber(JsObjects.Get(p, "lessThan"));
            if (p.ContainsKey("in")) return InList(JsObjects.Get(p, "in"), subject, zone);
            if (p.ContainsKey("contains"))
            {
                JToken? needle = JsObjects.Get(p, "contains");
                if (subject is JArray arr) return IndexOfStrict(arr, needle) > -1;
                return ToStringOrEmpty(subject, zone).Contains(Js.ToJsString(needle, zone), StringComparison.Ordinal);
            }
            if (p.ContainsKey("matches"))
            {
                try
                {
                    return JsRegex.Get(Js.ToJsString(JsObjects.Get(p, "matches"), zone)).IsMatch(ToStringOrEmpty(subject, zone));
                }
                catch (ArgumentException)
                {
                    return false; // the TS catches a bad pattern
                }
                catch (RegexMatchTimeoutException)
                {
                    return false; // treated like a bad pattern
                }
            }
            if (p.ContainsKey("exists")) return Js.Truthy(JsObjects.Get(p, "exists")) ? !Js.IsEmpty(subject) : Js.IsEmpty(subject);
            return !Js.IsEmpty(subject);
        }

        /// <summary><c>String(subject ?? '')</c>.</summary>
        private static string ToStringOrEmpty(JToken? subject, TimeZoneInfo zone) => Js.IsNil(subject) ? string.Empty : Js.ToJsString(subject, zone);

        /// <summary><c>(predicate.in ?? []).indexOf(subject) &gt; -1</c>.</summary>
        private static bool InList(JToken? list, JToken? subject, TimeZoneInfo zone)
        {
            if (Js.IsNil(list)) return false;
            if (list is JArray arr) return IndexOfStrict(arr, subject) > -1;
            if (Js.TypeOf(list) == JsType.String)
            {
                // String.prototype.indexOf: a substring search for String(subject).
                return Js.StringValue(list!).Contains(Js.ToJsString(subject, zone), StringComparison.Ordinal);
            }
            throw new InvalidOperationException("predicate.in.indexOf is not a function");
        }

        /// <summary>Array.prototype.indexOf (strict equality; NaN is never found).</summary>
        internal static int IndexOfStrict(JArray arr, JToken? value)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (Js.StrictEquals(JsObjects.Norm(arr[i]), value)) return i;
            }
            return -1;
        }
    }
}
