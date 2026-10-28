using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Security.Models.FriendshipModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserFriendshipService
    {
        Task<bool> CreateFriendrequest(GetUserInfoVm model);
        Task<bool> RequestAndConfirmFriendship(GetUserInfoVm model);
        Task<bool> ActionOnFriendRequest(string id, EFriendshipStatus action,string EmailTemplateId);
        Task<bool> DeleteFriend(string id);
        Task<PaginationResult<UserRequestsDto>> GetAllFriendRequest(SmartTableParam model);
        Task<PaginationResult<UserFriendsDto>> GetAllFriends(SmartTableParam model);
        Task<bool> CancelSendingRequest(string id);
        Task<PaginationResult<UserRequestsDto>> GetAllSentFriendRequest(SmartTableParam model);
        Task<UserFriendShipDto> GetUserProfile(string id);
        Task<PaginationResult<UserFriendShipDto>> GetAllUsers(SmartSearchTableParam<AllUsersVM> model);

        Task<List<UserFriendDto>> GetFriendsByUserId(long UserId, long SiteId);
    }
}
