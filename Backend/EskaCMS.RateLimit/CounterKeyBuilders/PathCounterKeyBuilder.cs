using EskaCMS.RateLimit.Models;

namespace EskaCMS.RateLimit.CounterKeyBuilders
{
    public class PathCounterKeyBuilder : ICounterKeyBuilder
    {
        public string Build(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            return $"_{requestIdentity.HttpVerb}_{requestIdentity.Path}";
        }
    }
}
