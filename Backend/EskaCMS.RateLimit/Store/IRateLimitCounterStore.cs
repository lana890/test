using EskaCMS.RateLimit.Models;

namespace EskaCMS.RateLimit.Store
{
    public interface IRateLimitCounterStore : IRateLimitStore<RateLimitCounter?>
    {
    }
}