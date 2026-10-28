using EskaCMS.RateLimit.Models;
using System.Threading.Tasks;

namespace EskaCMS.RateLimit.Store
{
    public interface IIpPolicyStore : IRateLimitStore<IpRateLimitPolicies>
    {
        Task SeedAsync();
    }
}