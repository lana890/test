using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Comments.Data;
using EskaCommerce.Comments.Services;
using EskaCommerce.Comments.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
 
namespace EskaCommerce.Comments
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICommentService, CommentServices>();
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
