

using EskaCMS.Infrastructure.Web;
using Microsoft.AspNetCore.Builder;

namespace Eska.Core.Api
{
    public static class JsonApiWrapperExtension
    {
        public static IApplicationBuilder UseJsonApiWrapper(this IApplicationBuilder app, string version, string name)
        {
            return app.UseMiddleware<EskaCMS.Middlewares.Middlewares.ApiWrapper>(version, name);
        }
    }

}
