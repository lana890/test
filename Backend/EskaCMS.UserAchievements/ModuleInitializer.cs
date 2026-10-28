using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using EskaCMS.UserAchievements.Services.Interface;
using EskaCMS.UserAchievements.Services;

namespace EskaCMS.UserAchievements
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IUserAchievementsServices, AchievementServices>();
        
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
