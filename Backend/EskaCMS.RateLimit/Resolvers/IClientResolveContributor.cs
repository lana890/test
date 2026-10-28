using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EskaCMS.RateLimit.Resolvers
{
    public interface IClientResolveContributor
    {
        Task<string> ResolveClientAsync(HttpContext httpContext);
    }
}
