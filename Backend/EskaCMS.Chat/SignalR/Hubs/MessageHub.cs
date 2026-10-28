using EskaCMS.Chat.DTO;
using EskaCMS.Chat.Services.Interface;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Chat;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MimeKit;
using Org.BouncyCastle.Cms;
using skaCMS.Chat.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Net.WebRequestMethods;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Chat.Services;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;

namespace EskaCMS.Chat.SignalR.Hubs
{

    public class MessageHub : Hub
    {
        private readonly IMessageServices messageServices;
        private readonly IRepositoryWithTypedId<Group, string> groupRepository;
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly IRepositoryWithTypedId<Entities.Entity.Chat.Connection, string> connectionRepository;
        private readonly IUserFriendshipService userFriendshipService;
        private readonly IHubContext<LastMessageTrackerHub> hubContext;

        public MessageHub(
            IMessageServices messageServices,
            IRepositoryWithTypedId<Group, string> groupRepository,
             IHubContext<PresenceHub> presenceHub,
            IRepositoryWithTypedId<Entities.Entity.Chat.Connection, string> connectionRepository,
            IUserFriendshipService userFriendshipService,
            IHubContext<LastMessageTrackerHub> hubContext

            )
        {
            this.messageServices = messageServices;
            this.groupRepository = groupRepository;
            this.presenceHub = presenceHub;
            this.connectionRepository = connectionRepository;
            this.userFriendshipService = userFriendshipService;
            this.hubContext = hubContext;
        }


        public override async Task OnConnectedAsync()
        {
            var messages = new List<MessageDto>();
            var httpContext = Context.GetHttpContext();

            var UserId = GetUserId();
            var User = await messageServices.GetUser(UserId);
            string groupName = string.Empty;
            long? RecipentUserId = null;
            long? RecipentGroupId = null;
            long? SiteId = null;

            if (httpContext.Request.Query["SiteId"].Any())
            {
                SiteId = Convert.ToInt64(httpContext.Request.Query["SiteId"]);
            }

            if (httpContext.Request.Query["RecipentUserId"].Any())
            {
                RecipentUserId = Convert.ToInt64(httpContext.Request.Query["RecipentUserId"]);
                groupName = GetGroupNameP2P(UserId.ToString(), httpContext.Request.Query["RecipentUserId"]);

            }
            else if (httpContext.Request.Query["RecipentGroupId"].Any())
            {
                RecipentGroupId = Convert.ToInt64(httpContext.Request.Query["RecipentGroupId"]);

                groupName = httpContext.Request.Query["RecipentGroupId"];

            }
            else
            {
                throw new Exception("At Least Send Name Group");
            }

            var group = await AddToGroup(groupName, User, RecipentUserId);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);


