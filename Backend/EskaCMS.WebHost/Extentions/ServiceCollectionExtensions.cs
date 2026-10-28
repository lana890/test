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
using EskaCMS.WebHost.IdentityServer;
using Swashbuckle.AspNetCore.SwaggerGen;
using EskaCMS.Infrastructure.Web.ModelBinders;
using Serilog;
using EskaCMS.RateLimit.Store;
using EskaCMS.RateLimit.Store.MemoryCache;
using EskaCMS.RateLimit.Core.ProcessingStrategies;
using EskaCMS.DataAccess.Data;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.RateLimit.Store.DistributedCache;
using EskaCMS.RateLimit.Middleware;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace EskaCMS.APIs.Extentions
{
    public static class ServiceCollectionExtensions
    {
        private static readonly IModuleConfigurationManager _modulesConfig = new ModuleConfigurationManager();
        private static EProvider Provider;

        /// <summary>
        /// Returns the MySQL ServerVersion from config.
        /// Defaults to 8.0.39 (known production version) to avoid AutoDetect
        /// opening an extra connection on startup just to query the server version.
        /// </summary>
        private static MySqlServerVersion GetMySqlServerVersion(IConfiguration configuration)
        {
            var configuredVersion = configuration.GetValue<string>("ConnectionStrings:MySqlServerVersion");
            if (!string.IsNullOrWhiteSpace(configuredVersion))
            {
                return new MySqlServerVersion(Version.Parse(configuredVersion));
            }

            return new MySqlServerVersion(new Version(8, 0, 39));
        }

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

        public static IServiceCollection AddCustomizedDataStore(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var provider = configuration.GetValue<string>("ConnectionStrings:DatabaseProvider");

            var enableDetailedErrors = configuration.GetValue<bool>("EntityFramework:EnableDetailedErrors");
            var enableSensitiveDataLogging = configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");

            // Pool size of 128 means EF Core reuses up to 128 DbContext instances
            // across requests instead of allocating a new one each time.
            // This dramatically reduces connection churn under load.
            // Note: 3 pools exist total (main + IS4 config + IS4 operational),
            // so set MaximumPoolSize in your connection string to at least 3 * poolSize.
            // Configurable so the pool can be matched to MySQL max_connections WITHOUT recompiling.
            // IMPORTANT: keep the connection string MaximumPoolSize >= dbContextPoolSize, and
            // MySQL max_connections >= (dbContextPoolSize * number_of_app_instances) + headroom,
            // otherwise contexts will wait on / exhaust the connection pool under load.
            var dbContextPoolSize = configuration.GetValue<int?>("EntityFramework:DbContextPoolSize") ?? 128;

            switch (provider)
            {
                case "MySql":
                    var mysqlServerVersion = GetMySqlServerVersion(configuration);

                    // Two-generic form: pool is typed as EskaDCMSDbContext (the service type)
                    // but instantiates EskaDCMSMySqlDbContext (the implementation).
                    // EskaDCMSMySqlDbContext MUST accept DbContextOptions<EskaDCMSDbContext>
                    // for the pool to work correctly — see EskaDCMSMySqlDbContext.cs.
                    services.AddDbContextPool<EskaDCMSDbContext, EskaDCMSMySqlDbContext>(options =>
                    {
                        options.UseMySql(
                            connectionString,
                            mysqlServerVersion,
                            mysqlOptions =>
                            {
                                mysqlOptions.CommandTimeout(120);
                                mysqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                                mysqlOptions.EnableRetryOnFailure(
                                    maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(10),
                                    errorNumbersToAdd: null);
                            });

                        if (enableDetailedErrors)
                            options.EnableDetailedErrors();

                        if (enableSensitiveDataLogging)
                            options.EnableSensitiveDataLogging();

                        // Diagnostics: log only slow (> threshold) or failed SQL. No parameter values.

                    }, poolSize: dbContextPoolSize);
                    break;

                case "PostgreSQL":
                    services.AddDbContextPool<EskaDCMSDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.CommandTimeout(120);
                            npgsqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                        });

                        if (enableDetailedErrors)
                            options.EnableDetailedErrors();

                        if (enableSensitiveDataLogging)
                            options.EnableSensitiveDataLogging();

                    }, poolSize: dbContextPoolSize);
                    break;

                case "SqlServer":
                    services.AddDbContextPool<EskaDCMSDbContext>(options =>
                    {
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.CommandTimeout(120);
                            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                            sqlOptions.MigrationsAssembly("EskaCMS.DataAccess");
                        });

                        if (enableDetailedErrors)
                            options.EnableDetailedErrors();

                        if (enableSensitiveDataLogging)
                            options.EnableSensitiveDataLogging();

                    }, poolSize: dbContextPoolSize);
                    break;

                case "Oracle":
                    services.AddDbContextPool<EskaDCMSDbContext>(options =>
                    {
                        options.UseOracle(connectionString, oracleOptions =>
                        {
                            oracleOptions.CommandTimeout(120);
                            oracleOptions.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21);
                            oracleOptions.MigrationsAssembly("EskaCMS.DataAccess");
                        });

                        if (enableDetailedErrors)
                            options.EnableDetailedErrors();

                        if (enableSensitiveDataLogging)
                            options.EnableSensitiveDataLogging();

                    }, poolSize: dbContextPoolSize);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider: {provider}");
            }

            return services;
        }

        public static IServiceCollection AddCustomizedInMemoryRateLimiting(this IServiceCollection services)
        {
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            return services;
        }

        public static IServiceCollection AddCustomizedDistributedRateLimit(this IServiceCollection services)
        {
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            return services;
        }

        public static IServiceCollection AddCustomizedIdentitySeed(this IServiceCollection services, string[] args)
        {
            if (args.Length > 0)
            {
                var seed = args.Contains("/seed");

                if (seed)
                {
                    args = args.Except(new[] { "/seed" }).ToArray();
                }

                if (seed)
                {
                    Log.Information("Seeding database...");
                    var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                    var connectionString = config.GetConnectionString("DefaultConnection");
                    SeedData.EnsureSeedData(connectionString, services);
                    Log.Information("Done seeding database.");
                }
            }
            return services;
        }

        public static IServiceCollection AddCustomizedIdentityServer4(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            Provider = configuration.GetValue<string>("ConnectionStrings:DatabaseProvider") switch
            {
                "MySql" => EProvider.MySql,
                "PostgreSQL" => EProvider.PostgreSQL,
                "SqlServer" => EProvider.SqlServer,
                "Oracle" => EProvider.Oracle,
                _ => throw new InvalidOperationException("Unsupported database provider for IdentityServer4")
            };

            if (Provider == EProvider.PostgreSQL)
            {
                services.AddIdentityServer()
                    .AddAspNetIdentity<User>()
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString, o =>
                            o.MigrationsAssembly("EskaCMS.DataAccess"));
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString, o =>
                            o.MigrationsAssembly("EskaCMS.DataAccess"));
                        options.EnableTokenCleanup = true;
                        options.TokenCleanupInterval = 3600;
                    })
                    .AddDeveloperSigningCredential();
            }
            else if (Provider == EProvider.SqlServer)
            {
                services.AddIdentityServer()
                    .AddAspNetIdentity<User>()
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString, o =>
                        {
                            o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                            o.MigrationsAssembly("EskaCMS.DataAccess");
                        });
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString, o =>
                        {
                            o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                            o.MigrationsAssembly("EskaCMS.DataAccess");
                        });
                        options.EnableTokenCleanup = true;
                        options.TokenCleanupInterval = 3600;
                    })
                    .AddDeveloperSigningCredential();
            }
            else if (Provider == EProvider.Oracle)
            {
                services.AddIdentityServer()
                    .AddAspNetIdentity<User>()
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseOracle(connectionString, o =>
                            o.MigrationsAssembly("EskaCMS.DataAccess"));
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = builder => builder.UseOracle(connectionString, o =>
                            o.MigrationsAssembly("EskaCMS.DataAccess"));
                        options.EnableTokenCleanup = true;
                        options.TokenCleanupInterval = 3600;
                    })
                    .AddDeveloperSigningCredential();
            }
            else if (Provider == EProvider.MySql)
            {
                var mysqlServerVersion = GetMySqlServerVersion(configuration);

                services.AddIdentityServer()
                    .AddAspNetIdentity<User>()
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder =>
                            builder.UseMySql(
                                connectionString,
                                mysqlServerVersion,
                                o =>
                                {
                                    o.CommandTimeout(120);
                                    o.MigrationsAssembly("EskaCMS.DataAccess");
                                    o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                                });
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = builder =>
                            builder.UseMySql(
                                connectionString,
                                mysqlServerVersion,
                                o =>
                                {
                                    o.CommandTimeout(120);
                                    o.MigrationsAssembly("EskaCMS.DataAccess");
                                    o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                                });

                        options.EnableTokenCleanup = true;
                        options.TokenCleanupInterval = 3600;
                    })
                    .AddDeveloperSigningCredential();
            }

            return services;
        }

        public static IServiceCollection AddCustomizedIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DataProtectionTokenProviderOptions>(
                x => x.TokenLifespan = TimeSpan.FromMinutes(2));

            services.AddIdentity<User, Role>(options =>
            {
                options.Password.RequireDigit            = false;
                options.Password.RequiredLength          = 6;
                options.Password.RequireNonAlphanumeric  = false;
                options.Password.RequireUppercase        = true;
                options.Password.RequireLowercase        = true;
                options.Password.RequiredUniqueChars     = 0;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                options.ClaimsIdentity.UserNameClaimType  = JwtRegisteredClaimNames.Sub;

                options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers      = true;

                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
                options.User.RequireUniqueEmail            = false;
                options.SignIn.RequireConfirmedAccount     = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedEmail       = false;
            })
            .AddEntityFrameworkStores<EskaDCMSDbContext>()
            .AddDefaultTokenProviders();

            var secret = configuration.GetSection("Authentication:Jwt:Key").Value;
            var issuer = configuration.GetSection("Authentication:Jwt:Issuer").Value;

            string userTokenCookieName = configuration["Authentication:UserTokenCookieName"];
            string guestTokenCookieName = configuration["Authentication:GuestTokenCookieName"];

            var key = Encoding.ASCII.GetBytes(secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken            = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience         = false,
                    ValidateIssuer           = false,
                    ValidIssuer              = issuer,
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime         = true,
                    RequireExpirationTime    = false,
                    ClockSkew                = TimeSpan.Zero
                };

                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var source = context.Request.Headers["source"].ToString();

                        var platform = EPlatformSource.Unknown;
                        if (!string.IsNullOrEmpty(source))
                        {
                            platform = (EPlatformSource)int.Parse(source);
                        }

                        var cookieToken = context.Request.Cookies[userTokenCookieName]
                                       ?? context.Request.Cookies[guestTokenCookieName];

                        if (!string.IsNullOrEmpty(cookieToken) &&
                            platform != EPlatformSource.Admin &&
                            platform != EPlatformSource.Mobile)
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddFacebook(x =>
            {
                x.AppId     = configuration["Authentication:Facebook:AppId"];
                x.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                x.Events    = new OAuthEvents { OnRemoteFailure = ctx => HandleRemoteLoginFailure(ctx) };
            })
            .AddGoogle(x =>
            {
                x.ClientId     = configuration["Authentication:Google:ClientId"];
                x.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                x.Events       = new OAuthEvents { OnRemoteFailure = ctx => HandleRemoteLoginFailure(ctx) };
            });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EskaCMS API", Version = "v1" });
                c.OperationFilter<AddAuthHeaderOperationFilter>();
                c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Description  = "`Token only!!!` - without `Bearer_` prefix",
                    Type         = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Scheme       = "bearer"
                });
            });
            return services;
        }

        public static IServiceCollection AddEskaDCMSFileStorage(
            this IServiceCollection services,
            string basePath = null,
            string baseUrl = null,
            FileStorageNaming naming = FileStorageNaming.UniqueFileNames,
            ServiceLifetime scope = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IStorage), sp => new FileStorage(basePath, baseUrl, naming), scope));
            return services;
        }

        public static void AddFirebase(this IServiceCollection services, IConfiguration configuration)
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase.json")),
            });
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
                        assembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(file.Name)));

                        if (assembly == null) throw;

                        string loadedAssemblyVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
                        string tryToLoadAssemblyVersion = FileVersionInfo.GetVersionInfo(file.FullName).FileVersion;

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

        private static Task HandleRemoteLoginFailure(RemoteFailureContext ctx)
        {
            ctx.Response.Redirect("/login");
            ctx.HandleResponse();
            return Task.CompletedTask;
        }

        public static IDynamicAuthorizationBuilder AddDynamicAuthorization<TDbContext>(
            this IServiceCollection services,
            Action<DynamicAuthorizationOptions> options)
            where TDbContext : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
        {
            var dynamicAuthorizationOptions = new DynamicAuthorizationOptions();
            options.Invoke(dynamicAuthorizationOptions);
            services.AddSingleton(dynamicAuthorizationOptions);

            services.Configure<MvcOptions>(mvcOptions =>
            {
                mvcOptions.Filters.Add(typeof(DynamicAuthorizationFilter<TDbContext>));
            });

            IDynamicAuthorizationBuilder builder = new DynamicAuthorizationBuilder(services);
            return builder;
        }
    }

    public class Policies
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static AuthorizationPolicy AdminPolicy() =>
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Admin).Build();

        public static AuthorizationPolicy UserPolicy() =>
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(User).Build();
    }

    public class AddAuthHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodInfo = context.MethodInfo;
            var declaringType = methodInfo?.DeclaringType;

            var controllerAttributes = declaringType?
                .GetCustomAttributes(true).ToList() ?? new List<object>();

            var methodAttributes = methodInfo?
                .GetCustomAttributes(true).ToList() ?? new List<object>();

            var hasControllerAuthorize = controllerAttributes.OfType<AuthorizeAttribute>().Any();
            var hasControllerAllowAnonymous = controllerAttributes.OfType<AllowAnonymousAttribute>().Any();
            var hasMethodAuthorize = methodAttributes.OfType<AuthorizeAttribute>().Any();
            var hasMethodAllowAnonymous = methodAttributes.OfType<AllowAnonymousAttribute>().Any();

            var isAuthorized =
                (hasControllerAuthorize && !hasControllerAllowAnonymous && !hasMethodAllowAnonymous)
                || (hasMethodAuthorize && !hasMethodAllowAnonymous);

            if (!isAuthorized) return;

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            var jwtBearerScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" }
            };

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement { [jwtBearerScheme] = Array.Empty<string>() });

            operation.Parameters ??= new List<OpenApiParameter>();
            if (!operation.Parameters.Any(p => p.Name == "SiteId" && p.In == ParameterLocation.Header))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name     = "SiteId",
                    In       = ParameterLocation.Header,
                    Required = false,
                    Schema   = new OpenApiSchema { Type = "string" }
                });
            }
        }
    }
}