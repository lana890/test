using System.Globalization;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// What a value-ops pipeline may read from its surroundings. In the browser this
    /// is the device clock and time zone; a server flow states them explicitly so a
    /// run is reproducible (and testable).
    /// </summary>
    public sealed class ValueOpsContext
    {
        /// <summary>The instant the <c>now</c> op returns (JS <c>new Date()</c>). Never read from the system clock by the engine.</summary>
        public DateTimeOffset Now { get; init; }

        /// <summary>
        /// The zone that plays the browser's "local" time zone: date formatting,
        /// <c>dateAdd</c> calendar arithmetic, <c>parseDate</c>, date-time strings
        /// without an offset, and Hijri conversion of a local day. Server flows use Asia/Riyadh.
        /// </summary>
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

        /// <summary>
        /// Kept for callers; the ops do not use it. JS string and number ops are
        /// locale-independent (<c>toUpperCase</c>, <c>Number()</c>, <c>String()</c>), so
        /// the engine is always culture-invariant.
        /// </summary>
        public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// Resolves a <c>{ "op": "use" | "ref", "name": … }</c> step (the page's
        /// <c>transformLibrary</c> in the browser). Return a JArray of ops to run, or
        /// null when the name is unknown (the step is then skipped, as in the TS).
        /// A string (raw JS) entry cannot run on the server and is skipped.
        /// </summary>
        public Func<string, JToken?>? ResolveRef { get; init; }

        /// <summary>Receives what the browser writes to the console: unknown ops, skipped browser-only ops, steps that threw.</summary>
        public Action<string>? Log { get; init; }

        /// <summary>
        /// Runs a page transform script: (argument name, script, value) to the result, as
        /// <c>new Function(arg, "return " + script)(value)</c>. Null when scripts cannot run
        /// here (the <c>script</c> op is then skipped, as before). Throws when the script fails.
        /// </summary>
        public Func<string, string, JToken?, JToken?>? RunScript { get; init; }
    }
}
