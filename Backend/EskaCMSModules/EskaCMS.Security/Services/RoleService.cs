using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.DynamicAuthorization.Core;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
    public class RoleService : IRoleService
    {

        private readonly RoleManager<Role> _roleManager;
        private readonly IControllerDiscovery _ControllerDiscovery;
        private readonly IRoleOptions _IRoleOptionService;
        private readonly IRoleAccessService _roleAccessService;
        private readonly IUserRoleService _UserRoleService;

        public RoleService(RoleManager<Role> roleManager, IRoleOptions IRoleOptionService,
                            IControllerDiscovery ControllerDiscovery,
                            IRoleAccessService roleAccessService,
                             IUserRoleService UserRoleService)
        {
            _roleManager = roleManager;
            _IRoleOptionService = IRoleOptionService;
            _ControllerDiscovery = ControllerDiscovery;
            _roleAccessService = roleAccessService;
            _UserRoleService = UserRoleService;
        }


        // getting Roles CUSTOMER and GUEST and Same Role that User Has
        public async Task<List<RoleListVM>> GetBySiteId(long SiteId, long UserId)
        {
            try
            {
                var UserRoles = await _UserRoleService.GetRoleByUserId(UserId);
                bool IsSystemAdmin = UserRoles.Any(ur => ur.Role.IsSystemAdmin );
                var RolesIds = UserRoles.Select(u => u.RoleId).ToList();
                bool IsSuperAdmin = RolesIds.Contains(1);

                var result = await _roleManager.Roles
                            .Where(x => (x.SiteId == SiteId || x.SiteId == null)
                                    && (
                                        x.NormalizedName.ToLower().Contains("CUSTOMER".ToLower())
                                        || x.NormalizedName.ToLower().Contains("GUEST".ToLower())
                                        || IsSystemAdmin
                                 //       || x.IsSystemAdmin == IsSystemAdmin
                                        || RolesIds.Contains(x.Id)
                                        || IsSuperAdmin
                                        ))
                            .Select(x =>
                                new RoleListVM
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    Type = x.Type,
                                    UsersCount = x.Users.Count(ur => ur.User.Status != EStatus.Deleted
                                                                  && ur.User.UserSites.Any(us => us.SiteId == SiteId)),
                                    PermissionsCount = x.RoleOptions.Count(o => o.OptionValue == 1),
                                })
                            .OrderBy(x => x.Id)
                            .ToListAsync();
                return result;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<Role> GetById(long RoleId)
        {
            try
            {
                return await _roleManager.Roles.Where(x => x.Id == RoleId).FirstOrDefaultAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<RolesSecurityVM> EditAsync(AddRoleOptionsViewModel roleModel)
        {
            try
            {

                //var oldRole = await GetById(roleModel.RoleId);
                //oldRole.Name = roleModel.RoleName;
                //oldRole.NormalizedName = roleModel.RoleName.ToUpper();
                //await _roleManager.UpdateAsync(oldRole);

                // Check role exit
                var role = await _roleManager.FindByIdAsync(roleModel.RoleId.ToString());
                if (role == null)
                {
                    return null;
                }

                // Update role if role's name is changed
                if (role.Name != roleModel.RoleName)
                {
                           role.Name = roleModel.RoleName;
                    role.Type = roleModel.RoleType;
                    var result = await _roleManager.UpdateAsync(role);
                    if (!result.Succeeded)
                    {
                        return null;
                    }
                }

                if (role.Type != roleModel.RoleType)
                {
                    role.Type = roleModel.RoleType;
                   
                    var result = await _roleManager.UpdateAsync(role);
                    if (!result.Succeeded)
                    {
                        return null;
                    }
                }

                if (roleModel.RoleId != 0)
                    await _IRoleOptionService.CreateAsync(roleModel);
                else
                    throw new Exception("Error in adding role");


                // Update role access list
                if (roleModel.SelectedControllers != null && roleModel.SelectedControllers.Any())
                {
                    foreach (var controller in roleModel.SelectedControllers)
                        foreach (var action in controller.Actions)
                        {
                            action.ControllerId = controller.Id;

                        }

                }

                var roleAccess = new RolesSecurityVM
                {
                    Controllers = roleModel.SelectedControllers.Select(x => new ControllerInfoVM
                    {
                        Actions = x.Actions,
                        AreaName = x.AreaName,
                        DisplayName = x.DisplayName,
                        Name = x.Name,
                        RoleAccessId = x.RoleAccessId,
                        Id = x.Id
                    }).ToList(),
                    RoleId = role.Id
                };
                await _roleAccessService.EditRoleAccessAsync(roleAccess);

                return roleAccess;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<IEnumerable<ControllerInfoVM>> GetControllersInfoAsync()
        {
            return await _ControllerDiscovery.GetControllers();

        }
        public async Task<bool> CreateAsync(AddRoleOptionsViewModel roleModel)
        {
            try
            {
                var newRole = new Role();
                newRole.Name = roleModel.RoleName;
                newRole.NormalizedName = roleModel.RoleName.ToUpper();
                newRole.Type = roleModel.RoleType;
                newRole.SiteId = roleModel.SiteId;
                newRole.ConcurrencyStamp = Guid.NewGuid().ToString();
                var Role = await _roleManager.CreateAsync(newRole);
                var RoleInfo = await _roleManager.GetRoleIdAsync(newRole);

                roleModel.RoleId = Convert.ToInt64(RoleInfo);

                if (roleModel.SelectedControllers != null && roleModel.SelectedControllers.Any())
                {
                    foreach (var controller in roleModel.SelectedControllers)
                        foreach (var action in controller.Actions)
                            action.ControllerId = controller.Id;

                    var roleAccess = new RolesSecurityVM
                    {
                        Controllers = roleModel.SelectedControllers.Select(x => new ControllerInfoVM
                        {
                            AreaName = x.AreaName,
                            DisplayName = x.DisplayName,
                            Name = x.Name,
                            RoleAccessId = x.RoleAccessId
                        }).ToList(),
                        RoleId = roleModel.RoleId
                    };
                    await _roleAccessService.AddRoleAccessAsync(roleAccess);
                }

                if (roleModel.RoleId != 0)
                    await _IRoleOptionService.CreateAsync(roleModel);
                else
                    throw new Exception("Error in adding role");



                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<IdentityResult> DeleteAsync(long RoleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(RoleId.ToString());
                await _IRoleOptionService.DeleteAsync(RoleId);
                return await _roleManager.DeleteAsync(role);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
    }
}
