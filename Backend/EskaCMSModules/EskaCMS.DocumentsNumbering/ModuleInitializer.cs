using EskaCMS.DocumentsNumbering.Interfaces;
using EskaCMS.DocumentsNumbering.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.DocumentsNumbering
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDocumentLoockupsService, DocumentLoockupsService>();
            services.AddTransient<IDocumentNumberService, DocumentNumberService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
