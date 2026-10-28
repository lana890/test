using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MediatR;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure;
using EskaCommerce.ProductRecentlyViewed.Data;
using EskaCMS.Core.Events;
using EskaCommerce.ProductRecentlyViewed.Events;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ProductRecentlyViewed.Services.Interfaces;
using EskaCommerce.ProductRecentlyViewed.Services;

namespace EskaCommerce.ProductRecentlyViewed
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();
            services.AddTransient<IRecentlyViewedProductServices, RecentlyViewedProductServices>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.recentlyViewed");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
