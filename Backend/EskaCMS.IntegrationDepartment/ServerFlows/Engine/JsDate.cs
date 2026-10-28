using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// JS Date arithmetic with the browser's "local" time zone replaced by an explicit
    /// <see cref="TimeZoneInfo"/>: MakeDay/MakeTime (with month/day rollover), local ↔ UTC,
    /// the toString family and <c>new Date(value)</c>.
    /// </summary>
    /// <remarks>
    /// Dates are epoch milliseconds (long). The representable range is that of
    /// <see cref="DateTimeOffset"/> (years 1–9999); JS allows ±275760 years. A date
    /// outside the range is treated as an Invalid Date.
    /// </remarks>
    internal static class JsDate
    {
        internal const long MinMs = -62135596800000L; // 0001-01-01T00:00:00Z
        internal const long MaxMs = 253402300799999L; // 9999-12-31T23:59:59.999Z
        internal const double MsPerDay = 86400000d;

        private static readonly string[] WeekDays = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static readonly string[] MonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        /// <summary>A Date token for an epoch-millisecond value.</summary>
        internal static JValue Create(long ms) => new JValue(DateTimeOffset.FromUnixTimeMilliseconds(ms));

        /// <summary>Days since 1970-01-01 of a proleptic Gregorian date (month 1–12).</summary>
        internal static long DaysFromCivil(long y, int m, int d)
        {
            y -= m <= 2 ? 1 : 0;
            long era = (y >= 0 ? y : y - 399) / 400;
            long yoe = y - era * 400;
            long doy = (153 * (m + (m > 2 ? -3 : 9)) + 2) / 5 + d - 1;
            long doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
            return era * 146097 + doe - 719468;
        }

        /// <summary>ES MakeDay: year/month/date with month and day rollover; NaN when not finite.</summary>
        internal static double MakeDay(double year, double month, double date)
        {
            if (!IsFinite(year) || !IsFinite(month) || !IsFinite(date)) return double.NaN;
            double y = Math.Truncate(year);
            double m = Math.Truncate(month);
            double dt = Math.Truncate(date);
            double ym = y + Math.Floor(m / 12);
            if (Math.Abs(ym) > 400000) return double.NaN;
            double mn = m - Math.Floor(m / 12) * 12;
            return DaysFromCivil((long)ym, (int)mn + 1, 1) + dt - 1;
        }

        /// <summary>ES MakeTime.</summary>
        internal static double MakeTime(double hour, double min, double sec, double ms)
        {
            if (!IsFinite(hour) || !IsFinite(min) || !IsFinite(sec) || !IsFinite(ms)) return double.NaN;
            return Math.Truncate(hour) * 3600000d + Math.Truncate(min) * 60000d + Math.Truncate(sec) * 1000d + Math.Truncate(ms);
        }

        /// <summary>ES MakeDate.</summary>
        internal static double MakeDate(double day, double time)
        {
            if (!IsFinite(day) || !IsFinite(time)) return double.NaN;
            return day * MsPerDay + time;
        }

        /// <summary>TimeClip, narrowed to the DateTimeOffset range: null = Invalid Date.</summary>
        internal static long? Clip(double t)
        {
            if (double.IsNaN(t) || t < MinMs || t > MaxMs) return null;
            return (long)Math.Truncate(t);
        }

        private static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

        /// <summary>The local (zone) fields of an instant, as the Date getters return them.</summary>
        internal readonly struct LocalParts
        {
            internal LocalParts(DateTime wall, TimeSpan offset, bool daylight)
            {
                Year = wall.Year;
                Month0 = wall.Month - 1;
                Day = wall.Day;
                Hour = wall.Hour;
                Minute = wall.Minute;
                Second = wall.Second;
                Millisecond = wall.Millisecond;
                WeekDay = (int)wall.DayOfWeek;
                OffsetMinutes = (int)Math.Round(offset.TotalMinutes);
                IsDaylight = daylight;
            }

            internal int Year { get; }

            internal int Month0 { get; }

            internal int Day { get; }

            internal int Hour { get; }

            internal int Minute { get; }

            internal int Second { get; }

            internal int Millisecond { get; }

            internal int WeekDay { get; }

            /// <summary>Minutes east of UTC (getTimezoneOffset() is the negation).</summary>
            internal int OffsetMinutes { get; }

            internal bool IsDaylight { get; }
        }

        internal static LocalParts Local(long ms, TimeZoneInfo zone)
        {
            DateTimeOffset utc = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            TimeSpan offset = zone.GetUtcOffset(utc);
            DateTime wall = utc.UtcDateTime + offset;
            return new LocalParts(wall, offset, zone.SupportsDaylightSavingTime && zone.IsDaylightSavingTime(utc));
        }

        internal static LocalParts Utc(long ms) => new LocalParts(DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime, TimeSpan.Zero, false);

        /// <summary>
        /// ES UTC(t): a wall-clock time in <paramref name="zone"/> to an instant. In a
        /// DST overlap the earlier instant wins; in a gap the offset from before the
        /// transition is used (the time moves forward) — the ES2023 rules V8 follows.
        /// </summary>
        internal static long? LocalToUtc(double localMs, TimeZoneInfo zone)
        {
            if (double.IsNaN(localMs) || localMs < MinMs - 2 * MsPerDay || localMs > MaxMs + 2 * MsPerDay) return null;
            if (zone == TimeZoneInfo.Utc) return Clip(localMs);
            long ticks = DateTime.UnixEpoch.Ticks + (long)Math.Truncate(localMs) * TimeSpan.TicksPerMillisecond;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return null;
            var wall = new DateTime(ticks, DateTimeKind.Unspecified);
            TimeSpan offset;
            if (zone.IsAmbiguousTime(wall))
            {
                TimeSpan[] offsets = zone.GetAmbiguousTimeOffsets(wall);
                offset = offsets.Length == 0 ? zone.GetUtcOffset(wall) : offsets.Max();
            }
            else if (zone.IsInvalidTime(wall))
            {
                try
                {
                    offset = zone.GetUtcOffset(DateTime.SpecifyKind(wall, DateTimeKind.Utc).AddDays(-1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    offset = zone.BaseUtcOffset;
                }
            }
            else
            {
                offset = zone.GetUtcOffset(wall);
            }
            return Clip(Math.Truncate(localMs) - offset.TotalMilliseconds);
        }

        /// <summary>
        /// <c>new Date(y, m, d, h, mi, s, ms)</c> in <paramref name="zone"/>: rollover
        /// included, and a year of 0–99 means 1900–1999 as the constructor specifies.
        /// </summary>
        internal static long? Construct(double year, double month0, double day, double hour, double minute, double second, double ms, TimeZoneInfo zone)
        {
            double y = year;
            if (!double.IsNaN(y))
            {
                double yi = JsNumber.ToIntegerOrInfinity(y);
                if (yi >= 0 && yi <= 99) y = 1900 + yi;
            }
            return LocalToUtc(MakeDate(MakeDay(y, month0, day), MakeTime(hour, minute, second, ms)), zone);
        }

        /// <summary>Date.UTC(y, m, d) as days since the epoch (whole days only).</summary>
        internal static long UtcDay(int year, int month0, int day) => DaysFromCivil(year, month0 + 1, day);

        /// <summary><c>new Date(value)</c> with one argument; null = Invalid Date.</summary>
        internal static long? FromValue(JToken? v, TimeZoneInfo zone)
        {
            switch (Js.TypeOf(v))
            {
                case JsType.Date:
                    return Js.DateMs(v!);
                case JsType.Undefined:
                    return null;
                case JsType.Null:
                    return 0;
                case JsType.Boolean:
                case JsType.Number:
                    return Clip(Js.ToNumber(v));
                case JsType.String:
                    return Clip(JsDateParser.Parse(Js.StringValue(v!), zone));
                default:
                    // ToPrimitive(default) of an array/object is its string form.
                    return Clip(JsDateParser.Parse(Js.ToJsString(v, zone), zone));
            }
        }

        /// <summary>value-ops.ts <c>toDate</c>: a valid Date, or null for nil, "" and anything unparseable.</summary>
        internal static long? ToDate(JToken? v, TimeZoneInfo zone)
        {
            JsType t = Js.TypeOf(v);
            if (t == JsType.Date) return Js.DateMs(v!);
            if (t == JsType.Undefined || t == JsType.Null) return null;
            if (t == JsType.String && Js.StringValue(v!).Length == 0) return null;
            return FromValue(v, zone);
        }

        // ── the toString family ────────────────────────────────────────────────

        internal static string ToIsoString(long ms)
        {
            LocalParts p = Utc(ms);
            return Pad(p.Year, 4) + "-" + Pad(p.Month0 + 1, 2) + "-" + Pad(p.Day, 2) + "T" +
                   Pad(p.Hour, 2) + ":" + Pad(p.Minute, 2) + ":" + Pad(p.Second, 2) + "." + Pad(p.Millisecond, 3) + "Z";
        }

        /// <summary>Date.prototype.toUTCString (and toGMTString): "Sun, 11 Oct 2026 08:05:03 GMT".</summary>
        internal static string ToUtcString(long ms)
        {
            LocalParts p = Utc(ms);
            return WeekDays[p.WeekDay] + ", " + Pad(p.Day, 2) + " " + MonthNames[p.Month0] + " " + Pad(p.Year, 4) + " " +
                   Pad(p.Hour, 2) + ":" + Pad(p.Minute, 2) + ":" + Pad(p.Second, 2) + " GMT";
        }

        /// <summary>Date.prototype.toDateString: "Sun Oct 11 2026".</summary>
        internal static string ToDateString(long ms, TimeZoneInfo zone) => DateString(Local(ms, zone));

        /// <summary>Date.prototype.toTimeString: "12:05:03 GMT+0300 (Zone Name)".</summary>
        internal static string ToTimeString(long ms, TimeZoneInfo zone)
        {
            LocalParts p = Local(ms, zone);
            return TimeString(p) + TimeZoneString(p, zone);
        }

        /// <summary>Date.prototype.toString: "Sun Oct 11 2026 12:05:03 GMT+0300 (Zone Name)".</summary>
        internal static string ToJsString(long ms, TimeZoneInfo zone)
        {
            LocalParts p = Local(ms, zone);
            return DateString(p) + " " + TimeString(p) + TimeZoneString(p, zone);
        }

        private static string DateString(LocalParts p) =>
            WeekDays[p.WeekDay] + " " + MonthNames[p.Month0] + " " + Pad(p.Day, 2) + " " + Pad(p.Year, 4);

        private static string TimeString(LocalParts p) =>
            Pad(p.Hour, 2) + ":" + Pad(p.Minute, 2) + ":" + Pad(p.Second, 2) + " GMT";

        private static string TimeZoneString(LocalParts p, TimeZoneInfo zone)
        {
            int offset = p.OffsetMinutes;
            var sb = new StringBuilder(32);
            sb.Append(offset >= 0 ? '+' : '-');
            int abs = Math.Abs(offset);
            sb.Append(Pad(abs / 60, 2)).Append(Pad(abs % 60, 2));
            // The zone name in parentheses is implementation-defined; browsers use
            // ICU names, .NET gives the platform's. Only this part may differ.
            string name = p.IsDaylight ? zone.DaylightName : zone.StandardName;
            if (!string.IsNullOrEmpty(name)) sb.Append(" (").Append(name).Append(')');
            return sb.ToString();
        }

        internal static string Pad(long n, int width) => n.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
    }
}
