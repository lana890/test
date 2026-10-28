using EskaCMS.Core.Models;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface  IUserSiteService
    {
        Task<UserCountryVM> GetUserCountry();
        Task<bool> IsLocalCountry();
       Task< string> GetSiteCountry(long SiteId);
        Task<List<long>> GetUserRoles(long siteId, long userId);
        Task UpdateUserAvailablity(long userId, long siteId, ConnectionStatus connectionStatus);
    }
}
