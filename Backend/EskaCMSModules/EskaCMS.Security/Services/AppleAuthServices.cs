using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.LoyaltyPoints.Services;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class AppleAuthServices : IAppleAuthServices
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IdcmsLogService dcmsLogService;
        private readonly ILoyaltyWalletService loyaltyWalletService;
        private readonly IHttpClientFactory httpClient;
        private readonly IWorkContext workcontext;
        private readonly IRepository<Site> siteRepository;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<DCMSLog> dcmslogrepository;
        private readonly UserManager<User> userManager;
        private readonly IUserService userService;

        public AppleAuthServices(IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository,
            IdcmsLogService dcmsLogService,
            ILoyaltyWalletService loyaltyWalletService,
            IHttpClientFactory httpClient, IWorkContext workcontext, 
            IRepository<Site> siteRepository,
            IRepository<User> userRepository,
            IRepository<DCMSLog>_dcmslogrepository,
            UserManager<User> userManager, IUserService userService)
        {
            siteSettingsRepository = _siteSettingsRepository;
            this.dcmsLogService = dcmsLogService;
            this.loyaltyWalletService = loyaltyWalletService;
            this.httpClient = httpClient;
            this.workcontext = workcontext;
            this.siteRepository = siteRepository;
            this.userRepository = userRepository;
            dcmslogrepository = _dcmslogrepository;
            this.userManager = userManager;
            this.userService = userService;
        }

        public async  Task<LoginResponse> Login(AppleLoginViewModel model)
        {
            try
            {
                var siteId = workcontext.GetCurrentSiteId();
                if (siteId == 0)
                    throw new Exception("SiteId cannot be null");

                var AppleSettings = await GetAppleSettings();
                var PlatoForm = workcontext.GetCurrentOS();

                string clientId = GetClientIdForPlatform(PlatoForm,AppleSettings);
                 
                var provider = new AppleAuth.AppleAuthProvider(clientId, AppleSettings.TeamId, AppleSettings.KeyId, AppleSettings.AppleUrl, "State1");
                     var authorizationToken = await provider.GetAuthorizationToken(model.AccessToken, AppleSettings.PrivateKey);


                var UpperEmail = authorizationToken.UserInformation.Email;
                var user = await userRepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups)
                    .ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions)
                    .Where(x => (x.NormalizedUserName == UpperEmail || x.NormalizedEmail == UpperEmail))
                    .FirstOrDefaultAsync();



                if (user == null)
                {
                    var userInfo = new User
                    {
                        UserName = authorizationToken.UserInformation.Email.Trim(),
                        Email = authorizationToken.UserInformation.Email.Trim(),
                        FullName = authorizationToken.UserInformation.Email.Trim(),
                        CreatedById = 1,
                        CreationDate = DateTimeOffset.Now,
                        IsFacebookRegistered = false,
                        IsGoogleRegistered = false,
                        IsAppleRegistered = true,
                        Status = EStatus.Active,
                        StatusDate = DateTimeOffset.Now,
                        Type = EUsersTypes.Customer,
                        UserSites = new List<UsersSites>() { new UsersSites() { SiteId = siteId, IsDefault = true } },
                        UserRoles = new List<UserRole>() { new UserRole() { RoleId = model.RoleId } },
                        CustomerGroups = new List<CustomerGroupUser>() { new CustomerGroupUser { CustomerGroupId = model.CustomerGroupId } },
                        TwoFactorEnabled = false


                    };
                    user = userInfo;
                    string randomPassword = GenerateRandomPassword(12);

                    var regRes = await userManager.CreateAsync(user, randomPassword);

                    if (regRes.Succeeded)
                    {
                        await loyaltyWalletService.CreateLoyaltyWallet(user.Id);

                        var GenratedCode = Guid.NewGuid().ToString("N").Substring(0, 4);
                        string userCode = $"{user.Id}{GenratedCode}";
                        user.Code = userCode;

                        await userManager.UpdateAsync(user);
                    }
                }
                else {

                    user.IsAppleRegistered = true;
                    user.IsGoogleRegistered = false;
                    user.IsFacebookRegistered = false;
                    await userManager.UpdateAsync(user);


                }
                return await userService.Login(new Core.Areas.Core.ViewModels.Account.LoginViewModel()
                {
                    Username = user.UserName,
                    LoginType = LoginTypes.UsernameOrEmail,
                    FromGoogleOrFacebookOrApple = true

                });
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        public async Task<AppleSettingsViewModel> GetAppleSettings()
        {
            try
            {


                var SiteId = workcontext.GetCurrentSiteId();
                var Setting = await siteSettingsRepository
                             .Query().AsNoTracking()
                             .Where(x => x.Module == "Apple" && x.SiteId == SiteId)
                             .ToListAsync();

                var AppleSettingsViewModel = new AppleSettingsViewModel
                {
                    AppleUrl = Setting.Where(x => x.Id == "AppleUrl").FirstOrDefault().Value,
                    ClientId = Setting.Where(x => x.Id == "ClientId").FirstOrDefault().Value,
                    BundleId = Setting.Where(x => x.Id == "BundleId").FirstOrDefault().Value,
                    KeyId = Setting.Where(x => x.Id == "KeyId").FirstOrDefault().Value,
                    PrivateKey = Setting.Where(x => x.Id == "PrivateKey").FirstOrDefault().Value,
                    TeamId = Setting.Where(x => x.Id == "TeamId").FirstOrDefault().Value
                };
                return AppleSettingsViewModel;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=[]{}|;:,.<>?";
            StringBuilder password = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(validChars.Length);
                password.Append(validChars[index]);
            }

            return password.ToString();
        }

        private string GetClientIdForPlatform(OS platform, AppleSettingsViewModel AppleSettings)
        {
            if (platform == OS.Web || platform == OS.Android)
            {
                return AppleSettings.ClientId;
            }
            else if (platform == OS.IOS)
            {
                return AppleSettings.BundleId; 
            }
            else
            {
                return AppleSettings.ClientId;
            }
        }
    }
}
