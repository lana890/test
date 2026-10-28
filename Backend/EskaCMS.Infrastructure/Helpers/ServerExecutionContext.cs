namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// Marks a DI scope as backend-only work: any background job, scheduled task or server-side
    /// chain that calls data sources (the post-payment runner is the first one). Registered as
    /// scoped: every web request gets its own instance with the flag off, and only code that creates
    /// its own scope switches it on. Nothing a client sends can set it.
    ///
    /// Data sources listed in configuration under DataSources:ServerOnlyIds run only in such a
    /// scope (checked in ApiMapperService).
    /// </summary>
    public sealed class ServerExecutionContext
    {
        public bool IsServerScope { get; private set; }

        /// <summary>Call once, right after creating the scope, before resolving anything that calls data sources.</summary>
        public void EnterServerScope() => IsServerScope = true;
    }
}
