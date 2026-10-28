using EskaCMS.RateLimit.Models;

namespace EskaCMS.RateLimit.CounterKeyBuilders
{
    public interface ICounterKeyBuilder
    {
        string Build(ClientRequestIdentity requestIdentity, RateLimitRule rule);
    }
}