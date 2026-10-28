using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using System.Collections.Generic;

namespace EskaCMS.Core.Extensions
{
    public interface IWorkContext
    {
        string GetIPAddress();

		Task<User> GetCurrentUser();
        Task<bool> IsGuestUser();
        Task<long> GetCurrentUserId();
	
        long GetCurrentUserIdValue();
		Task<long> GetCurrentSiteIdAsync();
        long GetCurrentSiteId();
        long GetCurrentCultureId();
        long? GetCartId();

		string GetCurrentHostname();
        Task<long> GetCurrentUserCultureCode();
        string GetTokenClaimValue(string Key);
        Task<long> GetCurrentUserCultureId(long SiteId = 0);

        Task<UserInfoClaimsVM> GetCurrentUserInfo();
        ETypeLogin GetSource();
       Task UpdateAnynoumsUser(string Username, string Email, long SiteId, string PhoneNumber = null);

        Task<long> GetCurrentUserSiteId(long? UserId = null , long? SiteId = null);

        string GetUserPublicKey();
        EPlatformSource GetCurrentPlatform();
        List<KeyValuePair<string, string>> GetCookies();
        Task DeleteCookie(string key);
        Task<bool> AppendCookies(string key, string value);
        Task<long> GetCurrentWarehouseId();
        long GetCurrentWarehouseIdValue();
        OS GetCurrentOS();
        Task<User> CurrentUserInfo();
        string GetCurrentGeoLocationLat();
        string GetCurrentGeoLocationLon();
        string GetCurrentGeoLocationAddress();
        Task AppendHttpOnlyCookie(string key, string value);
        Task AppendHttpOnlyCookie(string key, string value, int expiryInYears);
        Task<string> GetCookieValue(string key);
        Task SetRequestHeader(string Key, string Value);
        SourceTraffic GetTrafficSource();
        Task<string> GetSiteName();


    }
}
