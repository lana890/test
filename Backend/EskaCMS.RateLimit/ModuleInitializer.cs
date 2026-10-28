using EskaCMS.Infrastructure.Modules;
using EskaCMS.RateLimit.Core.ProcessingStrategies;
using EskaCMS.RateLimit.Middleware;
using EskaCMS.RateLimit.Store;
using EskaCMS.RateLimit.Store.DistributedCache;
using EskaCMS.RateLimit.Store.MemoryCache;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCMS.RateLimit
{
    public class ModuleInitializer : IModuleInitializer
    {

        public void ConfigureServices(IServiceCollection services)
        {

            //services.AddDistributedRateLimitingStores();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
        //public static IServiceCollection AddInMemoryRateLimiting<T>(this IServiceCollection services)where T : class
        //{
        //    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        //    services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        //    services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
        //    services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        //    services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        //        return services;
        //}

        //public static IServiceCollection AddDistributedRateLimiting<T>(this IServiceCollection services)where T : class 
        //{
        //    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        //    services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
        //    services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
        //    services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
        //    services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        //    return services;
        //}

        //public static IServiceCollection AddDistributedRateLimiting<T>(this IServiceCollection services)
        //    where T : class, IProcessingStrategy
        //{
        //    services.AddDistributedRateLimitingStores();
        //    services.AddSingleton<IProcessingStrategy, T>();
        //    return services;
        //}

        //private static IServiceCollection AddDistributedRateLimitingStores(this IServiceCollection services)
        //{
        //    services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
        //    services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
        //    services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
        //    return services;
        //}

        //public static IApplicationBuilder UseIpRateLimiting(this IApplicationBuilder builder)
        //{
        //    return builder.UseMiddleware<IpRateLimitMiddleware>();
        //}

        //public static IApplicationBuilder UseClientRateLimiting(this IApplicationBuilder builder)
        //{
        //    return builder.UseMiddleware<ClientRateLimitMiddleware>();
        //}
    }
}