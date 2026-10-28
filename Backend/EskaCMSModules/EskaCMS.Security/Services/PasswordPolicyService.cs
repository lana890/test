using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Services;
using EskaCMS.Sites.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
    public class PasswordPolicyService : IPasswordPolicyService
    {


        private readonly IRepository<RestrictedPasswords> _RestrictedPasswordsRepo;
        private readonly IRepository<UsersPasswordHistory> _UsersPasswordHistoryRepo;
        private readonly IRepository<SitesSecurityPolicy> _SitesSecurityPolicyRepo;
        private readonly IHashingHelper _hashingHelper;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UsersSites> _UsersSitesRepository;


        public PasswordPolicyService(
            IWorkContext workContext,
            IRepository<RestrictedPasswords> RestrictedPasswordsRepo,
            IRepository<UsersPasswordHistory> UsersPasswordHistoryRepo,
            IRepository<SitesSecurityPolicy> SitesSecurityPolicyRepo,
            IHashingHelper hashingHelper,
            IRepository<UsersSites> UsersSitesRepository

       )
        {
            _workContext = workContext;
            _RestrictedPasswordsRepo = RestrictedPasswordsRepo;
            _UsersPasswordHistoryRepo = UsersPasswordHistoryRepo;
            _SitesSecurityPolicyRepo = SitesSecurityPolicyRepo;
            _hashingHelper = hashingHelper;
            _UsersSitesRepository = UsersSitesRepository;
        }

        #region PasswordPolicy

        public async Task<long> UpsertPasswordPolicyAsync(PolicyPasswordVM Model)
        {
            try
            {
                var siteId = Model.SiteId != 0 && Model.SiteId.HasValue ? Model.SiteId : _workContext.GetCurrentSiteId();
                bool IsEditMode = Model.Id != 0 && Model.Id is not null;
                SitesSecurityPolicy Policy = new SitesSecurityPolicy();

                if (IsEditMode)
                {
                    Policy = await _SitesSecurityPolicyRepo
                                .Query()
                                .Where(s => s.Id == Model.Id && s.SiteId == siteId)
                                .FirstOrDefaultAsync();

                    if (Policy == null)
                    {
                        throw new Exception("Could Not Find The Policy");
                    }

                    Policy.ModificationDate = DateTimeOffset.Now;
                    Policy.ModifiedById = await _workContext.GetCurrentUserId();
                }
                else
                {
                    Policy.CreationDate = DateTimeOffset.Now;
                    Policy.CreatedById = await _workContext.GetCurrentUserId();

                    Policy.StatusDate = DateTimeOffset.Now;
                    Policy.SiteId = await _workContext.GetCurrentSiteIdAsync();
                }
                Policy.Status = EStatus.Published;
                Policy.MinUpperCaseLetters = Model.MinUpperCaseLetters;
                Policy.MinSpecialCharacters = Model.MinSpecialCharacters;
                Policy.MinDigits = Model.MinDigits;
                Policy.MinLength = Model.MinLength;
                Policy.UseUsernameSamePWD = Model.UseUsernameSamePWD;
                Policy.UseAlreadyUsedPassword = Model.UseAlreadyUsedPassword;
                Policy.NoRepeatedPassword = Model.NoRepeatedPassword;
                Policy.RegularExpression = Model.RegularExpression;

                if (!IsEditMode)
                {
                    _SitesSecurityPolicyRepo.Add(Policy);
                }

                await _SitesSecurityPolicyRepo.SaveChangesAsync();



                return Policy.SiteId;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<PolicyPasswordVM>> GetPasswordPolicies()
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();

            return await _SitesSecurityPolicyRepo
                                        .Query()
                                        .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted)
                                        .Select(x => new PolicyPasswordVM
                                        {
                                            MinLength = x.MinLength,
                                            MinLowerCaseLetters = x.MinLowerCaseLetters,
                                            MinSpecialCharacters = x.MinSpecialCharacters,
                                            UseAlreadyUsedPassword = x.UseAlreadyUsedPassword,
                                            UseUsernameSamePWD = x.UseUsernameSamePWD,
                                            NoRepeatedPassword = x.NoRepeatedPassword,
                                            RegularExpression = x.RegularExpression,
                                            MinDigits = x.MinDigits,
                                            MinUpperCaseLetters = x.MinUpperCaseLetters,
                                            Status = x.Status,
                                            StatusDate = x.StatusDate,

                                        }).AsNoTracking()
                                        .ToListAsync();
        }

        public async Task<bool> IsValidPasswordAsync(string Password, string UserName)
        {


            dynamic Violations = await GetPasswordViolationsAsync(Password, UserName);



            return Violations.IsValid;

        }

        public async Task<ExpandoObject> GetPasswordViolationsAsync(string Password, string UserName)
        {
            bool IsValid = true;
            dynamic Violations = new ExpandoObject();

            // 1 - Not In Restricted Passwords

            if (await IsRestrictedPassword(Password))
            {
                Violations.RestrictedPassword = true;
                IsValid = false;
            }




            // 2 - Password Policy Apply on it 

            if (!await IsApplicableForPasswordPolicy(Password, UserName, Violations))
            {
                IsValid = false;
            }

            Violations.IsValid = IsValid;
            return Violations;

        }

        public async Task<PolicyPasswordVM> GetBySiteId(long SiteId)
        {

            var Query = await _SitesSecurityPolicyRepo.Query().Where(x => x.SiteId == SiteId)
                .Select(x => new PolicyPasswordVM
                {
                    Id = x.Id,
                    MinLength = x.MinLength,
                    SiteId = x.SiteId,
                    MinLowerCaseLetters = x.MinLowerCaseLetters,
                    MinSpecialCharacters = x.MinSpecialCharacters,
                    UseAlreadyUsedPassword = x.UseAlreadyUsedPassword,
                    RegularExpression = x.RegularExpression,
                    UseUsernameSamePWD = x.UseUsernameSamePWD,
                    NoRepeatedPassword = x.NoRepeatedPassword,

                    MinDigits = x.MinDigits,
                    MinUpperCaseLetters = x.MinUpperCaseLetters,
                    Status = x.Status,
                    StatusDate = x.StatusDate,

                }).AsNoTracking()
            .FirstOrDefaultAsync();


            return Query;
        }

        public async Task<bool> UpdatePasswordPolicyStatusAsync(long Id, EStatus NewStatus)
        {

            var Policy = await _SitesSecurityPolicyRepo
                            .Query()
                            .Where(s => s.Id == Id)
                            .FirstOrDefaultAsync();

            if (Policy == null)
            {
                throw new Exception("Could Not Find The Policy");
            }

            Policy.Status = NewStatus;
            Policy.StatusDate = DateTimeOffset.Now;
            Policy.ModifiedById = await _workContext.GetCurrentUserId();
            Policy.ModificationDate = DateTimeOffset.Now;

            await _SitesSecurityPolicyRepo.SaveChangesAsync();
            return true;
        }

        #endregion




        #region Restricted Passwords

        public async Task<GetRetrictedResponseVm> GetRestrictedPasswords(SearchParamsRestrictedVm search)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();

                GetRetrictedResponseVm Query = new();

                var Data = await _RestrictedPasswordsRepo.Query()

                       .Where(r => (r.SiteId == SiteId

                       && r.Status != EStatus.Deleted) &&

                       (string.IsNullOrEmpty(search.SearchValue) || r.RestrictedText.ToLower().Contains(search.SearchValue.ToLower())))

                                    .Select(x => new RestrictedPasswordsVM
                                    {

                                        Id = x.Id,
                                        Status = x.Status,
                                        RestrictedText = x.RestrictedText
                                    })

                                    .ToListAsync();


                Query.TotalPasswordsRestricted = Data.Count;


                Query.Data = Data.Skip(search.PageNumber * search.PageSize)
                            .Take(search.PageSize)
                            .ToList();


                return Query;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<long> UpsertRestrictedPasswords(RestrictedPasswordsVM Model)
        {
            try
            {
                bool IsEditMode = Model.Id != 0;

                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserId = await _workContext.GetCurrentUserId();

                var RestrictedPassword = new RestrictedPasswords();
                if (IsEditMode)
                {
                    RestrictedPassword = await _RestrictedPasswordsRepo.Query()
                                                   .Where(r => r.Id == Model.Id)
                                                   .FirstOrDefaultAsync();
                    if (RestrictedPassword == null)
                    {
                        throw new Exception("Restricted Password Not Found");
                    }

                    RestrictedPassword.ModifiedById = UserId;
                    RestrictedPassword.ModificationDate = DateTimeOffset.Now;
                }
                else
                {
                    RestrictedPassword.CreatedById = UserId;
                    RestrictedPassword.CreationDate = DateTimeOffset.Now;
                    RestrictedPassword.SiteId = SiteId;
                    RestrictedPassword.Status = EStatus.Published;

                }

                RestrictedPassword.RestrictedText = Model.RestrictedText;

                if (!IsEditMode)
                {
                    _RestrictedPasswordsRepo.Add(RestrictedPassword);
                }

                await _RestrictedPasswordsRepo.SaveChangesAsync();
                return RestrictedPassword.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> UpdateRestrictedPasswordStatus(long Id, EStatus NewStatus)
        {
            try
            {
                var RestrictedPassword = await _RestrictedPasswordsRepo.Query()
                                              .Where(r => r.Id == Id)
                                              .FirstOrDefaultAsync();
                if (RestrictedPassword == null)
                {
                    throw new Exception("Restricted Password Not Found");
                }

                RestrictedPassword.Status = NewStatus;
                await _RestrictedPasswordsRepo.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        #endregion



        #region Helper Functions




        private async Task<bool> IsRestrictedPassword(string Password)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var RestrictedPassword = await _RestrictedPasswordsRepo.Query().AsNoTracking()
                                .Where(r => r.SiteId == SiteId &&
                                            r.Status == EStatus.Published &&
                                          r.RestrictedText.ToLower() == Password.ToLower())
                                .FirstOrDefaultAsync();

            return RestrictedPassword != null;

        }





        private async Task<bool> IsApplicableForPasswordPolicy(string Password, string UserName, dynamic Violations)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();

            var SecurityPolicy = await _SitesSecurityPolicyRepo
                                        .Query()
                                        .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published)
                                        .FirstOrDefaultAsync();
            if (SecurityPolicy == null)
            {
                return true;
            }
            return await IsValidPasswordForPolicy(Password, UserName, SecurityPolicy, Violations);



        }


        private async Task<bool> IsValidPasswordForPolicy(string Password, string UserName, SitesSecurityPolicy Policy, dynamic Violations)
        {
            bool result = true;
            if (Policy.UseAlreadyUsedPassword)
            {
                if (Policy.NoRepeatedPassword != 0 && await OldPasswordRepetition(Password, UserName) > Policy.NoRepeatedPassword)
                {
                    Violations.repeatedPassword = true;
                    result = false;
                }
            }
            else
            {
                if (await IsInOldPassword(Password, UserName))
                {
                    Violations.repeatedPassword = true;
                    result = false;
                }
            }

            if (!Policy.UseUsernameSamePWD)
            {
                if (UserName == Password)
                {
                    Violations.usernameSamePassword = true;
                    result = false;
                }

            }
            
            if (!string.IsNullOrEmpty(Policy.RegularExpression) )
            {
                string pattern = @$"{Policy.RegularExpression}";
                bool isValid = Regex.IsMatch(Password, pattern);

                if (!isValid)
                {
                    Violations.matchThePattern = true;
                    result = false;
                }

            }


            if (Policy.MinLength != 0 && Password.Length < Policy.MinLength)
            {
                Violations.minLength = Policy.MinLength;
                result = false;
            }




            if (!IValidCharachtersForPassword(Password, Policy, Violations))
            {
                result = false;
            };
            return result;
        }

        private async Task<long> OldPasswordRepetition(string Password, string username)
        {
            long UserSiteId = 0;
            long SiteId = _workContext.GetCurrentSiteId();
            if (string.IsNullOrEmpty(username))
            {
                UserSiteId = await _workContext.GetCurrentUserSiteId();
            }
            else
            {
                UserSiteId = await _UsersSitesRepository.Query().Include(x => x.User).Where(x => x.User.UserName.ToUpper()
                                                        .Equals(username) && x.SiteId == SiteId).Select(x => x.Id).FirstOrDefaultAsync();
            }

            var HashedPassword = _hashingHelper.GetHash(Password);
            var History = await _UsersPasswordHistoryRepo
                                    .Query()
                                    .Where(h => h.UsersSitesId == UserSiteId &&
                                           (h.OldPassword == HashedPassword || h.NewPassword == HashedPassword))
                                    .CountAsync();
            return History;
        }




        private async Task<bool> IsInOldPassword(string Password, string Username)
        {

            return await OldPasswordRepetition(Password, Username) > 0;

        }


        private bool IValidCharachtersForPassword(string Password, SitesSecurityPolicy Policy, dynamic Violations)
        {
            bool result = true;
            int SpecialCharactersCount = 0,
              LowerCaseLettersCount = 0,
              UpperCaseLettersCount = 0,
              DigitsCount = 0;


            foreach (char character in Password)
            {
                int Ascii = (int)character;
                if ((Ascii >= 32 && Ascii <= 47) ||
                    (Ascii >= 58 && Ascii <= 64) ||
                    (Ascii >= 91 && Ascii <= 96) ||
                    (Ascii >= 123 && Ascii <= 255)) // Special Characters 

                {
                    SpecialCharactersCount++;
                }
                else if (Ascii >= 48 && Ascii <= 57) // Numbers  

                {
                    DigitsCount++;
                }
                else if (Ascii >= 65 && Ascii <= 90) // Upper Case Letters 

                {
                    UpperCaseLettersCount++;
                }
                else if (Ascii >= 97 && Ascii <= 122) // Lower Case Letters Count 

                {
                    LowerCaseLettersCount++;
                }


            }



            if (Policy.MinDigits != 0 && DigitsCount < Policy.MinDigits)
            {
                Violations.minDigits = Policy.MinDigits;
                result = false;
            }

            if (Policy.MinSpecialCharacters != 0 && SpecialCharactersCount < Policy.MinSpecialCharacters)
            {
                Violations.minSpecialCharacters = Policy.MinSpecialCharacters;

                result = false;
            }

            if (Policy.MinLowerCaseLetters != 0 && LowerCaseLettersCount < Policy.MinLowerCaseLetters)
            {
                Violations.minLowerCaseLetters = Policy.MinLowerCaseLetters;

                result = false;
            }
            if (Policy.MinUpperCaseLetters != 0 && UpperCaseLettersCount < Policy.MinUpperCaseLetters)
            {
                Violations.minUpperCaseLetters = Policy.MinUpperCaseLetters;

                result = false;
            }

            return result;
        }


        #endregion


    }
}
