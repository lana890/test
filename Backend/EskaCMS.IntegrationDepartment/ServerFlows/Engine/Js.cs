using System.Numerics;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// JavaScript coercion rules over Newtonsoft tokens, so server flows compare,
    /// stringify and test values the way the Angular runtime does.
    /// </summary>
    /// <remarks>
    /// Value conventions used across the engine:
    /// <list type="bullet">
    /// <item>JS <c>undefined</c> is a C# <c>null</c> (<c>JToken?</c>); inside a container it is a
    /// <see cref="JTokenType.Undefined"/> value, read back as C# null.</item>
    /// <item>JS <c>null</c> is a <see cref="JValue"/> of type <see cref="JTokenType.Null"/>.</item>
    /// <item>Numbers are doubles; <see cref="FromNumber"/> stores integral ones as Integer tokens.</item>
    /// <item>A JS <c>Date</c> is a <see cref="JTokenType.Date"/> value holding a UTC
    /// <see cref="DateTimeOffset"/> with millisecond precision. A <see cref="DateTime"/> payload of
    /// kind Local is converted to UTC; Unspecified is read as UTC.</item>
    /// <item>Parse JSON with <see cref="ParseJson"/>, not <c>JToken.Parse</c>: Newtonsoft turns ISO
    /// strings into Date tokens and accepts non-JSON that <c>JSON.parse</c> rejects.</item>
    /// </list>
    /// The parameterless-zone overloads format dates in UTC; the engine's own ops use
    /// the zone from <see cref="ValueOpsContext"/>.
    /// </remarks>
    public static class Js
    {
        /// <summary>True for JS <c>undefined</c> (C# null or an Undefined token) and JS <c>null</c>.</summary>
        public static bool IsNil(JToken? v)
        {
            JsType t = TypeOf(v);
            return t == JsType.Undefined || t == JsType.Null;
        }

        /// <summary>
        /// value-ops.ts <c>isEmpty</c>: no value, a blank string (JS trim), an empty
        /// array or an object with no own keys. Numbers, booleans and valid dates are never empty.
        /// </summary>
        public static bool IsEmpty(JToken? v)
        {
            switch (TypeOf(v))
            {
                case JsType.Undefined:
                case JsType.Null:
                    return true;
                case JsType.String:
                    return JsStrings.Trim(StringValue(v!)).Length == 0;
                case JsType.Array:
                    return ((JArray)v!).Count == 0;
                case JsType.Object:
                    return !(v is JObject o) || o.Count == 0;
                default:
                    return false;
            }
        }

        /// <summary>JS truthiness: <c>"" 0 -0 NaN null undefined false</c> are falsy; everything else (including <c>[]</c> and <c>{}</c>) is truthy.</summary>
        public static bool Truthy(JToken? v)
        {
            switch (TypeOf(v))
            {
                case JsType.Undefined:
                case JsType.Null:
                    return false;
                case JsType.Boolean:
                    return (bool)((JValue)v!).Value!;
                case JsType.Number:
                    {
                        double d = NumberValue(v!);
                        return d != 0 && !double.IsNaN(d);
                    }
                case JsType.String:
                    return StringValue(v!).Length > 0;
                default:
                    return true;
            }
        }

        /// <summary>
        /// <c>String(v)</c>: arrays join with ",", objects give "[object Object]",
        /// numbers use JS formatting (1.0 → "1", 1e21 → "1e+21"), null → "null",
        /// undefined → "undefined", dates use Date.prototype.toString in UTC.
        /// </summary>
        public static string ToJsString(JToken? v) => ToJsString(v, TimeZoneInfo.Utc);

        /// <summary><c>String(v)</c> with dates written in <paramref name="zone"/> (the browser's "local" zone).</summary>
        public static string ToJsString(JToken? v, TimeZoneInfo zone)
        {
            switch (TypeOf(v))
            {
                case JsType.Undefined:
                    return "undefined";
                case JsType.Null:
                    return "null";
                case JsType.Boolean:
                    return (bool)((JValue)v!).Value! ? "true" : "false";
                case JsType.Number:
                    return JsNumber.ToJsString(NumberValue(v!));
                case JsType.String:
                    return StringValue(v!);
                case JsType.Date:
                    return JsDate.ToJsString(DateMs(v!), zone);
                case JsType.Array:
                    {
                        var arr = (JArray)v!;
                        if (arr.Count == 0) return string.Empty;
                        var sb = new StringBuilder();
                        for (int i = 0; i < arr.Count; i++)
                        {
                            if (i > 0) sb.Append(',');
                            JToken? item = JsObjects.Norm(arr[i]);
                            if (!IsNil(item)) sb.Append(ToJsString(item, zone));
                        }
                        return sb.ToString();
                    }
                default:
                    return "[object Object]";
            }
        }

        /// <summary>
        /// <c>Number(v)</c>: "" → 0, " 12 " → 12, "abc" → NaN, true → 1, null → 0,
        /// undefined → NaN, [] → 0, [5] → 5, {} → NaN, a date → its epoch milliseconds.
        /// </summary>
        public static double ToNumber(JToken? v)
        {
            switch (TypeOf(v))
            {
                case JsType.Undefined:
                    return double.NaN;
                case JsType.Null:
                    return 0;
                case JsType.Boolean:
                    return (bool)((JValue)v!).Value! ? 1 : 0;
                case JsType.Number:
                    return NumberValue(v!);
                case JsType.String:
                    return JsNumber.StringToNumber(StringValue(v!));
                case JsType.Date:
                    return DateMs(v!);
                default:
                    // ToPrimitive(hint number) on an array/object falls through valueOf
                    // (which returns the object) to toString.
                    return JsNumber.StringToNumber(ToJsString(v));
            }
        }

        /// <summary>JS <c>==</c>, including <c>null == undefined</c>, <c>"1" == 1</c> and <c>true == 1</c>.</summary>
        public static bool LooseEquals(JToken? a, JToken? b) => LooseEquals(a, b, TimeZoneInfo.Utc);

        internal static bool LooseEquals(JToken? a, JToken? b, TimeZoneInfo zone)
        {
            JsType ta = TypeOf(a);
            JsType tb = TypeOf(b);
            if (ta == tb || (IsObjectType(ta) && IsObjectType(tb))) return StrictEquals(a, b);
            if ((ta == JsType.Null && tb == JsType.Undefined) || (ta == JsType.Undefined && tb == JsType.Null)) return true;
            if (ta == JsType.Number && tb == JsType.String) return NumberValue(a!) == JsNumber.StringToNumber(StringValue(b!));
            if (ta == JsType.String && tb == JsType.Number) return JsNumber.StringToNumber(StringValue(a!)) == NumberValue(b!);
            if (ta == JsType.Boolean) return LooseEquals(FromNumber(ToNumber(a)), b, zone);
            if (tb == JsType.Boolean) return LooseEquals(a, FromNumber(ToNumber(b)), zone);
            if ((ta == JsType.String || ta == JsType.Number) && IsObjectType(tb)) return LooseEquals(a, new JValue(ToJsString(b, zone)), zone);
            if (IsObjectType(ta) && (tb == JsType.String || tb == JsType.Number)) return LooseEquals(new JValue(ToJsString(a, zone)), b, zone);
            return false;
        }

        /// <summary>
        /// JS <c>===</c>. Objects, arrays and dates compare by reference, as in the
        /// TS (which never deep-compares): two equal-looking objects are not equal
        /// unless they are the same token instance. NaN is never equal; -0 === 0.
        /// </summary>
        public static bool StrictEquals(JToken? a, JToken? b)
        {
            JsType ta = TypeOf(a);
            JsType tb = TypeOf(b);
            if (IsObjectType(ta) || IsObjectType(tb)) return ta == tb && ReferenceEquals(a, b);
            if (ta != tb) return false;
            switch (ta)
            {
                case JsType.Undefined:
                case JsType.Null:
                    return true;
                case JsType.Boolean:
                    return (bool)((JValue)a!).Value! == (bool)((JValue)b!).Value!;
                case JsType.Number:
                    return NumberValue(a!) == NumberValue(b!);
                default:
                    return string.Equals(StringValue(a!), StringValue(b!), StringComparison.Ordinal);
            }
        }

        /// <summary>A number token: integral doubles (within ±9.2e18) become Integer tokens, the rest (incl. NaN, ±∞) Float.</summary>
        public static JToken FromNumber(double d)
        {
            if (!double.IsNaN(d) && !double.IsInfinity(d) && Math.Floor(d) == d && Math.Abs(d) < 9.2e18)
            {
                return new JValue((long)d); // -0 becomes 0; JS prints and serialises both as 0
            }
            return new JValue(d);
        }

        /// <summary>
        /// <c>JSON.parse</c>: strict RFC 8259 (no comments, single quotes, trailing
        /// commas or NaN), numbers as doubles, ISO date strings kept as strings, keys
        /// in JS order, last duplicate key wins. Throws <see cref="FormatException"/> on bad input.
        /// </summary>
        public static JToken ParseJson(string text) => JsJson.Parse(text);

        /// <summary>
        /// <c>JSON.stringify(v, null, pretty ? 2 : undefined)</c>. Returns null for JS
        /// undefined. Undefined properties are dropped, undefined array items and
        /// non-finite numbers become null, dates become their ISO string.
        /// </summary>
        public static string? Stringify(JToken? v, bool pretty = false) => JsJson.Stringify(v, pretty ? "  " : null);

        // ── internals shared by the engine ─────────────────────────────────────

        internal static bool IsObjectType(JsType t) => t == JsType.Object || t == JsType.Array || t == JsType.Date;

        /// <summary>The JS kind of a token. Guid/Uri/TimeSpan/Bytes payloads read as strings.</summary>
        internal static JsType TypeOf(JToken? v)
        {
            if (v is null) return JsType.Undefined;
            switch (v.Type)
            {
                case JTokenType.Undefined:
                    return JsType.Undefined;
                case JTokenType.Null:
                    return JsType.Null;
                case JTokenType.Boolean:
                    return JsType.Boolean;
                case JTokenType.Integer:
                case JTokenType.Float:
                    return JsType.Number;
                case JTokenType.String:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                case JTokenType.Comment:
                    return ((JValue)v).Value is null ? JsType.Null : JsType.String;
                case JTokenType.Date:
                    return JsType.Date;
                case JTokenType.Array:
                    return JsType.Array;
                default:
                    return JsType.Object;
            }
        }

        internal static double NumberValue(JToken v)
        {
            object? raw = ((JValue)v).Value;
            return raw switch
            {
                double d => d,
                long l => l,
                int i => i,
                decimal m => (double)m,
                float f => f,
                BigInteger b => (double)b,
                ulong u => u,
                _ => Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture),
            };
        }

        internal static string StringValue(JToken v)
        {
            object? raw = ((JValue)v).Value;
            return raw switch
            {
                string s => s,
                byte[] bytes => Convert.ToBase64String(bytes),
                Uri u => u.OriginalString,
                Guid g => g.ToString("D"),
                TimeSpan ts => ts.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
                null => string.Empty,
                _ => Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }

        /// <summary>Epoch milliseconds of a Date token.</summary>
        internal static long DateMs(JToken v)
        {
            object? raw = ((JValue)v).Value;
            switch (raw)
            {
                case DateTimeOffset dto:
                    return dto.ToUnixTimeMilliseconds();
                case DateTime dt:
                    {
                        DateTime utc = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
                    }
                default:
                    throw new InvalidOperationException("Not a date token");
            }
        }

        /// <summary>A <c>String(v)</c> token, the shape most text ops return.</summary>
        internal static JValue Str(string s) => new JValue(s);

        /// <summary>
        /// IsLessThan (the Abstract Relational Comparison): both sides go through
        /// ToPrimitive(hint number) — a date gives its time value, an array/object its
        /// string. Two strings compare by UTF-16 code units; otherwise both become
        /// numbers and any NaN makes the result undefined (null here).
        /// </summary>
        internal static bool? LessThan(JToken? x, JToken? y)
        {
            JToken? px = ToPrimitiveNumberHint(x);
            JToken? py = ToPrimitiveNumberHint(y);
            if (TypeOf(px) == JsType.String && TypeOf(py) == JsType.String)
            {
                return string.CompareOrdinal(StringValue(px!), StringValue(py!)) < 0;
            }
            double nx = ToNumber(px);
            double ny = ToNumber(py);
            if (double.IsNaN(nx) || double.IsNaN(ny)) return null;
            return nx < ny;
        }

        /// <summary>JS <c>x &gt; y</c>.</summary>
        internal static bool GreaterThan(JToken? x, JToken? y) => LessThan(y, x) == true;

        /// <summary>JS <c>x &lt; y</c>.</summary>
        internal static bool IsLess(JToken? x, JToken? y) => LessThan(x, y) == true;

        /// <summary>JS <c>x &gt;= y</c>: false when the comparison is undefined (NaN).</summary>
        internal static bool GreaterOrEqual(JToken? x, JToken? y) => LessThan(x, y) == false;

        /// <summary>JS <c>x &lt;= y</c>: false when the comparison is undefined (NaN).</summary>
        internal static bool LessOrEqual(JToken? x, JToken? y) => LessThan(y, x) == false;

        private static JToken? ToPrimitiveNumberHint(JToken? v)
        {
            switch (TypeOf(v))
            {
                case JsType.Date:
                    return FromNumber(DateMs(v!));
                case JsType.Array:
                case JsType.Object:
                    return new JValue(ToJsString(v));
                default:
                    return v;
            }
        }
    }
}
