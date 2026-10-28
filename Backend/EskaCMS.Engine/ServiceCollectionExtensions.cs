using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Entities.Entity.Core;

using System.IdentityModel.Tokens.Jwt;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure;
using EskaCMS.Core.Data;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using EskaCMS.StorageLocal;

using EskaCMS.Module.Core;
using EskaCMS.Security.DynamicAuthorization.Core.Builder;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EskaCMS.Security.DynamicAuthorization.Models;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Security.DynamicAuthorization.Core;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using EskaCMS.Infrastructure.Web.ModelBinders;
using EskaCMS.RateLimit.Store;
using EskaCMS.RateLimit.Store.MemoryCache;
using EskaCMS.RateLimit.Core.ProcessingStrategies;
using EskaCMS.DataAccess.Data;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Search.Events;
using EskaCMS.Search.Services.Interfaces;
using EskaCMS.Search.Services;
using MediatR;
using Meilisearch;

using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.Infrastructure.Helpers;


namespace EskaCMS.Engine
{
    public static class ServiceCollectionExtensions
    {
        private static readonly IModuleConfigurationManager _modulesConfig = new ModuleConfigurationManager();
        private static ServerVersion GetMySqlServerVersion(IConfiguration configuration)
        {
            var configuredVersion = configuration.GetValue<string>("ConnectionStrings:MySqlServerVersion");

            return string.IsNullOrWhiteSpace(configuredVersion)
                ? new MySqlServerVersion(new System.Version(8, 0, 0))
                : ServerVersion.Parse(configuredVersion);
        }
        private static EProvider Provider;
        public static IServiceCollection AddModules(this IServiceCollection services)
        {
            try
            {
                foreach (var module in _modulesConfig.GetModules())
                {
                    if (!module.IsBundledWithHost)
                    {
                        TryLoadModuleAssembly(module.Id, module);
                        if (module.Assembly == null)
                        {
                            throw new Exception($"Cannot find main assembly for module {module.Id}");
                        }
                    }
                    else
                    {
                        module.Assembly = Assembly.Load(new AssemblyName(module.Id));
                    }

                    GlobalConfiguration.Modules.Add(module);
                }

                return services;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public static IServiceCollection AddCustomizedInMemoryRateLimiting(this IServiceCollection services)
        {
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
           // services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            return services;
        }
     


        public static IServiceCollection AddEskaDCMSFileStorage(
        this IServiceCollection services,
        string basePath = null,
        string baseUrl = null,
        FileStorageNaming naming = FileStorageNaming.UniqueFileNames,
        ServiceLifetime scope = ServiceLifetime.Singleton)
        {
            //App.Modules.Register<FileStorageModule>();

            services.Add(new ServiceDescriptor(typeof(IStorage), sp => new FileStorage(basePath, baseUrl, naming), scope));

            return services;
        }


        public static IServiceCollection AddCustomizedDataStore(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var enableEfDetailedErrors = configuration.GetValue<bool>("EntityFramework:EnableDetailedErrors");
            var enableEfSensitiveDataLogging = configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");

            Provider = DbProviderHelper.ParseProvider(configuration.GetValue<string>("ConnectionStrings:DatabaseProvider"));
            if (Provider ==EProvider.PostgreSQL)
            {
                services.AddDbContextPool<EskaDCMSDbContext,EskaDCMSMyPostgresDbContext>(options => // Context pool
                {
                    DatabaseNameHelper.ConfigureDbContext(options, () => 
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(120);
                        npgsqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                    }));
                    options.EnableSensitiveDataLogging();

                });
            }
            else if (Provider==EProvider.SqlServer)
            {
                services.AddDbContextPool<EskaDCMSDbContext, EskaDCMSSqlServerDbContext>(options => // Context pool
                {
                    DatabaseNameHelper.ConfigureDbContext(options, () => 
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(120);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        sqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                    }));
                    options.EnableSensitiveDataLogging();

                });
            }
            else if (Provider == EProvider.Oracle)
            {
                services.AddDbContextPool<EskaDCMSDbContext, EskaDCMSOracleDbContext>(options =>
                {
                    DatabaseNameHelper.ConfigureDbContext(options, () =>
                    options.UseOracle(connectionString, oracleOptions =>
                    {
                        oracleOptions.CommandTimeout(120);
                        oracleOptions.MigrationsAssembly("EskaCMS.DataAccess");
                    }));
                    options.EnableSensitiveDataLogging();

                });
            }
            else if (Provider == EProvider.MySql)
            {
                var mySqlServerVersion = GetMySqlServerVersion(configuration);
                services.AddDbContext<EskaDCMSDbContext, EskaDCMSMySqlDbContext>(options =>
                {
                    options.UseMySql(connectionString, mySqlServerVersion, mysqlOptions =>
                    {
                        mysqlOptions.CommandTimeout(120);
                        mysqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                        mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });

                    if (enableEfDetailedErrors)
                    {
                        options.EnableDetailedErrors();
                    }

                    if (enableEfSensitiveDataLogging)
                    {
                        options.EnableSensitiveDataLogging();
                    }
                });
            }


