using EskaCMS.RateLimit.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace EskaCMS.RateLimit.Store.DistributedCache
{
    public class DistributedCacheRateLimitCounterStore : DistributedCacheRateLimitStore<RateLimitCounter?>, IRateLimitCounterStore
    {
        public DistributedCacheRateLimitCounterStore(IDistributedCache cache) : base(cache)
        {
        }
    }
}