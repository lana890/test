using Microsoft.AspNetCore.Http;

namespace EskaCMS.RateLimit.Resolvers
{
    public interface IIpResolveContributor
    {
        string ResolveIp(HttpContext httpContext);
    }
}