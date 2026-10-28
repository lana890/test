using Microsoft.AspNetCore.Http;

namespace EskaCMS.Infrastructure.Web
{
    public static class HttpRequestExtentions
    {
        public static string GetFullHostingUrlRoot(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host.Value}";
        }
    }
}