            return services;
            //}
        }
        public static IServiceCollection AddCustomizedIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DataProtectionTokenProviderOptions>(
           x => x.TokenLifespan = TimeSpan.FromMinutes(2));

            services.AddIdentity<User, Role>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 0;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                options.ClaimsIdentity.UserNameClaimType = JwtRegisteredClaimNames.Sub;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<EskaDCMSDbContext>()
            .AddDefaultTokenProviders();


            return services;
        }

        private static void TryLoadModuleAssembly(string moduleFolderPath, ModuleInfo module)
        {
            const string binariesFolderName = "bin";
            var binariesFolderPath = Path.Combine(moduleFolderPath, binariesFolderName);
            var binariesFolder = new DirectoryInfo(binariesFolderPath);

            if (Directory.Exists(binariesFolderPath))
            {
                foreach (var file in binariesFolder.GetFileSystemInfos("*.dll", SearchOption.AllDirectories))
                {
                    Assembly assembly;
                    try
                    {
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName);
                    }
                    catch (FileLoadException)
                    {
                        // Get loaded assembly. This assembly might be loaded
                        assembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(file.Name)));

                        if (assembly == null)
                        {
                            throw;
                        }

                        string loadedAssemblyVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
                        string tryToLoadAssemblyVersion = FileVersionInfo.GetVersionInfo(file.FullName).FileVersion;

                        // Or log the exception somewhere and don't add the module to list so that it will not be initialized
                        if (tryToLoadAssemblyVersion != loadedAssemblyVersion)
                        {
                            throw new Exception($"Cannot load {file.FullName} {tryToLoadAssemblyVersion} because {assembly.Location} {loadedAssemblyVersion} has been loaded");
                        }
                    }

                    if (Path.GetFileNameWithoutExtension(assembly.ManifestModule.Name) == module.Id)
                    {
                        module.Assembly = assembly;
                    }
                }
            }
        }
        public static void AddElasticClient(this IServiceCollection serviceCollection , IConfiguration configuration)
        {
            serviceCollection.AddSingleton(sp =>
            {
                
                var elsasticSearchUri = configuration.GetSection("ElasticSearch:Url").Value;
                var elsasticSearchUsername = configuration.GetSection("ElasticSearch:Username").Value;
                var elsasticSearchPassword = configuration.GetSection("ElasticSearch:Password").Value;
                var elasticsearchClientSettings = new ElasticsearchClientSettings(new Uri(elsasticSearchUri))
                            .Authentication(new BasicAuthentication(elsasticSearchUsername, elsasticSearchPassword)).DisableDirectStreaming().RequestTimeout(TimeSpan.FromMinutes(5));
                var client = new ElasticsearchClient(elasticsearchClientSettings);

                return client;

            });

        }

        public static void AddMeiliClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            try
            {
                //var MeiliUrl = configuration.GetSection("MeiliSearch:Url").Value;
                //var MeiliKey = configuration.GetSection("MeiliSearch:ApiKey").Value;
                serviceCollection.AddSingleton(sp =>
                {


                    var MeiliUrl = configuration.GetSection("MeiliSearch:Url").Value;
                    var MeiliKey = configuration.GetSection("MeiliSearch:ApiKey").Value;
                    var client = new MeilisearchClient(MeiliUrl, MeiliKey);
                    return client;


                });
            }
            catch (Exception ex)
            {
                throw;
            }

        }

    }

}
