using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IRepository<UsersSites> userSitesRepository;
        private readonly IWorkContext _workcontext;
        private readonly IRepository<User> _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        public GoogleAuthService(IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository,
            IRepository<UsersSites> userSitesRepository,
            ILoyaltyWalletService loyaltyWalletService, IWorkContext workcontext, IRepository<Site> siteRepository, IRepository<User> userRepository, UserManager<User> userManager, IUserService userService)
        {
            siteSettingsRepository = _siteSettingsRepository;
            this.userSitesRepository = userSitesRepository;
            this._workcontext = workcontext;
            _userRepository = userRepository;
            _userManager = userManager;
            _userService = userService;
            _loyaltyWalletService = loyaltyWalletService;
        }



        public async Task<LoginResponse> Login(GoogleLoginViewModel model)
        {
            try
            {
                var Setting = await GetSettings();
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { Setting.Where(x => x.Id == "ClientIdGoogle").FirstOrDefault().Value }
                };
                var siteId = _workcontext.GetCurrentSiteId();
                if (siteId == 0)
                    throw new Exception("SiteId cannot be null");

                var GoogleInfo = await GoogleJsonWebSignature.ValidateAsync(model.credential, settings);

                var user = await _userRepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups)
                     .ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions)
                     .Where(x => (x.NormalizedUserName == GoogleInfo.Email || x.NormalizedEmail == GoogleInfo.Email.ToUpper()) && x.Status != EStatus.Deleted)

                     .FirstOrDefaultAsync();



                if (user == null)
                {
                    var userInfo = new User
                    {
                        UserName = GoogleInfo.Email.Trim(),
                        Email = GoogleInfo.Email.Trim(),
                        FullName = GoogleInfo.Name,
                        CreatedById = 1,
                        CreationDate = DateTimeOffset.Now,
                        IsFacebookRegistered = false,
                        IsGoogleRegistered = true,
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

                    var regRes = await _userManager.CreateAsync(user, randomPassword);
                    if (regRes.Succeeded)
                    {
                        await _loyaltyWalletService.CreateLoyaltyWallet(user.Id);

                        var GenratedCode = Guid.NewGuid().ToString("N").Substring(0, 4);
                        string userCode = $"{user.Id}{GenratedCode}";
                        user.Code = userCode;

                        await _userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    if (!user.UserSites.Any(x => x.SiteId == siteId))
                    {
                        var userSite = new UsersSites();
                        userSite.SiteId = siteId;
                        userSite.UserId = user.Id;
                        userSite.CultureId = null;
                        userSite.IsDefault = true;
                        userSite.CurrencyId = null;
                        userSite.IsLockout = false;
                        userSitesRepository.Add(userSite);
                        await userSitesRepository.SaveChangesAsync();


                    }
                    user.IsGoogleRegistered = true;
                    user.IsFacebookRegistered = false;
                    user.IsAppleRegistered = false;
                    await _userManager.UpdateAsync(user);


                }
                return await _userService.Login(new Core.Areas.Core.ViewModels.Account.LoginViewModel()
                {
                    Username = user.UserName,
                    LoginType = LoginTypes.UsernameOrEmail,
                    FromGoogleOrFacebookOrApple = true

                });


            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);

                throw new Exception(e.Message);
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

        public async Task<List<SiteSettings>> GetSettings()
        {
            try
            {


                var SiteId = _workcontext.GetCurrentSiteId();
                var Setting = await siteSettingsRepository
                             .Query()
                             .Where(x => x.Module == "Google" && x.SiteId == SiteId)
                             .ToListAsync();
                return Setting;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}

