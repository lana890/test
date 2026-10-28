using System.Collections.Concurrent;
using System.Security.Claims;
using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Middlewares
{
    public class SecurityMiddlewareOptions
    {
        public const string SectionName = "SecurityMiddleware";

        public bool Enabled { get; set; } = false;
        public CorsOptions Cors { get; set; } = new();
        public ActiveSessionOptions ActiveSession { get; set; } = new();
        public CspOptions Csp { get; set; } = new();
    }

    public class CspOptions
    {
        public bool Enabled { get; set; } = false;
        public List<string> ScriptSources { get; set; } = new();
        public List<string> StyleSources { get; set; } = new();
        public List<string> ImageSources { get; set; } = new();
        public List<string> FontSources { get; set; } = new();
        public List<string> ConnectSources { get; set; } = new();
        public List<string> DefaultSources { get; set; } = new();
        public bool AllowUnsafeInlineScripts { get; set; } = false;
        public bool AllowUnsafeInlineStyles { get; set; } = false;
        public string FrameAncestors { get; set; } = "none";
    }

    public class CorsOptions
    {
        public bool Enabled { get; set; } = false;
        public List<string> StrictSites { get; set; } = new();
    }

    public class ActiveSessionOptions
    {
        public List<string> ExcludedPaths { get; set; } = new();
    }

    public class SecurityValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityValidationMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly SecurityMiddlewareOptions _options;

        public SecurityValidationMiddleware(
            RequestDelegate next,
            ILogger<SecurityValidationMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            _options = new SecurityMiddlewareOptions();
            configuration.GetSection(SecurityMiddlewareOptions.SectionName).Bind(_options);
        }

        public async Task InvokeAsync(HttpContext context, IWorkContext workContext)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }
            if (_options.Csp.Enabled)
                ApplyCspHeaders(context);
            if (_options.Cors.Enabled && !ValidateCors(context))
                return;

            using var scope = _serviceScopeFactory.CreateScope();

            if (!await ValidateActiveSessionAsync(context, workContext, scope))
                return;

            await _next(context);
        }

        #region CORS Validation
        private void ApplyCspHeaders(HttpContext context)
        {
            try
            {
                var csp = _options.Csp;
                var policy = new System.Text.StringBuilder();

                AppendCspDirective(policy, "default-src", csp.DefaultSources);
                AppendCspDirective(policy, "script-src", csp.ScriptSources, false, blockScriptInjectionSources: true);
                AppendCspDirective(policy, "style-src", csp.StyleSources, csp.AllowUnsafeInlineStyles);
                AppendCspDirective(policy, "img-src", csp.ImageSources);
                AppendCspDirective(policy, "font-src", csp.FontSources);
                AppendCspDirective(policy, "connect-src", csp.ConnectSources);
                policy.Append("object-src 'none'; ");

                var frameAncestors = string.IsNullOrWhiteSpace(csp.FrameAncestors) ? "'none'" : csp.FrameAncestors;
                policy.Append($"frame-ancestors {frameAncestors}; ");
                policy.Append("base-uri 'self'; ");
                policy.Append("form-action 'self';");

                context.Response.Headers["Content-Security-Policy"] = policy.ToString();
                context.Response.Headers["X-Frame-Options"] = frameAncestors == "'none'" || frameAncestors == "none" ? "DENY" : "SAMEORIGIN";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply CSP headers");
            }
        }

        private static void AppendCspDirective(
                System.Text.StringBuilder policy,
                string directive,
                List<string> sources,
                bool allowUnsafeInline = false,
                bool blockScriptInjectionSources = false)
        {
            if (!sources.Any()) return;

            var safeSources = sources
                .Where(source => IsAllowedCspSource(source, blockScriptInjectionSources))
                .ToList();

            if (!safeSources.Any())
                safeSources.Add("'self'");

            policy.Append($"{directive} {string.Join(" ", safeSources)}");

            if (allowUnsafeInline)
                policy.Append(" 'unsafe-inline'");

            policy.Append("; ");
        }

        private static bool IsAllowedCspSource(string source, bool blockScriptInjectionSources)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            var normalized = source.Trim();

            if (normalized == "*")
                return false;

            if (!blockScriptInjectionSources)
                return true;

            return !normalized.Equals("'unsafe-inline'", StringComparison.OrdinalIgnoreCase)
                && !normalized.Equals("'unsafe-eval'", StringComparison.OrdinalIgnoreCase)
                && !normalized.Equals("data:", StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidateCors(HttpContext context)
        {
            var origin = $"{context.Request.Scheme}://{context.Request.Host}";

            if (string.IsNullOrWhiteSpace(origin))
                return true;

            try
            {
                var allowedOrigins = _options.Cors.StrictSites;

                if (!allowedOrigins.Any())
                {
                    _logger.LogWarning("CORS enabled but no allowed origins configured.");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return false;
                }

                if (!allowedOrigins.Any(x => x.Equals(origin, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Blocked CORS request from origin: {Origin}", origin);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return false;
                }

                SetStrictCorsHeaders(context, origin);

                if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enforce CORS policy");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return false;
            }
        }

        private static void SetStrictCorsHeaders(HttpContext context, string origin)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With, Accept";
        }
        #endregion

        #region Active Session Validation
        private async Task<bool> ValidateActiveSessionAsync(
            HttpContext context,
            IWorkContext workContext,
            IServiceScope scope)
        {
            if (ShouldSkipSessionValidation(context.Request.Path))
                return true;

            if (!context.User.Identity?.IsAuthenticated ?? true)
                return true;

            try
            {
                var token = ExtractToken(context);

                if (string.IsNullOrEmpty(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("No Token Provided");
                    return false;
                }

                if (!TryGetUserId(context, out long userId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid Token Claim");
                    return false;
                }

                var clientIP = workContext.GetIPAddress();
                var source = workContext.GetSource();

                var currentLoggedUserRepo = scope.ServiceProvider
                    .GetRequiredService<IRepositoryWithTypedId<CurrentLoggedInUsers, string>>();

                var activeSession = await currentLoggedUserRepo
                    .Query()
                    .AsNoTracking()
                    .Where(x =>
                        x.ClientIP == clientIP ||
                        (x.TypeLogin == (ETypeLogin)source
                         && x.UserId == userId
                         && x.ApiToken == token
                         && !x.KillSession))
                    .FirstOrDefaultAsync();

                if (activeSession == null)
                {
                    _logger.LogInformation("Invalid session attempt for UserId {UserId}", userId);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during session validation");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return false;
            }
        }

        private static string ExtractToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            return context.Request.Cookies["UserToken"]
                ?? context.Request.Cookies["GuestToken"];
        }

        private static bool TryGetUserId(HttpContext context, out long userId)
        {
            userId = 0;
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim) && long.TryParse(claim, out userId);
        }

        private bool ShouldSkipSessionValidation(PathString path)
        {
            return _options.ActiveSession.ExcludedPaths.Any(excludedPath =>
                path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
        }
        #endregion
    }

    public class LookUsersOutMiddleware
    {
        private readonly RequestDelegate Next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMemoryCache _memoryCache;
        private static readonly TimeSpan SessionCheckCacheDuration = TimeSpan.FromSeconds(10);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionCheckLocks = new();

        public LookUsersOutMiddleware(RequestDelegate requestDelegate,
                                  IServiceScopeFactory serviceScopeFactory,
                                  IMemoryCache memoryCache)
        {
            this.Next = requestDelegate;
            _serviceScopeFactory = serviceScopeFactory;
            _memoryCache = memoryCache;
        }

        public async Task Invoke(HttpContext context)
        {
            var apiUrl = context.Request.Path.Value ?? string.Empty;
            if (IsAnonymousRequest(context))
            {
                await this.Next(context);
                return;
            }

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                var usersSites = scope.ServiceProvider.GetRequiredService<IRepository<UsersSites>>();
                var loggedInUsers = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<CurrentLoggedInUsers, string>>();
                var userId = await workContext.GetCurrentUserId();

                if (userId <= 0)
                {
                    await this.Next(context);
                    return;
                }

                var siteId = await workContext.GetCurrentSiteIdAsync();
                var clientIp = workContext.GetIPAddress();

                if (await IsCurrentUserLockedAsync(usersSites, userId, siteId))
                {
                    context.Response.StatusCode = apiUrl.Contains("token/refresh", StringComparison.OrdinalIgnoreCase) ? 500 : 401;
                    await context.Response.WriteAsync("You are looked , please try again later");
                    return;
                }
                if (await IsCurrentUserKilledAsync(loggedInUsers, clientIp, userId))
                {
                    if (apiUrl.Contains("token/refresh", StringComparison.OrdinalIgnoreCase) || apiUrl.Contains("login", StringComparison.OrdinalIgnoreCase))
                    {
                        await RemoveloggedInUser(loggedInUsers, userId, clientIp);
                        _memoryCache.Remove(GetKilledCacheKey(clientIp, userId));
                        context.Response.StatusCode = 500;
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                    }

                    await context.Response.WriteAsync("You are Session End   , please try again later Or Log Out");
                    return;
                }

                await this.Next(context);
            }
        }

        private async Task<bool> IsCurrentUserLockedAsync(IRepository<UsersSites> usersSites, long userId, long siteId)
        {
            var cacheKey = $"look-users-out:locked:{siteId}:{userId}";
            return await GetCachedSessionFlagAsync(
                cacheKey,
                () => usersSites.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && x.SiteId == siteId)
                    .Select(x => x.IsLockout)
                    .FirstOrDefaultAsync());
        }

        private async Task<bool> IsCurrentUserKilledAsync(IRepositoryWithTypedId<CurrentLoggedInUsers, string> loggedInUsers, string clientIp, long userId)
        {
            var cacheKey = GetKilledCacheKey(clientIp, userId);
            return await GetCachedSessionFlagAsync(
                cacheKey,
                () => loggedInUsers.Query()
                    .AsNoTracking()
                    .Where(x => x.ClientIP == clientIp && x.UserId == userId)
                    .Select(x => x.KillSession)
                    .FirstOrDefaultAsync());
        }

        private static async Task RemoveloggedInUser(IRepositoryWithTypedId<CurrentLoggedInUsers, string> loggedInUsers, long UserId, string ClientIp)
        {
            var CurrentLoggedInUser = await loggedInUsers.Query().Where(x => x.ClientIP == ClientIp && x.UserId == UserId).ToListAsync();

            if (CurrentLoggedInUser.Any())
            {
                loggedInUsers.RemoveRange(CurrentLoggedInUser);
                await loggedInUsers.SaveChangesAsync();
            }
        }

        private async Task<bool> GetCachedSessionFlagAsync(string cacheKey, Func<Task<bool>> valueFactory)
        {
            if (_memoryCache.TryGetValue(cacheKey, out bool cachedValue))
            {
                return cachedValue;
            }

            var sessionCheckLock = SessionCheckLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await sessionCheckLock.WaitAsync();
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out cachedValue))
                {
                    return cachedValue;
                }

                cachedValue = await valueFactory();
                _memoryCache.Set(cacheKey, cachedValue, SessionCheckCacheDuration);
                return cachedValue;
            }
            finally
            {
                sessionCheckLock.Release();
                SessionCheckLocks.TryRemove(cacheKey, out _);
            }
        }

        private static string GetKilledCacheKey(string clientIp, long userId)
        {
            return $"look-users-out:killed:{clientIp}:{userId}";
        }

        private static bool IsAnonymousRequest(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            if (!StringValues.IsNullOrEmpty(context.Request.Headers["Authorization"]))
            {
                return false;
            }

            return !context.Request.Cookies.ContainsKey("UserToken")
                && !context.Request.Cookies.ContainsKey("GuestToken");
        }
    }
}
