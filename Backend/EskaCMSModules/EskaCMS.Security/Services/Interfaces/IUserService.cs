using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.BusinessModels.Manage;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Manage;
using EskaCMS.Pages.Models;
using EskaCMS.Security.Models;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserService
    {
        public Task<UserTokenResponse> GetUserContactInfo(string Username, long SiteId);
        public Task<long> InsureUser(RegisterViewModel Model);
        public Task AddUserToSite(string Username);
        public Task<RegisterationResultDTO> Register(RegisterViewModel model); 
        public Task<bool> LastUpdatePasswordEdit(long Id);
        public Task<LoginResponse> Login(LoginViewModel model);
        Task<bool> ValidateCredentials(LoginViewModel model);

        Task TwoFactorEnabled(User user,long SiteId=0);
        Task<bool> CheckExpiryPasswordForUser(User user);
        public Task<bool> EditProfile(EditProfileViewModel model, IFormFile file);
        public Task AddUpdateUserOptions(string RegistrationId, List<UserOptionsVM> userOptions);
        public Task AddUpdateUserOptions(List<UserOptionsVM> userOptions, long? userId=null);
        public Task CheckUserOptionValidation(List<CheckUserOptionValidationVM> validations);
        public Task LogOff();
        public Task<IdentityResult> AddRoleToUser(long RoleId);

        public Task<bool> ConfirmEmail(string code);
        public Task ForgotPasswordExternalLink(Core.Entities.ForgotPasswordViewModel model);

        public Task ChangePassword(ChangePasswordViewModel model);
        public Task ChangePasswordForAdmin(ChangePasswordForAdminViewModel model);
        Task UpdateLastLogin();

        Task<List<GetPasswordHistoryVM>> GetPasswordHistory(long UserId, ChangePasswordHistory? ChangePasswordType);

        public Task ResetPassword(Core.Entities.ResetPasswordViewModel model);

        public Task SendCode(SendCodeViewModel model);

        public Task VerifyCode(VerifyCodeViewModel model);

        public Task<EditProfileViewModel> GetProfile();
        Task<User> GetUserByUserName(long siteId , string normalizedUserName);
        public Task<UserTokenResponse> GetUserInfo(string Username);
        Task<List<UserOptionsVM>> GetUserOptionsAsync();

        Task<bool> MigrateUserToCustomer(string RegistrationId,string Name,string Phone);
        Task<bool> IsValidPointerId(string PublicKey);
        Task<string> TestDecrept(string CypherText);
        Task<CypherVM> HandShake(string PublicKey);
        Task<GetUserByUserOptionVm> GetUserByUserOption(string userOptionValue, string userOptionKey);
        Task<List<EUsersTypes>> GetUserTypes();
        Task<CustomerGroupVM> GetUsersByGroupId(long groupId, int pageIndex, int pageSize);

        Task DeleteAccount(DeleteAccountViewModel model);
        Task<bool> SetFavoritePage(long ParentId);
        Task<List<PasswordChangesHistoryVM>> GetPasswordChangesHistory(long UserId,long SiteId);
        //Task<List<string>> GetUserTokenByUserId(long UserId);
        Task<UserOTPVM> GetUserTokenByUserIdAndPurpose(string ProviderValue, string Purpose);

        Task<bool> ActivateUser(VerifyCodeViewModel model);

        Task<UserImageVM> GetUserImageById(long UserUd);

        Task<bool> UploadImage(UploadImageDto model);
        Task<LoginResponse> FingerLogin(FingerVM model);
        Task<bool> EnableFingerPrint(FingerPublicVM model);
        Task<bool> DisableFingerPrint();
        Task<List<string>> GetUserOptionKeys();
        Task<List<string>> GetRoleOptions();
        Task<Core.Models.TokenVM.TokenResponse> GetCrawlerToken(CrawlerViewModel model);
        Task SetUserCulturePreference();
        Task SetWarehousePreference(WarehousePreferenceViewModel model);
        Task<long> GetCurrentWarehouse();
    }
}
