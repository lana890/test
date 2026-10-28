using AspNetCoreRateLimit;
using AutoMapper;
using Eska.Core.Api;
using EskaCMS.APIs.Extensions;
using EskaCMS.APIs.Extentions;
using EskaCMS.Chat.SignalR.Hubs;
using EskaCMS.Core.Extensions;
using EskaCMS.DataAccess.Data;
using EskaCMS.Core.Hubs;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Localization.Extensions;
using EskaCMS.Middlewares.Handlers;
using EskaCMS.Middlewares.Middlewares;
using EskaCMS.Module.Core.Data;
using EskaCMS.Security.Services;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Helpers;
using EskaCMS.StorageLocal;
using EskaCMS.WebHost.Configurations;
using EskaCMS.WebHost.Extensions;
using EskaCMS.WorkFlow.MappingProfile;
using EskaCommerce.Catalog.Mapping.CategoryProfile;
using EskaCommerce.Comments.ProfileComment;
using EskaCommerce.LoyaltyPoints.MappingProfile;
using EskaCommerce.Notifications.Hubs;
using EskaCommerce.Reviews.Mapping;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Logger_Nuget.Helpers;
using Logger_Nuget.Helpers.Interfaces;
using Logger_Nuget.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using OcrService;
using Serilog;
using Serilog.Events;
using skaCMS.Chat.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading;
using System.Text.Unicode;
using System.Threading.Tasks;

// TEMPORARY MITIGATION (not a final fix): raise the thread-pool minimum so sudden
// concurrency bursts don't get throttled by the pool's slow (~1 thread/500ms) ramp.
// The real fix is removing the remaining sync-over-async (.Result/.Wait) blocking calls.
ThreadPool.GetMinThreads(out _, out var ioMin);
ThreadPool.SetMinThreads(Math.Max(Environment.ProcessorCount * 8, 200), ioMin);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CookiePolicyOptions>(option =>
{
    option.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    option.Secure = CookieSecurePolicy.Always;
    option.MinimumSameSitePolicy = SameSiteMode.Strict;
});

ConfigureService(args);

if (args.Contains("/seed"))
{
    return;
}

var app = builder.Build();

// Diagnostics: wire the slow-query interceptor's logger now that the provider exists.

await seedRateLimit(app);

app.UseCookiePolicy();
Configure();

app.Run();


