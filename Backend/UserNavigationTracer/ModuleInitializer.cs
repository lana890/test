using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.UserNavigationTracer.Services.IServices;
using EskaCMS.UserNavigationTracer.Services;
using EskaCMS.UserNavigationTracer.Entites;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;

namespace EskaCMS.UserNavigationTracer
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IRepository<UserNavigation>, Repository<UserNavigation>>();
            serviceCollection.AddScoped<IRepository<UserVisit>, Repository<UserVisit>>();
            serviceCollection.AddTransient<IUserTracerService, UserTracerService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
