
using EskaCMS.Core.DynamicAuthorization.Core;

using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Filters.Helpers;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Forms.Services.FormsService;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Module.Core.Data;
using EskaCMS.Pages.Services.Interfaces;
using EskaCMS.Security.Data;
using EskaCMS.Security.DynamicAuthorization.Core;
using EskaCMS.Security.Services;
using EskaCMS.Security.Services.Interface;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EskaCMS.Security
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //var serviceProvider = services.BuildServiceProvider();
            //var logger = serviceProvider.GetService<ILogger<ApplicationLogs>>();

            services.AddTransient<IOptionService, OptionService>();
           services.AddTransient<IRoleOptions, RoleOptionsService>();
            services.AddTransient<IRoleService, RoleService>();
            services.AddTransient<IControllerDiscovery, ControllerDiscovery>();
            services.AddScoped<IAdminApi, AdminApiService>();
            services.AddTransient<IGoogleAuthService, GoogleAuthService>();
            services.AddTransient<INotificationServices, NotificationServices>();

            services.AddScoped<IRoleAccessService, RoleAccessService>();
            services.AddScoped<IFormService, FormService>();
            services.AddScoped<IUserWorkingHoursService, UserWorkingHoursService>();
            services.AddScoped<IUserPreferancese, UserPreferanceService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRSA_Helper, RSA_Helper>();
            services.AddScoped<IAES_Helper, AES_Helper>();
            services.AddScoped<ITwoFactorAuthenticationServices, TwoFactorAuthenticationServices>();
            services.AddTransient<IHashingHelper, HashingHelper>();
            // services.AddSingleton(typeof(ILogger), logger);
            services.AddScoped<IFacebookAuthService, FacebookAuthService>();
            services.AddTransient<IAppleAuthServices, AppleAuthServices>();
            services.AddScoped<IUsersPasswordHistoryService, UsersPasswordHistoryService>();
            services.AddScoped<IUserLoginTransactionsService, UserLoginTransactionsService>();

            services.AddTransient<IPasswordPolicyService, PasswordPolicyService>();
            services.AddTransient<IUserTaskService,UserTaskService>();
            services.AddTransient<IUserFriendshipService, UserFriendshipService>();
            services.AddScoped<IUserOTPService, UserOTPService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
