
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCMS.Core.Hubs
{
    public class AlertHub : Hub
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly AlertTracker notificationTracker;

        public AlertHub(
                        IServiceScopeFactory serviceScopeFactory,
                        AlertTracker notificationTracker)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.notificationTracker = notificationTracker;
        }


        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var UserId = GetUserId();
            await notificationTracker.UserConnected(UserId, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var userId = GetUserId();
            await notificationTracker.UserDisconnected(userId, Context.ConnectionId);
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
