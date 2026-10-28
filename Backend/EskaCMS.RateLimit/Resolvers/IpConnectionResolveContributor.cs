using Microsoft.AspNetCore.Http;

namespace EskaCMS.RateLimit.Resolvers
{
    public class IpConnectionResolveContributor : IIpResolveContributor
    {

        public IpConnectionResolveContributor()
        {

        }

        public string ResolveIp(HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}