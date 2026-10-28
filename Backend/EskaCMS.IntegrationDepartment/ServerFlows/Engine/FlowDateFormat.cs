using System.Text;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The two date formatters the runtime uses:
    /// <list type="bullet">
    /// <item><see cref="Format"/> — value-ops.ts <c>formatDate</c> (the <c>date</c>, <c>gregorian</c>
    /// and <c>localTime</c> ops): tokens <c>yyyy yy MM dd HH mm ss SSS</c>, or the names
    /// <c>iso</c>, <c>isoLocal</c>, <c>timestamp</c>.</item>
    /// <item><see cref="FormatCurrentDate"/> — dynamic-api-caller.service.ts <c>changeFormatDate</c>
    /// (the <c>currentDate</c> mapping): a Date method name, or <c>other</c> + an Angular DatePipe pattern.</item>
    /// </list>
    /// "Local" time in both is <c>zone</c>.
    /// </summary>
    public static class FlowDateFormat
    {
        /// <summary>
        /// value-ops.ts <c>formatDate(date, format)</c>. <c>iso</c> is the UTC ISO string,
        /// <c>isoLocal</c> the zone wall clock as <c>yyyy-MM-ddTHH:mm:ss.SSS</c> (no offset),
        /// <c>timestamp</c> the epoch milliseconds as a string; any other format has its
        /// tokens replaced left to right (<c>yyyy</c> unpadded, the rest zero-padded) and
        /// everything else kept literally. A null format means <c>yyyy-MM-dd</c>, the
        /// <c>date</c> op's default (the TS never passes null).
        /// </summary>
        public static string Format(DateTimeOffset value, string? format, TimeZoneInfo zone) =>
            FormatMs(value.ToUnixTimeMilliseconds(), format ?? "yyyy-MM-dd", zone);

        internal static string FormatMs(long ms, string format, TimeZoneInfo zone)
        {
            if (format == "iso") return JsDate.ToIsoString(ms);
            JsDate.LocalParts d = JsDate.Local(ms, zone);
            if (format == "isoLocal")
            {
                return d.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + JsDate.Pad(d.Month0 + 1, 2) + "-" + JsDate.Pad(d.Day, 2) +
                       "T" + JsDate.Pad(d.Hour, 2) + ":" + JsDate.Pad(d.Minute, 2) + ":" + JsDate.Pad(d.Second, 2) + "." + JsDate.Pad(d.Millisecond, 3);
            }
            if (format == "timestamp") return JsNumber.ToJsString(ms);

            var sb = new StringBuilder(format.Length + 8);
            int i = 0;
            while (i < format.Length)
            {
                // Same alternation order as /yyyy|yy|MM|dd|HH|mm|ss|SSS/g.
                if (At(format, i, "yyyy"))
                {
                    sb.Append(d.Year.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    i += 4;
                }
                else if (At(format, i, "yy"))
                {
                    sb.Append(JsDate.Pad(d.Year % 100, 2));
                    i += 2;
                }
                else if (At(format, i, "MM"))
                {
                    sb.Append(JsDate.Pad(d.Month0 + 1, 2));
                    i += 2;
                }
                else if (At(format, i, "dd"))
                {
                    sb.Append(JsDate.Pad(d.Day, 2));
                    i += 2;
                }
                else if (At(format, i, "HH"))
                {
                    sb.Append(JsDate.Pad(d.Hour, 2));
                    i += 2;
                }
                else if (At(format, i, "mm"))
                {
                    sb.Append(JsDate.Pad(d.Minute, 2));
                    i += 2;
                }
                else if (At(format, i, "ss"))
                {
                    sb.Append(JsDate.Pad(d.Second, 2));
                    i += 2;
                }
                else if (At(format, i, "SSS"))
                {
                    sb.Append(JsDate.Pad(d.Millisecond, 3));
                    i += 3;
                }
                else
                {
                    sb.Append(format[i]);
                    i++;
                }
            }
            return sb.ToString();
        }

        /// <summary>value-ops.ts <c>formatCalendarParts</c>: tokens <c>yyyy yy MM dd</c> over Hijri parts.</summary>
        internal static string FormatCalendarParts(int year, int month, int day, string format)
        {
            var sb = new StringBuilder(format.Length + 4);
            int i = 0;
            while (i < format.Length)
            {
                if (At(format, i, "yyyy"))
                {
                    sb.Append(year.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    i += 4;
                }
                else if (At(format, i, "yy"))
                {
                    sb.Append(JsDate.Pad(year % 100, 2));
                    i += 2;
                }
                else if (At(format, i, "MM"))
                {
                    sb.Append(JsDate.Pad(month, 2));
                    i += 2;
                }
                else if (At(format, i, "dd"))
                {
                    sb.Append(JsDate.Pad(day, 2));
                    i += 2;
                }
                else
                {
                    sb.Append(format[i]);
                    i++;
                }
            }
            return sb.ToString();
        }

        private static bool At(string s, int i, string token) => i + token.Length <= s.Length && string.CompareOrdinal(s, i, token, 0, token.Length) == 0;

        /// <summary>
        /// dynamic-api-caller.service.ts <c>changeFormatDate(now, {formatDate, formatDateOther})</c>,
        /// the value of a <c>currentDate</c> mapping.
        /// <list type="bullet">
        /// <item><c>other</c> → Angular DatePipe with <paramref name="formatDateOther"/> (null → <c>mediumDate</c>), en-US, in <paramref name="zone"/>.</item>
        /// <item><c>toISOString</c>/<c>toJSON</c> → "2026-10-11T08:05:03.000Z"; <c>toUTCString</c>/<c>toGMTString</c> →
        /// "Sun, 11 Oct 2026 08:05:03 GMT"; <c>toDateString</c> → "Sun Oct 11 2026" (zone).</item>
        /// <item><c>getTime</c>/<c>valueOf</c> → epoch milliseconds as a JSON integer; the other zero-argument
        /// getters (<c>getFullYear</c>, <c>getUTCHours</c>, <c>getTimezoneOffset</c> …) → their number.</item>
        /// <item><c>toString</c>/<c>toTimeString</c> → the JS layout with the platform's zone name (best effort).</item>
        /// <item>null, empty, unknown names, <c>toLocale*String</c> and non-format methods → the ISO string
        /// (the TS fallback; <see cref="IsSupportedCurrentDateFormat"/> flags the ones where the browser differs).</item>
        /// </list>
        /// Throws when the DatePipe would throw (format over 256 characters, <c>b</c>/<c>B</c> day periods).
        /// </summary>
        public static JToken FormatCurrentDate(DateTimeOffset now, string? formatDate, string? formatDateOther, TimeZoneInfo zone)
        {
            long ms = now.ToUnixTimeMilliseconds();
            switch (formatDate)
            {
                case "other":
                    return new JValue(AngularDatePipe.Format(ms, formatDateOther ?? "mediumDate", zone));
                case "toUTCString":
                case "toGMTString":
                    return new JValue(JsDate.ToUtcString(ms));
                case "toDateString":
                    return new JValue(JsDate.ToDateString(ms, zone));
                case "toString":
                    return new JValue(JsDate.ToJsString(ms, zone));
                case "toTimeString":
                    return new JValue(JsDate.ToTimeString(ms, zone));
                case "getTime":
                case "valueOf":
                    return new JValue(ms);
                case "getFullYear":
                    return new JValue(JsDate.Local(ms, zone).Year);
                case "getYear":
                    return new JValue(JsDate.Local(ms, zone).Year - 1900);
                case "getMonth":
                    return new JValue(JsDate.Local(ms, zone).Month0);
                case "getDate":
                    return new JValue(JsDate.Local(ms, zone).Day);
                case "getDay":
                    return new JValue(JsDate.Local(ms, zone).WeekDay);
                case "getHours":
                    return new JValue(JsDate.Local(ms, zone).Hour);
                case "getMinutes":
                    return new JValue(JsDate.Local(ms, zone).Minute);
                case "getSeconds":
                    return new JValue(JsDate.Local(ms, zone).Second);
                case "getMilliseconds":
                    return new JValue(JsDate.Local(ms, zone).Millisecond);
                case "getTimezoneOffset":
                    return new JValue(-JsDate.Local(ms, zone).OffsetMinutes);
                case "getUTCFullYear":
                    return new JValue(JsDate.Utc(ms).Year);
                case "getUTCMonth":
                    return new JValue(JsDate.Utc(ms).Month0);
                case "getUTCDate":
                    return new JValue(JsDate.Utc(ms).Day);
                case "getUTCDay":
                    return new JValue(JsDate.Utc(ms).WeekDay);
                case "getUTCHours":
                    return new JValue(JsDate.Utc(ms).Hour);
                case "getUTCMinutes":
                    return new JValue(JsDate.Utc(ms).Minute);
                case "getUTCSeconds":
                    return new JValue(JsDate.Utc(ms).Second);
                case "getUTCMilliseconds":
                    return new JValue(JsDate.Utc(ms).Millisecond);
                default:
                    // toISOString, toJSON, and the TS fallback for anything that is not
                    // a Date method: date?.toISOString?.()
                    return new JValue(JsDate.ToIsoString(ms));
            }
        }

        /// <summary>
        /// Whether a <c>currentDate</c> format gives the same result on the server as in
        /// the browser. False (with a reason) for <c>toLocale*String</c> (browser locale
        /// and zone), <c>toString</c>/<c>toTimeString</c> (platform zone names), Date
        /// methods that are not formatters (setters, <c>constructor</c> …), and DatePipe
        /// patterns that throw (over 256 characters, <c>b</c>/<c>B</c> day periods).
        /// Unknown names are supported: both sides fall back to toISOString.
        /// </summary>
        public static bool IsSupportedCurrentDateFormat(string? formatDate, string? formatDateOther, out string? error)
        {
            error = null;
            if (formatDate == "other")
            {
                string pattern = formatDateOther ?? "mediumDate";
                if (pattern.Length > AngularDatePipe.MaxFormatLength)
                {
                    error = "formatDateOther is longer than 256 characters; Angular's DatePipe throws on it.";
                    return false;
                }
                foreach (string part in AngularDatePipe.SplitParts(AngularDatePipe.NamedFormat(pattern) ?? pattern))
                {
                    if (AngularDatePipe.IsDayPeriodSymbol(part))
                    {
                        error = "formatDateOther uses the day-period symbol '" + part + "', which throws in the browser (no extra locale data for en-US).";
                        return false;
                    }
                }
                return true;
            }
            switch (formatDate)
            {
                case "toLocaleString":
                case "toLocaleDateString":
                case "toLocaleTimeString":
                    error = "formatDate '" + formatDate + "' depends on the browser's locale and time zone settings, so the server cannot reproduce it (it falls back to toISOString). Use 'other' with an explicit pattern.";
                    return false;
                case "toString":
                case "toTimeString":
                    error = "formatDate '" + formatDate + "' includes the time-zone display name, which differs between the browser and the server. Use 'other' with an explicit pattern.";
                    return false;
            }
            if (formatDate != null && NonFormatDateMethods.Contains(formatDate))
            {
                error = "formatDate '" + formatDate + "' is not a formatting method of Date; the server falls back to toISOString.";
                return false;
            }
            return true;
        }

        private static readonly HashSet<string> NonFormatDateMethods = new HashSet<string>(StringComparer.Ordinal)
        {
            "constructor", "toTemporalInstant", "hasOwnProperty", "isPrototypeOf", "propertyIsEnumerable",
            "__defineGetter__", "__defineSetter__", "__lookupGetter__", "__lookupSetter__",
            "setDate", "setFullYear", "setHours", "setMilliseconds", "setMinutes", "setMonth", "setSeconds", "setTime",
            "setUTCDate", "setUTCFullYear", "setUTCHours", "setUTCMilliseconds", "setUTCMinutes", "setUTCMonth", "setUTCSeconds", "setYear",
        };
    }
}
