using EskaCMS.RateLimit.Models;
using System.Threading.Tasks;

namespace EskaCMS.RateLimit.Store
{
    public interface IClientPolicyStore : IRateLimitStore<ClientRateLimitPolicy>
    {
        Task SeedAsync();
    }
}