
using Microsoft.AspNetCore.Builder;

namespace EskaDCMSActiveDirectory.Extensions
{
    public static class JsonApiWrapperExtension
    {
        public static IApplicationBuilder UseJsonApiWrapper(this IApplicationBuilder app, string version, string name)
        {
            return app.UseMiddleware<ApiWrapper>(version, name);
        }
    }

}