void ConfigureService(string[] args)
{
    builder.Services.AddSingleton<OcrClientAccessor>();

    RegisterSeqServer();

    GlobalConfiguration.WebRootPath = builder.Environment.WebRootPath;
    GlobalConfiguration.ContentRootPath = builder.Environment.ContentRootPath;

    builder.WebHost.ConfigureKestrel(option =>
    {
        option.AddServerHeader = false;
    });

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    }).AddJsonProtocol(x => { x.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    builder.Services.AddModules();

    // Registers DbContextPool for MySQL — reuses EF contexts across requests
    // instead of allocating a new one per request, significantly reducing
    // connection churn and pool pressure.
    builder.Services.AddCustomizedDataStore(builder.Configuration);

    builder.Services.AddCustomizedIdentitySeed(args);
    builder.Services.AddCustomizedIdentity(builder.Configuration);
    // builder.Services.AddCustomizedIdentityServer4(builder.Configuration);

    builder.Services.AddHttpClient();

    var logPaths = builder.Configuration.GetSection("LogPaths").Get<List<string>>();
    if (logPaths?.Any(p => !string.IsNullOrWhiteSpace(p)) == true)
    {
        builder.Services.AddTransient<LoggingHandler>();

        builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(b =>
            {
                if (!b.AdditionalHandlers.Any(h => h is LoggingHandler))
                {
                    b.AdditionalHandlers.Add(
                        b.Services.GetRequiredService<LoggingHandler>());
                }
            });
        });
    }

    // Repositories are thin, stateless wrappers over the (scoped, pooled) DbContext.
    // Scoped matches the DbContext lifetime and means a request that injects the same
    // IRepository<T> at several call sites shares ONE wrapper instead of allocating a
    // fresh Transient wrapper per injection — fewer allocations per request, identical
    // behavior (they already shared the same scoped DbContext). All scope.CreateScope()
    // resolutions keep working; no repository is resolved from the root provider.
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped(typeof(IRepositoryWithTypedId<,>), typeof(RepositoryWithTypedId<,>));
    builder.Services.AddScoped(typeof(IRepositoryWithCustomId<>), typeof(RepositoryWithCustomId<>));

    builder.Services.AddScoped<IWorkContext, WorkContext>();

    builder.Services.AddCustomizedLocalization();

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<SiteMapperProfile>();
        cfg.AddProfile<CoreMappingProfile>();
        cfg.AddProfile<CategoryProfileMapper>();
        cfg.AddProfile<MappingComment>();
        cfg.AddProfile<ProfileReviewAndReply>();
        cfg.AddProfile<LoyaltyPointsMappingProfile>();
        cfg.AddProfile<WorkFlowMappingProfile>();
    });

    var mvcBuilder = builder.Services
        .AddControllers()
        .AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
        .AddXmlSerializerFormatters();

    // Module controllers live in plain class libraries that get MVC through a transitive
    // project reference, so they carry no Microsoft.AspNetCore.Mvc* entry in the host's
    // deps.json. DefaultAssemblyPartDiscoveryProvider keys off exactly that entry, which
    // means it only finds the host assembly and every module route/Swagger entry silently
    // disappears. Register the module assemblies as application parts explicitly instead.
    foreach (var moduleAssembly in GlobalConfiguration.Modules
                 .Select(m => m.Assembly)
                 .Where(a => a != null)
                 .Distinct())
    {
        mvcBuilder.AddApplicationPart(moduleAssembly);
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            builder => builder
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    builder.Services.Configure<WebEncoderOptions>(options =>
    {
        options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
    });

    builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-Token");
    builder.Services.AddCloudscribePagination();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    builder.Services.AddResponseCompression(options =>
    {
        IEnumerable<string> MimeTypes = new[]
        {
            "text/plain",
            "text/html",
            "text/css",
            "application/javascript",
            // Compress JSON/XML API payloads too (previously uncompressed -> large transfers).
            "application/json",
            "application/xml",
            "text/json",
            "image/svg+xml"
        };

        options.EnableForHttps = true;
        options.MimeTypes = MimeTypes;
        options.Providers.Add<GzipCompressionProvider>();
        options.Providers.Add<BrotliCompressionProvider>();
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    // needed to load configuration from appsettings.json
    builder.Services.AddOptions();

    // needed to store rate limit counters and ip rules
    builder.Services.AddMemoryCache();

    // Diagnostics: lightweight CPU/RAM/GC/thread-pool sampler (one log line per minute).

    // In-memory output caching (NO Redis). Only endpoints explicitly annotated with
    // [OutputCache(PolicyName=...)] are cached — nothing is cached by default, so this
    // is inert until applied. Entries vary by the cultureId/SiteId headers the app uses,
    // so cached responses stay correct per culture and per site.
    // NOTE: the store is per-process; if you run multiple instances each keeps its own
    // copy (acceptable for short-TTL public read data). Apply ONLY to non-user-specific
    // GET endpoints (public reference/catalog data), never to per-user responses.
    builder.Services.AddOutputCache(options =>
    {
        // Reference data that changes rarely (countries, lookups, site config).
        options.AddPolicy("reference", b => b
            .Expire(TimeSpan.FromSeconds(300))
            .SetVaryByHeader("cultureId", "SiteId")
            .Tag("reference"));

        // Catalog/listing data that changes more often (categories, products, menus).
        options.AddPolicy("catalog", b => b
            .Expire(TimeSpan.FromSeconds(60))
            .SetVaryByHeader("cultureId", "SiteId")
            .Tag("catalog"));
    });

    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(Path.Combine(Directory.GetCurrentDirectory(), "firebase.json")),
    });

    foreach (var module in GlobalConfiguration.Modules)
    {
        var moduleInitializerType = module.Assembly.GetTypes()
            .FirstOrDefault(t => typeof(IModuleInitializer).IsAssignableFrom(t));

        if ((moduleInitializerType != null) && (moduleInitializerType != typeof(IModuleInitializer)))
        {
            var moduleInitializer = (IModuleInitializer)Activator.CreateInstance(moduleInitializerType);
            builder.Services.AddSingleton(typeof(IModuleInitializer), moduleInitializer);
            moduleInitializer.ConfigureServices(builder.Services);
        }
    }

    // RateLimit based on ClientId or IP
    if (builder.Configuration.GetValue<bool>("IpRateLimiting:EnableRateLimiting"))
    {
        builder.Services.Configure<EskaCMS.RateLimit.Models.IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
        builder.Services.Configure<EskaCMS.RateLimit.Models.IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    }
    else if (builder.Configuration.GetValue<bool>("ClientRateLimiting:EnableRateLimiting"))
    {
        builder.Services.Configure<EskaCMS.RateLimit.Models.ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
        builder.Services.Configure<EskaCMS.RateLimit.Models.ClientRateLimitPolicies>(builder.Configuration.GetSection("ClientRateLimitPolicies"));
    }

    // RateLimit on local cache or Distributed cache
    if (builder.Configuration.GetValue<bool>("Caching:EnableDistributedCaching"))
    {
        builder.Services.AddCustomizedDistributedRateLimit();
    }
    else
    {
        builder.Services.AddCustomizedInMemoryRateLimiting();
    }

    builder.Services.AddScoped<ServiceFactory>(p => p.GetService);
    builder.Services.AddScoped<IMediator, Mediator>();

    if (builder.Environment.IsDevelopment())
    {
        builder.WebHost.UseKestrel(options =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(60);
        });
    }

    builder.Services.AddSwaggerConfiguration(builder.Configuration);
    builder.Services.AddEskaDCMSFileStorage(naming: FileStorageNaming.UniqueFolderNames);
    builder.Services.AddSwaggerGen(c => c.OperationFilter<CustomHeaderOperationFilter>());

    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequireUppercase      = false;
        options.Password.RequireLowercase      = false;
        options.Password.RequiredLength        = 0;
        options.Password.RequiredUniqueChars   = 0;
        options.Password.RequireDigit          = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = int.MaxValue;

        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/ ";
    });
}

