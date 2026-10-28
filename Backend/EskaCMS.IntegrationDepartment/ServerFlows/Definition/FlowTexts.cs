namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// Every sentence the server itself sends to a page or a calling system, in the caller's language.
    /// Each comes from, most specific first:
    ///  - an older flow's own sentence by language, when a call's answer said it failed
    ///    (server.refusedMessage; a call's "On error" message is worked out by <see cref="FlowErrorMessages"/>);
    ///  - the site's translation: the Translations key "ServerFlows.{key}" in the caller's language;
    ///  - the built-in English sentence.
    /// A sentence written for the caller's language always wins. Only when none exists is a
    /// sentence the call has for every language ("*") used, then one it has in another language
    /// (specific beats generic), then the built-in one.
    /// </summary>
    public static class FlowTexts
    {
        public const string Failed = "failed";
        public const string Running = "running";
        public const string SignIn = "signIn";
        public const string SignInCustomer = "signInCustomer";
        public const string NoCart = "noCart";
        public const string TooMany = "tooMany";
        public const string WrongKey = "wrongKey";
        public const string InputRequired = "inputRequired";
        public const string InputText = "inputText";
        public const string InputNumber = "inputNumber";
        public const string InputBoolean = "inputBoolean";
        public const string InputTooLong = "inputTooLong";
        public const string InputNotObject = "inputNotObject";
        public const string InputTooLarge = "inputTooLarge";
        public const string NotStarted = "notStarted";
        public const string NeedsRunId = "needsRunId";
        public const string NoSuchRun = "noSuchRun";
        public const string NoSuchFlow = "noSuchFlow";
        public const string SwitchedOff = "switchedOff";
        public const string BadSignature = "badSignature";
        public const string TimeMissing = "timeMissing";
        public const string TimeTooFar = "timeTooFar";
        public const string TimeInMilliseconds = "timeInMilliseconds";
        public const string SignedOnly = "signedOnly";
        public const string AddressRefused = "addressRefused";
        public const string BadJson = "badJson";

        /// <summary>The key for "every language" in a texts map.</summary>
        public const string AnyLanguage = "*";

        /// <summary>The built-in English sentences; {0}, {1} are filled in where they appear.</summary>
        public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Failed] = "This could not be finished. Our team has been told and will follow it up.",
            [Running] = "Still working on it. Please try again in a moment.",
            [SignIn] = "Sign in first.",
            [SignInCustomer] = "Sign in first: this can only be done by a signed-in customer.",
            [NoCart] = "Start your purchase first: this step belongs to a cart, and there is none yet.",
            [TooMany] = "Too many requests. Try again in a minute.",
            [WrongKey] = "The key is missing or wrong.",
            [InputRequired] = "\"{0}\" is required.",
            [InputText] = "\"{0}\" must be text.",
            [InputNumber] = "\"{0}\" must be a number.",
            [InputBoolean] = "\"{0}\" must be true or false.",
            [InputTooLong] = "\"{0}\" is longer than {1} characters.",
            [InputNotObject] = "The input must be an object of named values.",
            [InputTooLarge] = "The input is larger than {0} KB.",
            [NotStarted] = "It has not been started yet.",
            [NeedsRunId] = "This flow starts a new run every time it is asked, so its result is read with the runId the start answered.",
            [NoSuchRun] = "There is no such run.",
            [NoSuchFlow] = "There is no active flow \"{0}\" that can be started this way.",
            [SwitchedOff] = "Server flows are switched off on this site.",
            [BadSignature] = "The request's signature is missing or does not match.",
            [TimeMissing] = "The request has no time: send X-Flow-Timestamp with the Unix time in seconds, the one you signed.",
            [TimeTooFar] = "The request's time ({0}) is {1} seconds from the server's time ({2}); at most {3} are allowed. Sign each request when you send it, and check the clock of the calling server.",
            [TimeInMilliseconds] = "X-Flow-Timestamp is in seconds since 1970 (Unix time), not milliseconds: the server's time now is {0}. Sign and send the time in seconds.",
            [SignedOnly] = "This flow takes signed requests only: sign the request instead of sending the key.",
            [AddressRefused] = "This flow does not take calls from this address ({0}).",
            [BadJson] = "The body is not valid JSON.",
        };

        /// <summary>"ar-SA", "AR", "ar" → "ar"; null or empty → "".</summary>
        public static string LanguageOf(string? culture)
        {
            var text = (culture ?? "").Trim().ToLowerInvariant();
            var dash = text.IndexOfAny(new[] { '-', '_' });
            return dash > 0 ? text[..dash] : text;
        }

        /// <summary>
        /// The sentence for <paramref name="key"/> in <paramref name="language"/>. <paramref name="own"/>
        /// are the call's own sentences by language (most specific first); <paramref name="siteText"/>
        /// is the site's translation in that language (null when it has none).
        /// </summary>
        public static string Pick(string key, string? language, string? siteText, params IReadOnlyDictionary<string, string>?[] own)
        {
            var lang = LanguageOf(language);
            foreach (var map in own)
                if (Exact(map, lang) is { } text)
                    return text;
            if (!string.IsNullOrWhiteSpace(siteText))
                return siteText.Trim();
            foreach (var map in own)
                if (Any(map) is { } text)
                    return text;
            foreach (var map in own)
                if (First(map) is { } text)
                    return text;
            return Defaults.TryGetValue(key, out var builtIn) ? builtIn : key;
        }

        /// <summary>A configured sentence with a wrong placeholder is shown as written, never an error.</summary>
        public static string Format(string text, params object[] args)
        {
            if (args.Length == 0)
                return text;
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        private static string? Exact(IReadOnlyDictionary<string, string>? map, string lang) =>
            map != null && lang.Length > 0 && map.TryGetValue(lang, out var text) ? text : null;

        private static string? Any(IReadOnlyDictionary<string, string>? map) =>
            map != null && (map.TryGetValue(AnyLanguage, out var text) || map.TryGetValue("", out text)) ? text : null;

        private static string? First(IReadOnlyDictionary<string, string>? map) => map?.Values.FirstOrDefault();
    }

    /// <summary>A sentence for the caller by its <see cref="FlowTexts"/> key, with the values it names ("\"{0}\" is required.").</summary>
    public sealed record CallerText(string Key, params object[] Args)
    {
        /// <summary>The built-in English sentence.</summary>
        public override string ToString() => FlowTexts.Format(FlowTexts.Defaults.TryGetValue(Key, out var text) ? text : Key, Args);
    }
}
