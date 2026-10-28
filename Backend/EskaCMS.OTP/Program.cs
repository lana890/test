using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.OTP.BackgroundServices;
using EskaCMS.OTP.Data;
using EskaCMS.OTP.Helpers;
using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Middlewares;
using EskaCMS.OTP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using static EskaCMS.Entities.Enum.GeneralEnums;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices();

var app = builder.Build();

Configure();

app.Run();

void ConfigureServices()
{
    builder.WebHost.ConfigureKestrel(option =>
    {
        option.AddServerHeader = false;
    });

    AddDataStore(builder.Services, builder.Configuration);
    AddAuthentication(builder.Services, builder.Configuration);

    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();
    builder.Services.AddOptions();

    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped(typeof(IRepositoryWithTypedId<,>), typeof(RepositoryWithTypedId<,>));

    builder.Services.AddScoped<IWorkContext, WorkContext>();

    builder.Services.AddScoped<IRSA_Helper, RSA_Helper>();
    builder.Services.AddTransient<IHashingHelper, HashingHelper>();
    builder.Services.AddScoped<IUserOTPService, UserOTPService>();
    builder.Services.AddScoped<IOtpService, OtpService>();
    builder.Services.AddScoped<IdcmsLogService, dcmsLogService>();
    builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
    builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
    builder.Services.AddScoped<IEmailSender, EmailSender>();
    builder.Services.AddScoped<ISmsSender, SmsSender>();
    builder.Services.AddScoped<IWhatsAppServices, WhatsAppServices>();
    builder.Services.AddScoped<ITestNotifier, TestNotifier>();
    builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();
    builder.Services.AddScoped<INotificationDistributer, NotificationDistributer>();
    builder.Services.AddScoped<IApiLogService, ApiLogService>();

    // Account/login + Guest/CreateUser
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<ILoggedInUserService, LoggedInUserService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddHostedService<OtpBackgroundService>();

    builder.Services
        .AddControllers()
        .AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
        .AddXmlSerializerFormatters();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            policy => policy
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "EskaCMS OTP API", Version = "v1" });
    });
}

void Configure()
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

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

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    app.MapGet("/live", () => Results.Ok(new { status = "Healthy" }));
    app.MapGet("/ready", CheckDatabaseReady);

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EskaCMS OTP API V1");
    });

    app.UseAuthentication();
    app.UseMiddleware<SecurityValidationMiddleware>();
    app.UseAuthorization();
    app.Use(async (context, next) =>
    {
        var hasBearerToken = context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorization)
            && authorization.Any(value => value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

        var hasCookies = context.Request.Cookies.Count > 0;

        if (!hasBearerToken && !hasCookies)
        {
            await next();
            return;
        }

        var principal = new ClaimsPrincipal();

        if (hasCookies)
        {
            var cookiesAuthResult = await context.AuthenticateAsync("Identity.Application");
            if (cookiesAuthResult?.Principal != null)
            {
                principal.AddIdentities(cookiesAuthResult.Principal.Identities);
            }
        }

        if (hasBearerToken)
        {
            var bearerAuthResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (bearerAuthResult?.Principal != null)
            {
                principal.AddIdentities(bearerAuthResult.Principal.Identities);
            }
        }

        if (principal.Identities.Any())
        {
            context.User = principal;
        }

        await next();
    });

    // == UseCustomizedRequestLocalization() in WebHost: request culture limited to the cultures in the
    // "Culture" table, default en-US (affects culture-sensitive formatting such as [dateTime] in SendCode).
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var cultureRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<Culture, string>>();
            var supportedCultures = cultureRepository.Query().Select(c => c.Id).ToArray();
            app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures)
                    .SetDefaultCulture("en-US"));
        }
    }
    catch
    {
        app.UseRequestLocalization(options =>
            options
                .AddSupportedCultures("en-US")
                .AddSupportedUICultures("en-US")
                .SetDefaultCulture("en-US"));
    }

    app.UseCors("CorsPolicy");

    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
        a => a.UseMiddleware<ApiWrapper>("1.0", "API Mapper")
    );

    app.UseMiddleware<LookUsersOutMiddleware>();

    app.MapControllers();
}

static void AddDataStore(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var enableEfDetailedErrors = configuration.GetValue<bool>("EntityFramework:EnableDetailedErrors");
    var enableEfSensitiveDataLogging = configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");
    var provider = configuration.GetValue<string>("ConnectionStrings:DatabaseProvider");

    void IgnoreWarnings(DbContextOptionsBuilder options)
    {
        options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandError));
        options.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.SaveChangesFailed));
    }

    switch (provider)
    {
        case "PostgreSQL":
            services.AddDbContextPool<OtpDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.CommandTimeout(120));
                IgnoreWarnings(options);
                options.EnableSensitiveDataLogging();
            });
            break;

        case "SqlServer":
            services.AddDbContextPool<OtpDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(120);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                IgnoreWarnings(options);
                options.EnableSensitiveDataLogging();
            });
            break;

        case "Oracle":
            services.AddDbContextPool<OtpDbContext>(options =>
            {
                options.UseOracle(connectionString, oracleOptions => oracleOptions.CommandTimeout(120));
                IgnoreWarnings(options);
                options.EnableSensitiveDataLogging();
            });
            break;

        case "MySql":
            var configuredVersion = configuration.GetValue<string>("ConnectionStrings:MySqlServerVersion");
            var mySqlServerVersion = string.IsNullOrWhiteSpace(configuredVersion)
                ? new MySqlServerVersion(new Version(8, 0, 0))
                : ServerVersion.Parse(configuredVersion);

            services.AddDbContext<OtpDbContext>(options =>
            {
                options.UseMySql(connectionString, mySqlServerVersion, mysqlOptions =>
                {
                    mysqlOptions.CommandTimeout(120);
                    mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });

                if (enableEfDetailedErrors)
                    options.EnableDetailedErrors();

                if (enableEfSensitiveDataLogging)
                    options.EnableSensitiveDataLogging();
            });
            break;

        default:
            throw new InvalidOperationException($"Unsupported database provider: {provider}");
    }
}

static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var secret = configuration.GetSection("Authentication:Jwt:Key").Value;
    var issuer = configuration.GetSection("Authentication:Jwt:Issuer").Value;

    string userTokenCookieName = configuration["Authentication:UserTokenCookieName"];
    string guestTokenCookieName = configuration["Authentication:GuestTokenCookieName"];

    var key = Encoding.ASCII.GetBytes(secret);

    services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidIssuer = issuer,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = false,
            ClockSkew = TimeSpan.Zero
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
    .AddCookie("Identity.Application");

    services.AddAuthorization();
}

static async Task<IResult> CheckDatabaseReady(OtpDbContext dbContext, CancellationToken cancellationToken)
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
