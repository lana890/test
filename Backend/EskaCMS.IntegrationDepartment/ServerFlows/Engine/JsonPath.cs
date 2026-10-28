using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// Path reads and writes with the runtime's rules: <see cref="Get"/> is
    /// dynamic-api-caller.service.ts <c>getNestedValue</c> (+ <c>parsePathWithSpaces</c>),
    /// <see cref="Set"/> is value-ops.ts <c>writePath</c>.
    /// </summary>
    public static class JsonPath
    {
        private const int MaxCachedPaths = 4096;
        private static readonly ConcurrentDictionary<string, string[]> PathCache = new ConcurrentDictionary<string, string[]>(StringComparer.Ordinal);

        /// <summary>
        /// <c>getNestedValue(root, path)</c>. An empty or null path returns
        /// <paramref name="root"/>; a falsy root (undefined, null, "", 0, false, NaN)
        /// returns undefined. Segments are split on dots and brackets: <c>a.b[0]</c>,
        /// <c>a[key with spaces].b</c>. Bracket text is taken literally — quotes are
        /// NOT stripped, so <c>a["x"]</c> reads the key <c>"x"</c> including the quotes,
        /// exactly as the TS does. A whitespace-only path is a key, not "no path".
        /// After each step, a string value longer than one character that starts with
        /// <c>{</c> or <c>[</c> is JSON-parsed (kept as is when it is not valid JSON)
        /// so the walk continues through stringified JSON.
        /// </summary>
        public static JToken? Get(JToken? root, string? path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            if (!Js.Truthy(root)) return null;

            JToken? result = root;
            foreach (string key in ParsePath(path))
            {
                if (Js.IsNil(result)) return null;
                result = JsObjects.Get(result, key);
                if (Js.TypeOf(result) == JsType.String)
                {
                    string s = Js.StringValue(result!);
                    if (s.Length > 1 && (s[0] == '{' || s[0] == '[') && JsJson.TryParse(s, out JToken? parsed))
                    {
                        result = parsed;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// value-ops.ts <c>writePath(root, path, value)</c>: returns a new root with
        /// <paramref name="value"/> written at the dot path (no brackets here — the TS
        /// splits on '.' only). The input is never mutated: the root is copied (an array
        /// stays an array; anything else becomes an object via spread, so a string root
        /// becomes its characters), each intermediate is a copy or a new object, and a
        /// missing or nullish intermediate becomes <c>{}</c>. A C# null value writes JS
        /// undefined (the key exists, JSON.stringify drops it).
        /// </summary>
        /// <remarks>
        /// Unlike the TS's shallow spreads, untouched siblings are deep copies, so object
        /// identity (<c>===</c>) with the input is not preserved. A non-index key written
        /// into an array is a JS expando property JSON cannot carry and is dropped.
        /// </remarks>
        public static JToken Set(JToken? root, string path, JToken? value)
        {
            string[] keys = path.Split('.');
            JContainer output = root is JArray arr ? (JContainer)JsObjects.ArrayOf(JsObjects.Items(arr)) : JsObjects.Spread(root);
            JContainer cursor = output;
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (i == keys.Length - 1)
                {
                    JsObjects.AssignRaw(cursor, key, JsObjects.Slot(value));
                    break;
                }
                JToken? existing = JsObjects.Get(cursor, key);
                JObject next = Js.IsNil(existing) ? new JObject() : JsObjects.Spread(existing);
                JsObjects.AssignRaw(cursor, key, next);
                // When the assignment was dropped (a non-index key on an array) the
                // writes below go into an orphan object, as they do in JS.
                cursor = next;
            }
            return output;
        }

        /// <summary>parsePathWithSpaces, cached like the TS caches it.</summary>
        internal static string[] ParsePath(string path)
        {
            if (PathCache.TryGetValue(path, out string[]? cached)) return cached;

            var keys = new List<string>();
            var current = new StringBuilder();
            int depth = 0;
            foreach (char ch in path)
            {
                if (ch == '[')
                {
                    if (current.Length > 0 && depth == 0)
                    {
                        keys.Add(current.ToString());
                        current.Clear();
                    }
                    depth++;
                    if (depth > 1) current.Append(ch);
                }
                else if (ch == ']')
                {
                    depth--;
                    if (depth > 0)
                    {
                        current.Append(ch);
                    }
                    else if (current.Length > 0)
                    {
                        keys.Add(current.ToString());
                        current.Clear();
                    }
                }
                else if (ch == '.' && depth == 0)
                {
                    if (current.Length > 0)
                    {
                        keys.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            if (current.Length > 0) keys.Add(current.ToString());

            string[] result = keys.ToArray();
            if (PathCache.Count >= MaxCachedPaths) PathCache.Clear();
            PathCache[path] = result;
            return result;
        }
    }
}
