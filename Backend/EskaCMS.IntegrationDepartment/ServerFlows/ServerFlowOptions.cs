namespace EskaCMS.IntegrationDepartment.ServerFlows
{
    /// <summary>
    /// The "ServerFlows" section of appsettings.json. Read through IOptionsMonitor, so a change
    /// applies without a restart.
    ///
    /// Tokens for data sources are NOT here as a property on purpose: a request names a token
    /// ("server": { "authToken": "gi" }) and the runner reads ServerFlows:Tokens:gi from
    /// configuration at call time, so a flow can never reach any other configuration value.
    /// </summary>
    public sealed class ServerFlowOptions
    {
        public const string SectionName = "ServerFlows";

        /// <summary>Master switch. Off: nothing is triggered or run (admin pages still work).</summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether THIS process carries out the calls and sweeps (due runs, missed payments, schedules,
        /// order statuses, old runs). Runs that pages, other systems and payments start are created in
        /// every process, so a site can create runs on the web servers and leave the work to another
        /// one: set it to false there and true on the server that should do the work.
        ///
        /// It changes nothing about safety — a run is claimed with a lease, so any number of processes
        /// may have it true. Setting it to false everywhere means runs are created and never carried out.
        /// </summary>
        public bool RunWorker { get; set; } = true;

        /// <summary>How many runs one process works on at the same time.</summary>
        public int WorkerConcurrency { get; set; } = 4;

        /// <summary>How often the worker looks for due runs and for paid orders that missed their trigger.</summary>
        public int SweepSeconds { get; set; } = 30;

        /// <summary>
        /// How long a worker owns a run before another worker may take it over (renewed before every
        /// call, and every third of it while a call is out). At least 300: a shorter lease could run
        /// out during a long call and let a second worker send it again.
        /// </summary>
        public int LeaseSeconds
        {
            get => _leaseSeconds;
            set => _leaseSeconds = Math.Max(MinLeaseSeconds, value);
        }

        private const int MinLeaseSeconds = 300;
        private int _leaseSeconds = MinLeaseSeconds;

        /// <summary>How far back the sweep looks for paid orders without a run.</summary>
        public int MissedTriggerLookbackHours { get; set; } = 48;

        /// <summary>A run that was picked up this many times without finishing stops and asks for a person.</summary>
        public int MaxRunPickups { get; set; } = 30;

        /// <summary>
        /// Time zone for dates when a flow does not set settings.timeZone (an IANA name such as
        /// "Asia/Riyadh"). Not set: the server's own time zone. Read it through <see cref="TimeZoneId"/>.
        /// </summary>
        public string? DefaultTimeZone { get; set; }

        /// <summary>The time zone flows use when they set none: DefaultTimeZone, else the server's own (as an IANA name when it has one).</summary>
        public string TimeZoneId => !string.IsNullOrWhiteSpace(DefaultTimeZone) ? DefaultTimeZone.Trim() : ServerTimeZone;

        private static string ServerTimeZone =>
            TimeZoneInfo.Local.HasIanaId || !TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out var iana)
                ? TimeZoneInfo.Local.Id
                : iana;

        /// <summary>Largest flow definition accepted, in characters.</summary>
        public int MaxDefinitionLength { get; set; } = 256 * 1024;

        /// <summary>
        /// Finished runs (done or stopped) are deleted after this many days, unless a flow says
        /// otherwise (settings.privacy.keepRunsDays). 0 = keep them.
        /// </summary>
        public int KeepRunsDays { get; set; }

        /// <summary>
        /// The longest one request of a page or another system is held open waiting for a run's
        /// result (5-90 s); a flow that waits longer has the caller ask again. Keep it below the
        /// timeouts of the proxies in front of the site.
        /// </summary>
        public int HoldSeconds { get; set; } = 30;

        /// <summary>How many runs one visitor may start per flow and minute.</summary>
        public int PageStartsPerMinute { get; set; } = 20;

        /// <summary>How many calls with its key one webhook flow takes per minute, from everyone together.</summary>
        public int WebhookCallsPerMinute { get; set; } = 60;

        /// <summary>
        /// How many other calls (no key or a wrong one, an unknown site or flow) one calling address
        /// may make per minute to one site and flow; they never use up a flow's allowance.
        /// </summary>
        public int WebhookWrongKeysPerMinute { get; set; } = 10;

        /// <summary>
        /// How far a signed request's time (X-Flow-Timestamp) may be from the server's clock, either
        /// way, in seconds (30-900, default 300). Once that has passed, a copy of the request is refused.
        /// </summary>
        public int SignatureMaxAgeSeconds { get; set; } = 300;

        /// <summary>
        /// Whether a call that reaches the server from Cloudflare is taken to come from the address
        /// Cloudflare names in CF-Connecting-IP (the caller's own address, for a flow's allowed addresses
        /// and the per-address limits). Only calls from <see cref="CloudflareRanges"/> are read so.
        /// </summary>
        public bool ReadCloudflareAddress { get; set; } = true;

        /// <summary>Cloudflare's address ranges; not set: the published ones (WebhookAuth.CloudflareRanges).</summary>
        public string[]? CloudflareRanges { get; set; }

        /// <summary>How far back a scheduled time that was missed (the server was down) is still run, in hours.</summary>
        public int ScheduleCatchUpHours { get; set; } = 24;

        /// <summary>After an e-mail about a stopped run, other runs of the same flow that stop within this many minutes are not e-mailed.</summary>
        public int AlertQuietMinutes { get; set; } = 5;
    }
}
