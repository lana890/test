

using EskaCMS.BLL.Services.Interfaces;
using EskaCMS.General.Services;
using EskaCMS.General.Services.Interfaces;
using EskaCMS.Infrastructure.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.General
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IGeneral, GeneralServices>();
            services.AddTransient<ILookupServices, LookupSevices>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
