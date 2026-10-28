using DinkToPdf;
using DinkToPdf.Contracts;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Orders.Events;
using EskaCommerce.Orders.Services;
using EskaCommerce.Orders.Services.CustomersService;
using EskaCommerce.Orders.Services.CustomersService.Interface;
using EskaCommerce.Orders.Services.EcomAnalticsServices;
using EskaCommerce.Orders.Services.EcomAnalticsServices.Interface;
using EskaCommerce.Orders.Services.OrderAddressServices;
using EskaCommerce.Orders.Services.OrderAddressServices.Interfaces;
using EskaCommerce.Shipments.Events;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
//using EskaCommerce.UserCatalog.Services.Interfaces;

namespace EskaCommerce.Orders
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IOrderService, OrderService>();

            services.AddTransient<IOrderApiService, OrderApiService>();
            services.AddTransient<IUserAddressService, UserAddressService>();
            services.AddTransient<IOrderAdditionalFeesServices, OrderAdditionalFeesServices>();
            services.AddTransient<INotificationHandler<OrderChanged>, OrderChangedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderWarehouseNotification>, OrderWarehouseNotificationHandler>();
            services.AddTransient<IEcommerceService, EcommerceService>();
            services.AddTransient<ICustomerService,CustomerService>();
            services.AddTransient<INotificationHandler<OrderDetailGot>, OrderDetailGotHandler>();

            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            GlobalConfiguration.RegisterAngularModule("simplAdmin.orders");
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
        }
    }

}
