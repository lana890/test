using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Reviews.Services.Interfaces;
using EskaCommerce.Reviews.Services;

namespace EskaCommerce.Reviews
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<Services.Interfaces.IReplyServices, ReplyServices>();
            services.AddTransient<IReviewServices, ReviewServices>();
            services.AddTransient<IUserInteractionServices, UserInteractionServices>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
