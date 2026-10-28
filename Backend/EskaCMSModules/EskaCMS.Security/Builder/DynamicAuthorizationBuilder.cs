using Microsoft.Extensions.DependencyInjection;

namespace EskaCMS.Security.DynamicAuthorization.Core.Builder
{
    public class DynamicAuthorizationBuilder : IDynamicAuthorizationBuilder
    {
        public DynamicAuthorizationBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}