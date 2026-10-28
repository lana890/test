using EskaCMS.Dashboard.Services.DashboardService;
using EskaCMS.Dashboard.Services.DashboardService.Interfaces;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Module.Core.Data;
using EskaCMS.UserDashboard.Services;
using EskaCMS.UserDashboard.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Dashboard
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDashboard, DashboardService>();
            services.AddScoped<IRepository<UserNavigations>, Repository<UserNavigations>>();
            services.AddScoped<IRepository<UserVisits>, Repository<UserVisits>>();
            services.AddTransient<IUserDashboardService, UserDashboardService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
