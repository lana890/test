
using EskaCMS.Entities.Entity;

using EskaCMS.Security.DynamicAuthorization.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.DynamicAuthorization.Core
{
    public class DynamicAuthorizationFilter<EskaCMSDBContext> : IAuthorizationFilter, IAsyncAuthorizationFilter
        where EskaCMSDBContext : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
    {
        private readonly DynamicAuthorizationOptions _authorizationOptions;
        private readonly EskaCMSDBContext _identityDbContext;
        private readonly IRoleAccessService _roleAccessService;

        public DynamicAuthorizationFilter(
            DynamicAuthorizationOptions authorizationOptions,
            EskaCMSDBContext identityDbContext,
       IRoleAccessService roleAccessService
        )
        {
            _authorizationOptions = authorizationOptions;
            _identityDbContext = identityDbContext;
            _roleAccessService = roleAccessService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            OnAuthorizationAsync(context).RunSynchronously();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!IsProtectedAction(context))
                return;

            if (!IsUserAuthenticated(context))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userName = context.HttpContext.User.Identity.Name;
            if (userName.Equals(_authorizationOptions.DefaultAdminUser, StringComparison.CurrentCultureIgnoreCase))
                return;

            var actionId = GetActionId(context);
            var roles = await (
                from user in _identityDbContext.Users
                join userRole in _identityDbContext.UserRoles on user.Id equals userRole.UserId
                join role in _identityDbContext.Roles on userRole.RoleId equals role.Id
                where user.UserName == userName
                select role.Id
            ).FirstOrDefaultAsync();

            if (await _roleAccessService.HasAccessToActionAsync(actionId, roles))
                return;

            context.Result = new ForbidResult();
        }
#if NETCORE3

        private static bool IsProtectedAction(AuthorizationFilterContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if(controllerActionDescriptor == null)
                return false;

            var controllerTypeInfo = controllerActionDescriptor.ControllerTypeInfo;

            var anonymousAttribute = controllerTypeInfo.GetCustomAttribute<AllowAnonymousAttribute>();
            if (anonymousAttribute != null)
                return false;

            var actionMethodInfo = controllerActionDescriptor.MethodInfo;
            anonymousAttribute = actionMethodInfo.GetCustomAttribute<AllowAnonymousAttribute>();
            if (anonymousAttribute != null)
                return false;

            var authorizeAttribute = controllerTypeInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            authorizeAttribute = actionMethodInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            return false;
        }

#else

        private static bool IsProtectedAction(AuthorizationFilterContext context)
        {
            if (context.Filters.Any(item => item is IAllowAnonymousFilter))
                return false;

            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
                return false;

            var controllerTypeInfo = controllerActionDescriptor.ControllerTypeInfo;
            var actionMethodInfo = controllerActionDescriptor.MethodInfo;

            var authorizeAttribute = controllerTypeInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            authorizeAttribute = actionMethodInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            return false;
        }

#endif

        private static bool IsUserAuthenticated(AuthorizationFilterContext context)
        {
            return context.HttpContext.User.Identity.IsAuthenticated;
        }

        private static string GetActionId(AuthorizationFilterContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var area = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue;
            var controller = controllerActionDescriptor.ControllerName;
            var action = controllerActionDescriptor.ActionName;

            return $"{area}:{controller}:{action}";
        }
    }
}