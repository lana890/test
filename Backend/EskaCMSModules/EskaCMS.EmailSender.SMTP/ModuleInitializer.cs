using EskaCommerce.Infrastructure.Modules;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;

using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Module.EmailSenderSmtp.Data;

namespace EskaCommerce.Module.EmailSenderSmtp
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IEmailSender, EmailSender>();
            serviceCollection.AddTransient<IEmailTemplateService, EmailTemplateService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
