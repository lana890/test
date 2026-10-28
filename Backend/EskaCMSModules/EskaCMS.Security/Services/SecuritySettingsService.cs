using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;

namespace EskaCMS.Security.Services
{
    public class SecuritySettingsService : ISecuritySettingsService
    {

        private readonly IRepository<SecuritySettings> _securitysettingsRepository;

        public SecuritySettingsService(IRepository<SecuritySettings> securitysettingsRepository

            )
        {
            _securitysettingsRepository = securitysettingsRepository;


        }

        public async Task<SecuritySettingsVM> GetSecuritySetting(long SiteId)
        {
            var Result = _securitysettingsRepository.Query().Where(x => x.SiteId == SiteId).Select(x => new SecuritySettingsVM
            {
                AllowedForNewUsers = x.AllowedForNewUsers,
                AllowedUserNameCharacters = x.AllowedUserNameCharacters,
                DefaultLockoutTimeSpan = x.DefaultLockoutTimeSpan,
                MaxFailedAccessAttempts = x.MaxFailedAccessAttempts,
                RequireConfirmedAccount = x.RequireConfirmedAccount,
                RequireConfirmedEmail = x.RequireConfirmedEmail,
                RequireConfirmedPhoneNumber = x.RequireConfirmedPhoneNumber,
                RequireDigit = x.RequireDigit,
                RequiredLength = x.RequiredLength,
                RequiredUniqueChars = x.RequiredUniqueChars,
                RequireLowercase = x.RequireLowercase,
                RequireNonAlphanumeric = x.RequireNonAlphanumeric,
                RequireUniqueEmail = x.RequireUniqueEmail,
                RequireUppercase = x.RequireUppercase

            }).FirstOrDefault();
            return Result;
        }

        //public async Task<RoleOptionsViewModel> CreateAsync(AddRoleOptionsViewModel RoleOptions)
        //{
        //    using (var transaction = _rolesoptionsRepository.BeginTransaction())
        //    {
        //        try
        //        {
        //            var currentOptions = await _rolesoptionsRepository.Query().Where(x => x.RoleId == RoleOptions.RoleId).ToListAsync();
        //            foreach (var currentOption in currentOptions)
        //            {
        //                currentOption.OptionValue = 0;
        //                _rolesoptionsRepository.Update(currentOption);

        //            }
        //            foreach (var option in RoleOptions.ListOfOptions)
        //            {
        //                bool founded = false;

        //                foreach (var currentOption in currentOptions)
        //                {
        //                    if (currentOption.OptionId == option.OptionId)
        //                    {
        //                        currentOption.OptionValue = 1;
        //                        currentOption.ModifiedById = RoleOptions.UserId;
        //                        currentOption.ModificationDate = DateTimeOffset.Now;
        //                        founded = true;
        //                        _rolesoptionsRepository.Update(currentOption);

        //                        break;
        //                    }


        //                }
        //                if (!founded)
        //                {
        //                    var ObjOptions = new Options_in_Role();
        //                    ObjOptions.RoleId = RoleOptions.RoleId;
        //                    ObjOptions.OptionId = option.OptionId;
        //                    ObjOptions.OptionValue = 1;
        //                    ObjOptions.CreatedById = RoleOptions.UserId;
        //                    ObjOptions.CreationDate = DateTimeOffset.Now;
        //                    _rolesoptionsRepository.Add(ObjOptions);
        //                }


        //            }

        //            await _rolesoptionsRepository.SaveChangesAsync();

        //            transaction.Commit();
        //            return await GetAsync(RoleOptions.RoleId);
        //        }
        //        catch (Exception exc)
        //        {
        //            transaction.Rollback();
        //            throw exc;
        //        }
        //    }
        //}
    }
}
