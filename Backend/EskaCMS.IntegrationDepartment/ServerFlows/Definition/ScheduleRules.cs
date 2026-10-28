using System.Globalization;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// When a scheduled flow is due. Pure (no clock, no database), so the rules are unit-tested:
    /// times are counted in the flow's time zone, and every time is turned back into an exact
    /// moment (UTC) for the run key and for "is it due yet".
    ///
    /// Summer time, in a zone that has it: a time the clock skips when it goes forward does not run
    /// that day; when the clock goes back, a time in the repeated hour runs once, at its second
    /// showing, so an everyMinutes schedule leaves a gap of an hour more across the change.
    /// </summary>
    public static class ScheduleRules
    {
        /// <summary>How far back a missed time is still run (after a stop) when the server does not say: one day.</summary>
        public static readonly TimeSpan DefaultCatchUp = TimeSpan.FromDays(1);

        /// <summary>
        /// The latest time at or before <paramref name="now"/> (and not before <paramref name="notBefore"/>,
        /// e.g. when the version was switched on, and not older than <paramref name="catchUp"/>), or null.
        /// </summary>
        public static DateTimeOffset? LatestDue(ScheduleSettings schedule, TimeZoneInfo zone, DateTimeOffset now, DateTimeOffset notBefore, TimeSpan? catchUp = null)
        {
            var window = catchUp ?? DefaultCatchUp;
            var floor = notBefore > now - window ? notBefore : now - window;
            var localNow = TimeZoneInfo.ConvertTime(now, zone);
            // Today and the days before it, newest first, until one has a time in the window.
            var days = (int)Math.Ceiling(window.TotalDays) + 1;
            for (var back = 0; back <= days; back++)
            {
                var day = localNow.Date.AddDays(-back);
                if (schedule.Days.Count > 0 && !schedule.Days.Contains(day.DayOfWeek))
                    continue;
                foreach (var minute in MinutesOf(schedule).Reverse())
                {
                    var at = Moment(day, minute, zone);
                    if (at == null || at > now)
                        continue;
                    return at >= floor ? at : null;
                }
            }
            return null;
        }

        /// <summary>The next <paramref name="count"/> times after <paramref name="after"/>, for showing the schedule.</summary>
        public static IReadOnlyList<DateTimeOffset> Next(ScheduleSettings schedule, TimeZoneInfo zone, DateTimeOffset after, int count)
        {
            var list = new List<DateTimeOffset>();
            var localAfter = TimeZoneInfo.ConvertTime(after, zone);
            // A weekly schedule needs a week per time; anything more often stops much sooner.
            for (var ahead = 0; ahead <= 7 * Math.Max(1, count) + 1 && list.Count < count; ahead++)
            {
                var day = localAfter.Date.AddDays(ahead);
                if (schedule.Days.Count > 0 && !schedule.Days.Contains(day.DayOfWeek))
                    continue;
                foreach (var minute in MinutesOf(schedule))
                {
                    var at = Moment(day, minute, zone);
                    if (at == null || at <= after)
                        continue;
                    list.Add(at.Value);
                    if (list.Count == count)
                        break;
                }
            }
            return list;
        }

        /// <summary>
        /// The run key of one time: one run per flow (by name, across versions) and time. Written with
        /// the invariant culture: a server whose culture uses another calendar (ar-SA: Hijri) must
        /// write the same key for the same moment.
        /// </summary>
        public static string RunKey(long siteId, string flowName, DateTimeOffset at) =>
            string.Create(CultureInfo.InvariantCulture, $"schedule:{siteId}:{flowName}:{at.UtcDateTime:yyyyMMddHHmm}");

        /// <summary>
        /// The key the versions before 2026-10-15 wrote for a time: in the server's own culture, so on
        /// a server with another calendar (ar-SA: Hijri) it differs from <see cref="RunKey"/>. Checked
        /// too while such keys may still be the latest, so a time is not run a second time after the
        /// update; null when it is the same key.
        /// </summary>
        public static string? OlderRunKey(long siteId, string flowName, DateTimeOffset at)
        {
            var older = string.Create(CultureInfo.CurrentCulture, $"schedule:{siteId}:{flowName}:{at.UtcDateTime:yyyyMMddHHmm}");
            return older == RunKey(siteId, flowName, at) ? null : older;
        }

        /// <summary>The run key of a run an admin started outside the schedule (Try it): never the same as a time's.</summary>
        public static string ManualRunKey(long siteId, string flowName) =>
            string.Create(CultureInfo.InvariantCulture, $"schedule:{siteId}:{flowName}:now:{Guid.NewGuid():N}");

        /// <summary>The minutes after midnight the flow runs at, in order.</summary>
        public static IEnumerable<int> MinutesOf(ScheduleSettings schedule)
        {
            if (schedule.EveryMinutes is int every && every > 0)
            {
                for (var m = 0; m < 24 * 60; m += every)
                    yield return m;
                yield break;
            }
            foreach (var m in schedule.Times)
                yield return m;
        }

        /// <summary>A local day and minute as an exact moment; null for a time the clock skips (summer time).</summary>
        private static DateTimeOffset? Moment(DateTime localDay, int minute, TimeZoneInfo zone)
        {
            var local = DateTime.SpecifyKind(localDay.AddMinutes(minute), DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local))
                return null;
            return new DateTimeOffset(local, zone.GetUtcOffset(local));
        }
    }
}
