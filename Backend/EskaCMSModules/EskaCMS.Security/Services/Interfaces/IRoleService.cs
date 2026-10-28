using EskaCMS.Entities.Entity;
using EskaCMS.Security.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
   public interface IRoleService
    {
        Task<List<RoleListVM>> GetBySiteId(long SiteId, long UserId);
        Task<Role> GetById(long RoleId);
        Task<RolesSecurityVM> EditAsync(AddRoleOptionsViewModel roleModel);
        Task<IEnumerable<ControllerInfoVM>> GetControllersInfoAsync();
        Task<bool> CreateAsync(AddRoleOptionsViewModel model);
        Task<IdentityResult> DeleteAsync(long RoleId);
    }
}
