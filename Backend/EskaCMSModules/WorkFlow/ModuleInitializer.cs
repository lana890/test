using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure.Data;
using EskaCMS.WorkFlow.Services.Interface;
using EskaCMS.WorkFlow.Services;

namespace EskaCMS.WorkFlow
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IWorkFlowService, WorkFlowService>();
            services.AddTransient<IStageServices, StageServices>();
            services.AddTransient<IWorkFlowItemService, WorkFlowItemService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
