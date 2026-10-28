using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Infrastructure
{
    public interface IWorkContext
    {
        Task<User> GetCurrentUser();
        Task<long> GetCurrentUserId();
        long GetCurrentUserIdValue();
        long GetCurrentSiteId();
        Task<long> GetCurrentSiteIdAsync();
        EPlatformSource GetCurrentPlatform();
        ETypeLogin GetSource();
        string GetIPAddress();
        string GetCurrentHostname();
        Task<long> GetCurrentUserSiteId(long? UserId = null, long? SiteId = null);
        Task AppendHttpOnlyCookie(string key, string value);
    }

    public class WorkContext : IWorkContext
    {
        private readonly HttpContext _httpContext;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UsersSites> _usersSitesRepository;
        private readonly IConfiguration _configuration;
        private User _currentUser;

        public WorkContext(IHttpContextAccessor contextAccessor, IRepository<User> userRepository,
                           IRepository<UsersSites> usersSitesRepository, IConfiguration configuration)
        {
            _httpContext = contextAccessor.HttpContext;
            _userRepository = userRepository;
            _usersSitesRepository = usersSitesRepository;
            _configuration = configuration;
        }

        private static readonly string _serverIPv4 = ResolveServerIPv4();

        private static string ResolveServerIPv4()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
            }
            return "";
        }

        public string GetIPAddress()
        {
            try
            {
                if (_httpContext.Request.Headers["X-Forwarded-For"].Any())
                {
                    return _httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }
                return _serverIPv4;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<User> GetCurrentUser()
        {
            var id = _httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                _currentUser = null;
                return _currentUser;
            }

            var userId = long.Parse(id);
            _currentUser = await _userRepository.Query().FirstOrDefaultAsync(u => u.Id == userId);
            return _currentUser;
        }

        public Task<long> GetCurrentUserId()
        {
            return Task.FromResult(GetCurrentUserIdValue());
        }

        public long GetCurrentUserIdValue()
        {
            try
            {
                var contextUser = _httpContext.User;
                if (contextUser.FindFirst(ClaimTypes.NameIdentifier) != null)
                    return long.Parse(contextUser.FindFirst(ClaimTypes.NameIdentifier).Value);

                else if (_httpContext.Request.Headers["userId"].Any())
                    return long.Parse(_httpContext.Request.Headers["userId"].FirstOrDefault());

                else return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public long GetCurrentSiteId()
        {
            try
            {
                var siteId = _httpContext.Request.Headers["siteId"];

                if (!string.IsNullOrEmpty(siteId))
                {
                    return long.Parse(siteId);
                }
                return 1;
            }
            catch (Exception exc)
            {
                return 0;
            }
        }

        public async Task<long> GetCurrentSiteIdAsync()
        {
            try
            {
                return GetCurrentSiteIdValueOrZero();
            }
            catch (Exception E)
            {
                return 0;
            }
        }

        private long GetCurrentSiteIdValueOrZero()
        {
            try
            {
                var siteId = _httpContext.Request.Headers["siteId"];

                return long.Parse(siteId);
            }
            catch
            {
                return 0;
            }
        }

        public EPlatformSource GetCurrentPlatform()
        {
            try
            {
                if (_httpContext.Request.Headers["source"].Any())
                {
                    var source = _httpContext.Request.Headers["source"];

                    return (EPlatformSource)int.Parse(source);
                }
                return EPlatformSource.Unknown;
            }
            catch (Exception exc)
            {
                return EPlatformSource.Unknown;
            }
        }

        public ETypeLogin GetSource()
        {
            if (!_httpContext.Request.Headers.TryGetValue("source", out var sourceValue))
                return ETypeLogin.Web;

            if (Enum.TryParse<ETypeLogin>(sourceValue, out var source))
                return source;

            return ETypeLogin.Web;
        }

        public async Task<long> GetCurrentUserSiteId(long? UserId = null, long? SiteId = null)
        {
            try
            {
                if (!UserId.HasValue)
                    UserId = GetCurrentUserIdValue();

                if (!SiteId.HasValue)
                    SiteId = await GetCurrentSiteIdAsync();

                var userSite = await _usersSitesRepository.Query()
                    .AsNoTracking()
                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                    .FirstOrDefaultAsync();

                return userSite.Id;
            }
            catch
            {
                return 0;
            }
        }

        // == EskaCMS.Core WorkContext.GetCurrentHostname
        public string GetCurrentHostname()
        {
            var request = _httpContext?.Request;
            if (request == null)
                return "localhost";

            string host = null;

            if (request.Headers.TryGetValue("X-Forwarded-Host", out var forwardedHost))
            {
                var fh = forwardedHost.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fh) && fh != request.Host.Host && !fh.Contains("localhost"))
                    host = fh;
            }

            if (string.IsNullOrWhiteSpace(host)
                && request.Headers.TryGetValue("Origin", out var origin)
                && Uri.TryCreate(origin.FirstOrDefault(), UriKind.Absolute, out var originUri)
                && !originUri.Host.Contains("localhost"))
            {
                host = originUri.Host;
            }

            if (string.IsNullOrWhiteSpace(host)
                && request.Headers.TryGetValue("Referer", out var referer)
                && Uri.TryCreate(referer.FirstOrDefault(), UriKind.Absolute, out var refererUri)
                && !refererUri.Host.Contains("localhost"))
            {
                host = refererUri.Host;
            }

            if (string.IsNullOrWhiteSpace(host) && !request.Host.Host.Contains("localhost"))
                host = request.Host.Host;

            return string.IsNullOrWhiteSpace(host) ? "localhost" : CleanHost(host);
        }

        private static string CleanHost(string host)
        {
            return host
                .Replace("https://", "")
                .Replace("http://", "")
                .Split(':')[0]
                .Trim()
                .TrimEnd('/');
        }

        // == EskaCMS.Core WorkContext.AppendHttpOnlyCookie (1 year expiry)
        public async Task AppendHttpOnlyCookie(string key, string value)
        {
            var request = _httpContext?.Request;
            if (_httpContext == null || request == null)
                return;

            var domain = GetRootDomain(GetHostFromRequest(request));

            await DeleteCookie(key);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            };

            if (_configuration.GetValue<bool>("IsSharedCookie", false) && !string.IsNullOrWhiteSpace(domain))
                cookieOptions.Domain = domain;

            _httpContext.Response.Cookies.Append(key, value, cookieOptions);
        }

        private Task DeleteCookie(string key)
        {
            var request = _httpContext?.Request;
            if (_httpContext == null || request == null)
                return Task.CompletedTask;

            var domain = GetRootDomain(GetHostFromRequest(request));

            _httpContext.Response.Cookies.Delete(key, new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            if (!string.IsNullOrWhiteSpace(domain))
            {
                _httpContext.Response.Cookies.Delete(key, new CookieOptions
                {
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Domain = domain
                });
            }

            return Task.CompletedTask;
        }

        private static string GetHostFromRequest(HttpRequest request)
        {
            var host = request.Host.Host;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                && request.Headers.TryGetValue("Origin", out var originValues)
                && Uri.TryCreate(originValues.FirstOrDefault(), UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return host;
        }

        private string GetRootDomain(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return null;

            if (!_configuration.GetValue<bool>("IsSharedCookie", false))
                return host.Replace("https://", "").Replace("http://", "");

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(host, out _))
                return null;

            var parts = host.Split('.');
            if (parts.Length < 2)
                return null;

            if (parts.Length >= 3)
                return "." + string.Join(".", parts.Skip(parts.Length - 2));

            return host;
        }
    }
}
