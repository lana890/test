
using EskaCMS.Events.Models;
using EskaCMS.Events.Repositories;
using EskaCMS.Events.Repositories.Interfaces;
using EskaCMS.Events.Services;
using EskaCMS.Events.Services.Interfaces;
using EskaCMS.Events.Services.Repositories;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Events
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IEventsRepository, EventsRepository>();
            services.AddTransient<IEventOccurrenceRepository, EventOccurrenceRepository>();
            services.AddTransient<ILocationsRepository, LocationsRepository>();
            services.AddTransient<IEventsService, EventsService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
