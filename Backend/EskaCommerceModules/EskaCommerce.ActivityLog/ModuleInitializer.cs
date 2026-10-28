using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EskaCommerce.Module.ActivityLog.Data;
using EskaCommerce.Module.ActivityLog.Events;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Core.Events;
using EskaCMS.Infrastructure;

namespace EskaCommerce.Module.ActivityLog
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IActivityTypeRepository, ActivityRepository>();
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();

           // GlobalConfiguration.RegisterAngularModule("simplAdmin.activityLog");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
