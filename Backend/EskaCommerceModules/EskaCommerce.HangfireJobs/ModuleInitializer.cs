using EskaCMS.Infrastructure.Modules;
using EskaCommerce.HangfireJobs.Extensions;
using EskaCommerce.HangfireJobs.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.HangfireJobs
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var configuration = sp.GetRequiredService<IConfiguration>();

            //services.AddHangfireService(config =>
            //{
            //    config.usey(configuration.GetConnectionString("DefaultConnection"));
            //});

            //overwrite HangfireBaseOptions
            //services.PostConfigure<HangfireConfigureOptions>(o =>
            //{
            //    o.Dasbhoard.AuthorizationCallback = httpContext =>
            //    {
            //        var user = httpContext.User;
            //        return user.Identity.IsAuthenticated && user.IsInRole("admin");
            //    };
            //});
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHangfire();
            app.InitializeHangfireJobs();
        }
    }

}
