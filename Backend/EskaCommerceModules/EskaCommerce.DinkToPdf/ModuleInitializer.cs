using DinkToPdf;
using DinkToPdf.Contracts;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.DinkToPdf
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

            serviceCollection.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            serviceCollection.AddTransient<IPdfConverter, DinkToPdfConverter>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }

}
