using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Middlewares.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Middlewares.Middlewares
{
    public class SecurityValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityValidationMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration configuration;
        private readonly SecurityMiddlewareOptions _options;

        public SecurityValidationMiddleware(
            RequestDelegate next,
            ILogger<SecurityValidationMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<SecurityMiddlewareOptions> options,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            this.configuration=configuration;

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
                context.Response.Headers["X-Frame-Options"]         = frameAncestors == "'none'" || frameAncestors == "none" ? "DENY" : "SAMEORIGIN";
                context.Response.Headers["X-Content-Type-Options"]  = "nosniff";
                context.Response.Headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
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

        /// <summary>
        /// Extracts the bearer token from the Authorization header,
        /// falling back to UserToken / GuestToken cookies.
        /// </summary>
        private static string? ExtractToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            return context.Request.Cookies["UserToken"]
                ?? context.Request.Cookies["GuestToken"];
        }

        /// <summary>
        /// Tries to parse the NameIdentifier claim as a long userId.
        /// </summary>
        private static bool TryGetUserId(HttpContext context, out long userId)
        {
            userId = 0;
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim) && long.TryParse(claim, out userId);
        }

        /// <summary>
        /// Returns true if the request path starts with any of the configured excluded paths.
        /// </summary>
        private bool ShouldSkipSessionValidation(PathString path)
        {
            return _options.ActiveSession.ExcludedPaths.Any(excludedPath =>
                path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
