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

namespace EskaCMS.Chat.SignalR.Hubs
{
    public class UserReadMassagsHub : Hub
    {
        private readonly IUserFriendshipService userFriendshipService;
        private readonly IMessageServices messageServices;

        public UserReadMassagsHub(IUserFriendshipService userFriendshipService, IMessageServices messageServices)
        {
            this.userFriendshipService = userFriendshipService;
            this.messageServices = messageServices;
        }

        public override async Task OnConnectedAsync()
        {

            var UserId = GetUserId();
            var OtherUser = new User();
            var group = new CustomerGroup();
            long SiteId=0;
            long? RecipentUserId = null;
            long? RecipentGroupId = null;
            var httpContext = Context.GetHttpContext();

            var CurrentUser = await messageServices.GetUser(UserId);
            if (httpContext.Request.Query["SiteId"].Any())
            {
                SiteId = Convert.ToInt64(httpContext.Request.Query["SiteId"]);
            }

            if (httpContext.Request.Query["RecipentUserId"].Any())
            {
                RecipentUserId = Convert.ToInt64(httpContext.Request.Query["RecipentUserId"]);

            }
            else if (httpContext.Request.Query["RecipentGroupId"].Any())
            {
                RecipentGroupId = Convert.ToInt64(httpContext.Request.Query["RecipentGroupId"]);


            }
            if (RecipentUserId.HasValue)
            {
                OtherUser = await messageServices.GetUser(RecipentUserId.Value);
                if (OtherUser == null)
                {
                    throw new HubException("Not found user");
                }
            }
            else
            {
                group = await messageServices.GetUserGroup(RecipentGroupId.Value);
                if (group == null)
                {
                    throw new HubException("Not found Group");
                }
            }

            //P2P
            if (RecipentUserId.HasValue)
            {

                await messageServices.EditUnReadMassagesForP2P(OtherUser.Id, CurrentUser.Id, SiteId);

                var connections = await PresenceTracker.GetConnectionsForUser(CurrentUser.Id);
                if (connections.Count > 0)
                {
                    var Friends = await userFriendshipService.GetFriendsByUserId(UserId, SiteId);


                    var LastMassages = await messageServices.GetLatestMassages(Friends, UserId);


                    await Clients.Caller.SendAsync("UpdateLatestMassagesChat", LastMassages);



                }


            }
            //Person To Group
            else if (RecipentGroupId.HasValue)
            {

                await messageServices.EditUnReadMassagesForGroup(RecipentGroupId.Value, CurrentUser.Id, SiteId);

                var connectionUser = await PresenceTracker.GetConnectionsForUser(CurrentUser.Id);

                var Friends = await userFriendshipService.GetFriendsByUserId(UserId, SiteId);


                var UpdateLatestMassages = await messageServices.GetLatestMassages(Friends, UserId);

                if (connectionUser != null)
                {
                    await Clients.Caller.SendAsync("UpdateLatestMassagesChat", UpdateLatestMassages);

                }
            }

        }
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await base.OnDisconnectedAsync(ex);
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
    }
}
