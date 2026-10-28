using EskaCMS.Core.Models;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Extensions
{
    public class WorkContext : IWorkContext
    {

        private User _currentUser;
        private UserManager<User> _userManager;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.Site> _SitesRepository;

        private readonly IRepository<User> userRepository;
        private readonly IConfiguration configuration;
        private HttpContext _httpContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool IsSharedCookie =false;
		protected readonly EskaDCMSDbContext _context;


		public WorkContext(
         UserManager<User> userManager,
		 EskaDCMSDbContext context,

		 IHttpContextAccessor contextAccessor,
         IRepository<UsersSites> UsersSitesRepository,
         IRepository<User> UserRepository,IConfiguration configuration, 
              IRepository<EskaCMS.Entities.Entity.Core.Site> SitesRepository

            )
        {
            _userManager = userManager;
            _UsersSitesRepository = UsersSitesRepository;
            userRepository = UserRepository;
            _context = context;

			this.configuration=configuration;
            _httpContext = contextAccessor.HttpContext;
            _httpContextAccessor = contextAccessor;
            _SitesRepository = SitesRepository;
        }

		//public async Task ClearUserFromTracker()
		//{
		//	// Find all tracked User entries and detach them
		//	var trackedUsers = _context.ChangeTracker
		//		.Entries<User>()
		//		.ToList();

		//	foreach (var entry in trackedUsers)
		//	{
		//		entry.State = EntityState.Detached;
		//		Console.WriteLine($"Detached User Id={entry.Entity.Id}");
		//	}
		//}

		public async Task UpdateAnynoumsUser(string Username, string Email, long SiteId, string PhoneNumber = null)
        
        {
            try
            {
				var CurrentUser = await CurrentUserInfo();
				
				if (CurrentUser != null)
                {

                    if (CurrentUser.Type == EUsersTypes.GuestClient ||
                        CurrentUser.IsFacebookRegistered ||
                        CurrentUser.IsGoogleRegistered ||
                        CurrentUser.IsAppleRegistered)
                    {
                        if (!string.IsNullOrEmpty(Email))
                            CurrentUser.Email = Email;
                        if (!string.IsNullOrEmpty(PhoneNumber))
                            CurrentUser.PhoneNumber = PhoneNumber;

                        if (!string.IsNullOrEmpty(Username))
                            CurrentUser.FullName = Username;
                        await _userManager.UpdateAsync(CurrentUser);
                        await userRepository.SaveChangesAsync();
                    }
                }
            }
			catch (DbUpdateConcurrencyException ex)
			{
				foreach (var entry in ex.Entries)
				{
					Console.WriteLine("🔥 Concurrency error on entity: "
									  + entry.Entity.GetType().Name);

					var primaryKey = entry.Metadata.FindPrimaryKey()
						.Properties
						.Select(p => entry.Property(p.Name).CurrentValue)
						.FirstOrDefault();

					Console.WriteLine("Key: " + primaryKey);
				}

				throw;
			}

			catch (Exception ex)
            {
                throw ex;
			
				
			}
        }

        // Resolve the server's own IPv4 ONCE at startup instead of doing a blocking
        // Dns.GetHostEntry(Dns.GetHostName()) on a thread-pool thread for EVERY request
        // (a per-request blocking call that contributes to thread-pool starvation under load).
        // The returned value is unchanged — it is the same server IPv4 the per-request lookup produced.
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
                // Ignore — fall back to empty string, same as the original on failure.
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
            var contextUser = _httpContext.User;
            _currentUser = await _userManager.GetUserAsync(contextUser);

			return _currentUser;
        }
     

        public string GetTokenClaimValue(string Key)
        {
            var contextUser = _httpContext.User;
            string value = contextUser.FindFirstValue(Key);
            return value is null ? "" : value;
        }

        public async Task<bool> IsGuestUser()
        {
            var contextUser = _httpContext.User;
            if (contextUser.FindFirst("UserType") != null)
            {
                var UserType = (EUsersTypes)Enum.Parse(typeof(EUsersTypes), contextUser.FindFirst("UserType").Value);
                return UserType == EUsersTypes.GuestClient;
            }
            return false;
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

		
		public async Task<UserInfoClaimsVM> GetCurrentUserInfo()
        {
            try
            {
                var contextUser = _httpContext.User;
                string[] UserRolearr = contextUser.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
                //string[] UserRoles = UserRolearr.Split(',');

                UserInfoClaimsVM UserInfo = new UserInfoClaimsVM();
                UserInfo.UserId = long.Parse(contextUser.FindFirst(ClaimTypes.NameIdentifier).Value);
                UserInfo.Email = contextUser.FindFirst(ClaimTypes.Email).Value;
                UserInfo.FullName = contextUser.FindFirst(ClaimTypes.Name).Value;
                UserInfo.MobileNumber = contextUser.FindFirst(ClaimTypes.MobilePhone).Value;
                UserInfo.RoleId = UserRolearr;
                UserInfo.Username = contextUser.FindFirst("Username").Value;

                if (contextUser.FindFirst("DefaultShippingAddressId") != null)
                    UserInfo.DefaultShippingAddressId = contextUser.FindFirst("DefaultShippingAddressId").Value;

                var currencyClaim= contextUser.FindFirst("CurrencyCultureId");
                if(currencyClaim != null&& long.TryParse(contextUser.FindFirst("CurrencyCultureId").Value, out var currencyCultureId))
                    UserInfo.CurrencyCultureId = currencyCultureId;


                var cultureClaim = contextUser.FindFirst("CultureId");
                if (cultureClaim != null&& long.TryParse(contextUser.FindFirst("CultureId").Value,out var CultureId))
                    UserInfo.CultureId = CultureId;

                UserInfo.UserType = (EUsersTypes)Enum.Parse(typeof(EUsersTypes), contextUser.FindFirst("UserType").Value);

                if (contextUser.FindFirst(ClaimTypes.DateOfBirth) != null)
                {
                    var dobClaim = contextUser.FindFirst(ClaimTypes.DateOfBirth).Value;
                    if (DateTimeOffset.TryParse(dobClaim, out var dateOfBirth))
                    {
                        UserInfo.DateofBirth = dateOfBirth;
                    }
                }
                UserInfo.UserType = (EUsersTypes)Enum.Parse(typeof(EUsersTypes), contextUser.FindFirst("UserType").Value);


                return UserInfo;
            }catch(Exception ex)
            {
                throw;
            }

        }
        public async Task<long> GetCurrentUserCultureCode()
        {
            try
            {
                var UserId = await GetCurrentUserId();
                var SiteId = await GetCurrentSiteIdAsync();

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .AsNoTracking()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .Include(u => u.Culture)
                                    .FirstOrDefault();
                return UserSiteObj.CultureId.GetValueOrDefault();

            }
            catch (Exception)
            {
                return 0;
            }
        }


        public async Task<long> GetCurrentUserCultureId(long SiteId = 0)
        {
            try
            {
                var UserId = await GetCurrentUserId();
                SiteId = SiteId == 0 ? await GetCurrentSiteIdAsync() : SiteId;

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .AsNoTracking()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .Include(u => u.Culture)
                                    .FirstOrDefault();

                if (UserSiteObj != null && UserSiteObj.CultureId.HasValue)
                {
                    return UserSiteObj.CultureId.Value;
                }
                else
                {
                    return 0;
                }


            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<string> GetSiteName()
        {
            try
            {
                var siteid = GetCurrentSiteId();
                var siteName=await _SitesRepository.Query().Where(x=>x.Id==siteid).Select(x=>x.Name).FirstOrDefaultAsync();
                return siteName??string.Empty;
            }catch(Exception ex)
            {
                throw;
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

		public long? GetCartId()
		{
			try
			{
				var cartId = _httpContext.Request.Headers["CartId"];

				if (!string.IsNullOrEmpty(cartId))
				{
					return long.Parse(cartId);
				}
				return null;
			}
			catch (Exception exc)
			{

				return null;
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


        public async Task<string> GetCookieValue(string key)
        {
            try
            {



                var Cookie = _httpContextAccessor.HttpContext.Request.Cookies.FirstOrDefault(c => c.Key == key);
                return Cookie.Value;

            }
            catch (Exception exc)
            {

                throw new Exception("Cannot Find ");
            }


        }
        public OS GetCurrentOS()
        {
            try
            {
                var source = _httpContext.Request.Headers["os"];

                if (!string.IsNullOrEmpty(source))
                {
                    return (OS)int.Parse(source);
                }
                return OS.Web;
            }
            catch (Exception exc)
            {

                return OS.Web;
            }


        }
        public async Task<User> CurrentUserInfo()
        {
            var UserId = await GetCurrentUserId();
            var SiteId = GetCurrentSiteId();
            _currentUser = await _UsersSitesRepository.Query().Include(x => x.Site).Include(x => x.User).ThenInclude(x => x.UserRoles).Where(t => t.UserId == UserId && t.SiteId == SiteId).Select(t => t.User).FirstOrDefaultAsync();
			return _currentUser;
        }
        public ETypeLogin GetSource()
        {


            //var source = _httpContext.Request.Headers["source"];
            if (!_httpContext.Request.Headers.TryGetValue("source", out var sourceValue))
                return ETypeLogin.Web;

            if (Enum.TryParse<ETypeLogin>(sourceValue, out var source))
                return source;

            return ETypeLogin.Web;
        }


            public string GetUserPublicKey()
            {
                try
                {
                    var UserPublicKey = _httpContext.Request.Headers["handshaking"];

                    return UserPublicKey;
                }
                catch (Exception exc)
                {

                    throw exc;
                }


            }


        public long GetCurrentCultureId()
        {
            try
            {

                var CultureId = _httpContext.Request.Headers["cultureId"];
                if (!string.IsNullOrEmpty(CultureId))
                {
                    return long.Parse(CultureId);
                }
                return 0;

            }
            catch (Exception E)
            {
                return 0;
            }

        }
        public async Task<long> GetCurrentWarehouseId()
        {
            try
            {
                var userId = GetCurrentUserIdValue();
                var siteId = GetCurrentSiteIdValueOrZero();

                var warehouseId = await _UsersSitesRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && siteId == x.SiteId)
                    .Select(x => x.WarehouseId)
                    .FirstOrDefaultAsync();

                return warehouseId ?? 0;
            }
            catch
            {
                return 0;
            }
        }


        public long GetCurrentWarehouseIdValue()
        {
            try
            {
                var userId = GetCurrentUserIdValue();
                var siteId = GetCurrentSiteIdValueOrZero();

                var warehouseId = _UsersSitesRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && siteId == x.SiteId)
                    .Select(x => x.WarehouseId)
                    .FirstOrDefault();

                return warehouseId ?? 0;
            }
            catch
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
        public async Task<long> GetCurrentUserSiteId(long? UserId = null, long? SiteId = null)
        {
            try
            {
                if (!UserId.HasValue)
                {
                    UserId = await GetCurrentUserId();
                }


                if (!SiteId.HasValue)
                {
                    SiteId = await GetCurrentSiteIdAsync();

                }

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .AsNoTracking()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .FirstOrDefault();

                return UserSiteObj.Id;

            }
            catch (Exception E)
            {
                return 0;
            }
        }
        public string GetCurrentHostname()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return "localhost";

            string host = null;

            // 1. Try X-Forwarded-Host but ignore if it matches host or is localhost
            if (request.Headers.TryGetValue("X-Forwarded-Host", out var forwardedHost))
            {
                var fh = forwardedHost.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fh) && fh != request.Host.Host && !fh.Contains("localhost"))
                    host = fh;
            }

            // 2. Try Origin ONLY if forwarded host was useless
            if (string.IsNullOrWhiteSpace(host))
            {
                if (request.Headers.TryGetValue("Origin", out var origin) &&
                    Uri.TryCreate(origin.FirstOrDefault(), UriKind.Absolute, out var originUri) &&
                    !originUri.Host.Contains("localhost"))
                {
                    host = originUri.Host;
                }
            }

            // 3. Try Referer if still not found
            if (string.IsNullOrWhiteSpace(host))
            {
                if (request.Headers.TryGetValue("Referer", out var referer) &&
                    Uri.TryCreate(referer.FirstOrDefault(), UriKind.Absolute, out var refererUri) &&
                    !refererUri.Host.Contains("localhost"))
                {
                    host = refererUri.Host;
                }
            }

            // 4. Try Request.Host if still empty and not localhost
            if (string.IsNullOrWhiteSpace(host) && !request.Host.Host.Contains("localhost"))
                host = request.Host.Host;

            // 5. FINAL fallback = localhost
            return string.IsNullOrWhiteSpace(host) ? "localhost" :CleanHost(host);

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


        public List<KeyValuePair<string, string>> GetCookies()
        {
            try
            {
                var allcookies = _httpContextAccessor.HttpContext.Request.Cookies;
                var result = allcookies.ToList();

                return result;
            }
            catch (Exception exc)
            {

                throw;
            }


        }



        public async Task<bool> AppendCookies(string key, string value)
        {
            try
            {


                _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);


                _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value);





                return true;
            }
            catch (Exception exc)
            {

                throw;
            }


        }





        public string GetCurrentGeoLocationLat()
        {
            try
            {
                var lat = _httpContext.Request.Headers["lat"].ToString();
                if (!string.IsNullOrEmpty(lat))
                {
                    return lat;
                }
                return string.Empty;
            }
            catch (Exception exc)
            {

                return string.Empty;
            }
        }

        public string GetCurrentGeoLocationLon()
        {
            try
            {
                var lon = _httpContext.Request.Headers["lon"].ToString();
                if (!string.IsNullOrEmpty(lon))
                {
                    return lon;
                }
                return string.Empty;
            }
            catch (Exception exc)
            {

                return string.Empty;
            }
        }

        public string GetCurrentGeoLocationAddress()
        {
            try
            {
                var Address = _httpContext.Request.Headers["address"].ToString();
                if (!string.IsNullOrEmpty(Address))
                {
                    return Address;
                }
                return string.Empty;
            }
            catch (Exception exc)
            {

                return string.Empty;
            }
        }

        public async Task AppendHttpOnlyCookie(string key, string value, int expiryInYears)
        {
            var context = _httpContextAccessor.HttpContext;
            var request = context?.Request;
            if (context == null || request == null)
                return;

            // Get host from request, fallback to origin header
            var host = GetHostFromRequest(request);
            var domain = GetRootDomain(host);

            await DeleteCookie(key);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddYears(expiryInYears),
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            };
            IsSharedCookie = configuration.GetValue<bool>("IsSharedCookie", false);

            if (IsSharedCookie)
            {
                if (!string.IsNullOrWhiteSpace(domain))
                    cookieOptions.Domain = domain;
            }
            context.Response.Cookies.Append(key, value, cookieOptions);
        }

        public Task DeleteCookie(string key)
        {
            var context = _httpContextAccessor.HttpContext;
            var request = context?.Request;
            if (request == null || context == null)
                return Task.CompletedTask;

            var host = GetHostFromRequest(request);
            var domain = GetRootDomain(host);

            context.Response.Cookies.Delete(key, new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            if (!string.IsNullOrWhiteSpace(domain))
            {
                context.Response.Cookies.Delete(key, new CookieOptions
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

        private string GetHostFromRequest(HttpRequest request)
        {
            var host = request.Host.Host;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Headers.TryGetValue("Origin", out var originValues))
                {
                    var origin = originValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(origin))
                    {
                        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            return uri.Host;
                        }
                    }
                }
            }

            return host;
        }

        private string? GetRootDomain(string host)
        {
            IsSharedCookie = configuration.GetValue<bool>("IsSharedCookie", false);

            if (string.IsNullOrWhiteSpace(host))
                return null;

            if (IsSharedCookie)
            {
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    IPAddress.TryParse(host, out _))
                {
                    return null; 
                }

                var parts = host.Split('.');
                if (parts.Length < 2)
                    return null;

                if (parts.Length >= 3)
                {
                    var rootDomain = string.Join(".", parts.Skip(parts.Length - 2));
                    return "." + rootDomain; 
                }

                return host; // e.g., "eskadenia.com"
            }
            else
            {
                return host.Replace("https://", "").Replace("http://", "");
            }
        }




        public async Task AppendHttpOnlyCookie(string key, string value)
        {



            await AppendHttpOnlyCookie(key, value, 1);
        }

        public async Task SetRequestHeader(string Key, string Value)
        {

            _httpContextAccessor.HttpContext.Request.Headers.Remove(Key);

            _httpContextAccessor.HttpContext.Request.Headers.Add(Key, Value);

        }
        
        public SourceTraffic GetTrafficSource()
        {
          //  var referer = _httpContext.Request.Headers["Reffrer"].ToString();
            return SourceTraffic.None;

        }
    }
}
