using System.Globalization;
using System.Security.Claims;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Controllers
{
    /// <summary>The admin and site an admin request was checked for (set by <see cref="ServerFlowAdminFilter"/>).</summary>
    public sealed record ServerFlowAdmin(long UserId, long SiteId);

    /// <summary>
    /// Lets a request through only for an admin of the site it names:
    ///  - the user id comes from the token's NameIdentifier claim (never from the userId header,
    ///    which anyone can send);
    ///  - the user must be an Admin in the database, neither deleted nor switched off, and linked to
    ///    the site in the siteId header, and not locked out there. Role claims are not trusted: they are
    ///    read fresh from the database.
    /// </summary>
    public sealed class ServerFlowAdminFilter : IAsyncActionFilter
    {
        public const string ItemKey = "ServerFlowAdmin";

        private readonly IRepository<User> _users;
        private readonly IRepository<UsersSites> _userSites;

        public ServerFlowAdminFilter(IRepository<User> users, IRepository<UsersSites> userSites)
        {
            _users = users;
            _userSites = userSites;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var claim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (http.User.Identity?.IsAuthenticated != true || !long.TryParse(claim, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!long.TryParse(http.Request.Headers["siteId"].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var siteId) || siteId <= 0)
            {
                context.Result = new BadRequestObjectResult("The siteId header is required.");
                return;
            }

            // A deleted or switched-off admin keeps a token until it runs out: the account itself is checked.
            var isAdmin = await _users.Query().AsNoTracking().AnyAsync(u => u.Id == userId && u.Type == EUsersTypes.Admin
                    && u.Status != EStatus.Deleted && u.Status != EStatus.Inactive)
                && await _userSites.Query().AsNoTracking().AnyAsync(s => s.UserId == userId && s.SiteId == siteId && !s.IsLockout);
            if (!isAdmin)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            http.Items[ItemKey] = new ServerFlowAdmin(userId, siteId);
            await next();
        }
    }
}
