

using EskaCMS.DynamicForms.HelperClasses;
using EskaCMS.DynamicForms.HelperClasses.IHelperClasses;
using EskaCMS.DynamicForms.Repositories;
using EskaCMS.DynamicForms.Repositories.IRepositories;
using EskaCMS.DynamicForms.Services;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.Forms.Services;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Forms
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
          //  services.AddTransient<IComponents, ComponentsService>();
           services.AddTransient<ISubmittedFormService, SubmittedFormService>();
            services.AddTransient<IFormItemService, FormItemService>();

            services.AddTransient<IFormFieldService, FormFieldService>();
            services.AddTransient<ISubmissionServices, SubmissionServices>();

            services.AddTransient<IDynamicFormsService, DynamicFormsService>();

            services.AddTransient<IConditionsManager, ConditionsManager>();

            // Perf #13: IOperationsManager was registered twice with CONFLICTING lifetimes.
            // The Scoped registration below is registered last, so it is the one the container
            // already resolves - this Transient entry was dead. Commented out to make the
            // effective lifetime (Scoped) explicit; resolution is unchanged.
            // services.AddTransient<IOperationsManager, OperationsManager>();

            services.AddScoped<IOperationsManager, OperationsManager>();


            // Confirmed unused: IDynamicFormUnitOfWork has no consumer anywhere in the solution
            // (only its interface declaration, its implementation, and this registration).
            // services.AddScoped<IDynamicFormUnitOfWork, DynamicFormUnitOfWork>();
            services.AddScoped<IPricingMatrixRepository, PricingMatrixRepository>();
            services.AddScoped<IFormItemServiceNew, FormItemServiceNew>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
