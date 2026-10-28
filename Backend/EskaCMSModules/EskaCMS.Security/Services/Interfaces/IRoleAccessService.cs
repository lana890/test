using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Security.Models;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IRoleAccessService
    {
        Task<bool> AddRoleAccessAsync(RolesSecurityVM roleAccess);

        Task<bool> EditRoleAccessAsync(RolesSecurityVM roleAccess);

        Task<bool> RemoveRoleAccessAsync(long roleId, RolesSecurityVM roleAccess);

        Task<RoleAccess> GetRoleAccessAsync(long roleId);

        Task<bool> HasAccessToActionAsync(string actionId, long role);
    }
}