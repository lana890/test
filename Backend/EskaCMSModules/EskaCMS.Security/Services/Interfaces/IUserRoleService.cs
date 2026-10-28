using EskaCMS.Entities.Entity;

using EskaCMS.Security.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services.Interfaces
{
   public interface IUserRoleService
    {
          Task<IdentityResult> AssignRoleToUser(long UserId, long RoleId);

         Task<IList<UserRole>> GetRoleByUserId(long? UserId);
        Task<List<Role>> GetAllRolesByType(int roleType);
    }
}