void RegisterSeqServer()
{
    var SeriLog = builder.Configuration.GetSection("Serilog");
    var Seq = builder.Configuration.GetSection("Seq");
    var IsEnabled = Convert.ToBoolean(builder.Configuration["Seq:Enabled"]);

    if (Seq.Exists() && SeriLog.Exists() && IsEnabled)
    {
        try
        {
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Fatal)
                .Enrich.FromLogContext()
                .WriteTo.Seq(
                    serverUrl: context.Configuration["Seq:ServerUrl"],
                    apiKey: context.Configuration["Seq:ApiKey"],
                    restrictedToMinimumLevel: LogEventLevel.Error
                ));
        }
        catch (Exception ex)
        {
            builder.Configuration["Seq:Enabled"] = "false";
        }
    }
}

void Configure()
{
    var IsEnabled = Convert.ToBoolean(builder.Configuration["Seq:Enabled"]);

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Diagnostics: per-request timing — logs a Warning for any request over the configured
    // threshold (appsettings "RequestTiming:SlowRequestThresholdMs", default 1000ms). Placed
    // first so it measures true end-to-end latency (auth, rate limiting, everything). No
    // request/response body buffering — cheap on the fast path (Stopwatch + one comparison).
    app.UseMiddleware<RequestTimingMiddleware>();

    // Global exception handler — logs unhandled exceptions with correlation ID
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var incomingCorrelationId)
            && !string.IsNullOrWhiteSpace(incomingCorrelationId.ToString())
            ? incomingCorrelationId.ToString()
            : (context.Items.TryGetValue("CorrelationId", out var existingCorrelationId) && existingCorrelationId != null
                ? existingCorrelationId.ToString()
                : context.TraceIdentifier);

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("GlobalExceptionHandler");

            logger.LogError(ex,
                "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                context.TraceIdentifier);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Internal server error",
                    correlationId,
                    traceId = context.TraceIdentifier
                }));
            }
        }
    });

    // Security headers middleware
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(state =>
        {
            try
            {
                var httpContext = (HttpContext)state;
                httpContext.Response.Headers.Remove("Server");
                httpContext.Response.Headers.Remove("X-Powered-By");
                httpContext.Response.Headers.Remove("X-AspNet-Version");
                httpContext.Response.Headers.Remove("X-AspNetMvc-Version");
                httpContext.Response.Headers.Remove("X-SourceFiles");
                httpContext.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                httpContext.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
                httpContext.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
                httpContext.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
                httpContext.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
                httpContext.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }
        }, context);

        await next();
    });

    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
            a => a.UseExceptionHandler("/Home/Error")
        );
        app.UseHsts();
    }

    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
        a => a.UseStatusCodePagesWithReExecute("/Home/ErrorWithCode/{0}")
    );

    app.UseHttpsRedirection();
    app.UseCustomizedStaticFiles(builder.Environment);
    app.UseRouting();

    // Health / readiness endpoints
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    app.MapGet("/live", () => Results.Ok(new { status = "Healthy" }));

    // /ready uses CanConnectAsync — lighter than ExecuteSqlRawAsync("SELECT 1")
    // and avoids unnecessarily consuming a pool connection on every probe.
    app.MapGet("/ready", CheckDatabaseReady);

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EskaDCMS API V1");
    });

    app.UseCustomizedIdentity();
    app.UseCustomizedRequestLocalization();
    app.UseCors("CorsPolicy");

    // Output caching must come after UseRouting + UseCors and before endpoints.
    // Only endpoints annotated with [OutputCache] are affected.
    app.UseOutputCache();

    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                   && !context.Request.Path.ToString().Contains("/GetSiteMaps", StringComparison.OrdinalIgnoreCase),
        a => a.UseJsonApiWrapper("1.0", "API Mapper")
    );

    app.UseMiddleware<LookUsersOutMiddleware>();

    app.MapHub<MessageHub>("messagehub");
    app.MapHub<PresenceHub>("presencehub");
    app.MapHub<NotificationHub>("notificationhub");
    app.MapHub<LastMessageTrackerHub>("lastmassagetrackerhub");
    app.MapHub<UserReadMassagsHub>("userreadmassagehub");
    app.MapHub<AlertHub>("Alerthub");

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller}/{action=Index}/{id?}");
    });

    var moduleInitializers = app.Services.GetServices<IModuleInitializer>();
    foreach (var moduleInitializer in moduleInitializers)
    {
        moduleInitializer.Configure(app, app.Environment);
    }
}

async Task seedRateLimit(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    if (builder.Configuration.GetValue<bool>("IpRateLimiting:EnableRateLimiting"))
    {
        var ipPolicyStore = scope.ServiceProvider.GetRequiredService<EskaCMS.RateLimit.Store.IIpPolicyStore>();
        await ipPolicyStore.SeedAsync();
        app.UseCustomizedIPRateLimiting();
    }
    else if (builder.Configuration.GetValue<bool>("ClientRateLimiting:EnableRateLimiting"))
    {
        var clientPolicyStore = scope.ServiceProvider.GetRequiredService<EskaCMS.RateLimit.Store.IClientPolicyStore>();
        await clientPolicyStore.SeedAsync();
        app.UseCustomizedClientRateLimiting();
    }
}

// Uses CanConnectAsync instead of ExecuteSqlRawAsync("SELECT 1").
// CanConnectAsync is cheaper — it borrows a connection just long enough to
// verify the server is reachable, then returns it to the pool immediately.
// This prevents /ready probes from stacking up pool connections under load.
static async Task<IResult> CheckDatabaseReady(EskaDCMSDbContext dbContext, CancellationToken cancellationToken)
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "Healthy" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
