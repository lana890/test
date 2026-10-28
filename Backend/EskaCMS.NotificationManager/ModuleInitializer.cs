using EskaCMS.Infrastructure.Modules;
using EskaCMS.NotificationManager.Services;
using EskaCMS.NotificationManager.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<INewsLetterService, NewsLetterService>();

        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

       
    }
}
