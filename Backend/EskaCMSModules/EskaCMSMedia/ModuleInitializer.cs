using EskaCMS.Core.Extensions;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.Media.Services;

namespace EskaCMS.Media
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

            

            serviceCollection.AddTransient<EskaCMS.Media.Services.Interfaces.IMediaService, EskaCMS.Media.Services.MediaService>();
            serviceCollection.AddTransient<IUserMediaService, UserMediaService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
