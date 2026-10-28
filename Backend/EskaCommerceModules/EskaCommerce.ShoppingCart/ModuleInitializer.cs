using EskaCMS.Core.Events;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.ShoppingCart.EcommerceLog.Services;
using EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface;
using EskaCommerce.ShoppingCart.Events;
using EskaCommerce.ShoppingCart.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShoppingCart
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICartService, CartService>();
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<IEcommerceLogService, EcommerceLogService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }

}
