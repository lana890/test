using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
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
    public class RoleOptionsService : IRoleOptions
    {
        private readonly IRepository<Options_in_Role> _rolesoptionsRepository;
        private readonly IRepository<RoleAccess> _rolesAccessRepository;
        private readonly IOptionService _optionService;
        //private readonly IRoleService _RoleService;
        private readonly IUserRoleService _UserRoleService;


        public RoleOptionsService(IRepository<Options_in_Role> rolesoptionsRepository,
                                  IRepository<RoleAccess> rolesAccessRepository,
                                  IOptionService optionService,
                                  IUserRoleService UserRoleService
            )
        {
            _rolesoptionsRepository = rolesoptionsRepository;
            _optionService = optionService;
            _rolesAccessRepository = rolesAccessRepository;
            _UserRoleService = UserRoleService;


        }
        public async Task<RoleOptionsViewModel> CreateAsync(AddRoleOptionsViewModel RoleOptions)
        {
            await _rolesoptionsRepository.ExecuteInTransactionAsync(async () =>
            {
                var currentOptions = await _rolesoptionsRepository.Query().Where(x => x.RoleId == RoleOptions.RoleId).ToListAsync();
                foreach (var currentOption in currentOptions)
                {
                    currentOption.OptionValue = 0;
                    _rolesoptionsRepository.Update(currentOption);
                }
                foreach (var option in RoleOptions.ListOfOptions)
                {
                    bool founded = false;
                    foreach (var currentOption in currentOptions)
                    {
                        if (currentOption.OptionId == option.OptionId)
                        {
                            currentOption.OptionValue = 1;
                            currentOption.ModifiedById = RoleOptions.UserId;
                            currentOption.ModificationDate = DateTimeOffset.Now;
                            founded = true;
                            _rolesoptionsRepository.Update(currentOption);
                            break;
                        }
                    }
                    if (!founded)
                    {
                        var ObjOptions = new Options_in_Role();
                        ObjOptions.RoleId = RoleOptions.RoleId;
                        ObjOptions.OptionId = option.OptionId;
                        ObjOptions.OptionValue = 1;
                        ObjOptions.CreatedById = RoleOptions.UserId;
                        ObjOptions.CreationDate = DateTimeOffset.Now;
                        _rolesoptionsRepository.Add(ObjOptions);
                    }
                }
                await _rolesoptionsRepository.SaveChangesAsync();
            });
            return await GetAsync(RoleOptions.RoleId);
        }

        public async Task<RoleOptionsViewModel> GetAsync(long RoleId)
        {
            try
            {

                // var Role = await _RoleService.GetById(RoleId);
                var RoleInfo = await _rolesoptionsRepository.Query()
                    .Where(x => x.RoleId == RoleId && x.OptionValue == 1)
                    .Select(x => new OptionsForRoles
                    {
                        OptionId = x.Option.Id,
                        CategoryId = x.Option.CategoryId,
                        OptionCode = x.Option.Code,
                        OptionValue = x.OptionValue

                    })
                    .ToListAsync();

                RoleOptionsViewModel lstRolesOptions = new RoleOptionsViewModel();
                lstRolesOptions.RoleId = RoleId;
                //foreach (var item in JoinResult)
                //{
                //    OptionsForRoles objOptions = new OptionsForRoles();
                //    objOptions.OptionId = item.Options.Id;
                //    objOptions.OptionValue = item.RoleOptions.OptionValue;
                //    objOptions.OptionCode = item.Options.Code;
                //    objOptions.CategoryId = item.Options.CategoryId;
                //    lstRolesOptions.lstOptions.Add(objOptions);
                //}
                lstRolesOptions.lstOptions = RoleInfo;
                return lstRolesOptions;
                // return null;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<RoleOptionsViewModel> GetByUserIdAsync(long UserId)
        {
            try
            {
                var RoleList = await _UserRoleService.GetRoleByUserId(UserId);
                var RoleIds = RoleList.Select(u => u.RoleId).ToList();
                bool IsSuperAdmin = RoleIds.Contains(1);
                if (IsSuperAdmin) 
                {
                    return null;
                }

                var RoleInfo = await _rolesoptionsRepository.Query()
                    .Where(x => RoleIds.Contains(x.RoleId) && x.OptionValue == 1)
                    .Select(x => new OptionsForRoles
                    {
                        OptionId = x.Option.Id,
                        CategoryId = x.Option.CategoryId,
                        OptionCode = x.Option.Code,
                        OptionValue = x.OptionValue

                    })
                    .ToListAsync();


                RoleOptionsViewModel lstRolesOptions = new RoleOptionsViewModel();
                lstRolesOptions.lstOptions = RoleInfo;
                return lstRolesOptions;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<Boolean> DeleteAsync(long RoleId)
        {
            try
            {
                List<Options_in_Role> currentOptions = await _rolesoptionsRepository.Query().Where(x => x.RoleId == RoleId).ToListAsync();
                List<RoleAccess> securityAccessRoles = await _rolesAccessRepository.Query().Where(x => x.RoleId == RoleId).ToListAsync();
                foreach (var currentOption in currentOptions)
                {
                     _rolesoptionsRepository.Remove(currentOption);
                    _rolesoptionsRepository.SaveChanges();
                }
                foreach (var currentRole in securityAccessRoles)
                {
                    _rolesAccessRepository.Remove(currentRole);
                    _rolesAccessRepository.SaveChanges();
                }
                return true;
             }
            catch (Exception exc)
            {
                throw exc;
            }
        }


    }
}
