
using EskaCMS.Chat.Services;
using EskaCMS.Chat.Services.Interface;
using EskaCMS.Chat.SignalR.Hubs;
using EskaCMS.Infrastructure.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using skaCMS.Chat.SignalR.Hubs;

namespace EskaCMS.Chat
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IMessageServices, MessageServices>();
            services.AddSingleton<PresenceTracker>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
