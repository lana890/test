using EskaCMS.Entities.Entity;
using EskaCMS.Core.ViewModels.LoggedInUser;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface ILoggedInUserService
    {

        Task<bool> ChangeLookedOutUserStatus(long Id,bool LookedOut);


        Task<bool> AddCurrentLoggedInUsers(string token, ETypeLogin Source, string RefreshToken, User user, long SiteId);

        Task<bool> RemoveloggedInUser(List<long> UsersId,string IpAddress);
        Task<PaginationResult<LoggedInUserVM>> GetAll(SearchParamsVM searchParams);

        //Task<bool> CheckIfLookedOutUser(long UserId, long SiteId);
        Task<bool> CheckIfLookedOutUser(long UserId,long SiteId);
        Task<List<SessionsUserViewModel>> GetUserSessions(string UserName);
        Task<bool> RemoveSession(string Id);
        Task<List<LoggedInUserVM>> GetLoggedInUsers(long UserId);
        Task<bool> IsCurrentUserLooked();
        Task<bool> RemoveloggedInUser(long UserId, string ClientIp);
        Task<bool> KillSessionsOnCurrentUser(string id);
        Task<bool> CheckUserIsKilled(string ClientIp, long UserId);
    }

}
