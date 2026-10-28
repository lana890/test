
using DinkToPdf;
using DinkToPdf.Contracts;
using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.Services;

using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Data.Interfaces;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Core.Hubs;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Repositories;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Module.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace EskaCMS.Module.Core
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

            // Perf #13: duplicate registration - the identical registration lower in this same
            // method (line ~57) is the one the container resolves (last wins). Commented out to
            // remove the redundant ServiceDescriptor; resolution is unchanged.
            // serviceCollection.AddTransient<ITokenService, TokenService>();

            // Perf #13: duplicate of the identical ISmsSender registration below (line ~64).
            // serviceCollection.AddScoped<ISmsSender, SmsSender>();
            // Perf #13: duplicate of the identical SettingDefinitionProvider registration below (line ~65).
            // serviceCollection.AddSingleton<SettingDefinitionProvider>();
            serviceCollection.AddTransient<ISlugServices,SlugServices>();
            serviceCollection.AddTransient<IQrCodeService, QrCodeService>();
            serviceCollection.AddTransient<ISiteHostNameServices, SiteHostNameServices>();
            serviceCollection.AddTransient<IClientCacheService, ClientCacheService>();
            serviceCollection.AddTransient<IEntityService, EntityService>();
            serviceCollection.AddTransient<IAssetsService, AssetsService>();
            serviceCollection.AddTransient<ITemplateAssetsServices, TemplateAssetsServices>();
            // serviceCollection.AddTransient<IMediaService, MediaService>();
            serviceCollection.AddTransient<IGeoLocationServices, GeoLocationServices>();
            serviceCollection.AddTransient<IExcelGenerateServices, ExcelGenerateServices>();
            serviceCollection.AddTransient<IWhatsAppServices, WhatsAppServices>();


            serviceCollection.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            serviceCollection.AddTransient<IpdfConverter, DinktoPdfConverter>();

            serviceCollection.AddTransient<ITokenService, TokenService>();
            serviceCollection.AddTransient<IEmailSettingsServices, EmailSettingsServices>();
            serviceCollection.AddTransient<IHttpHelper, HttpHelper>();
            serviceCollection.AddTransient<IAlertServices, AlertServices>();
            serviceCollection.AddSingleton<AlertTracker>();

            serviceCollection.AddScoped<IWorkContext, WorkContext>();
            serviceCollection.AddScoped<ISmsSender, SmsSender>();
            serviceCollection.AddSingleton<SettingDefinitionProvider>();
            serviceCollection.AddScoped<ISiteSettingsService, SiteSettingsService>();
            serviceCollection.AddTransient<ILoyalityService, LoyalityService>();
            serviceCollection.AddScoped<ICurrencyService, CurrencyService>();
            serviceCollection.AddTransient<IApiMapper, ApiMapperService>();
            serviceCollection.AddScoped<EskaCMS.Infrastructure.Helpers.ServerExecutionContext>();
            serviceCollection.AddScoped<IApiLogService, ApiLogService>();
            serviceCollection.AddTransient<IdcmsLogService, dcmsLogService>();
            serviceCollection.AddTransient<ICurrencyRatesApiService, CurrencyRatesApiService>();
            serviceCollection.AddTransient<IOrganizationService, OrganizationService>();
            serviceCollection.AddTransient<IMediaRepository, MediaRepository>();
            serviceCollection.AddTransient<IBackgroundSettingsService, BackgroundSettingsService>();
            serviceCollection.AddTransient<IMediaService, MediaService>();
            serviceCollection.AddTransient<ILoggedInUserService, LoggedInUserServices>();
            serviceCollection.AddTransient<ISiteCultureService, SiteCultureService>();
            serviceCollection.AddTransient<ICultureService, CultureService>();
            serviceCollection.AddTransient<ICustomerGroup, CustomerGroupService>();
            serviceCollection.AddTransient<IUserSiteService, UserSiteService>();
            serviceCollection.AddSingleton<ICacheService, CacheService>();
            serviceCollection.AddTransient<IGroupTypeService, GroupTypeService>();
            serviceCollection.AddTransient<IRestrictedWordsServices, RestrictedWordsServices>();
             serviceCollection.AddTransient<ITagsServices, TagsServices>();

            //  serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

            RegisterRedisServer(serviceCollection);

        }
        // Redis Configuration
        void RegisterRedisServer(IServiceCollection serviceCollection)
        {
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var redisSection = configuration.GetSection("Redis");

            if (redisSection.Exists())
            {
                var url = configuration.GetValue<string>("Redis:Url");
                var password = configuration.GetValue<string>("Redis:Password");
                var instanceName = configuration.GetValue<string>("Redis:InstanceName");
                var options = ConfigurationOptions.Parse(url+ ",password=" + password);
                options.ConnectTimeout = 3000;
                options.AbortOnConnectFail=false;
                // Perf #08: this multiplexer is only a reachability probe - AddStackExchangeRedisCache
                // opens its own connection. It was previously left undisposed, leaking one Redis
                // connection (and its heartbeat timer) for the lifetime of the process. Disposing it
                // after the probe does not affect the cache connection created below.
                bool redisReachable;
                using (var redis = ConnectionMultiplexer.Connect(options))
                {
                    redisReachable = redis.IsConnected;
                }

                if (redisReachable)
                {
                    serviceCollection.AddStackExchangeRedisCache(options =>
                            {
                                options.Configuration = string.IsNullOrWhiteSpace(password) ? url : $"{url},password={password}";
                                options.InstanceName = instanceName;

                            });
                }
                else
                {
                    configuration["Caching:EnableDistributedCaching"] = "false";
                }
            }else
            {
                configuration["Caching:EnableDistributedCaching"] = "false";
            }
        }

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
