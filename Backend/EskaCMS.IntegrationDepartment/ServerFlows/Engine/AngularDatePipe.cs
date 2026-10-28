using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// Angular's <c>formatDate</c> (what <c>DatePipe.transform</c> runs) for the en-US
    /// locale the dynamic site uses (no LOCALE_ID is registered) and no explicit
    /// timezone (the pipe then formats in the browser's zone — here, the given zone).
    /// Ported from @angular/common 21.2 (<c>_common_module-chunk.mjs</c>).
    /// </summary>
    internal static class AngularDatePipe
    {
        internal const int MaxFormatLength = 256;

        // Angular's DATE_FORMATS_SPLIT, unchanged. The input is config, so it is time-boxed.
        private static readonly Regex FormatsSplit = new Regex(
            @"((?:[^BEGHLMOSWYZabcdhmswyz']+)|(?:'(?:[^']|'')*')|(?:G{1,5}|y{1,4}|Y{1,4}|M{1,5}|L{1,5}|w{1,2}|W{1}|d{1,2}|E{1,6}|c{1,6}|a{1,5}|b{1,5}|B{1,5}|h{1,2}|H{1,2}|m{1,2}|s{1,2}|S{1,3}|z{1,4}|Z{1,5}|O{1,4}))([\s\S]*)",
            RegexOptions.CultureInvariant,
            JsRegex.MatchTimeout);

        private static readonly string[] MonthsNarrow = { "J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
        private static readonly string[] MonthsAbbr = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private static readonly string[] MonthsWide = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        private static readonly string[] DaysNarrow = { "S", "M", "T", "W", "T", "F", "S" };
        private static readonly string[] DaysAbbr = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static readonly string[] DaysWide = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        private static readonly string[] DaysShort = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };

        // en-US CLDR data as Angular ships it; U+202F (narrow no-break space) before AM/PM is real.
        private const string ShortDate = "M/d/yy";
        private const string MediumDate = "MMM d, y";
        private const string LongDate = "MMMM d, y";
        private const string FullDate = "EEEE, MMMM d, y";
        private const string ShortTime = "h:mm\u202Fa";
        private const string MediumTime = "h:mm:ss\u202Fa";
        private const string LongTime = "h:mm:ss\u202Fa z";
        private const string FullTime = "h:mm:ss\u202Fa zzzz";

        /// <summary>DatePipe.transform(date, format) for a valid date.</summary>
        internal static string Format(long ms, string format, TimeZoneInfo zone)
        {
            if (format.Length > MaxFormatLength)
            {
                throw new InvalidOperationException("Date format is too long. Exceeded maximum length of 256 characters.");
            }
            List<string> parts = SplitParts(NamedFormat(format) ?? format);
            JsDate.LocalParts local = JsDate.Local(ms, zone);
            var text = new StringBuilder();
            foreach (string part in parts)
            {
                string? formatted = FormatPart(part, local);
                if (formatted != null) text.Append(formatted);
                else if (part == "''") text.Append('\'');
                else text.Append(StripQuotes(part));
            }
            return text.ToString();
        }

        /// <summary>The format split into symbol / literal / quoted parts, exactly as Angular's loop does it.</summary>
        internal static List<string> SplitParts(string format)
        {
            var parts = new List<string>();
            string rest = format;
            while (rest.Length > 0)
            {
                Match m = FormatsSplit.Match(rest);
                if (!m.Success)
                {
                    parts.Add(rest);
                    break;
                }
                // Text before the match (only possible for a lone unmatched quote) is dropped, as in Angular.
                parts.Add(m.Groups[1].Value);
                rest = m.Groups[2].Value;
            }
            return parts;
        }

        /// <summary>The en-US expansion of a named format, or null.</summary>
        internal static string? NamedFormat(string format)
        {
            switch (format)
            {
                case "shortDate": return ShortDate;
                case "mediumDate": return MediumDate;
                case "longDate": return LongDate;
                case "fullDate": return FullDate;
                case "shortTime": return ShortTime;
                case "mediumTime": return MediumTime;
                case "longTime": return LongTime;
                case "fullTime": return FullTime;
                // en dateTimeFormats are ['{1}, {0}', u, u, u]: every width falls back to '{1}, {0}'.
                case "short": return ShortDate + ", " + ShortTime;
                case "medium": return MediumDate + ", " + MediumTime;
                case "long": return LongDate + ", " + LongTime;
                case "full": return FullDate + ", " + FullTime;
                default: return null;
            }
        }

        /// <summary>True for the day-period symbols that throw in Angular for en (no extra locale data loaded).</summary>
        internal static bool IsDayPeriodSymbol(string part) => part.Length > 0 && part.Length <= 5 && (part[0] == 'b' || part[0] == 'B') && part.Trim(part[0]).Length == 0;

        private static string StripQuotes(string part)
        {
            string s = part;
            if (s.StartsWith('\'')) s = s.Substring(1);
            if (s.EndsWith('\'')) s = s.Substring(0, s.Length - 1);
            return s.Replace("''", "'");
        }

        private static string? FormatPart(string part, JsDate.LocalParts d)
        {
            switch (part)
            {
                case "G":
                case "GG":
                case "GGG":
                    return d.Year <= 0 ? "BC" : "AD";
                case "GGGG":
                    return d.Year <= 0 ? "Before Christ" : "Anno Domini";
                case "GGGGG":
                    return d.Year <= 0 ? "B" : "A";
                case "y":
                    return DateGetter(d.Year, 1, 0, false, true);
                case "yy":
                    return DateGetter(d.Year, 2, 0, true, true);
                case "yyy":
                    return DateGetter(d.Year, 3, 0, false, true);
                case "yyyy":
                    return DateGetter(d.Year, 4, 0, false, true);
                case "Y":
                    return PadNumber(WeekNumberingYear(d), 1, false, false);
                case "YY":
                    return PadNumber(WeekNumberingYear(d), 2, true, false);
                case "YYY":
                    return PadNumber(WeekNumberingYear(d), 3, false, false);
                case "YYYY":
                    return PadNumber(WeekNumberingYear(d), 4, false, false);
                case "M":
                case "L":
                    return DateGetter(d.Month0, 1, 1, false, false);
                case "MM":
                case "LL":
                    return DateGetter(d.Month0, 2, 1, false, false);
                case "MMM":
                case "LLL":
                    return MonthsAbbr[d.Month0];
                case "MMMM":
                case "LLLL":
                    return MonthsWide[d.Month0];
                case "MMMMM":
                case "LLLLL":
                    return MonthsNarrow[d.Month0];
                case "w":
                    return PadNumber(IsoWeek(d), 1, false, false);
                case "ww":
                    return PadNumber(IsoWeek(d), 2, false, false);
                case "W":
                    return PadNumber(WeekOfMonth(d), 1, false, false);
                case "d":
                    return DateGetter(d.Day, 1, 0, false, false);
                case "dd":
                    return DateGetter(d.Day, 2, 0, false, false);
                case "c":
                case "cc":
                    return DateGetter(d.WeekDay, 1, 0, false, false);
                case "ccc":
                case "E":
                case "EE":
                case "EEE":
                    return DaysAbbr[d.WeekDay];
                case "cccc":
                case "EEEE":
                    return DaysWide[d.WeekDay];
                case "ccccc":
                case "EEEEE":
                    return DaysNarrow[d.WeekDay];
                case "cccccc":
                case "EEEEEE":
                    return DaysShort[d.WeekDay];
                case "a":
                case "aa":
                case "aaa":
                case "aaaa":
                    return d.Hour < 12 ? "AM" : "PM";
                case "aaaaa":
                    return d.Hour < 12 ? "a" : "p";
                case "h":
                    return HourGetter(d.Hour, 1);
                case "hh":
                    return HourGetter(d.Hour, 2);
                case "H":
                    return DateGetter(d.Hour, 1, 0, false, false);
                case "HH":
                    return DateGetter(d.Hour, 2, 0, false, false);
                case "m":
                    return DateGetter(d.Minute, 1, 0, false, false);
                case "mm":
                    return DateGetter(d.Minute, 2, 0, false, false);
                case "s":
                    return DateGetter(d.Second, 1, 0, false, false);
                case "ss":
                    return DateGetter(d.Second, 2, 0, false, false);
                case "S":
                    return PadNumber(d.Millisecond, 3, false, false).Substring(0, 1);
                case "SS":
                    return PadNumber(d.Millisecond, 3, false, false).Substring(0, 2);
                case "SSS":
                    return PadNumber(d.Millisecond, 3, false, false).Substring(0, 3);
                case "Z":
                case "ZZ":
                case "ZZZ":
                    return TimeZone(d, 0);
                case "ZZZZZ":
                    return TimeZone(d, 3);
                case "O":
                case "OO":
                case "OOO":
                case "z":
                case "zz":
                case "zzz":
                    return TimeZone(d, 1);
                case "OOOO":
                case "ZZZZ":
                case "zzzz":
                    return TimeZone(d, 2);
                default:
                    if (IsDayPeriodSymbol(part))
                    {
                        // getLocaleExtraDayPeriodRules throws: the built-in en data has no extra day periods.
                        throw new InvalidOperationException("Missing extra locale data for the locale \"en-US\". Use \"registerLocaleData\" to load new data.");
                    }
                    return null;
            }
        }

        private static string DateGetter(int part, int size, int offset, bool trim, bool negWrap)
        {
            if (offset > 0 || part > -offset) part += offset;
            return PadNumber(part, size, trim, negWrap);
        }

        private static string HourGetter(int hour, int size)
        {
            int part = hour;
            if (part > 12) part -= 12;
            if (part == 0) part = 12;
            return PadNumber(part, size, false, false);
        }

        private static string PadNumber(int num, int digits, bool trim, bool negWrap)
        {
            string neg = string.Empty;
            if (num < 0 || (negWrap && num <= 0))
            {
                if (negWrap)
                {
                    num = -num + 1;
                }
                else
                {
                    num = -num;
                    neg = "-";
                }
            }
            string s = num.ToString(CultureInfo.InvariantCulture);
            if (s.Length < digits) s = s.PadLeft(digits, '0');
            if (trim) s = s.Substring(s.Length - digits);
            return neg + s;
        }

        private static string TimeZone(JsDate.LocalParts d, int width)
        {
            int zone = d.OffsetMinutes; // -getTimezoneOffset()
            int hours = zone > 0 ? (int)Math.Floor(zone / 60d) : (int)Math.Ceiling(zone / 60d);
            string sign = zone >= 0 ? "+" : string.Empty;
            string minutes = PadNumber(Math.Abs(zone % 60), 2, false, false);
            switch (width)
            {
                case 0:
                    return sign + PadNumber(hours, 2, false, false) + minutes;
                case 1:
                    return "GMT" + sign + PadNumber(hours, 1, false, false);
                case 2:
                    return "GMT" + sign + PadNumber(hours, 2, false, false) + ":" + minutes;
                default:
                    return zone == 0 ? "Z" : sign + PadNumber(hours, 2, false, false) + ":" + minutes;
            }
        }

        private static long DayOf(JsDate.LocalParts d) => JsDate.DaysFromCivil(d.Year, d.Month0 + 1, d.Day);

        private static int WeekDayOf(long day) => (int)(((day + 4) % 7 + 7) % 7); // 1970-01-01 was a Thursday

        private static long ThursdayThisIsoWeek(JsDate.LocalParts d)
        {
            int delta = d.WeekDay == 0 ? -3 : 4 - d.WeekDay;
            return DayOf(d) + delta;
        }

        private static int YearOfDay(long day) => DateOnly.FromDayNumber((int)(day + 719162)).Year;

        private static int WeekNumberingYear(JsDate.LocalParts d) => YearOfDay(ThursdayThisIsoWeek(d));

        private static int IsoWeek(JsDate.LocalParts d)
        {
            long thursday = ThursdayThisIsoWeek(d);
            int year = YearOfDay(thursday);
            long jan1 = JsDate.DaysFromCivil(year, 1, 1);
            int firstDay = WeekDayOf(jan1);
            long firstThursday = jan1 + (firstDay <= 4 ? 4 : 11) - firstDay;
            return 1 + (int)Math.Round((thursday - firstThursday) / 7d);
        }

        private static int WeekOfMonth(JsDate.LocalParts d)
        {
            int daysBeforeFirst = WeekDayOf(JsDate.DaysFromCivil(d.Year, d.Month0 + 1, 1)) - 1;
            return 1 + (int)Math.Floor((d.Day + daysBeforeFirst) / 7d);
        }
    }
}
