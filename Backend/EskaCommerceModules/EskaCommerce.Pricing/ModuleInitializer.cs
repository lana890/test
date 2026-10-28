using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Pricing.Services;
using EskaCommerce.Pricing.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Pricing
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICouponService, CouponService>();
            services.AddTransient<ISendRuleSms, SendRuleSms>();
            services.AddTransient<IServicePromotions, ServicePromotion>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.pricing");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }

}
