using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using EskaCMS.Entities.Entity.Core;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace EskaCMS.Core.Security
{
    // Implements IAsyncAuthorizationFilter so identity lookups are awaited instead of
    // blocking the request thread via .Result (avoids thread-pool starvation under load).
    // Authorization behavior is unchanged.
    public class AuthorizeActionFilter : IAsyncAuthorizationFilter
    {
        //private readonly string _permission;
       // private readonly IRepository<Policies> _PoliciesRepository;
       // private readonly IRepository<RoleClaims> _RoleClaimsRepository;
       // private readonly IRepository<Claims> _ClaimsRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        public AuthorizeActionFilter(/*IRepository<Policies> PoliciesRepository, IRepository<RoleClaims> RoleClaimsRepository, IRepository<Claims> ClaimsRepository,*/ UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // _permission = permission;
          //  _PoliciesRepository = PoliciesRepository;
          //  _RoleClaimsRepository = RoleClaimsRepository;
           // _ClaimsRepository = ClaimsRepository;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // bool isAuthorized = CheckUserPermission(context.HttpContext.User);
            var Username = context.HttpContext.User.Identity.Name;
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var UserRole = await _userManager.GetRolesAsync(user);
                if (UserRole.Count > 0)
                {
                    var Role = await _roleManager.FindByNameAsync(UserRole[0]);
                    var actionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
                    var controller = actionDescriptor.ControllerName;
                    var action = actionDescriptor.ActionName;

                   //// var Policy = _PoliciesRepository.Query().Where(x => x.Name.ToUpper() == controller.ToUpper()).FirstOrDefault();
                   // if (Policy == null)
                   //     context.Result = new UnauthorizedResult();

                   // var Claim = _ClaimsRepository.Query().Where(x => x.PolicyId == Policy.Id).ToList();
                   // if (Claim.Count > 0)
                   // {
                   //     var ClaimAction = Claim.Find(x => x.ClaimType.ToUpper().Contains(action.ToUpper()));
                   //     if (ClaimAction == null)
                   //         context.Result = new UnauthorizedResult();

                   //     var Permission = _RoleClaimsRepository.Query().Where(x => x.ClaimId == ClaimAction.Id && x.RoleId == Role.Id).Count();
                   //     if (Permission <= 0)
                   //         context.Result = new UnauthorizedResult();
                   // }

                }
            }
            // context.Result = new UnauthorizedResult();

            //  var Claims= _ClaimsRepository.Query().Where()
            //if (action != rdaction || controller != rdcontroller)
            //{
            //    context.RouteData.Values["action"] = action;

            //    path = path.Replace(rdcontroller, controller).Replace(rdaction, action);
            //    context.Result = new RedirectResult(path, true);
            //}
            // if (!isAuthorized)
            //  {
            //    context.Result = new UnauthorizedResult();
            // }
        }


        private bool CheckUserPermission(ClaimsPrincipal user, string permission)
        {
            // Logic for checking the user permission goes here. 

            //  user.Claims();

            // Let's assume this user has only read permission.
            return permission == "Read";
        }
    }
}
