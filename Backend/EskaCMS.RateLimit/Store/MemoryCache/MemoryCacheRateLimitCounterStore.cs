using EskaCMS.RateLimit.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EskaCMS.RateLimit.Store.MemoryCache
{
    public class MemoryCacheRateLimitCounterStore : MemoryCacheRateLimitStore<RateLimitCounter?>, IRateLimitCounterStore
    {
        public MemoryCacheRateLimitCounterStore(IMemoryCache cache) : base(cache)
        {
        }
    }
}