using EskaCMS.RateLimit.Core;
using EskaCMS.RateLimit.Core.ProcessingStrategies;
using EskaCMS.RateLimit.Models;
using EskaCMS.RateLimit.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCMS.RateLimit.Middleware
{
    public class IpRateLimitMiddleware : RateLimitMiddleware<IpRateLimitProcessor>
    {
        private readonly ILogger<IpRateLimitMiddleware> _logger;

        public IpRateLimitMiddleware(RequestDelegate next,
            IProcessingStrategy processingStrategy,
            IOptions<IpRateLimitOptions> options,
            IRateLimitCounterStore counterStore,
            IIpPolicyStore policyStore,
            IRateLimitConfiguration config,
            ILogger<IpRateLimitMiddleware> logger
        )
            : base(next, options?.Value, new IpRateLimitProcessor(options?.Value, counterStore, policyStore, config, processingStrategy), config)
        {
            _logger = logger;
        }

        protected override void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            _logger.LogInformation($"Request {identity.HttpVerb}:{identity.Path} from IP {identity.ClientIp} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.Count - rule.Limit}. Blocked by rule {rule.Endpoint}, TraceIdentifier {httpContext.TraceIdentifier}. MonitorMode: {rule.MonitorMode}");
        }
    }
}