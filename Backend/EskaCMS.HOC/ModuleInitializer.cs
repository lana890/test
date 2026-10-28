using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using EskaCMS.Infrastructure.Modules;

using EskaCMS.Tax.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.HOC.SiteDuplication.Services;
using EskaCMS.HOC.SiteDuplication.Repositories;
using EskaCMS.HOC.RelatedSlug.Services.Interfcaes;
using EskaCMS.HOC.RelatedSlug.Services;

using EskaCMS.Core.Data;
//using EskaCMS.HOC.RecurringPayments.PaytabsRecuring.BGS;

namespace EskaCMS.Module.HOC
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IRelatedSlugServices, RelatedSlugServices>();
            serviceCollection.AddTransient<ISiteDuplicationService, SiteDuplicationService>();
            serviceCollection.AddTransient<ISiteDuplicationRepositories, SiteDuplicationRepositories>();


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
