using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using EskaCMS.Infrastructure.Modules;

using EskaCommerce.TelrPayment.Services.Interfaces;
using EskaCommerce.TelrPayment.Services;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Module.EmailSenderSmtp.Data;
using EskaCommerce.TelrPayment.Data;

namespace EskaCommerce.TelrPayment
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ITelrPayment, TerlPaymentService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