            if (RecipentUserId.HasValue)
            {

                messages = await messageServices.GetMessageP2PThread(User.Id, RecipentUserId.Value, SiteId);
            }
            else if (RecipentGroupId.HasValue)
            {
                messages = await messageServices.GetMessageGroupThread(User.Id, RecipentGroupId.Value, SiteId);
            }

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Id).SendAsync("UpdatedGroup");
            await base.OnDisconnectedAsync(ex);
        }


        public async Task SendMessage(CreateMassageDTO createMessageDto)
        {

            var UserId = GetUserId();
            var recipientUser = new User();
            var groupRecipent = new CustomerGroup();
            var groupName = string.Empty;

            if (createMessageDto.RecipientUserId.HasValue)
            {
                if (UserId == createMessageDto.RecipientUserId)
                    throw new HubException("You cannot send messages to yourself");
            }
            var sender = await messageServices.GetUser(UserId);

            if (createMessageDto.RecipientUserId.HasValue)
            {
                recipientUser = await messageServices.GetUser(createMessageDto.RecipientUserId.Value);
                if (recipientUser == null)
                {
                    throw new HubException("Not found user");
                }
                groupName = GetGroupNameP2P(sender.Id.ToString(), recipientUser.Id.ToString());
            }
            else
            {
                groupRecipent = await messageServices.GetUserGroup(createMessageDto.RecipientCustomerGroupId.Value);
                if (groupRecipent == null)
                {
                    throw new HubException("Not found Group");
                }
                groupName = createMessageDto.RecipientCustomerGroupId.Value.ToString();
            }

            var group = await messageServices.GetMessageGroup(groupName);

            var message = new Message
            {
                SenderId = UserId,
                RecipientCustomerGroupId = messageServices.GetValueOrDefault(createMessageDto.RecipientCustomerGroupId),
                RecipientId = messageServices.GetValueOrDefault(createMessageDto.RecipientUserId),
                Content = createMessageDto.Content,
                SiteId = createMessageDto.SiteId.HasValue ? createMessageDto.SiteId.Value : null
            };



            var ObjMassage = await messageServices.CreateMessage(new CreateMassageDTO
            {

                Content = message.Content,
            //    ReplyId = message.ReplyId,
              //  IsForwarded = message.IsForwarded,
                RecipientCustomerGroupId = message.RecipientCustomerGroupId,
                RecipientUserId = message.RecipientId,
                DateRead = message.DateRead,
                UserId = UserId,
                SiteId = message.SiteId
            });


            await Task.Run(async () =>
            {
                await messageServices.UpdateMessageContent(ObjMassage.Id, ObjMassage.Content);
            });
            //P2P
            if (createMessageDto.RecipientUserId.HasValue)
            {

                if (group.Connections.Any(x => x.UserId == createMessageDto.RecipientUserId.Value))
                {
                    message.DateRead = DateTime.UtcNow;
                }
                else
                {
                    var connections = await PresenceTracker.GetConnectionsForUser(recipientUser.Id);
                    if (group.Connections.Count > 0)
                    {
                        var FriendsRecipent = await userFriendshipService.GetFriendsByUserId(createMessageDto.RecipientUserId.Value, createMessageDto.SiteId.Value);



                        var LastMessageRecipent = await messageServices.GetLatestMassages(FriendsRecipent, createMessageDto.RecipientUserId.Value);

                       


                        if (connections != null && connections.Count() > 0)
                        {
                          
                            await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                            new
                            {
                                username = sender.UserName,
                                fullname=sender.FullName,
                                content = message.Content,
                                senderId = sender.Id.ToString(),

                                userId = EncryptDecryptHelper.Encrypt(sender.Id.ToString())
                            });
                            await presenceHub.Clients.Clients(connections).SendAsync("ClientsLatestMessagesChat", LastMessageRecipent);
                        }
                    }
                }
            }
            // Person To Group
            else if (createMessageDto.RecipientCustomerGroupId.HasValue)
            {

                await messageServices.AddDateReadMessagesForGroup(groupRecipent.Users.ToList(), message.SiteId.Value, ObjMassage.Id, group.Connections);


                var connections = new List<string>();
                foreach (var user in groupRecipent.Users)
                {
                    var connectionPerUser = await PresenceTracker.GetConnectionsForUser(user.UserId);
                    if (connectionPerUser != null)
                    {
                        connections.AddRange(connectionPerUser);
                    }
                }

                if (connections != null && connections.Count() > 0)
                {
                    await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                        new
                        {
                            username = sender.UserName,
                            fullname=sender.FullName,
                            senderId = sender.Id.ToString(),
                            content = message.Content,
                            groupId = group.Id,
                            customergroupId = groupRecipent.Id,
                            groupType = groupRecipent.GroupTypeId,
                            groupTypeName = groupRecipent.GroupTypeId.HasValue? groupRecipent.GroupType.Name:null,
                            groupName = groupRecipent.customerGroupDetails.Select(x=>x.Name).FirstOrDefault(),
                            userId = EncryptDecryptHelper.Encrypt(sender.Id.ToString())
                        });
                    foreach (var user in groupRecipent.Users.Where(x=>x.UserId != UserId))
                    {
                        var Friends = await userFriendshipService.GetFriendsByUserId(user.UserId, createMessageDto.SiteId.Value);

                        var LastMassages = await messageServices.GetLatestMassages(Friends, user.UserId);

                        var connectionPerUser = await PresenceTracker.GetConnectionsForUser(user.UserId);

                        if (connectionPerUser != null)
                        {

                            await presenceHub.Clients.Clients(connectionPerUser).SendAsync("ClientsLatestMessagesChat", LastMassages);
                        }
                    }

                }
            }
            await Clients.Group(groupName).SendAsync("NewMessage", ObjMassage);

        }



        #region helpers
        private async Task<Group> RemoveFromMessageGroup()
        {
            try
            {
                var group = await messageServices.GetGroupForConnection(Context.ConnectionId);

                var connection = group.Connections
                    .FirstOrDefault(x => x.Id == Context.ConnectionId);



                connectionRepository.Remove(connection);

                await connectionRepository.SaveChangesAsync();

                return group;
            }
            catch (Exception ex)
            {

                throw new HubException(ex.Message);
            }
        }
        private string GetGroupNameP2P(string caller, string other)
        {
            try
            {
                var stringCompare = string.CompareOrdinal(caller, other) < 0;
                return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async Task<Group> AddToGroup(string groupName, User user, long? RecipentUserId = null)
        {
            try
            {
                var group = await messageServices.GetMessageGroup(groupName);
                var connection = new Entities.Entity.Chat.Connection(Context.ConnectionId, user.Id, DateTimeOffset.Now);

                if (group == null)
                {
                    group = new Group(groupName, DateTimeOffset.Now);
                    groupRepository.Add(group);

                }
                if (group.Connections != null && group.Connections.Count() > 0)
                {
                    var Connections = group.Connections.Where(x => x.UserId == user.Id).ToList();
                    if (Connections.Count() > 0)
                    {
                        connectionRepository.RemoveRange(Connections);
                        await connectionRepository.SaveChangesAsync();
                    }
                }
                connection.RecipentId = RecipentUserId;
                group.RecipentId = RecipentUserId;
                group.Connections.Add(connection);

                await groupRepository.SaveChangesAsync();

                return group;


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private long GetUserId()
        {
            var httpContext = Context.GetHttpContext();

            string accessToken = httpContext.Request.Query["access_token"].ToString();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            var claims = jwtToken.Claims.ToList();

            var nameIdentifierClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            return Convert.ToInt64(nameIdentifierClaim.Value);

        }
        #endregion
    }
}

