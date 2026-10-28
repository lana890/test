using EskaCMS.EskaCoreReports.Repositories;
using EskaCMS.EskaCoreReports.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaCoreReports
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IReportRepository, ReportRepository>();
            services.AddTransient<ICoreReports, CoreReports>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment  env)
        {
        }
    }
}
