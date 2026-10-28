using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Discount.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EskaCommerce.Discount
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IDiscountService, DiscountService>();
            serviceCollection.AddTransient<ISendDiscountSms, SendDiscountSms>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}

