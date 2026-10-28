using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The server subset of value-ops.ts <c>VALUE_OPS</c>, one method per op, each a
    /// line-for-line port. <c>v</c> is never nil here unless the op is in RUNS_ON_NIL
    /// (the pipeline skips the rest). An op throws where the TS op throws; the
    /// pipeline then keeps the previous value, as <c>applyValueOps</c> does.
    /// </summary>
    internal static class ValueOpFunctions
    {
        internal delegate JToken? OpFn(JToken? v, JObject a, ValueOpsContext ctx, int depth);

        // ── argument helpers ────────────────────────────────────────────────────

        /// <summary><c>a[name]</c>: undefined when absent.</summary>
        private static JToken? Arg(JObject a, string name) => a.TryGetValue(name, out JToken? t) ? JsObjects.Norm(t) : null;

        /// <summary><c>x ?? fallback</c>.</summary>
        private static JToken Or(JToken? x, JToken fallback) => Js.IsNil(x) ? fallback : x!;

        private static JToken Or(JToken? x, string fallback) => Js.IsNil(x) ? new JValue(fallback) : x!;

        private static JToken Or(JToken? x, double fallback) => Js.IsNil(x) ? Js.FromNumber(fallback)! : x!;

        /// <summary><c>x === false</c>.</summary>
        private static bool IsFalse(JToken? x) => x != null && x.Type == JTokenType.Boolean && !(bool)((JValue)x).Value!;

        /// <summary>A format/pattern argument the TS calls string methods on: anything else is a TypeError.</summary>
        private static string StringArg(JToken value, string what)
        {
            if (Js.TypeOf(value) != JsType.String) throw new InvalidOperationException(what + " is not a string (TypeError in the browser)");
            return Js.StringValue(value);
        }

        private static string S(JToken? v, ValueOpsContext ctx) => Js.ToJsString(v, ctx.TimeZone);

        private static JToken Num(double d) => Js.FromNumber(d)!;

        /// <summary>The values a <c>for…of</c> / spread over <paramref name="iterable"/> yields; TypeError if not iterable.</summary>
        private static List<JToken?> Iterate(JToken? iterable, string what)
        {
            if (iterable is JArray arr) return JsObjects.Items(arr);
            if (Js.TypeOf(iterable) == JsType.String)
            {
                var list = new List<JToken?>();
                foreach (string cp in JsStrings.CodePoints(Js.StringValue(iterable!))) list.Add(new JValue(cp));
                return list;
            }
            throw new InvalidOperationException(what + " is not iterable");
        }

        /// <summary><c>(x ?? []).forEach</c>: arrays only; anything else is a TypeError.</summary>
        private static List<JToken?> ForEachList(JToken? x, string what)
        {
            if (Js.IsNil(x)) return new List<JToken?>();
            if (x is JArray arr) return JsObjects.Items(arr);
            throw new InvalidOperationException(what + ".forEach is not a function");
        }

        // ── text ────────────────────────────────────────────────────────────────

        // Server port: a Date is written as Date.prototype.toString in ctx.TimeZone; only the zone name in parentheses can differ from the browser's ICU name.
        internal static JToken? Text(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Js.Str(S(v, ctx));

        internal static JToken? Trim(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Js.Str(JsStrings.Trim(S(v, ctx)));

        // Server port: invariant case mapping plus JS's unconditional expansions (ß, ligatures); Greek iota-subscript special casing is not reproduced.
        internal static JToken? Upper(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Js.Str(JsStrings.ToUpper(S(v, ctx)));

        // Server port: invariant case mapping plus the dotted capital I expansion; JS's final-sigma context rule is not reproduced.
        internal static JToken? Lower(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Js.Str(JsStrings.ToLower(S(v, ctx)));

        internal static JToken? Capitalize(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string s = S(v, ctx);
            if (s.Length == 0) return Js.Str(string.Empty);
            return Js.Str(JsStrings.ToUpper(s[0].ToString()) + s.Substring(1));
        }

        /// <summary>`find` is literal text unless `regex` is set. `all` (default true) replaces every match.</summary>
        internal static JToken? Replace(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string subject = S(v, ctx);
            string find = S(Or(Arg(a, "find"), string.Empty), ctx);
            string with = S(Or(Arg(a, "with"), string.Empty), ctx);
            bool firstOnly = IsFalse(Arg(a, "all"));
            if (Js.Truthy(Arg(a, "regex")))
            {
                try
                {
                    return Js.Str(JsRegex.Get(find).Replace(subject, with, !firstOnly));
                }
                catch (ArgumentException)
                {
                    return Js.Str(subject); // the TS catches a bad pattern and keeps the text
                }
                catch (RegexMatchTimeoutException)
                {
                    return Js.Str(subject); // Server port: a timeout is treated like a bad pattern
                }
            }
            if (firstOnly) return Js.Str(JsStrings.ReplaceFirst(subject, find, with));
            // split/join: every occurrence, and no $-patterns in the replacement.
            return Js.Str(string.Join(with, JsStrings.Split(subject, find)));
        }

        internal static JToken? StripSpaces(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            Js.Str(JsStrings.ReplaceWhiteSpaceRuns(S(v, ctx), string.Empty));

        /// <summary>Arabic-Indic (U+0660–0669) and Persian (U+06F0–06F9) digits to ASCII.</summary>
        internal static JToken? LatinDigits(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string s = S(v, ctx);
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= '\u0660' && c <= '\u0669') chars[i] = (char)('0' + (c - '\u0660'));
                else if (c >= '\u06F0' && c <= '\u06F9') chars[i] = (char)('0' + (c - '\u06F0'));
            }
            return Js.Str(new string(chars));
        }

        internal static JToken? ArabicDigits(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string s = S(v, ctx);
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '0' && chars[i] <= '9') chars[i] = (char)('\u0660' + (chars[i] - '0'));
            }
            return Js.Str(new string(chars));
        }

        /// <summary>`tail` (default) keeps the last `keep` chars; `email` keeps first/last letter of the local part and the domain.</summary>
        // Server port: a filler repeated past 10M characters throws (V8's limit is ~512M); the step is then skipped.
        internal static JToken? Mask(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string text = S(v, ctx);
            string filler = S(Or(Arg(a, "with"), "*"), ctx);
            JToken? mode = Arg(a, "mode");
            if (Js.TypeOf(mode) == JsType.String && Js.StringValue(mode!) == "email")
            {
                int at = text.IndexOf('@');
                if (at < 1) return Js.Str(text);
                string local = text.Substring(0, at);
                if (local.Length <= 2) return Js.Str(local[0] + filler + text.Substring(at));
                return Js.Str(local[0] + JsStrings.Repeat(filler, local.Length - 2) + local[local.Length - 1] + text.Substring(at));
            }
            double keep = Js.ToNumber(Or(Arg(a, "keep"), 2));
            if (text.Length <= keep) return Js.Str(text);
            double hidden = text.Length - keep;
            return Js.Str(JsStrings.Repeat(filler, hidden) + JsStrings.Slice(text, hidden, null));
        }

        private static readonly Dictionary<string, string> KeepOnlySets = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["digits"] = "0-9",
            ["letters"] = "a-zA-Z",
            ["alphanumeric"] = "a-zA-Z0-9",
            ["arabic"] = "\\u0600-\\u06FF",
        };

        /// <summary>Keeps only the characters in a named set or a regex class body.</summary>
        internal static JToken? KeepOnly(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JToken? characters = Arg(a, "characters");
            string range;
            if (!Js.IsNil(characters) && KeepOnlySets.TryGetValue(S(characters, ctx), out string? named)) range = named;
            else range = Js.IsNil(characters) ? "a-zA-Z0-9" : S(characters, ctx);
            string text = S(v, ctx);
            try
            {
                return Js.Str(JsRegex.Get("[^" + range + "]").Replace(text, string.Empty, true));
            }
            catch (ArgumentException)
            {
                return Js.Str(text);
            }
            catch (RegexMatchTimeoutException)
            {
                return Js.Str(text); // Server port: a timeout is treated like a bad pattern
            }
        }

        internal static JToken? Split(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string[] parts = JsStrings.Split(S(v, ctx), S(Or(Arg(a, "by"), ","), ctx));
            var arr = new JArray();
            foreach (string part in parts) arr.Add(new JValue(part));
            return arr;
        }

        internal static JToken? Slice(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            double from = Js.ToNumber(Or(Arg(a, "from"), 0));
            // `a.to` passed as is: missing = undefined (to the end), null = 0.
            JToken? toArg = a.TryGetValue("to", out JToken? rawTo) ? rawTo : null;
            double? to = toArg == null || toArg.Type == JTokenType.Undefined ? null : Js.ToNumber(toArg);
            if (v is JArray arr)
            {
                int start = JsStrings.RelativeIndex(from, arr.Count);
                int end = to.HasValue ? JsStrings.RelativeIndex(to.Value, arr.Count) : arr.Count;
                var items = JsObjects.Items(arr);
                var result = new List<JToken?>();
                for (int i = start; i < end; i++) result.Add(items[i]);
                return JsObjects.ArrayOf(result);
            }
            return Js.Str(JsStrings.Slice(S(v, ctx), from, to));
        }

        // Server port: a target length over 10M characters throws (V8's limit is ~512M); the step is then skipped.
        internal static JToken? Pad(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string s = S(v, ctx);
            double width = Js.ToNumber(Or(Arg(a, "length"), 0));
            string filler = S(Or(Arg(a, "with"), "0"), ctx);
            JToken? side = Arg(a, "side");
            bool atEnd = Js.TypeOf(side) == JsType.String && Js.StringValue(side!) == "end";
            return Js.Str(JsStrings.Pad(s, width, filler, atEnd));
        }

        /// <summary>`${value}` is the whole value, `${some.path}` reads into it (readPath).</summary>
        internal static JToken? Template(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string pattern = S(Or(Arg(a, "pattern"), string.Empty), ctx);
            var sb = new StringBuilder(pattern.Length + 16);
            int i = 0;
            while (i < pattern.Length)
            {
                int open = pattern.IndexOf("${", i, StringComparison.Ordinal);
                int close = open < 0 ? -1 : pattern.IndexOf('}', open + 2);
                if (open < 0 || close < 0)
                {
                    sb.Append(pattern, i, pattern.Length - i);
                    break;
                }
                sb.Append(pattern, i, open - i);
                string path = pattern.Substring(open + 2, close - open - 2);
                JToken? part = path == "value" || path.Length == 0 ? v : ValuePredicate.ReadPath(v, new JValue(path));
                if (!Js.IsNil(part)) sb.Append(S(part, ctx));
                i = close + 1;
            }
            return Js.Str(sb.ToString());
        }

        internal static JToken? EscapeHtml(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            Js.Str(S(v, ctx).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;"));

        internal static JToken? NewlinesToHtml(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            Js.Str(S(v, ctx).Replace("\n", "<br>"));

        /// <summary>Removes the characters that break an unencoded URL query (default <c>&amp;#+%&lt;&gt;</c>), then collapses spaces.</summary>
        // Server port: a regex match that exceeds the 100 ms timeout throws, so the step is skipped like any other failure.
        internal static JToken? UrlSafe(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string text = S(v, ctx);
            JToken? chars = Arg(a, "chars");
            string replaced;
            if (Js.Truthy(chars))
            {
                // No try/catch in the TS: a bad class (e.g. "z-a") throws and the step is skipped.
                replaced = JsRegex.Get("[" + JsStrings.EscapeRegex(S(chars, ctx)) + "]").Replace(text, " ", true);
            }
            else
            {
                var sb = new StringBuilder(text.Length);
                foreach (char c in text) sb.Append(c is '&' or '#' or '+' or '%' or '<' or '>' ? ' ' : c);
                replaced = sb.ToString();
            }
            return Js.Str(JsStrings.Trim(JsStrings.ReplaceWhiteSpaceRuns(replaced, " ")));
        }

        // ── numbers ─────────────────────────────────────────────────────────────

        internal static JToken? Number(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            double n = JsNumber.StringToNumber(S(v, ctx).Replace(",", string.Empty));
            return double.IsNaN(n) ? Or(Arg(a, "fallback"), 0) : Num(n);
        }

        internal static JToken? Round(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            double decimals = Js.ToNumber(Or(Arg(a, "decimals"), 0));
            double factor = Math.Pow(10, decimals);
            return Num(JsNumber.Round(Js.ToNumber(v) * factor) / factor);
        }

        /// <summary>One arithmetic step; each key applies when present (even as null, which is 0).</summary>
        internal static JToken? MathOp(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            double n = Js.ToNumber(v);
            if (a.ContainsKey("add")) n += Js.ToNumber(Arg(a, "add"));
            if (a.ContainsKey("subtract")) n -= Js.ToNumber(Arg(a, "subtract"));
            if (a.ContainsKey("multiply")) n *= Js.ToNumber(Arg(a, "multiply"));
            if (a.ContainsKey("divide"))
            {
                double d = Js.ToNumber(Arg(a, "divide"));
                if (d != 0) n /= d;
            }
            return Num(n);
        }

        internal static JToken? Clamp(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            double n = Js.ToNumber(v);
            if (a.ContainsKey("min")) n = Math.Max(n, Js.ToNumber(Arg(a, "min")));
            if (a.ContainsKey("max")) n = Math.Min(n, Js.ToNumber(Arg(a, "max")));
            return Num(n);
        }

        internal static JToken? Abs(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Num(Math.Abs(Js.ToNumber(v)));

        // ── dates ───────────────────────────────────────────────────────────────

        /// <summary>`format` takes yyyy-MM-dd style tokens, or "iso" / "isoLocal" / "timestamp".</summary>
        // Server port: dates are limited to years 1-9999 (DateTimeOffset); JS allows +/-275760, which no config date reaches.
        internal static JToken? Date(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            long? ms = JsDate.ToDate(v, ctx.TimeZone);
            if (ms == null)
            {
                JToken? fallback = Arg(a, "fallback");
                if (!Js.IsNil(fallback)) return fallback;
                return Js.IsNil(v) ? v : Js.Str(S(v, ctx));
            }
            string format = StringArg(Or(Arg(a, "format"), "yyyy-MM-dd"), "format");
            return Js.Str(FlowDateFormat.FormatMs(ms.Value, format, ctx.TimeZone));
        }

        /// <summary>Shifts a date with the local-time setters (in order: years, months, days, hours, minutes, seconds).</summary>
        internal static JToken? DateAdd(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            long? start = JsDate.ToDate(v, ctx.TimeZone);
            if (start == null) return v;
            long ms = start.Value;
            TimeZoneInfo zone = ctx.TimeZone;
            ms = Shift(ms, a, "years", zone, (p, n) => JsDate.MakeDate(JsDate.MakeDay(p.Year + n, p.Month0, p.Day), TimeOfDay(p)));
            ms = Shift(ms, a, "months", zone, (p, n) => JsDate.MakeDate(JsDate.MakeDay(p.Year, p.Month0 + n, p.Day), TimeOfDay(p)));
            ms = Shift(ms, a, "days", zone, (p, n) => JsDate.MakeDate(JsDate.MakeDay(p.Year, p.Month0, p.Day + n), TimeOfDay(p)));
            ms = Shift(ms, a, "hours", zone, (p, n) => JsDate.MakeDate(DayOf(p), JsDate.MakeTime(p.Hour + n, p.Minute, p.Second, p.Millisecond)));
            ms = Shift(ms, a, "minutes", zone, (p, n) => JsDate.MakeDate(DayOf(p), JsDate.MakeTime(p.Hour, p.Minute + n, p.Second, p.Millisecond)));
            ms = Shift(ms, a, "seconds", zone, (p, n) => JsDate.MakeDate(DayOf(p), JsDate.MakeTime(p.Hour, p.Minute, p.Second + n, p.Millisecond)));
            return JsDate.Create(ms);
        }

        private static double TimeOfDay(JsDate.LocalParts p) => JsDate.MakeTime(p.Hour, p.Minute, p.Second, p.Millisecond);

        private static double DayOf(JsDate.LocalParts p) => JsDate.MakeDay(p.Year, p.Month0, p.Day);

        private static long Shift(long ms, JObject a, string unit, TimeZoneInfo zone, Func<JsDate.LocalParts, double, double> rebuild)
        {
            JToken? amount = Arg(a, unit);
            if (!Js.Truthy(amount)) return ms;
            double local = rebuild(JsDate.Local(ms, zone), Js.ToNumber(amount));
            long? result = JsDate.LocalToUtc(local, zone);
            // Server port: JS would carry an Invalid Date on; the server cannot hold one, so the step fails (value kept).
            if (result == null) throw new InvalidOperationException("dateAdd produced an invalid date");
            return result.Value;
        }

        internal static JToken? Now(JToken? v, JObject a, ValueOpsContext ctx, int depth) => JsDate.Create(ctx.Now.ToUnixTimeMilliseconds());

        /// <summary>Reads a date written in a stated pattern (default dd/MM/yyyy) as local time.</summary>
        internal static JToken? ParseDate(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (Js.TypeOf(v) == JsType.Date) return v;
            string pattern = StringArg(Or(Arg(a, "pattern"), "dd/MM/yyyy"), "pattern");
            long? ms = ParseByPattern(S(v, ctx), pattern, ctx.TimeZone);
            if (ms != null) return JsDate.Create(ms.Value);
            JToken? fallback = Arg(a, "fallback");
            return Js.IsNil(fallback) ? v : fallback;
        }

        // ── calendars ───────────────────────────────────────────────────────────

        /// <summary>Gregorian → Hijri (Umm al-Qura, ICU table). `format` takes dd/MM/yyyy tokens or "parts".</summary>
        internal static JToken? Hijri(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JToken? parse = Arg(a, "parse");
            long? source = Js.Truthy(parse)
                ? ParseByPattern(S(v, ctx), StringArg(parse!, "parse"), ctx.TimeZone)
                : ToLocalDay(v, ctx.TimeZone);
            JToken? fallback = Arg(a, "fallback");
            if (source == null) return Js.IsNil(fallback) ? v : fallback;

            JsDate.LocalParts local = JsDate.Local(source.Value, ctx.TimeZone);
            long utcDay = JsDate.UtcDay(local.Year, local.Month0, local.Day);
            // No Hijri day (before year 1 AH) reads like the browser's empty formatToParts: fallback.
            if (!UmAlQura.TryFromDay(utcDay, out int year, out int month, out int day)) return Js.IsNil(fallback) ? v : fallback;

            JToken? format = Arg(a, "format");
            if (Js.TypeOf(format) == JsType.String && Js.StringValue(format!) == "parts")
            {
                var parts = new JObject();
                JsObjects.SetOwn(parts, "year", Num(year));
                JsObjects.SetOwn(parts, "month", Num(month));
                JsObjects.SetOwn(parts, "day", Num(day));
                return parts;
            }
            return Js.Str(FlowDateFormat.FormatCalendarParts(year, month, day, StringArg(Or(format, "dd/MM/yyyy"), "format")));
        }

        /// <summary>Hijri → Gregorian. Takes a string (read with `parse`, default dd/MM/yyyy) or a {year, month, day} object.</summary>
        internal static JToken? Gregorian(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JsType type = Js.TypeOf(v);
            (double Year, double Month, double Day)? parts = type == JsType.Object || type == JsType.Array
                ? ToCalendarParts(v)
                : CalendarPartsByPattern(S(v, ctx), StringArg(Or(Arg(a, "parse"), "dd/MM/yyyy"), "parse"));
            JToken? fallback = Arg(a, "fallback");
            if (parts == null) return Js.IsNil(fallback) ? v : fallback;

            long? date = HijriToGregorian(parts.Value, ctx.TimeZone);
            if (date == null) return Js.IsNil(fallback) ? v : fallback;

            JToken? format = Arg(a, "format");
            if (Js.Truthy(format)) return Js.Str(FlowDateFormat.FormatMs(date.Value, StringArg(format!, "format"), ctx.TimeZone));
            return JsDate.Create(date.Value);
        }

        /// <summary>
        /// The TS searches 1900-01-01 … 2100-01-01 for the day whose Hijri ordinal
        /// (y·10000 + m·100 + d) equals the wanted one, so an ordinal that happens to
        /// name a real day is accepted even from odd parts (month 6, day 130 = 1411/07/30).
        /// Decoding the ordinal reproduces that exactly.
        /// </summary>
        private static long? HijriToGregorian((double Year, double Month, double Day) parts, TimeZoneInfo zone)
        {
            double wanted = parts.Year * 10000 + parts.Month * 100 + parts.Day;
            if (double.IsNaN(wanted) || wanted <= 0 || wanted > int.MaxValue || Math.Floor(wanted) != wanted) return null;
            int ordinal = (int)wanted;
            int year = ordinal / 10000;
            int month = ordinal / 100 % 100;
            int day = ordinal % 100;
            if (!UmAlQura.TryToDay(year, month, day, out long utcDay)) return null;
            long low = JsDate.UtcDay(1900, 0, 1);
            long high = JsDate.UtcDay(2100, 0, 1);
            if (utcDay < low || utcDay > high) return null;
            DateOnly civil = DateOnly.FromDayNumber((int)(utcDay + 719162));
            // Local midnight, so formatting with local getters never lands on the day before.
            return JsDate.Construct(civil.Year, civil.Month - 1, civil.Day, 0, 0, 0, 0, zone);
        }

        private static (double, double, double)? ToCalendarParts(JToken? value)
        {
            double year = Js.ToNumber(Or2(JsObjects.Get(value, "year"), JsObjects.Get(value, "yyyy")));
            double month = Js.ToNumber(Or2(JsObjects.Get(value, "month"), JsObjects.Get(value, "MM")));
            double day = Js.ToNumber(Or2(JsObjects.Get(value, "day"), JsObjects.Get(value, "dd")));
            if (!IsTruthyNumber(year) || !IsTruthyNumber(month) || !IsTruthyNumber(day)) return null;
            return (year, month, day);
        }

        private static JToken? Or2(JToken? x, JToken? y) => Js.IsNil(x) ? y : x;

        private static bool IsTruthyNumber(double d) => d != 0 && !double.IsNaN(d);

        private static (double, double, double)? CalendarPartsByPattern(string input, string pattern)
        {
            Dictionary<string, double>? parts = NumbersByPattern(input, pattern);
            if (parts == null) return null;
            double year = parts.TryGetValue("yyyy", out double yyyy) ? yyyy : parts.TryGetValue("yy", out double yy) ? 1400 + yy : double.NaN;
            if (!IsTruthyNumber(year) || !parts.TryGetValue("MM", out double month) || !IsTruthyNumber(month) ||
                !parts.TryGetValue("dd", out double day) || !IsTruthyNumber(day))
            {
                return null;
            }
            return (year, month, day);
        }

        /// <summary>value-ops.ts <c>toLocalDay</c>: "yyyy-MM-dd" is that local calendar day, not UTC midnight.</summary>
        private static long? ToLocalDay(JToken? v, TimeZoneInfo zone)
        {
            if (Js.TypeOf(v) == JsType.String)
            {
                string s = Js.StringValue(v!);
                Match m = IsoDay.Match(s);
                if (m.Success)
                {
                    return JsDate.Construct(
                        double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                        double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) - 1,
                        double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                        0, 0, 0, 0, zone);
                }
            }
            return JsDate.ToDate(v, zone);
        }

        private static readonly Regex IsoDay = new Regex(
            "^[" + JsStrings.WhiteSpaceClassBody + "]*([0-9]{4})-([0-9]{2})-([0-9]{2})[" + JsStrings.WhiteSpaceClassBody + "]*\\z",
            RegexOptions.CultureInvariant,
            JsRegex.MatchTimeout);

        /// <summary>value-ops.ts <c>parseByPattern</c>: a local Date, year from yyyy or 2000 + yy.</summary>
        internal static long? ParseByPattern(string input, string pattern, TimeZoneInfo zone)
        {
            Dictionary<string, double>? parts = NumbersByPattern(input, pattern);
            if (parts == null) return null;
            double year = parts.TryGetValue("yyyy", out double yyyy) ? yyyy : parts.TryGetValue("yy", out double yy) ? 2000 + yy : double.NaN;
            if (!IsTruthyNumber(year) || !parts.TryGetValue("MM", out double month) || !IsTruthyNumber(month) ||
                !parts.TryGetValue("dd", out double day) || !IsTruthyNumber(day))
            {
                return null;
            }
            return JsDate.Construct(
                year,
                month - 1,
                day,
                parts.TryGetValue("HH", out double h) ? h : 0,
                parts.TryGetValue("mm", out double mi) ? mi : 0,
                parts.TryGetValue("ss", out double s) ? s : 0,
                0,
                zone);
        }

        private static readonly string[] PatternTokens = { "yyyy", "yy", "MM", "dd", "HH", "mm", "ss" };

        /// <summary>value-ops.ts <c>numbersByPattern</c>: tokens yyyy yy MM dd HH mm ss, everything else literal.</summary>
        private static Dictionary<string, double>? NumbersByPattern(string input, string pattern)
        {
            var order = new List<string>();
            var source = new StringBuilder();
            var literal = new StringBuilder();
            int i = 0;
            while (i < pattern.Length)
            {
                string? token = null;
                foreach (string t in PatternTokens)
                {
                    if (i + t.Length <= pattern.Length && string.CompareOrdinal(pattern, i, t, 0, t.Length) == 0)
                    {
                        token = t;
                        break;
                    }
                }
                if (token == null)
                {
                    literal.Append(pattern[i]);
                    i++;
                    continue;
                }
                source.Append(Regex.Escape(literal.ToString()));
                literal.Clear();
                source.Append(token == "yyyy" ? "([0-9]{4})" : "([0-9]{1,2})");
                order.Add(token);
                i += token.Length;
            }
            source.Append(Regex.Escape(literal.ToString()));

            string ws = "[" + JsStrings.WhiteSpaceClassBody + "]*";
            Match found;
            try
            {
                found = JsRegex.Raw("^" + ws + source + ws + "\\z").Match(input);
            }
            catch (RegexMatchTimeoutException)
            {
                return null; // Server port: a timeout reads as "does not match"
            }
            if (!found.Success) return null;
            var parts = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int k = 0; k < order.Count; k++)
            {
                parts[order[k]] = double.Parse(found.Groups[k + 1].Value, CultureInfo.InvariantCulture);
            }
            return parts;
        }

        // ── shape ───────────────────────────────────────────────────────────────

        internal static JToken? Json(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            string? text = JsJson.Stringify(v, Js.Truthy(Arg(a, "pretty")) ? "  " : null);
            return text == null ? null : Js.Str(text);
        }

        /// <summary>Safe JSON.parse — a value that is not JSON passes through untouched.</summary>
        internal static JToken? Parse(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (Js.TypeOf(v) != JsType.String) return v;
            return JsJson.TryParse(Js.StringValue(v!), out JToken? parsed) ? parsed : v;
        }

        internal static JToken? Get(JToken? v, JObject a, ValueOpsContext ctx, int depth) => ValuePredicate.ReadPath(v, Arg(a, "path"));

        // Server port: untouched siblings are deep copies (the TS shallow-spreads), so object identity with the input is not kept; see JsonPath.Set.
        internal static JToken? Set(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            JsonPath.Set(v, S(Arg(a, "path"), ctx), Arg(a, "value"));

        internal static JToken? Pick(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            var output = new JObject();
            foreach (JToken? field in ForEachList(Arg(a, "fields"), "fields"))
            {
                JToken? part = ValuePredicate.ReadPath(v, field);
                if (part == null) continue;
                if (Js.TypeOf(field) != JsType.String) throw new InvalidOperationException("f.split is not a function");
                string[] segments = Js.StringValue(field!).Split('.');
                JsObjects.SetOwn(output, segments[segments.Length - 1], part);
            }
            return output;
        }

        internal static JToken? Omit(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JObject output = JsObjects.Spread(v);
            foreach (JToken? field in ForEachList(Arg(a, "fields"), "fields")) output.Remove(S(field, ctx));
            return output;
        }

        /// <summary>{ "fields": { "newName": "old.path" } }</summary>
        internal static JToken? Rename(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            var output = new JObject();
            foreach (var entry in JsObjects.Entries(Or(Arg(a, "fields"), new JObject())))
            {
                JsObjects.SetOwn(output, entry.Key, ValuePredicate.ReadPath(v, entry.Value));
            }
            return output;
        }

        /// <summary>Adds fixed fields. `under` nests the value instead of spreading it.</summary>
        internal static JToken? Merge(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JToken? under = Arg(a, "under");
            if (Js.Truthy(under))
            {
                JObject nested = JsObjects.Spread(Arg(a, "with"));
                JsObjects.SetOwn(nested, S(under, ctx), v);
                return nested;
            }
            JObject merged = JsObjects.Spread(v);
            JsObjects.Spread(Arg(a, "with"), merged);
            return merged;
        }

        /// <summary>Fills only the keys the value is missing.</summary>
        internal static JToken? Defaults(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JObject output = JsObjects.Spread(Arg(a, "with"));
            JsObjects.Spread(v, output);
            return output;
        }

        internal static JToken? Keys(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            var arr = new JArray();
            foreach (string key in JsObjects.Keys(v)) arr.Add(new JValue(key));
            return arr;
        }

        internal static JToken? Values(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            JsObjects.ArrayOf(JsObjects.Entries(v).Select(e => e.Value));

        internal static JToken? EntriesOp(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            var arr = new JArray();
            foreach (var entry in JsObjects.Entries(v))
            {
                var item = new JObject();
                JsObjects.SetOwn(item, "key", new JValue(entry.Key));
                JsObjects.SetOwn(item, "value", entry.Value);
                arr.Add(item);
            }
            return arr;
        }

        internal static JToken? ToArray(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (v is JArray) return v;
            if (Js.IsNil(v)) return new JArray();
            return JsObjects.ArrayOf(new[] { v });
        }

        // ── lists ───────────────────────────────────────────────────────────────

        /// <summary>Either reads one path out of every item, or runs a whole pipeline on each.</summary>
        internal static JToken? Map(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            List<JToken?> list = v is JArray arr ? JsObjects.Items(arr) : new List<JToken?>();
            JToken? ops = Arg(a, "ops");
            var results = new List<JToken?>(list.Count);
            if (Js.Truthy(ops))
            {
                foreach (JToken? item in list) results.Add(ValueOps.ApplyCore(item, ops, ctx, depth + 1));
            }
            else
            {
                JToken? path = Arg(a, "path");
                foreach (JToken? item in list) results.Add(ValuePredicate.ReadPath(item, path));
            }
            return JsObjects.ArrayOf(results);
        }

        internal static JToken? Filter(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            JToken? where = Arg(a, "where");
            return JsObjects.ArrayOf(JsObjects.Items(arr).Where(i => ValuePredicate.Test(i, where, ctx.TimeZone)).ToList());
        }

        internal static JToken? Find(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return null;
            JToken? where = Arg(a, "where");
            foreach (JToken? item in JsObjects.Items(arr))
            {
                if (ValuePredicate.Test(item, where, ctx.TimeZone)) return item;
            }
            return null;
        }

        internal static JToken? First(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            v is JArray arr ? (arr.Count > 0 ? JsObjects.Norm(arr[0]) : null) : v;

        internal static JToken? Last(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            v is JArray arr ? (arr.Count > 0 ? JsObjects.Norm(arr[arr.Count - 1]) : null) : v;

        /// <summary>A negative `index` counts from the end; a non-integer index reads nothing.</summary>
        internal static JToken? At(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            double index = Js.ToNumber(Or(Arg(a, "index"), 0));
            double key = index < 0 ? arr.Count + index : index;
            return JsObjects.Get(arr, JsNumber.ToJsString(key));
        }

        /// <summary>Drops null, undefined, '' and empty rows.</summary>
        internal static JToken? Compact(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            v is JArray arr ? JsObjects.ArrayOf(JsObjects.Items(arr).Where(i => !Js.IsEmpty(i)).ToList()) : v;

        internal static JToken? Unique(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            JToken? by = Arg(a, "by");
            var seen = new HashSet<(int Kind, string? Text, double Number)>();
            var kept = new List<JToken?>();
            foreach (JToken? item in JsObjects.Items(arr))
            {
                JToken? key = Js.Truthy(by) ? ValuePredicate.ReadPath(item, by) : item;
                if (seen.Add(SetKey(key))) kept.Add(item);
            }
            return JsObjects.ArrayOf(kept);
        }

        /// <summary>
        /// The identity a JS Set gives the TS's id: objects (and null — typeof null is
        /// 'object') become their JSON text, so they collide with an equal string, as in JS.
        /// </summary>
        private static (int, string?, double) SetKey(JToken? key)
        {
            switch (Js.TypeOf(key))
            {
                case JsType.Undefined:
                    return (0, null, 0);
                case JsType.Boolean:
                    return (1, null, Js.Truthy(key) ? 1 : 0);
                case JsType.Number:
                    {
                        double d = Js.NumberValue(key!);
                        if (d == 0) d = 0; // SameValueZero: -0 and +0 are one key
                        if (double.IsNaN(d)) d = double.NaN;
                        return (2, null, d);
                    }
                case JsType.String:
                    return (3, Js.StringValue(key!), 0);
                default:
                    return (3, JsJson.Stringify(key, null), 0);
            }
        }

        internal static JToken? Flatten(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            double flatDepth = JsNumber.ToIntegerOrInfinity(Js.ToNumber(Or(Arg(a, "depth"), 1)));
            if (flatDepth < 0) flatDepth = 0;
            var output = new List<JToken?>();
            FlattenInto(output, arr, flatDepth);
            return JsObjects.ArrayOf(output);
        }

        private static void FlattenInto(List<JToken?> output, JArray source, double remaining)
        {
            foreach (JToken t in source)
            {
                JToken? item = JsObjects.Norm(t);
                if (remaining > 0 && item is JArray inner) FlattenInto(output, inner, remaining - 1);
                else output.Add(item);
            }
        }

        /// <summary>
        /// Sort with the TS comparator (<c>===</c> → 0, else <c>l &gt; r</c> ? 1 : -1, times
        /// direction). undefined items go last uncompared, as Array.prototype.sort does.
        /// Below 64 items this is V8's TimSort exactly (one run + binary insertion), so even
        /// an inconsistent comparison (mixed types, NaN) orders items as Chrome does.
        /// Server port: from 64 items a stable merge sort is used instead of TimSort's
        /// galloping merges — identical whenever the comparison is consistent.
        /// </summary>
        internal static JToken? Sort(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            JToken? sortBy = Arg(a, "by");
            JToken? directionArg = Arg(a, "direction");
            int direction = Js.TypeOf(directionArg) == JsType.String && Js.StringValue(directionArg!) == "desc" ? -1 : 1;
            bool hasBy = Js.Truthy(sortBy);

            var defined = new List<JToken?>();
            int undefinedCount = 0;
            foreach (JToken? item in JsObjects.Items(arr))
            {
                if (item == null) undefinedCount++;
                else defined.Add(item);
            }
            int Compare(JToken? x, JToken? y)
            {
                JToken? l = hasBy ? ValuePredicate.ReadPath(x, sortBy) : x;
                JToken? r = hasBy ? ValuePredicate.ReadPath(y, sortBy) : y;
                if (Js.StrictEquals(l, r)) return 0;
                return (Js.GreaterThan(l, r) ? 1 : -1) * direction;
            }
            List<JToken?> sorted = defined.Count < 64 ? V8ShortSort(defined, Compare) : MergeSort(defined, Compare);
            for (int i = 0; i < undefinedCount; i++) sorted.Add(null);
            return JsObjects.ArrayOf(sorted);
        }

        /// <summary>
        /// V8's TimSort for n &lt; 64 (minrun = n, so no merges): CountAndMakeRun, then
        /// BinaryInsertionSort for the rest — same comparator calls, same argument order.
        /// </summary>
        private static List<JToken?> V8ShortSort(List<JToken?> items, Func<JToken?, JToken?, int> compare)
        {
            var a = new List<JToken?>(items);
            int n = a.Count;
            if (n < 2) return a;

            // CountAndMakeRun(0, n)
            int runLength = 2;
            bool descending = compare(a[1], a[0]) < 0;
            JToken? previous = a[1];
            for (int idx = 2; idx < n; idx++)
            {
                int order = compare(a[idx], previous);
                if (descending ? order >= 0 : order < 0) break;
                previous = a[idx];
                runLength++;
            }
            if (descending) a.Reverse(0, runLength);

            // BinaryInsertionSort(0, runLength, n)
            for (int start = runLength; start < n; start++)
            {
                int left = 0;
                int right = start;
                JToken? pivot = a[start];
                while (left < right)
                {
                    int mid = left + ((right - left) >> 1);
                    if (compare(pivot, a[mid]) < 0) right = mid;
                    else left = mid + 1;
                }
                for (int p = start; p > left; p--) a[p] = a[p - 1];
                a[left] = pivot;
            }
            return a;
        }

        private static List<JToken?> MergeSort(List<JToken?> items, Func<JToken?, JToken?, int> compare)
        {
            if (items.Count <= 1) return new List<JToken?>(items);
            int mid = items.Count / 2;
            List<JToken?> left = MergeSort(items.GetRange(0, mid), compare);
            List<JToken?> right = MergeSort(items.GetRange(mid, items.Count - mid), compare);
            var merged = new List<JToken?>(items.Count);
            int li = 0;
            int ri = 0;
            while (li < left.Count && ri < right.Count)
            {
                // <= keeps equal items in their original order (stable).
                if (compare(left[li], right[ri]) <= 0) merged.Add(left[li++]);
                else merged.Add(right[ri++]);
            }
            while (li < left.Count) merged.Add(left[li++]);
            while (ri < right.Count) merged.Add(right[ri++]);
            return merged;
        }

        internal static JToken? Join(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return Js.Str(S(v, ctx));
            string separator = S(Or(Arg(a, "with"), ", "), ctx);
            var sb = new StringBuilder();
            for (int i = 0; i < arr.Count; i++)
            {
                if (i > 0) sb.Append(separator);
                JToken? item = JsObjects.Norm(arr[i]);
                if (!Js.IsNil(item)) sb.Append(S(item, ctx));
            }
            return Js.Str(sb.ToString());
        }

        internal static JToken? Sum(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return Num(OrZero(Js.ToNumber(v)));
            JToken? of = Arg(a, "of");
            double total = 0;
            foreach (JToken? item in JsObjects.Items(arr))
            {
                total += OrZero(Js.ToNumber(Js.Truthy(of) ? ValuePredicate.ReadPath(item, of) : item));
            }
            return Num(total);
        }

        /// <summary><c>x || 0</c> for a number.</summary>
        private static double OrZero(double d) => double.IsNaN(d) || d == 0 ? 0 : d;

        internal static JToken? Count(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            Num(v is JArray arr ? arr.Count : Js.IsEmpty(v) ? 0 : 1);

        internal static JToken? Limit(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            int end = JsStrings.RelativeIndex(Js.ToNumber(Or(Arg(a, "count"), 0)), arr.Count);
            return JsObjects.ArrayOf(JsObjects.Items(arr).Take(end).ToList());
        }

        /// <summary>Groups a list into { key: [items] } (key = String(readPath(item, by))).</summary>
        // Server port: keys such as "__proto__" or "constructor" are ordinary keys here; in JS they hit Object.prototype and the step throws.
        internal static JToken? GroupBy(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (!(v is JArray arr)) return v;
            JToken? by = Arg(a, "by");
            var groups = new JObject();
            foreach (JToken? item in JsObjects.Items(arr))
            {
                string key = S(ValuePredicate.ReadPath(item, by), ctx);
                if (!(groups[key] is JArray bucket))
                {
                    bucket = new JArray();
                    JsObjects.SetOwnRaw(groups, key, bucket);
                }
                bucket.Add(JsObjects.Slot(item));
            }
            return groups;
        }

        // ── choosing ────────────────────────────────────────────────────────────

        /// <summary>Supplies a value when there is none. `whenEmpty` (default true) also catches '' and [].</summary>
        internal static JToken? Default(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            bool missing = IsFalse(Arg(a, "whenEmpty")) ? Js.IsNil(v) : Js.IsEmpty(v);
            return missing ? Arg(a, "value") : v;
        }

        /// <summary>First value in the list that is actually there.</summary>
        internal static JToken? Coalesce(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            List<JToken?> candidates;
            if (v is JArray arr)
            {
                candidates = JsObjects.Items(arr);
            }
            else
            {
                candidates = new List<JToken?> { v };
                JToken? then = Arg(a, "then");
                if (!Js.IsNil(then)) candidates.AddRange(Iterate(then, "then"));
            }
            return candidates.FirstOrDefault(c => !Js.IsEmpty(c));
        }

        /// <summary>Translates a value through a table (own keys only); a miss gives `fallback`.</summary>
        internal static JToken? Lookup(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JToken table = Or(Arg(a, "table"), new JObject());
            string key = Js.Truthy(Arg(a, "caseInsensitive"))
                ? JsStrings.Trim(JsStrings.ReplaceWhiteSpaceRuns(JsStrings.ToLower(S(v, ctx)), " "))
                : S(v, ctx);
            JToken? hit = JsObjects.HasOwn(table, key) ? JsObjects.Get(table, key) : null;
            return hit ?? Arg(a, "fallback");
        }

        /// <summary>Ordered cases; the first whose `when` passes wins.</summary>
        internal static JToken? Switch(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JToken? cases = Arg(a, "cases");
            if (!Js.IsNil(cases))
            {
                foreach (JToken? branch in Iterate(cases, "cases"))
                {
                    if (Js.IsNil(branch)) throw new InvalidOperationException("Cannot read properties of " + (branch == null ? "undefined" : "null") + " (reading 'when')");
                    if (!ValuePredicate.Test(v, JsObjects.Get(branch, "when"), ctx.TimeZone)) continue;
                    JToken? branchOps = JsObjects.Get(branch, "ops");
                    if (Js.Truthy(branchOps)) return ValueOps.ApplyCore(v, branchOps, ctx, depth + 1);
                    return JsObjects.Get(branch, "then");
                }
            }
            JToken? elseOps = Arg(a, "elseOps");
            if (Js.Truthy(elseOps)) return ValueOps.ApplyCore(v, elseOps, ctx, depth + 1);
            return a.ContainsKey("else") ? Arg(a, "else") : v;
        }

        /// <summary>Runs a pipeline only when the value passes a test.</summary>
        internal static JToken? When(JToken? v, JObject a, ValueOpsContext ctx, int depth) =>
            ValuePredicate.Test(v, Arg(a, "is"), ctx.TimeZone)
                ? ValueOps.ApplyCore(v, Or(Arg(a, "then"), new JArray()), ctx, depth + 1)
                : v;

        internal static JToken? Boolean(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            JsType type = Js.TypeOf(v);
            bool truthy = !Js.IsEmpty(v)
                && !(type == JsType.Boolean && !Js.Truthy(v))
                && !(type == JsType.String && Js.StringValue(v!) == "false")
                && !(type == JsType.Number && Js.NumberValue(v!) == 0)
                && !(type == JsType.String && Js.StringValue(v!) == "0");
            if (a.ContainsKey("ifTrue") || a.ContainsKey("ifFalse")) return truthy ? Arg(a, "ifTrue") : Arg(a, "ifFalse");
            return new JValue(truthy);
        }

        internal static JToken? Exists(JToken? v, JObject a, ValueOpsContext ctx, int depth) => new JValue(!Js.IsEmpty(v));

        internal static JToken? IsEmptyOp(JToken? v, JObject a, ValueOpsContext ctx, int depth) => new JValue(Js.IsEmpty(v));

        /// <summary>Ignores what came in and emits a fixed value.</summary>
        internal static JToken? Constant(JToken? v, JObject a, ValueOpsContext ctx, int depth) => Arg(a, "value");

        /// <summary>
        /// value-ops.ts <c>script</c>: <c>new Function('value', 'return ' + code)(value)</c>; a script
        /// that throws leaves the value as it was (the browser logs and goes on). Skipped where the
        /// context cannot run scripts.
        /// </summary>
        internal static JToken? Script(JToken? v, JObject a, ValueOpsContext ctx, int depth)
        {
            if (ctx.RunScript == null)
            {
                ctx.Log?.Invoke("[valueOps] browser-only op \"script\" skipped on the server");
                return v;
            }
            var code = Arg(a, "code");
            try
            {
                return ctx.RunScript("value", Js.TypeOf(code) == JsType.Undefined ? "value" : Js.ToJsString(code), v);
            }
            catch (Exception e)
            {
                ctx.Log?.Invoke("[valueOps] script failed: " + e.Message);
                return v;
            }
        }
    }
}
