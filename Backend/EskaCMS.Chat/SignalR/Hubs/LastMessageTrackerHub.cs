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
    public class LastMessageTrackerHub: Hub
    {
        private readonly IUserFriendshipService userFriendshipService;
        private readonly IMessageServices messageServices;


        public LastMessageTrackerHub(
            IUserFriendshipService userFriendshipService,
            IMessageServices messageServices
            )
        {
            this.userFriendshipService = userFriendshipService;
            this.messageServices = messageServices;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            var UserId = GetUserId();

            long? SiteId = Convert.ToInt64(httpContext.Request.Query["SiteId"]);

            var Friends =await userFriendshipService.GetFriendsByUserId(UserId, SiteId.Value);


            var LastMassages = await messageServices.GetLatestMassages(Friends, UserId);


            await Clients.Caller.SendAsync("LatestMassages", LastMassages);

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
