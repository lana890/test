
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Menus.Services.Interfaces;
using EskaCMS.Menus.Services;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Security.Services;
using EskaCMS.Infrastructure.Data;

namespace EskaCMS.Menus
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
          
         
          serviceCollection.AddTransient<IMenu, MenuService>();
            serviceCollection.AddTransient<IMenuRole, MenuRoleService>();
            serviceCollection.AddTransient<IUserRoleService, UserRoleService>();
           // serviceCollection.AddTransient<IRepository<MenuGroup>>();



        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
