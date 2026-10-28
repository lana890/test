
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.DataAccess.Data;
using EskaCMS.Module.Core.Data;
using EskaCMS.Sites.Helpers;
using EskaCMS.StorageLocal;

using EskaCommerce.Catalog.Mapping.CategoryProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using EskaCommerce.Comments.ProfileComment;
using EskaCommerce.Reviews.Mapping;
using EskaCommerce.LoyaltyPoints.MappingProfile;

using EskaCMS.WorkFlow.MappingProfile;
using EskaCMS.Core.Hubs;
using EskaCMS.Engine.BGS;
using EskaCMS.Engine.ExportDataBase;
using EskaCMS.Engine.NewsLetter.BGS;
using EskaCMS.Engine.OrderCancelation.BGS;
using EskaCMS.Engine.Otp.BGS;
using EskaCMS.Engine.PagePreviewCode.BGS;
using EskaCMS.Engine.Services.Interfaces;
using EskaCMS.Engine.Services;
using EskaCMS.Engine.UserAchievement.BGS;
using EskaCMS.Engine;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
    
using EskaCMS.Middlewares.Middlewares;
using EskaCMS.Engine.RecurringPayments.BGS;
using EskaCMS.Engine.ExpiredBookingsCancellation;
using EskaCMS.Enigine.UpdateChromDriverVersion.BGS;
using EskaCMS.Engine.StockNotifiction.BGS;
using EskaCMS.Engine.UserManagment.BGS;
using EskaCMS.Engine.Elastic.BGS;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Search.Services;
using EskaCMS.Engine.Elastic.Services.Interfaces;
using EskaCMS.Engine.Elastic.Services;
using EskaCMS.HOC.ManageStockQuantity.BGS;
using EskaCMS.Engine.NotifyCart.BG;
using EskaCMS.Engine.MeiliSearch.BGS;
using EskaCMS.Engine.MeiliSearch.Services.Intefaces;
using EskaCMS.Engine.MeiliSearch.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

ConfigureService(args);

var app = builder.Build();
Configure();

app.Run();


void ConfigureService(string[] args)
{
    builder.WebHost.UseKestrel(option =>
    {
        option.ListenAnyIP(0);
    });

    GlobalConfiguration.WebRootPath = builder.Environment.WebRootPath;
    GlobalConfiguration.ContentRootPath = builder.Environment.ContentRootPath;

    builder.Services.AddModules();
    builder.Services.AddCustomizedDataStore(builder.Configuration);
    builder.Services.AddCustomizedIdentity(builder.Configuration);
   

    builder.Services.AddEskaDCMSFileStorage(naming: FileStorageNaming.UniqueFolderNames);
    builder.Services.AddControllers(); // If you are building an API
    builder.Services.AddElasticClient(builder.Configuration);
    builder.Services.AddMeiliClient(builder.Configuration);
    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<SiteMapperProfile>();
        cfg.AddProfile<CategoryProfileMapper>();
        cfg.AddProfile<MappingComment>();
        cfg.AddProfile<ProfileReviewAndReply>();
        cfg.AddProfile<LoyaltyPointsMappingProfile>();
        cfg.AddProfile<WorkFlowMappingProfile>();
    });

    builder.Services.AddHttpClient();
    builder.Services.AddOptions();
    builder.Services.AddSignalR();
    builder.Services.AddHttpClient();
    builder.Services.AddTransient<IHttpHelper, HttpHelper>();
    builder.Services.AddTransient<IActiveDirectoryServices, ActiveDirectoryServices>();
    builder.Services.AddTransient<IElasticHelperService, ElasticHelperService>();
    builder.Services.AddScoped<IMeiliSearchHelperService, MeilisearchHelperService>();

    // Scoped to match the DbContext lifetime (same fix already applied in EskaCMS.WebHost) —
    // this host runs the background services, which each resolve repos via scope.CreateScope(),
    // so nothing depends on a fresh instance per injection point.
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped(typeof(IRepositoryWithTypedId<,>), typeof(RepositoryWithTypedId<,>));
    builder.Services.AddScoped(typeof(IRepositoryWithCustomId<>), typeof(RepositoryWithCustomId<>));

    builder.Services.AddHostedService<OrderCancellationBackgroundService>();
    builder.Services.AddHostedService<AutoReproccessLogsBackgroundService>();
    builder.Services.AddHostedService<ADUsersSyncBackgroundService>();
    builder.Services.AddHostedService<UserAvailabilityBackgroundService>();
    builder.Services.AddHostedService<ContentPublishBackgroundService>();
    builder.Services.AddHostedService<CypherParamsBackgroundService>();
    builder.Services.AddHostedService<ExpiredBookingsCancellationBackgroundService>();
    builder.Services.AddHostedService<ExportDataBaseBackgrounService>();
    builder.Services.AddHostedService<StockQuantityBackgroundService>();
    builder.Services.AddHostedService<NewsLetterBackgroundService>();
    builder.Services.AddHostedService<OtpBackgroundService>();
    builder.Services.AddHostedService<PayTabsRecuringBackgroundServices>();
    builder.Services.AddHostedService<PagePreviewCodeBackgroundService>();
    builder.Services.AddHostedService<NotifyActiveCartBeforeBackgroundServices>();
    builder.Services.AddHostedService<StockBackgroundService>();
    builder.Services.AddHostedService<UpdateChromeVersionBGS>();
    builder.Services.AddHostedService<UserAchievementBackgroundService>();
    builder.Services.AddHostedService<CurrentUsersBackgroundServices>();
    builder.Services.AddHostedService<ElasticSearchBackgroundService>();
    builder.Services.AddHostedService<ProductElasticSearchBackgroundService>();
    builder.Services.AddHostedService<MeiliSearchBackgroundService>();

    builder.Services.AddMemoryCache();


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

    builder.Services.AddScoped<ServiceFactory>(p => p.GetService);
    builder.Services.AddScoped<IMediator, Mediator>();
   
}

void Configure()
{
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var incomingCorrelationId)
            && !string.IsNullOrWhiteSpace(incomingCorrelationId.ToString())
            ? incomingCorrelationId.ToString()
            : context.TraceIdentifier;

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
            logger.LogError(ex, "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}. TraceId: {TraceId}",
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

    app.UseRouting();
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    app.MapGet("/live", () => Results.Ok(new { status = "Healthy" }));
    app.MapGet("/ready", CheckDatabaseReady);

    var moduleInitializers = app.Services.GetServices<IModuleInitializer>();
    foreach (var moduleInitializer in moduleInitializers)
    {
        moduleInitializer.Configure(app, app.Environment);
    }
    app.MapHub<AlertHub>("Alerthub");

    app.UseAuthentication();
    app.UseAuthorization();  
    app.MapControllers();
    app.UseMiddleware<LookUsersOutMiddleware>();
    app.UseEndpoints(endpoints =>
    {

        endpoints.MapControllers();

    });
}

static async Task<IResult> CheckDatabaseReady(EskaDCMSDbContext dbContext, CancellationToken cancellationToken)
{
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return Results.Ok(new { status = "Healthy" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
