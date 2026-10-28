using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class FacebookAuthService : IFacebookAuthService
    {
        // move to appsettings

        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IHttpClientFactory _httpClient;
        private readonly IWorkContext _workcontext;
        private readonly IRepository<Site> _siteRepository;
        private readonly IRepository<User> _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        public FacebookAuthService(IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository, ILoyaltyWalletService loyaltyWalletService, IHttpClientFactory httpClient, IWorkContext workcontext, IRepository<Site> siteRepository, IRepository<User> userRepository, UserManager<User> userManager, IUserService userService)
        {
            siteSettingsRepository = _siteSettingsRepository;
            this._httpClient = httpClient;
            this._workcontext = workcontext;
            _siteRepository = siteRepository;
            _userRepository = userRepository;
            _userManager = userManager;
            _userService = userService;
            _loyaltyWalletService = loyaltyWalletService;
        }



        public async Task<LoginResponse> Login(FacebookLoginViewModel model)
        {
            try
            {
                var siteId = _workcontext.GetCurrentSiteId();
                var os = _workcontext.GetCurrentOS();
                if (siteId == 0)
                    throw new Exception("SiteId cannot be null");
                var Setting = await GetSettings();
                var UserId = "";
                var FaceBookInfoUserResult = new FaceBookInfoUserResult();
                if (os != OS.IOS)
                {
                    var validatedAccessToken = await ValidateAccessTokenAsync(model.AccessToken,
                                                              Setting.Where(x => x.Id == "TokenValidationUrlFacebook").FirstOrDefault().Value,
                                                              Setting.Where(x => x.Id == "AppIdFacebook").FirstOrDefault().Value,
                                                              Setting.Where(x => x.Id == "AppSecretFacebook").FirstOrDefault().Value

                                                              );

                    if (!validatedAccessToken.FacebookValidationData.IsValid)
                        throw new Exception("Token Not Valid");


                    FaceBookInfoUserResult = await GetUserInfoAsync(model.AccessToken,
                                                         validatedAccessToken.FacebookValidationData.UserId,
                                                         Setting.Where(x => x.Id == "UserInfoUrlFacebook").FirstOrDefault().Value
                                                         );

                }
                else
                {
                    FaceBookInfoUserResult = await ValidateFacebookTokenIOSPlatform(model.AccessToken, Setting.Where(x => x.Id == "AppIdFacebook").FirstOrDefault().Value, Setting.Where(x => x.Id == "TokenValidationIosPlatform").FirstOrDefault().Value);

                }



                if (FaceBookInfoUserResult == null)
                    throw new Exception("Wrong_Facebook_Email");

                var user = await _userRepository.Query()
                                                .Include(x => x.UserSites)
                                                .ThenInclude(x => x.Site)
                                                .Include(x => x.CustomerGroups)
                                                .ThenInclude(x => x.CustomerGroup)
                                                .Include(x => x.UserRoles)
                                                .ThenInclude(x => x.Role).
                                                Include(x => x.UserOptions)
                                                 .Where(x => (x.NormalizedUserName == FaceBookInfoUserResult.Email
                                                 || x.NormalizedEmail == FaceBookInfoUserResult.Email)
                                                 && x.UserSites.Any(a => a.SiteId == siteId))
                                                 .FirstOrDefaultAsync();


                if (user == null)
                {
                    var userInfo = new User
                    {
                        UserName = FaceBookInfoUserResult.Email.Trim(),
                        Email = FaceBookInfoUserResult.Email.Trim(),
                        FullName = FaceBookInfoUserResult.Name,
                        CreatedById = 1,
                        CreationDate = DateTimeOffset.Now,
                        IsFacebookRegistered = true,
                        IsGoogleRegistered = false,
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

                    user.IsFacebookRegistered = true;
                    user.IsAppleRegistered = false;
                    user.IsGoogleRegistered = false;
                    await _userManager.UpdateAsync(user);


                }
                return await _userService.Login(new Core.Areas.Core.ViewModels.Account.LoginViewModel()
                {
                    Username = user.UserName,
                    LoginType = LoginTypes.UsernameOrEmail,
                    FromGoogleOrFacebookOrApple = true,
                });


            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);

                throw new Exception(e.Message);
            }
        }


        private async Task<FacebookTokenValidationResult> ValidateAccessTokenAsync(string accessToken, string TokenValidationUrl, string AppId, string AppSecret)
        {
            var formatedUrl = string.Format(TokenValidationUrl, accessToken, AppId, AppSecret);
            return JsonConvert.DeserializeObject<FacebookTokenValidationResult>(await HttpGetRequest(formatedUrl));
        }
        private async Task<FaceBookInfoUserResult> GetUserInfoAsync(string accessToken, string UserId, string userInfoUrl)
        {
            var formatedUrl = string.Format(userInfoUrl, UserId, accessToken);
            return JsonConvert.DeserializeObject<FaceBookInfoUserResult>(await HttpGetRequest(formatedUrl));
        }

        private async Task<string> HttpGetRequest(string url)
        {
            try
            {
                var result = await _httpClient.CreateClient().GetAsync(url);
                return await result.Content.ReadAsStringAsync();
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

        public async Task<List<SiteSettings>> GetSettings()
        {
            try
            {


                var SiteId = _workcontext.GetCurrentSiteId();
                var Setting = await siteSettingsRepository
                             .Query()
                             .Where(x => x.Module == "Facebook" && x.SiteId == SiteId)
                             .ToListAsync();
                return Setting;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// IOS
        /// </summary>
        /// <param name="token"></param>
        /// <param name="appId"></param>
        /// <returns></returns>

        public async Task<FaceBookInfoUserResult> ValidateFacebookTokenIOSPlatform(string token, string appId, string BaseUrl)
        {
            try
            {
                return await GetLimitedFacebookUser(token, appId, BaseUrl);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        private async Task<FaceBookInfoUserResult> GetLimitedFacebookUser(string token, string appId, string BaseUrl)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwksUri = BaseUrl;
            var jwksResponse = await _httpClient.CreateClient().GetStringAsync(jwksUri);
            var signingKeys = new JsonWebKeySet(jwksResponse).GetSigningKeys();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://www.facebook.com",
                ValidateAudience = true,
                ValidAudience = appId,
                ValidateLifetime = true,
                IssuerSigningKeys = signingKeys
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            if (jwtToken == null || !jwtToken.Payload.ContainsKey("sub") || !jwtToken.Payload.ContainsKey("name"))
            {
                throw new Exception("Invalid token (missing sub or name)");
            }

            var result = new FaceBookInfoUserResult
            {
                id = jwtToken.Payload["sub"].ToString(),
                Name = jwtToken.Payload["name"].ToString(),
                Email = jwtToken.Payload["email"]?.ToString()
            };

            return result;

        }

    }



}
