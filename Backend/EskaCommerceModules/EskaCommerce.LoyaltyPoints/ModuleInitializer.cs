using EskaCMS.Infrastructure.Modules;
using EskaCommerce.LoyaltyPoints.Services;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ILoyaltySetupService, LoyaltySetupService>();
            services.AddTransient<IPointsConversionService, PointsConversionService>();
            services.AddTransient<ILoyaltyWalletService, LoyaltyWalletService>();
            

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
