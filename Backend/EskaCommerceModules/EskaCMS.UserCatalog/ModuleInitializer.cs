using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

//using EskaCommerce.Module.Catalog.Events;
using EskaCommerce.Catalog.Services;

using EskaCommerce.Catalog.Data;

using EskaCMS.Infrastructure.Modules;
using EskaCommerce.UserCatalog.Services.Interfaces;
using EskaCommerce.UserCatalog.Services;

namespace EskaCommerce.UserCatalog
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IPlanService, PlanService>();
            services.AddTransient<IUserBookingService, UserBookingService>();
            services.AddTransient<IUserBookingApiService, UserBookingApiService>();
            services.AddTransient<IPlanProductVersionService, PlanProductVersionService>();
            services.AddTransient<IPlansVersionsService, PlansVersionsService>();
            services.AddTransient<IUserPlansService, UserPlansService>();
            services.AddTransient<IUserPlansApiService, UserPlansApiService>();
            services.AddTransient<IProviderTimeSlotsService, ProviderTimeSlotsService>();
            services.AddTransient<ISubscriptionServices, SubscriptionServices>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
