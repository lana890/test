

using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Vendors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCommerce.Vendors
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IVendorService, VendorService>();
            //GlobalConfiguration.RegisterAngularModule("simplAdmin.vendors");


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
