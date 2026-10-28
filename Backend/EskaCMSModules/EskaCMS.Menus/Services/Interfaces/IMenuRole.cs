
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Menus.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Menus;

namespace EskaCMS.Menus.Services.Interfaces
{
    public interface IMenuRole
    {
        Task<bool> SaveMenuAssginedRoles(MenuRolesViewModel vm);
        Task<MenuRolesViewModel> GetMenuAssignedRoles(long Id, long SiteId);
        Task<bool> DeleteAsync(long Id);
        Task<List<MenusRoles>> GetMenusByRoleId(long RoleId);

    }
}
