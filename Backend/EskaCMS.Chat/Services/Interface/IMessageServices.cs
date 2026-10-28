using EskaCMS.Chat.DTO;
using EskaCMS.Entities.Entity.Chat;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Security.Models.FriendshipModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Chat.Services.Interface
{
    public interface IMessageServices
    {
        Task<MessageDto> CreateMessage(CreateMassageDTO model);
        Task DeleteMessage(int id);
        Task<PaginationResult<MessageDto>> GetMessagesForUser(MessageParams messageParams);
        Task<CustomerGroup> GetUserGroup(long CustomerId);
        Task<User> GetUser(long UserId);

        Task<Group> GetMessageGroup(string groupName);
        Task<List<MessageDto>> GetMessageP2PThread(long SenderId,long RecipentUserId, long? SiteId = null);
        Task<List<MessageDto>> GetMessageGroupThread(long SenderId, long RecipentGroup, long? SiteId = null);
        Task<Group> GetGroupForConnection(string connectionId);
        long? GetValueOrDefault(long? Id);
        Task<MessageUserDto> GetLatestMassages(List<UserFriendDto> userFriend, long CurrentUserId);
        Task AddDateReadMessagesForGroup(List<CustomerGroupUser> customerGroupUsers,long SiteId,long messageId, ICollection<Connection> connections);

        Task EditUnReadMassagesForP2P(long OtherUser, long CurrentUserId, long SiteId);
        Task EditUnReadMassagesForGroup(long CustomerGroupId, long CurrentUserId, long SiteId);
        Task UpdateMessageContent(long Id, string content);
    }
}
