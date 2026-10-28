
using EskaCMS.Entities.Entity;
using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Menus.Services
{
    public class MenuRoleService : IMenuRole
    {

        private readonly IRepository<MenusRoles> _MenuRolesRepository;
        private readonly RoleManager<Role> _roleManager;



        public MenuRoleService(RoleManager<Role> roleManager, IRepository<MenusRoles> MenuRolesRepository)
        {
            _MenuRolesRepository = MenuRolesRepository;
            _roleManager = roleManager;

        }



        public async Task<MenuRolesViewModel> GetMenuAssignedRoles(long Id, long SiteId)
        {
            try
            {


                var data = new MenuRolesViewModel();

                data.assingedRoles = await _MenuRolesRepository.Query().Include(x => x.Role).Where(x => x.MenuId == Id).Select(x =>
                 new Role
                 {
                     Id = x.Role.Id,
                     Name = x.Role.Name,
                     Type = x.Role.Type,
                 }).ToListAsync();


                var unassingedRoles = await _roleManager.Roles.Where(x => x.SiteId == SiteId || x.SiteId == null).Select(x =>
                   new Role
                   {
                       Id = x.Id,
                       Name = x.Name,
                       Type = x.Type,
                   }).ToListAsync();


                var found = false;
                data.unassingedRoles = new List<Role>();
                foreach (var item in unassingedRoles)
                {
                    found = false;
                    foreach (var item2 in data.assingedRoles)
                    {
                        if (item.Id == item2.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        data.unassingedRoles.Add(item);
                    }

                }


                return data;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> SaveMenuAssginedRoles(MenuRolesViewModel vm)
        {
            try
            {
                var oldRoles = await _MenuRolesRepository.Query().Where(x => x.MenuId == vm.Id).Select(x => x).ToListAsync();
                foreach (var item in oldRoles)
                {
                    _MenuRolesRepository.Remove(item);
                }

                await _MenuRolesRepository.SaveChangesAsync();

                foreach (var item in vm.assingedRoles)
                {
                    MenusRoles newRole = new MenusRoles();
                    newRole.MenuId = vm.Id;
                    newRole.RoleId = item.Id;
                    newRole.CreatedById = vm.UserId;

                    _MenuRolesRepository.Add(newRole);
                }
                await _MenuRolesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> DeleteAsync(long Id)
        {
            try
            {
                var MenuRole = await _MenuRolesRepository.Query().Where(y => y.Id == Id).FirstOrDefaultAsync();
                _MenuRolesRepository.Remove(MenuRole);
                await _MenuRolesRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<MenusRoles>> GetMenusByRoleId(long RoleId) {
            try
            {
                var MenuRole = await _MenuRolesRepository.Query().Where(y => y.RoleId == RoleId).ToListAsync();
                return MenuRole;
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }


    }
}
