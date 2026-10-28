using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Extensions;

namespace EskaCMS.Security.Services
{
    public class UserRoleService : IUserRoleService
    {
        private readonly IRepository<User> _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IWorkContext _workContext;


        public UserRoleService(IRepository<User> userRepository,
                               UserManager<User> userManager,
                               IWorkContext workContext,
                               RoleManager<Role> roleManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _workContext = workContext;

        }

        public async Task<IdentityResult> AssignRoleToUser(long UserId, long RoleId)
        {
            var User = await _userManager.FindByIdAsync(UserId.ToString());
            var Role = await _roleManager.FindByIdAsync(RoleId.ToString());
            //_userRepository.Query().Where(x=>x.)
            return await _userManager.AddToRoleAsync(User, Role.Name);
        }

        public async Task<IList<UserRole>> GetRoleByUserId(long? UserId)
        {
            var User = await _userManager.Users.Where(x => x.Id == UserId).Include("UserRoles.Role").FirstOrDefaultAsync();
            return User?.UserRoles ?? new List<UserRole>();
            //var user = await _userManager.FindByIdAsync(UserId.ToString());
            //return await _userManager.GetRolesAsync(user);

        }

        public async Task<List<Role>> GetAllRolesByType(int roleType)
        {
            var siteId = _workContext.GetCurrentSiteId();


            var allRoles = await _roleManager.Roles.Where(x => ((int)x.Type) == roleType && (x.SiteId == siteId || x.SiteId == null)).Select(x =>
                new Role
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Type,
                }).ToListAsync();

            return allRoles;
        }


    }
}
