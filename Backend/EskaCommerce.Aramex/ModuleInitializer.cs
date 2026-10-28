using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Aramex.Services;
using EskaCommerce.Aramex.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IAramexShippingService, AramexShippingService>();
            serviceCollection.AddTransient<IIntagrateDCMSFeatures, IntegrateDCMSFeatures>();

        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

       
    }
}
