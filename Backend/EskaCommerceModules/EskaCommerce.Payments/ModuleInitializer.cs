using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Payments.Services;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.Payments.Services.PayFort;
using EskaCommerce.Payments.Services.PayFort.Interfaces;
using EskaCommerce.Payments.Services.DirectPay;
using EskaCommerce.Payments.Services.DirectPay.Interfaces;
using EskaCommerce.Payments.Services.PaymentMEPS;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Models.PaymentMEPS;
using EskaCommerce.Payments.Helper.PaymentMEPS;

namespace EskaCommerce.Payments
{
    /// <summary>
    /// Single initializer for the whole payments module: the module-level
    /// services plus every provider's registrations.
    /// </summary>
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentCoD");
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentStripe");

            serviceCollection.AddTransient<IPaymentServices, PaymentServices>();

            serviceCollection.AddTransient<IPayFortServices, PayFortServices>();
            serviceCollection.AddTransient<IPayFortConfigurationServices, PayFortConfigurationServices>();

            serviceCollection.AddTransient<IDirectPayService, DirectPayService>();

            serviceCollection.AddTransient<IPaymentMepsService, PaymentMepsService>();
            serviceCollection.AddScoped<GatewayApiClient>();
            serviceCollection.AddScoped<NVPApiClient>();
            serviceCollection.AddScoped<MepsConfigFactory>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
