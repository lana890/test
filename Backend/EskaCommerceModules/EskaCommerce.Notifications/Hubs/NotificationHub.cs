
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
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

namespace EskaCommerce.Notifications.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly NotificationTracker notificationTracker;
        private readonly INotificationRepository notificationRepository;
        private readonly ITestNotifier testNotifier;

        public NotificationHub(
                        IServiceScopeFactory serviceScopeFactory,
                        NotificationTracker notificationTracker,

            INotificationRepository notificationRepository,
            ITestNotifier testNotifier
            )
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.notificationTracker = notificationTracker;
            this.notificationRepository = notificationRepository;
            this.testNotifier = testNotifier;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var UserId = GetUserId();
            await notificationTracker.UserConnected(UserId, Context.ConnectionId);
            int? PageSize = Convert.ToInt32(httpContext.Request.Query["PageSize"])!=0? Convert.ToInt32(httpContext.Request.Query["PageSize"]) :null;

            int? PageNumber = Convert.ToInt32(httpContext.Request.Query["PageNumber"]) != 0 ? Convert.ToInt32(httpContext.Request.Query["PageNumber"]) : null;

            var Result = await notificationRepository.GetNotifications(PageSize??10, PageNumber??0, UserId, NotificationType.SignalR);

            await Clients.Caller.SendAsync("Notifications", Result);
        }
       
        public async Task UpdateNotification(NotificationUpdateDto notificationDto)
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                var UserId = GetUserId();

                await notificationRepository.UpdateNotification(notificationDto.NotificationSchemeId, UserId, notificationDto.MarkAllNotificationsAsRead);

                var Result = await notificationRepository.GetNotifications(notificationDto.PageSize, notificationDto.PageNumber , UserId, NotificationType.SignalR);

                await Clients.Caller.SendAsync("UpdateNotification", Result);

            }
            catch (Exception ex)
            {

                throw ex;
            }
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
