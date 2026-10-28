using System.Globalization;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The JS object model over Newtonsoft tokens: property reads (<c>obj[key]</c>),
    /// own-key order, spread, and how a JS <c>undefined</c> lives inside a container.
    /// </summary>
    /// <remarks>
    /// Undefined: a top-level JS <c>undefined</c> is a C# null. Inside a JArray or
    /// JObject, where null is not allowed, it is stored as
    /// <see cref="JValue.CreateUndefined"/> and turned back into C# null on read —
    /// so <c>{ a: undefined }</c> still has the key <c>a</c> (Object.keys sees it,
    /// JSON.stringify drops it) exactly like JS.
    ///
    /// Key order: JS enumerates integer-like keys first (ascending), then the rest
    /// in insertion order. Objects this engine builds are kept in that order; objects
    /// that come in are enumerated in it.
    /// </remarks>
    internal static class JsObjects
    {
        /// <summary>Maps an Undefined-typed token back to C# null (JS undefined).</summary>
        internal static JToken? Norm(JToken? t) => t is null || t.Type == JTokenType.Undefined ? null : t;

        /// <summary>
        /// The token to place in a container. Always a deep clone, so building a result
        /// never re-parents (mutates) a token that belongs to the input or the config.
        /// </summary>
        internal static JToken Slot(JToken? v) => v is null ? JValue.CreateUndefined() : v.DeepClone();

        /// <summary>True for a canonical array index ("0", "17"; not "01", "-0", "1.0"), below 2^32 − 1.</summary>
        internal static bool IsArrayIndex(string key, out uint index)
        {
            index = 0;
            if (key.Length == 0 || key.Length > 10) return false;
            if (key.Length > 1 && key[0] == '0') return false;
            ulong value = 0;
            foreach (char c in key)
            {
                if (c < '0' || c > '9') return false;
                value = value * 10 + (ulong)(c - '0');
            }
            if (value >= uint.MaxValue) return false;
            index = (uint)value;
            return true;
        }

        /// <summary>Own properties of an object in JS enumeration order.</summary>
        internal static IReadOnlyList<JProperty> OwnProperties(JObject o)
        {
            var props = new List<JProperty>(o.Count);
            bool seenNonIndex = false;
            bool ordered = true;
            uint lastIndex = 0;
            bool anyIndex = false;
            foreach (JProperty p in o.Properties())
            {
                props.Add(p);
                if (IsArrayIndex(p.Name, out uint idx))
                {
                    if (seenNonIndex || (anyIndex && idx < lastIndex)) ordered = false;
                    anyIndex = true;
                    lastIndex = idx;
                }
                else
                {
                    seenNonIndex = true;
                }
            }
            if (ordered) return props;

            var indexed = new List<(uint Index, JProperty Prop)>();
            var named = new List<JProperty>();
            foreach (JProperty p in props)
            {
                if (IsArrayIndex(p.Name, out uint idx)) indexed.Add((idx, p));
                else named.Add(p);
            }
            indexed.Sort((a, b) => a.Index.CompareTo(b.Index));
            var result = new List<JProperty>(props.Count);
            foreach (var (_, p) in indexed) result.Add(p);
            result.AddRange(named);
            return result;
        }

        /// <summary>
        /// <c>o[key] = value</c>: an existing key keeps its position; a new key goes
        /// where JS would enumerate it. The value is stored through <see cref="Slot"/>.
        /// </summary>
        internal static void SetOwn(JObject o, string key, JToken? value) => SetOwnRaw(o, key, Slot(value));

        /// <summary>Like <see cref="SetOwn"/> but adopts <paramref name="slot"/> as is (it must be unparented).</summary>
        internal static void SetOwnRaw(JObject o, string key, JToken slot)
        {
            JProperty? existing = o.Property(key, StringComparison.Ordinal);
            if (existing != null)
            {
                existing.Value = slot;
                return;
            }
            var prop = new JProperty(key, slot);
            if (IsArrayIndex(key, out uint idx))
            {
                foreach (JProperty p in o.Properties())
                {
                    if (!IsArrayIndex(p.Name, out uint other) || other > idx)
                    {
                        p.AddBeforeSelf(prop);
                        return;
                    }
                }
            }
            o.Add(prop);
        }

        /// <summary>
        /// Property read <c>container[key]</c> for the shapes JSON can hold: own
        /// properties of objects, indices and <c>length</c> of arrays and strings.
        /// Everything else (numbers, booleans, dates, prototype members) is undefined.
        /// </summary>
        internal static JToken? Get(JToken? container, string key)
        {
            switch (container)
            {
                case JObject o:
                    return o.TryGetValue(key, out JToken? v) ? Norm(v) : null;
                case JArray a:
                    if (key == "length") return Js.FromNumber(a.Count);
                    return IsArrayIndex(key, out uint i) && i < a.Count ? Norm(a[(int)i]) : null;
            }
            if (Js.TypeOf(container) == JsType.String)
            {
                string s = Js.StringValue(container!);
                if (key == "length") return Js.FromNumber(s.Length);
                return IsArrayIndex(key, out uint i) && i < s.Length ? new JValue(s[(int)i].ToString()) : null;
            }
            return null;
        }

        /// <summary>Object.prototype.hasOwnProperty.call(container, key).</summary>
        internal static bool HasOwn(JToken? container, string key)
        {
            switch (container)
            {
                case JObject o:
                    return o.Property(key, StringComparison.Ordinal) != null;
                case JArray a:
                    return key == "length" || (IsArrayIndex(key, out uint i) && i < a.Count);
            }
            if (Js.TypeOf(container) == JsType.String)
            {
                string s = Js.StringValue(container!);
                return key == "length" || (IsArrayIndex(key, out uint i) && i < s.Length);
            }
            return false;
        }

        /// <summary>
        /// <c>{ ...source }</c> into <paramref name="into"/> (or a new object): own
        /// enumerable properties — chars of a string, items of an array, keys of an
        /// object; nothing for null, undefined, numbers, booleans and dates.
        /// </summary>
        internal static JObject Spread(JToken? source, JObject? into = null)
        {
            JObject target = into ?? new JObject();
            switch (source)
            {
                case JObject o:
                    foreach (JProperty p in OwnProperties(o)) SetOwn(target, p.Name, Norm(p.Value));
                    return target;
                case JArray a:
                    for (int i = 0; i < a.Count; i++) SetOwn(target, i.ToString(CultureInfo.InvariantCulture), Norm(a[i]));
                    return target;
            }
            if (Js.TypeOf(source) == JsType.String)
            {
                string s = Js.StringValue(source!);
                for (int i = 0; i < s.Length; i++) SetOwn(target, i.ToString(CultureInfo.InvariantCulture), new JValue(s[i].ToString()));
            }
            return target;
        }

        /// <summary>Object.keys(v) for a non-nullish v.</summary>
        internal static List<string> Keys(JToken? v)
        {
            var keys = new List<string>();
            switch (v)
            {
                case JObject o:
                    foreach (JProperty p in OwnProperties(o)) keys.Add(p.Name);
                    return keys;
                case JArray a:
                    for (int i = 0; i < a.Count; i++) keys.Add(i.ToString(CultureInfo.InvariantCulture));
                    return keys;
            }
            if (Js.TypeOf(v) == JsType.String)
            {
                string s = Js.StringValue(v!);
                for (int i = 0; i < s.Length; i++) keys.Add(i.ToString(CultureInfo.InvariantCulture));
            }
            return keys;
        }

        /// <summary>Object.entries(v) for a non-nullish v, values normalised (undefined = null).</summary>
        internal static List<KeyValuePair<string, JToken?>> Entries(JToken? v)
        {
            var entries = new List<KeyValuePair<string, JToken?>>();
            switch (v)
            {
                case JObject o:
                    foreach (JProperty p in OwnProperties(o)) entries.Add(new KeyValuePair<string, JToken?>(p.Name, Norm(p.Value)));
                    return entries;
                case JArray a:
                    for (int i = 0; i < a.Count; i++) entries.Add(new KeyValuePair<string, JToken?>(i.ToString(CultureInfo.InvariantCulture), Norm(a[i])));
                    return entries;
            }
            if (Js.TypeOf(v) == JsType.String)
            {
                string s = Js.StringValue(v!);
                for (int i = 0; i < s.Length; i++) entries.Add(new KeyValuePair<string, JToken?>(i.ToString(CultureInfo.InvariantCulture), new JValue(s[i].ToString())));
            }
            return entries;
        }

        /// <summary>A new JArray holding <paramref name="items"/> (undefined kept as undefined).</summary>
        internal static JArray ArrayOf(IEnumerable<JToken?> items)
        {
            var arr = new JArray();
            foreach (JToken? item in items) arr.Add(Slot(item));
            return arr;
        }

        /// <summary>The items of an array, normalised (undefined = null).</summary>
        internal static List<JToken?> Items(JArray a)
        {
            var list = new List<JToken?>(a.Count);
            foreach (JToken t in a) list.Add(Norm(t));
            return list;
        }

        /// <summary>
        /// <c>container[key] = value</c> where the container is an object or array.
        /// Arrays take index keys (growing with undefined holes) and <c>length</c>;
        /// any other key on an array is a JS expando property that JSON cannot hold,
        /// so it is dropped (JSON.stringify would drop it too).
        /// </summary>
        internal static void AssignRaw(JContainer container, string key, JToken slot)
        {
            if (container is JObject o)
            {
                SetOwnRaw(o, key, slot);
                return;
            }
            var a = (JArray)container;
            if (IsArrayIndex(key, out uint idx))
            {
                if (idx > (uint)a.Count + 1_000_000u)
                {
                    // JS would make a sparse array; a dense JArray that big is not a sane server allocation.
                    throw new ArgumentOutOfRangeException(nameof(key), "Array index too far beyond the end");
                }
                while (a.Count < idx) a.Add(JValue.CreateUndefined());
                if (idx == a.Count) a.Add(slot);
                else a[(int)idx] = slot;
                return;
            }
            if (key == "length")
            {
                uint? length = JsNumber.ToArrayLength(Js.ToNumber(Norm(slot)));
                if (length == null) throw new ArgumentOutOfRangeException(nameof(key), "Invalid array length");
                if (length.Value > (uint)a.Count + 1_000_000u) throw new ArgumentOutOfRangeException(nameof(key), "Invalid array length");
                while (a.Count > length.Value) a.RemoveAt(a.Count - 1);
                while (a.Count < length.Value) a.Add(JValue.CreateUndefined());
            }
        }
    }
}
