using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.Services;
using EskaCMS.Entities.Entity.UserDashboard;

namespace EskaCMS.UserDashboard
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IRepository<UserNavigations>, Repository<UserNavigations>>();
            serviceCollection.AddScoped<IRepository<UserVisits>, Repository<UserVisits>>();
            serviceCollection.AddTransient<IUserDashboardService, UserDashboardService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
