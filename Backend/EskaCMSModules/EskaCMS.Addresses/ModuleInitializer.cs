
using EskaCMS.Addresses.Services;
using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Addresses
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICountryApiService, CountryApiService>(); 
            serviceCollection.AddHostedService<CountryCacheWarmupService>();
            serviceCollection.AddTransient<IStateOrProvinceApiService, StateOrProvinceApiService>(); 
            serviceCollection.AddTransient<IDistrictApiService, DistrictApiService>(); 
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
