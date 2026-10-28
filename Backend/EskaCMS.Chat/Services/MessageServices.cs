using EskaCMS.Chat.DTO;
using EskaCMS.Chat.Services.Interface;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Chat;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Security.Models.FriendshipModels;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using NUglify;
using Org.BouncyCastle.Cms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace EskaCMS.Chat.Services
{
    public class MessageServices : IMessageServices
    {
        private readonly IRepository<Message> messageRepository;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<MassagesScheduledGroup> massagesScheduledGroupRepository;
        private readonly IRepositoryWithTypedId<Group, string> grouprepository;
        private readonly IRepository<Friendships> userFriendshipsRepository;
        private readonly IRepository<CustomerGroup> customerGroupRepository;
        private readonly IWorkContext workContext;
        private readonly IRestrictedWordsServices _restrictedWordsServices;
        protected static byte[] key = { 167, 45, 20, 187, 124, 15, 97, 27, 103, 193, 40, 157, 225, 128, 65, 137, 157, 93, 26, 123, 169, 96, 183, 68, 173, 83, 84, 5, 126, 117, 30, 246 };
        protected static byte[] IV = { 166, 220, 28, 159, 17, 192, 129, 215, 189, 102, 122, 194, 199, 44, 154, 62 };

        public MessageServices(IRepository<Message> MessageRepository
                             , IRepository<User> userRepository
                             , IRepository<MassagesScheduledGroup> MassagesScheduledGroupRepository
                             , IRepositoryWithTypedId<Group, string> grouprepository,
                              IRepository<Friendships> userFriendshipsRepository

                             , IRepository<CustomerGroup> CustomerGroupRepository,
                              IRestrictedWordsServices restrictedWordsServices,
                               IWorkContext workContext)
        {
            messageRepository = MessageRepository;
            this.userRepository = userRepository;
            massagesScheduledGroupRepository = MassagesScheduledGroupRepository;
            this.grouprepository = grouprepository;
            this.userFriendshipsRepository = userFriendshipsRepository;
            this.grouprepository = grouprepository;
            customerGroupRepository = CustomerGroupRepository;
            this.workContext = workContext;
            _restrictedWordsServices = restrictedWordsServices;
        }

        public async Task<MessageDto> CreateMessage(CreateMassageDTO model)
        {
            try
            {
                var SenderId =model.UserId.HasValue? model.UserId.Value: await workContext.GetCurrentUserId();

                var RecipientCustomerGroup = new CustomerGroup();
                var RecipientUser = new User();

                var isViolationMessage = await _restrictedWordsServices.IsRestrictedWord(model.Content,model.SiteId);

                if (model.RecipientUserId.HasValue)
                {
                    if (SenderId == model.RecipientUserId.Value)
                        throw new("You cannot send messages to yourself");
                    await GetUser(model.RecipientUserId.Value);
                }
                else
                {
                    await GetUserGroup(model.RecipientCustomerGroupId.Value);
                }

                var message = new Message
                {
                    SenderId = SenderId,
                    RecipientId = GetValueOrDefault(model.RecipientUserId),
                    RecipientCustomerGroupId = GetValueOrDefault(model.RecipientCustomerGroupId),
                    Content = model.Content,
                  //  ReplyId = GetValueOrDefault(model.ReplyId),
                    IsViolation=isViolationMessage,
                    SiteId=model.SiteId,
                 //   IsForwarded = model.IsForwarded

                };
                messageRepository.Add(message);

                await messageRepository.SaveChangesAsync();

                return await GetMessageById(message.Id);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaginationResult<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            try
            {
                var Result = new MessageUserDto();
                var User = await GetUser(await workContext.GetCurrentUserId());

               // messageParams.Username = User.UserName;

                var query = messageRepository.Query()
                                    .AsNoTrackingWithIdentityResolution()
                                    .Include(x => x.Sender)
                                    .Include(x => x.Recipient)
                                    .Include(x => x.RecipientCustomerGroup)
                                        .ThenInclude(x => x.Users)
                                            .ThenInclude(x => x.User)
                                    //.Include(x => x.Reply)
                                    //.Include("Reply.Sender")
                                    //.Include("Reply.Recipient")
                                    //.Include("Reply.RecipientCustomerGroup")
                                    //    .Include("Reply.RecipientCustomerGroup.Users")
                                    //        .Include("Reply.RecipientCustomerGroup.Users.User")
                                  
                             .OrderByDescending(x => x.MessageSent)
                             .AsQueryable();

               
                
                var messages = await query.
                                            Select(u => new MessageDto
                                            {
                                                Id = u.Id,
                                         //       ReplyId = u.ReplyId,
                                                SenderId = u.SenderId,
                                                SenderUsername = u.Sender.UserName,
                                                MessageSent = u.MessageSent,
                                                Content = EncryptDecryptHelper.Decrypt(u.Content),
                                                DateRead = u.DateRead,
                                                RecipientId = u.RecipientId,
                                                RecipientUsername = u.Recipient != null ? u.Recipient.UserName : null,
                                                RecipientGroup = u.RecipientCustomerGroup != null ?
                                                    u.RecipientCustomerGroup.Users.Select(x => x.User).Select(r => new UserGroupDTO
                                                    {
                                                        RecipientId = r.Id,
                                                        RecipientUsername = r.UserName
                                                    }).ToList() :
                                                    new List<UserGroupDTO>(),
                                               // Reply = u.ReplyId.HasValue ? GetReply(u.Reply) : null,
                                               // IsForwarded = u.IsForwarded
                                            }).PaginateAsync(messageParams.PageSize, messageParams.PageNumber);

                return messages;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task DeleteMessage(int id)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var message = await messageRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
                if (message.SenderId != UserId)
                    throw new Exception("UnAuthorized To Deleted");
                message.SenderDeleted = true;
                if (message.SenderId == UserId)
                    message.SenderDeleted = true;

                messageRepository.Remove(message);

                await messageRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region Helpers
        public async Task<CustomerGroup> GetUserGroup(long CustomerId)
        {
            try
            {
                var Recipient = await customerGroupRepository.Query().Include(x=>x.customerGroupDetails).Include(x=>x.GroupType).Include(x => x.Users).Where(x => x.Id == CustomerId).Include(x=>x.Users).ThenInclude(x=>x.User).FirstOrDefaultAsync();
                if (Recipient == null)
                {
                    throw new Exception("Recipent Not Exist");

                }

                return Recipient;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<User> GetUser(long UserId)
        {
            try
            {
                var User = await userRepository.Query().Where(x => x.Id == UserId).FirstOrDefaultAsync();

                if (User == null)
                {
                    throw new Exception("User Not Exist");
                }

                return User;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public MessageDto GetReply(Message message)
        {
            try
            {

                var objmessage = new MessageDto
                {
                    Id = message.Id,
               //     ReplyId = message.ReplyId,
                    SenderId = message.SenderId,
                    SenderUsername = message.Sender.UserName,
                    SenderImage = message.Sender.Image,
                    SenderFullName = message.Sender.FullName,
                    MessageSent = message.MessageSent,
                    Content = EncryptDecryptHelper.Decrypt(message.Content ),
                    DateRead = message.DateRead,
                    RecipientId = message.RecipientId,
                    RecipientUsername = message.Recipient != null ? message.Recipient.UserName : null,
                    RecipientGroup = message.RecipientCustomerGroup != null ?
                                                        message.RecipientCustomerGroup.Users.Select(x => x.User).Select(r => new UserGroupDTO
                                                        {
                                                            RecipientId = r.Id,
                                                            RecipientUsername = r.UserName
                                                        }).ToList() :
                                                    new List<UserGroupDTO>(),  
                };
                return objmessage;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public long? GetValueOrDefault(long? Id)
        {
            if (Id == null || Id == 0)
            {
                return null;
            }
            else
            {
                return Id;
            }
        }
        private async Task<MessageDto> GetMessageById(long id)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var message = await messageRepository.Query()
                                    .Where(x => x.Id == id )
                                    .Include(x => x.Sender)
                                    .Include(x => x.Recipient)
                                    .Include(x => x.RecipientCustomerGroup)
                                        .ThenInclude(x => x.Users)
                                            .ThenInclude(x => x.User)
                                     .Include(x=>x.MassagesScheduledGroup)
                                  //   .Include(x=>x.Reply)
                                    //  .Include("Reply.Sender")
                                    //.Include("Reply.Recipient")
                                    //.Include("Reply.RecipientCustomerGroup")
                                    //    .Include("Reply.RecipientCustomerGroup.Users")
                                    //        .Include("Reply.RecipientCustomerGroup.Users.User")
                               

                                    .AsNoTrackingWithIdentityResolution()
                                    .Select(u => new MessageDto
                                    {
                                        SenderId = u.SenderId,
                                        SenderUsername = u.Sender.UserName,
                                        SenderFullName = u.Sender.FullName,
                                        SenderImage = u.Sender.Image,
                                        MessageSent = u.MessageSent,
                                        Content =u.Content,
                                        DateRead = u.DateRead,
                                        Id = u.Id,
                                        IsViolation = u.IsViolation,
                                     //   ReplyId = u.ReplyId,
                                        RecipientId = u.RecipientId,
                                        RecipientUsername = u.Recipient != null ? u.Recipient.UserName : null,
                                        RecipientGroup = u.RecipientCustomerGroup != null ?
                                            u.RecipientCustomerGroup.Users.Select(x => x.User).Select(r => new UserGroupDTO
                                            {
                                                RecipientId = r.Id,
                                                RecipientUsername = r.UserName
                                            }).ToList() :
                                            new List<UserGroupDTO>(),
                                        ReadMassageGroup = u.MassagesScheduledGroup.Count() == 0 ? new List<ReadMassageGroupDto>() :u.MassagesScheduledGroup.Select(y => new ReadMassageGroupDto
                                        {

                                            DateRead = y.DateRead,
                                            UserId = y.UserId,
                                            FullName = y.User.FullName

                                        }).ToList(),
                                       // Reply = u.ReplyId.HasValue? GetReply(u.Reply):null,
                          //              IsForwarded = u.IsForwarded

                                    }).FirstOrDefaultAsync();

                return message;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await grouprepository.Query()
                .Include(x => x.Connections)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == groupName);
        }
        public async Task<List<MessageDto>> GetMessageP2PThread(long SenderId, long RecipentUserId,long?SiteId=null)
        {
            try
            {
                var query = messageRepository.Query()
                    .Include(x => x.Sender)
                    .Include(x => x.Recipient)
                            .Where(
                                m => ((m.RecipientId == SenderId && m.RecipientDeleted == false &&
                                m.SenderId == RecipentUserId) ||
                              (m.RecipientId == RecipentUserId && m.SenderDeleted == false &&
                                m.SenderId == SenderId)) &&
                                 (m.IsViolation ?  m.SenderId == SenderId : true)
                            )
                         //   .Include(x => x.Reply)
                            //.Include("Reply.Sender")
                            //.Include("Reply.Recipient")
                            //.Include("Reply.RecipientCustomerGroup")
                            //    .Include("Reply.RecipientCustomerGroup.Users")
                            //        .Include("Reply.RecipientCustomerGroup.Users.User")
                           
                            .Where(x=>x.SiteId== SiteId)
                            .OrderBy(m => m.MessageSent)
                            .AsQueryable();
                var unreadMessages = query.Where(m => m.DateRead == null).ToList();

                if (unreadMessages.Any())
                {
                    foreach (var message in unreadMessages)
                    {
                        message.DateRead = DateTime.UtcNow;
                    }
                }

              
                var messages = await query.Select(u => new MessageDto
                {
                    Id = u.Id,
              //      ReplyId = u.ReplyId,
                    SenderId = u.SenderId,
                    SenderUsername = u.Sender.UserName,
                    SenderImage = u.Sender.Image,
                    SenderFullName = u.Sender.FullName,
                    MessageSent = u.MessageSent,
                    Content = EncryptDecryptHelper.Decrypt( u.Content),
                    DateRead = u.DateRead,
                    IsViolation=u.IsViolation,
                    RecipientId = u.RecipientId,
                    RecipientUsername = u.Recipient != null ? u.Recipient.UserName : null,
             //       Reply = u.ReplyId.HasValue ? GetReply(u.Reply) : null,
                 //   IsForwarded = u.IsForwarded

                }).ToListAsync();
              await  messageRepository.SaveChangesAsync();
                return messages;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<MessageDto>> GetMessageGroupThread(long SenderId, long RecipentGroupId, long? SiteId = null)
        {
            try
            {

                var query = messageRepository.Query()
                    .Include(x => x.Sender)
                    .Include(x => x.Recipient)
                            .Where(
                                m => (m.RecipientCustomerGroupId == RecipentGroupId && m.RecipientDeleted == false && m.SenderDeleted == false) &&
                                    (m.IsViolation ?  m.SenderId == SenderId : true)
                            )
                            .Where(x=>x.SiteId== SiteId)
                            .OrderBy(m => m.MessageSent)
                           // .Include(x => x.Reply)
                            //.Include("Reply.Sender")
                            //.Include("Reply.Recipient")
                            //.Include("Reply.RecipientCustomerGroup")
                            //    .Include("Reply.RecipientCustomerGroup.Users")
                            //        .Include("Reply.RecipientCustomerGroup.Users.User")
                       
                                    .AsQueryable();
                var unreadMessages = query.Where(m => m.DateRead == null).ToList();

                if (unreadMessages.Any())
                {
                    foreach (var message in unreadMessages)
                    {
                        message.DateRead = DateTime.UtcNow;
                    }
                }

                var messages = await query.Select(u => new MessageDto
                {
                    Id = u.Id,
                   // ReplyId = u.ReplyId,
                    SenderId = u.SenderId,
                    SenderUsername = u.Sender.UserName,
                    SenderFullName = u.Sender.FullName,
                    SenderImage = u.Sender.Image,
                    MessageSent = u.MessageSent,
                    Content = EncryptDecryptHelper.Decrypt(u.Content),
                    DateRead = u.DateRead,
                    IsViolation=u.IsViolation,
                    RecipientId = u.RecipientId,
                    RecipientUsername = u.Recipient != null ? u.Recipient.UserName : null,
                 //   Reply = u.ReplyId.HasValue ? GetReply(u.Reply) : null,
                  //  IsForwarded = u.IsForwarded

                }).ToListAsync();

            
                await messageRepository.SaveChangesAsync();


                return messages;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await grouprepository.Query()
                .Include(x => x.Connections)
                .Where(x => x.Connections.Any(c => c.Id == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<MessageUserDto> GetLatestMassages(List<UserFriendDto> userFriends, long CurrentUserId)
        {
            try
            {
                var MessageUser = new MessageUserDto();
                var  massagesP2P=await messageRepository.Query().Include(x=>x.Sender)
                                                             .Include(x=>x.Recipient)
                                                             .Where(x=>x.SenderId==CurrentUserId || x.RecipientId==CurrentUserId)
                                                             .Where(x=>!x.RecipientCustomerGroupId.HasValue)
                                                            // .Include(x => x.Reply)
                                                            //.Include("Reply.Sender")
                                                            //.Include("Reply.Recipient")  
                                                             .AsNoTracking()
                                                             .ToListAsync();
                if (massagesP2P != null)
                {
                    foreach (var userFriend in userFriends)
                    {
                        var MassageP2P = massagesP2P.Where(
                                m => ((m.RecipientId == CurrentUserId && m.RecipientDeleted == false &&
                                m.SenderId == userFriend.UserId) ||
                              (m.RecipientId == userFriend.UserId && m.SenderDeleted == false &&
                                m.SenderId == CurrentUserId)) &&
                                 (m.IsViolation ? m.SenderId == CurrentUserId : true))
                                 .ToList();
                            
                           
                        if (MassageP2P != null&&MassageP2P.Count>0)
                        {
                             var LastMassageP2P= MassageP2P.OrderByDescending(x => x.MessageSent).FirstOrDefault();

                            MessageUser.P2PMessage.Add(new GetP2PMessageUser
                            {

                                MessageSent = LastMassageP2P.MessageSent,
                                SenderId = LastMassageP2P.SenderId,
                                SenderUsername = LastMassageP2P.Sender.FullName,
                                Content = EncryptDecryptHelper.Decrypt(LastMassageP2P.Content),
                                RecipientId = LastMassageP2P.RecipientId,
                                RecipientUsername = LastMassageP2P.Recipient.FullName,
                                UserFriendId = userFriend.UserId,
                                DateRead= LastMassageP2P.SenderId== CurrentUserId?DateTimeOffset.Now:LastMassageP2P.DateRead,
                                CountUnReadMassage= MassageP2P.Where(x=>x.RecipientId== CurrentUserId && x.DateRead==null).Count(),
                                //Reply = LastMassageP2P.ReplyId.HasValue ? new GetP2PMessageReplay {

                                //    MessageSent = LastMassageP2P.Reply.MessageSent,
                                //    SenderId = LastMassageP2P.Reply.SenderId,
                                //    SenderUsername = LastMassageP2P.Reply.Sender.FullName,
                                //    Content = EncryptDecryptHelper.Decrypt(LastMassageP2P.Reply.Content),
                                //    RecipientId = LastMassageP2P.Reply.RecipientId,
                                //    RecipientUsername = LastMassageP2P.Reply.Recipient.FullName,
                                //    DateRead = LastMassageP2P.Reply.SenderId == CurrentUserId ? DateTimeOffset.Now : LastMassageP2P.Reply.DateRead,

                                //} : null,
                      //          IsForwarded = LastMassageP2P.IsForwarded

                            });
                          }
                    }
                    MessageUser.UnreadMassagesCount += MessageUser.P2PMessage.Select(x=>x.CountUnReadMassage).Sum();
                }
                var latestMessagesGroups = await messageRepository
                                                    .Query()
                                                    .AsNoTrackingWithIdentityResolution()
                                                    .Include(x => x.RecipientCustomerGroup)
                                                    .ThenInclude(x => x.Users)
                                                    .Include(x=>x.Sender)
                                                    .Include(x=>x.MassagesScheduledGroup)
                                                    .ThenInclude(x=>x.User)
                                                    .Where(x => x.SenderId == CurrentUserId || x.RecipientCustomerGroup.Users.Any(x => x.UserId == CurrentUserId))
                                                    .Where(x => x.RecipientCustomerGroupId.HasValue)
                                                    //.Include(x => x.Reply)
                                                    //.Include("Reply.RecipientCustomerGroup")
                                                    //.Include("Reply.RecipientCustomerGroup.Users")
                                                    //.Include("Reply.MassagesScheduledGroup")
                                                    //.Include("Reply.MassagesScheduledGroup.User")
                                                    .ToListAsync();
                var distinctGroups = latestMessagesGroups
                                                  .Where(x => x.SenderId == CurrentUserId || x.RecipientCustomerGroup.Users.Any(x => x.UserId == CurrentUserId))
                                                  .Where(x => x.RecipientCustomerGroupId.HasValue)
                                                  .Select(x => x.RecipientCustomerGroupId)
                                                  .Distinct()
                                                  .ToList();

                foreach (var distinctGroup in distinctGroups)
                {
                    var MassageGroup = latestMessagesGroups.Where(x => x.RecipientCustomerGroupId == distinctGroup.Value).ToList();
                    var latestMassageGroup  = MassageGroup.OrderByDescending(x => x.MessageSent)
                                                                 .Select(x => new GetGroupMessageUser
                                                                 {
                                                                     SenderId = x.SenderId,
                                                                     SenderUsername = x.Sender.FullName,
                                                                     MessageSent = x.MessageSent,
                                                                     Content = EncryptDecryptHelper.Decrypt(x.Content),
                                                                     GroupId = x.RecipientCustomerGroupId.Value,
                                                                     ReadMassageGroupDto=x.MassagesScheduledGroup.Count()==0?new List<ReadMassageGroupDto>():x.MassagesScheduledGroup.Select(y=>new ReadMassageGroupDto
                                                                     { 
                                                                         DateRead = y.DateRead,
                                                                         UserId = y.UserId,
                                                                         FullName=y.User.FullName
                                                                     }).ToList()
                                                                     ,CountUnReadMassage= MassageGroup.SelectMany(x => x.MassagesScheduledGroup).Where(x => x.UserId == CurrentUserId && x.DateRead == null).Count(),
                                                                     //Reply = x.ReplyId.HasValue ?  new GetGroupMessageReplay {


                                                                     //    SenderId = x.Reply.SenderId,
                                                                     //    SenderUsername = x.Reply.Sender.FullName,
                                                                     //    MessageSent = x.Reply.MessageSent,
                                                                     //    Content = EncryptDecryptHelper.Decrypt(x.Reply.Content),
                                                                     //    GroupId = x.Reply.RecipientCustomerGroupId.Value
                                                                     //,
                                                                     //    ReadMassageGroupDto = x.Reply.MassagesScheduledGroup.Count() == 0 ? new List<ReadMassageGroupDto>() : x.Reply.MassagesScheduledGroup.Select(y => new ReadMassageGroupDto
                                                                     //    {
                                                                     //        DateRead = y.DateRead,
                                                                     //        UserId = y.UserId,
                                                                     //        FullName = y.User.FullName
                                                                     //    }).ToList()
                                                                     //} : null,
                                                                     //IsForwarded = x.IsForwarded
                                                                 }).FirstOrDefault();
                    MessageUser.GroupMessage.Add(latestMassageGroup);


                   MessageUser.UnreadMassagesCount += MessageUser.GroupMessage.Count()>0 ? MassageGroup.SelectMany(x=>x.MassagesScheduledGroup).Where(x=>x.UserId==CurrentUserId&& x.DateRead==null).Count():0;
                }
                return MessageUser;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }

        public async Task AddDateReadMessagesForGroup(List<CustomerGroupUser> customerGroupUsers,long SiteId, long MessageId, ICollection<Connection> connections)
        {
            try
            {
                var MassagesScheduled = await massagesScheduledGroupRepository.Query().Where(x => x.MessageId == MessageId).ToListAsync();

                if (MassagesScheduled==null || MassagesScheduled.Count() == 0)
                {
                    List<MassagesScheduledGroup> scheduledGroups = new List<MassagesScheduledGroup>();

                    foreach (var group in customerGroupUsers)
                    {

                        scheduledGroups.Add(new MassagesScheduledGroup
                        {
                            DateRead = connections.Any(x => group.UserId == x.UserId) ? DateTime.UtcNow : null,
                            CustomerGroupId = group.CustomerGroupId,
                            UserId = group.UserId,
                            MessageId = MessageId,
                            SiteId= SiteId,

                        });
                    }
                    massagesScheduledGroupRepository.AddRange(scheduledGroups);
                }
                else
                {
                    foreach (var MassageScheduled in MassagesScheduled)
                    {
                        MassageScheduled.DateRead = connections.Any(x => customerGroupUsers.Any(y=>y.UserId == x.UserId)) ? DateTime.UtcNow : null;
                    }
                
                }
                await massagesScheduledGroupRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task EditUnReadMassagesForP2P(long OtherUser, long CurrentUserId,long SiteId)
        {
            try
            {
                var query =await messageRepository.Query()
                  .Include(x => x.Sender)
                  .Include(x => x.Recipient)
                          .Where(
                              m => ((m.RecipientId == CurrentUserId && m.RecipientDeleted == false &&
                              m.SenderId == OtherUser) ||
                            (m.RecipientId == OtherUser && m.SenderDeleted == false &&
                              m.SenderId == CurrentUserId)) &&
                               (m.IsViolation ? m.SenderId == CurrentUserId : true)
                          )
                          .Where(x => x.SiteId == SiteId)
                          .OrderBy(m => m.MessageSent)
                          .ToListAsync();
                var unreadMessages = query.Where(m => m.DateRead == null && m.RecipientId== CurrentUserId).ToList();

                if (unreadMessages.Any())
                {
                    foreach (var message in unreadMessages)
                    {
                        message.DateRead = DateTime.UtcNow;
                    }
                  await messageRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }

        public async Task EditUnReadMassagesForGroup(long CustomerGroupId, long CurrentUserId, long SiteId)
        {
            try
            {
                var unreadMessages = await massagesScheduledGroupRepository.Query()
                          .Where(
                              m => (CustomerGroupId==m.CustomerGroupId && 
                              m.UserId == CurrentUserId && m.DateRead == null) )
                                  .Where(x => x.SiteId == SiteId)
                                  .ToListAsync();
                if (unreadMessages != null && unreadMessages.Count() > 0)
                {
                    if (unreadMessages.Any())
                    {
                        foreach (var message in unreadMessages)
                        {
                            message.DateRead = DateTime.UtcNow;
                        }
                        await messageRepository.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task UpdateMessageContent(long Id,string content)
        {
            try
            {
                var query = await messageRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                query.Content = await EncryptAsync(content);

               await messageRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> EncryptAsync(string pDataToEncrypt)
        {
            using (RijndaelManaged myRijndael = new RijndaelManaged())
            using (ICryptoTransform encryptor = myRijndael.CreateEncryptor(key, IV))
            using (MemoryStream msEncrypt = new MemoryStream())
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                byte[] toEncrypt = Encoding.UTF8.GetBytes(pDataToEncrypt);
                await csEncrypt.WriteAsync(toEncrypt, 0, toEncrypt.Length);
                await csEncrypt.FlushAsync();
                await csEncrypt.FlushFinalBlockAsync();

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }

        #endregion

    }
}
