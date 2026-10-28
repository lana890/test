using EskaCMS.RateLimit.CounterKeyBuilders;
using EskaCMS.RateLimit.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.RateLimit.Core.ProcessingStrategies
{
    public interface IProcessingStrategy
    {
        Task<RateLimitCounter> ProcessRequestAsync(ClientRequestIdentity requestIdentity, RateLimitRule rule, ICounterKeyBuilder counterKeyBuilder, RateLimitOptions rateLimitOptions, CancellationToken cancellationToken = default);
    }
}