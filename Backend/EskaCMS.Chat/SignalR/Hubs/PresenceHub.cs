using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace skaCMS.Chat.SignalR.Hubs;

//[Authorize]
public class PresenceHub : Hub
{
    private readonly PresenceTracker _tracker;
    private readonly IWorkContext workContext;
    private readonly IUserSiteService userSiteService;

    public PresenceHub(PresenceTracker tracker,IWorkContext workContext, IUserSiteService userSiteService)
    {
        _tracker = tracker;
        this.workContext = workContext;
        this.userSiteService = userSiteService;
    }

    public override async Task OnConnectedAsync()
    {
        var ConnectionIds=new List<string>();
        var httpContext = Context.GetHttpContext();
        var UserId = GetUserId();
        long SiteId = 0;

        if (httpContext.Request.Query["SiteId"].Any())
        {
            SiteId = Convert.ToInt64(httpContext.Request.Query["SiteId"]);
        }
        if (httpContext.Request.Query["ConnectionIds"].Any())
        {
            ConnectionIds = httpContext.Request.Query["ConnectionIds"].ToString().Split(",").ToList();
            foreach (var connectionId in ConnectionIds)
            {
                await _tracker.UserDisconnected(UserId, connectionId);
            }
        }
        var userId = GetUserId();
        var isOnline = await _tracker.UserConnected(userId, Context.ConnectionId);
        if (isOnline)
            await Clients.Others.SendAsync("UserIsOnline", userId);

        var currentUsers = await _tracker.GetOnlineUsers();
        await userSiteService.UpdateUserAvailablity(userId, SiteId, EskaCMS.Entities.Enum.ConnectionStatus.Available);
        await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
        await Clients.Caller.SendAsync("GetCurrentConnection",new { ConnectionId = Context.ConnectionId });
    }

    public override async Task OnDisconnectedAsync(Exception ex)
    {
        var userId = GetUserId();
        long SiteId = 0;
        var httpContext = Context.GetHttpContext();

        if (httpContext.Request.Query["SiteId"].Any())
        {
            SiteId = Convert.ToInt64(httpContext.Request.Query["SiteId"]);
        }
        var isOffline = await _tracker.UserDisconnected(userId, Context.ConnectionId);
        await userSiteService.UpdateUserAvailablity(userId, SiteId, EskaCMS.Entities.Enum.ConnectionStatus.UnAvailable);

        if (isOffline)
            await Clients.Others.SendAsync("UserIsOffline", userId);

        await base.OnDisconnectedAsync(ex);
    }

    #region helpers

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
