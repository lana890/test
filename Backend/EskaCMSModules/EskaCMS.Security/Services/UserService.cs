using EskaCMS.Core;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.BusinessModels.Manage;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Security;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Services.Model;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using FirebaseAdmin.Messaging;
using Hangfire.MemoryStorage.Database;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Core.Models.TokenVM;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class UserService : IUserService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CrawlerTokenLocks = new();


        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        public readonly IRepositoryWithTypedId<CurrentLoggedInUsers, string> _CurrentLoggedInUsers;
        private readonly SignInManager<User> _signInManager;
        private readonly IRepository<User> _userrepository;
        private readonly IdcmsLogService dcmsLogService;
        public const string ProdKey = "ASD";

        public string AdBaseUrl = "";
        public string adpassword = "";
        public string adusername = "";
        private readonly IRepository<Site> _siteRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRepositoryWithTypedId<CypherParam, string> _cypherParamRepo;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private readonly IWorkContext _workContext;
        private readonly ITokenService _tokenService;
        private readonly ILoggedInUserService _loggedInUserService;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.UserOptions> _UserOptionsRepository;
        private readonly IMediator _mediator;
        private readonly ITestNotifier _testNotifier;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IRSA_Helper _RSA_Helper;
        private readonly IConfiguration configuration;
        private readonly long EXPIRY_LIMIT_IN_DAYS = 10;
        private readonly IRepository<UsersPasswordHistory> _UsersPasswordHistoryRepo;
        private readonly IUsersPasswordHistoryService _usersPasswordHistory;
        private readonly IUserLoginTransactionsService _userLoginTransactionsService;
        private readonly ICacheService cacheService;
        private readonly IRepository<CustomerGroup> _customerGrouprepository;
        private readonly IRepository<UserPreferance> _userPreferanceRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _sitesettings;
        public readonly IPasswordPolicyService _PasswordPolicy;
        public readonly ILoyaltyWalletService _loyaltyWalletService;
        public readonly IAES_Helper _IAES_Helper;

        private readonly IUserOTPService _UserOTPService;
        public UserService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<UserService> logger,
            RoleManager<Role> roleManager,
            IRepository<UsersSites> UsersSitesRepository,
            IWorkContext workContext,
            IRepository<CustomerGroup> customerGrouprepository,
              IRepository<UserPreferance> userPreferanceRepository,
            ITokenService tokenService,
            ILoggedInUserService loggedInUserService,
            IRepository<EskaCMS.Entities.Entity.Core.UserOptions> UserOptionsRepository,
            IRepositoryWithTypedId<CypherParam, string> cypherParamRepo,
            IRepository<User> userrepository,
            IServiceProvider serviceProvider,
            IdcmsLogService dcmsLogService,
            IRepository<Site> siteRepository,
            IServiceScopeFactory serviceScopeFactory,
            IRepositoryWithTypedId<SiteSettings, string> sitesettings,
            IRepositoryWithTypedId<CurrentLoggedInUsers, string> CurrentLoggedInUsers,
            IRepository<UsersPasswordHistory> UsersPasswordHistoryRepo,
            IMediator mediator,
            IPasswordPolicyService PasswordPolicy,
            IUsersPasswordHistoryService usersPasswordHistory,
            IUserLoginTransactionsService userLoginTransactionsService,
                       ICacheService cacheService,

           IEmailTemplateService emailTemplateService,
           ITestNotifier testNorifier,
           IRSA_Helper RSA_Helper,
           IConfiguration configuration,
           IUserOTPService UserOTPService,
            ILoyaltyWalletService loyaltyWalletService
,
            IAES_Helper iAES_Helper

 )
        {
            _customerGrouprepository = customerGrouprepository;
            _userPreferanceRepository = userPreferanceRepository;
            _CurrentLoggedInUsers = CurrentLoggedInUsers;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _workContext = workContext;
            _UsersSitesRepository = UsersSitesRepository;
            _UserOptionsRepository = UserOptionsRepository;
            _userrepository = userrepository;
            this.dcmsLogService = dcmsLogService;
            _tokenService = tokenService;
            _loggedInUserService = loggedInUserService;
            _mediator = mediator;
            _emailTemplateService = emailTemplateService;
            _testNotifier = testNorifier;
            _cypherParamRepo = cypherParamRepo;
            _RSA_Helper = RSA_Helper;
            this.configuration = configuration;
            _PasswordPolicy = PasswordPolicy;
            _usersPasswordHistory = usersPasswordHistory;
            _userLoginTransactionsService = userLoginTransactionsService;
            this.cacheService=cacheService;
            _UsersPasswordHistoryRepo = UsersPasswordHistoryRepo;
            _siteRepository = siteRepository;
            _serviceScopeFactory = serviceScopeFactory;
            _UserOTPService = UserOTPService;
            _loyaltyWalletService = loyaltyWalletService;
            _sitesettings = sitesettings;
            _IAES_Helper = iAES_Helper;
            AdBaseUrl = configuration.GetSection("ActiveDirectory:ActiveDirectoryUrl")?.Value;
            adpassword = configuration.GetSection("ActiveDirectory:AdPassword")?.Value;
            adusername = configuration.GetSection("ActiveDirectory:AdUserName")?.Value;
        }

        public async Task<UserImageVM> GetUserImageById(long UserId)
        {
            try
            {

                var UserImageDetails = await _userrepository.Query()
                    .Include(x => x.Thumbnail)
                    .Where(x => x.Id == UserId)

                    .Select(x => new UserImageVM
                    {
                        ImageId = x.ThumbnailId.Value,
                        PublicUrl = x.Image
                    }).FirstOrDefaultAsync();


                return UserImageDetails;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<RegisterationResultDTO> Register(RegisterViewModel model)
        {
            try
            {
                long SiteId = 0;
                if (!model.SiteId.HasValue)
                {
                    SiteId = _workContext.GetCurrentSiteId();
                    if (SiteId == 0)
                        throw new Exception("SiteId cannot be null");
                }
                else
                    SiteId = model.SiteId.Value;
                //The SiteId was added to the model in order to let other entities that uses this function to create the user into the reqiured site

                var CheckRole = await _roleManager.FindByIdAsync(model.RoleId.ToString());
                if (CheckRole == null)
                    throw new Exception("Invalid role Id");

                var user = new User
                {
                    UserName = string.IsNullOrEmpty(model.UserName) ? model.Email.Trim() : model.UserName.Trim(),
                    Email = model.Email,
                    FullName = model.FullName.Trim(),
                    CreatedById = 1,
                    CreationDate = DateTimeOffset.Now,
                    PhoneNumber = model.PhoneNumber ?? null,
                    Status = model.Status,// EStatus.Active,
                    StatusDate = DateTimeOffset.Now,
                    Type = EUsersTypes.Customer,


                };

                if (model.DateOfBirth != null)
                {
                    user.DateOfBirth = new DateTimeOffset(model.DateOfBirth.Value.DateTime, TimeSpan.Zero);
                }

                var UserToSite = new UsersSites { User = user, SiteId = SiteId, IsDefault = true };

                user.UserSites.Add(UserToSite);
                if (model.GroupId.HasValue)
                {
                    var CustomerGroup = await _customerGrouprepository.Query().Where(x => x.Id == model.GroupId.GetValueOrDefault()).FirstOrDefaultAsync();
                    var userCustomerGroup = new CustomerGroupUser
                    {
                        CustomerGroupId = model.GroupId.GetValueOrDefault(),
                        User = user,
                        CreationDate = DateTimeOffset.Now
                    };

                    user.CustomerGroups.Add(userCustomerGroup);

                    user.TwoFactorEnabled = CustomerGroup.EnableTwoFactorAuthentication ? true : false;

                }
                var Role = new UserRole { User = user, Role = CheckRole };
                user.UserRoles.Add(Role);

                if (model.UserOptions != null)
                {
                    foreach (var item in model.UserOptions)
                    {
                        EskaCMS.Entities.Entity.Core.UserOptions objUserOpt = new EskaCMS.Entities.Entity.Core.UserOptions();

                        objUserOpt.PropertyName = item.PropertyName;
                        objUserOpt.Value = item.Value;
                        objUserOpt.IsEditable = item.IsEditable;
                        objUserOpt.IsVisible = item.IsVisible;
                        objUserOpt.ControlType = item.ControlType;
                        objUserOpt.UserId = user.Id;
                        user.UserOptions.Add(objUserOpt);
                    }

                }
                var IsValidPassword = await _PasswordPolicy.IsValidPasswordAsync(model.Password, user.UserName);


                if (IsValidPassword == false)
                {
                    throw new Exception("InValid Password !");
                }
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {

                    await _loyaltyWalletService.CreateLoyaltyWallet(user.Id);

                    var GenratedCode = Guid.NewGuid().ToString("N").Substring(0, 4);
                    string userCode = $"{user.Id}{GenratedCode}";
                    user.Code = userCode;

                    await _userManager.UpdateAsync(user);

                    if (model.UserOptions != null)
                    {
                        foreach (var item in model.UserOptions)
                        {
                            if (item.Value != null && item.Value.Length > 760)
                            {
                                await dcmsLogService.AddInfo(user.Id, "UserOption", item.PropertyName, item.Value, SiteId);

                            }
                        }
                    }

                    await _loyaltyWalletService.ProcessRegistrationLoyaltySetups(user.Id, model.ReferrerCode);


                    Site site = _UsersSitesRepository.Query().Include(x => x.Site).Where(x => x.SiteId == SiteId).Select(x => x.Site).FirstOrDefault();

                    var isUserConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user);
                    RegisterationResultDTO Regresult = new RegisterationResultDTO()
                    {
                        UserId = user.Id,
                    };
                    if (_userManager.Options.SignIn.RequireConfirmedAccount && !isUserConfirmed)
                    {
                        _logger.LogInformation("Confirm account.");
                        Regresult.Message = "Confirm account is reqiured";
                        return Regresult;
                    }
                    // await _signInManager.SignInAsync(user, isPersistent: false);
                    //await _mediator.Publish(new UserSignedIn { UserId = long.Parse(userId) });
                    Regresult.Message = "User created a new account with password.";
                    //NotificationObject data = new NotificationObject();
                    //data.UserId = [user.Id];
                    //data.Severity = 0;
                    //data.SendValue = [user.Email];
                    ////Send Email To New User
                    //if (!string.IsNullOrEmpty(model.EmailTempleteId))
                    //{
                    //    var EmailTempleteObj = await _emailTemplateService.GetEmailTemplateById(model.EmailTempleteId);
                    //    if (EmailTempleteObj != null)
                    //    {

                    //        data.EmailTemplateId = model.EmailTempleteId;
                    //        data.MessageTitle = EmailTempleteObj.Subject;
                    //        data.MessageBody = EmailTempleteObj.EmailBody;


                    //    }
                    //}
                    //else
                    //{
                    //    data.MessageBody= $@"\r\n<html>\r\n  <body>\r\n    <p>Hello {user.FullName},</p>\r\n    <p>Thank you for registering at Our Website.</p>\r\n <p>If you did not register, please ignore this email.</p>\r\n    <br/>\r\n    <p>Best regards,<br/>The OurApp Team</p>\r\n  </body>\r\n</html>\";
                    //    data.MessageTitle= "Welcome to OurApp";

                    //}
                    //await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, SiteId);



                    return Regresult;

                }
                else
                    throw new Exception("Something went wrong !");

            }
            catch (Exception e)
            {



                throw new Exception("Something Went Wrong !" + e.Message);
            }
        }

        public async Task<UserTokenResponse> GetUserContactInfo(string Username, long SiteId)
        {
            string NormalizedUserName = Username.ToUpper();
            return await _UsersSitesRepository.Query()
                .Include(us => us.User)
                .Where(us => us.SiteId == SiteId && us.User.NormalizedUserName == NormalizedUserName)
                .Select(us => us.User)
                .Select(x => new UserTokenResponse
                {
                    PhoneNumber = _RSA_Helper.Encrypt(x.PhoneNumber),
                    Email = _RSA_Helper.Encrypt(x.Email),
                    EncryptedId = EncryptDecryptHelper.Encrypt(x.Id.ToString()),
                    Id = x.Id,
                    EncodedPhoneNumber = EncodeEmailPhone(E2FAProviders.Phone, x.PhoneNumber),
                    EncodedEmail = EncodeEmailPhone(E2FAProviders.Email, x.Email),
                    Username = x.UserName
                }).FirstOrDefaultAsync();
        }

        public async Task AddUserToSite(string Username)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            string NormalizedUserName = Username.ToUpper();
            var User = _userrepository.Query().Where(u => u.NormalizedUserName == NormalizedUserName).Include(u => u.UserSites).FirstOrDefault();
            if (User == null)
            {
                throw new Exception("User Not Found");
            }

            if (!User.UserSites.Any(us => us.SiteId == SiteId))
            {
                UsersSites UserSite = new UsersSites()
                {
                    UserId = User.Id,
                    SiteId = SiteId,

                };
                _UsersSitesRepository.Add(UserSite);
                await _UsersSitesRepository.SaveChangesAsync();
            }
        }

        public async Task<long> InsureUser(RegisterViewModel Model)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            string NormalizedUserName = Model.UserName.ToUpper();
            var User = _userrepository.Query().Where(u => u.NormalizedUserName == NormalizedUserName).Include(u => u.UserSites).FirstOrDefault();
            long UserId;
            if (User == null)
            {
                var Result = await Register(Model);
                UserId = Result.UserId;
            }
            else
            {
                UserId = User.Id;

                if (!User.UserSites.Any(us => us.SiteId == SiteId))
                {
                    UsersSites UserSite = new UsersSites()
                    {
                        UserId = User.Id,
                        SiteId = SiteId,

                    };
                    _UsersSitesRepository.Add(UserSite);
                    await _UsersSitesRepository.SaveChangesAsync();
                }
            }

            return UserId;
        }
        public async Task<bool> ActivateUser(VerifyCodeViewModel model)
        {
            try
            {
                var user = await _userrepository.Query().Include(x => x.UserOTP)
                      .Select(x => x.UserName == model.Username
                      &&
                      x.UserOTP.Any(o => o.Value == model.Code && o.Purpose == model.Purpose)).FirstOrDefaultAsync();

                if (user)
                {
                    var objuser = new User();
                    objuser.Status = EStatus.Active;
                    objuser.StatusDate = DateTimeOffset.Now;
                    _userrepository.Update(objuser);
                }
                else
                {
                    throw new Exception("Somthing went wrong");
                }
                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<LoginResponse> Login(LoginViewModel model)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                Site site = await _siteRepository.Query().FirstOrDefaultAsync(s => s.Id == SiteId);

                User user = new User();
                string ModelUsername = model.Username.ToUpper();
                switch (model.LoginType)
                {
                    case LoginTypes.UserName:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.NormalizedUserName == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.Email:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.NormalizedEmail == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.PhoneNumber:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.PhoneNumber == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.AdditionInfo:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.UserOptions.Any(x => x.NormalizedPropertyName == model.AdditionalInfoValue.ToUpper() && x.NormalizedValue == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.UsernameOrEmail:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName == ModelUsername || x.NormalizedEmail == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.UsernameOrPhone:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName == ModelUsername || x.PhoneNumber == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.PhoneOrEmail:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedEmail == ModelUsername || x.PhoneNumber == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                    default:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName.ToUpper() == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        break;
                }

                if (user == null)
                {
                    throw new Exception("INVALID_CREDENTIALS");
                }

                if (model.FromGoogleOrFacebookOrApple)
                {
                    if (user.IsFacebookRegistered || user.IsGoogleRegistered || user.IsAppleRegistered)
                    {
                        return await DCMSLoginWithoutPassword(model, SiteId, false);

                    }
                }

                return await DCMSLogin(user, model, SiteId, false);
            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);


                throw new Exception(e.Message);
            }
        }
        public async Task<bool> ValidateCredentials(LoginViewModel model)
        {
            try
            {

                long SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                Site site = await _siteRepository.Query().FirstOrDefaultAsync(s => s.Id == SiteId);

                User user = new User();
                string ModelUsername = model.Username.ToUpper();
                switch (model.LoginType)
                {
                    case LoginTypes.UserName:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.NormalizedUserName == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("UserName doesn't exists!");
                        break;
                    case LoginTypes.Email:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.NormalizedEmail == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("Email doesn't exists!");
                        break;

                    case LoginTypes.PhoneNumber:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.PhoneNumber == ModelUsername && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("PhoneNumber doesn't exists!");
                        break;

                    case LoginTypes.AdditionInfo:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => x.UserOptions.Any(x => x.NormalizedPropertyName == model.AdditionalInfoValue.ToUpper() && x.NormalizedValue == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("AdditionInfo doesn't exists!");
                        break;

                    case LoginTypes.UsernameOrEmail:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName == ModelUsername || x.NormalizedEmail == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("UsernameOrEmail doesn't exists!");
                        break;

                    case LoginTypes.UsernameOrPhone:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName == ModelUsername || x.PhoneNumber == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("UsernameOrPhone doesn't exists!");
                        break;

                    case LoginTypes.PhoneOrEmail:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedEmail == ModelUsername || x.PhoneNumber == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("PhoneOrEmail doesn't exists!");
                        break;

                    default:
                        user = await _userrepository.Query().Include(x => x.UserSites).ThenInclude(x => x.Site).Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.UserOptions).Where(x => (x.NormalizedUserName.ToUpper() == ModelUsername) && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();
                        if (user == null) throw new Exception("User doesn't exists!");
                        break;
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return true;
                }
                throw new Exception("INVALID CREDENTIALS");

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> CheckExpiryPasswordForUser(User user)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    var _UserRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();


                    var Today = DateTime.Now;
                    var Days = user.CustomerGroups.Select(x => x.CustomerGroup.PasswordExpiryInDays).FirstOrDefault();

                    if (user.lastPasswordUpdate == null)
                    {
                        user.lastPasswordUpdate = Today;
                        _UserRepo.Update(user);
                        _UserRepo.SaveChanges();


                    }
                    else
                    {
                        if (Days != 0)
                        {
                            var UserLastChangePassword = user.lastPasswordUpdate.Value.AddDays(Days);

                            if (DateTime.Compare(UserLastChangePassword, Today) <= 0)
                            {
                                throw new Exception("Your password has expired.Kindly update your password to proceed");
                            }
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    //string Host = _workContext.GetCurrentHostname();


                    //await ForgotPasswordExternalLink(new ForgotPasswordViewModel { Email = user.Email, EmailTemplateId = "ForgetPassword", Host = Host });

                    throw ex;
                }
            }
        }

        public async Task<List<UserOptionsVM>> GetUserOptionsAsync()
        {

            var userId = await _workContext.GetCurrentUserId();
            var userOptionEntities = await _userrepository.Query().Include(x => x.UserOptions).Where(x => x.Id == userId)
                .Select(x => x.UserOptions).FirstOrDefaultAsync();

            var userOptions = userOptionEntities?.Select(userOption => new UserOptionsVM
                {
                    ControlType = userOption.ControlType,
                    IsEditable = userOption.IsEditable,
                    IsVisible = userOption.IsVisible,
                    PropertyName = userOption.PropertyName,
                    Value = userOption.Value
                }).ToList();

            if (userOptions == null)
            {
                return new List<UserOptionsVM>();
            }

            return userOptions;
        }

        public async Task<List<string>> GetUserOptionKeys()
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userOptionKeys = await _UserOptionsRepository.Query()
                .AsNoTracking()
                .AsSplitQuery()
                //.Where(x => x.User.UserSites
                //    .Select(us => us.SiteId)
                //    .Contains(siteId))
                .Select(x => x.PropertyName)
                .Distinct()
                .ToListAsync();

            return userOptionKeys;

        }

        private async Task<LoginResponse> DCMSLogin(User user, LoginViewModel model, long SiteId, bool isCoreIntegration)
        {
            try
            {
                bool isPasswordValid = false;
                if (user == null)
                    throw new Exception("INVALID_CREDENTIALS");
                if (user.UserRoles == null)
                    throw new Exception("INVALID_CREDENTIALS");
                if (user.UserSites == null)
                    throw new Exception("INVALID_CREDENTIALS");

                if (user.Status == EStatus.Inactive || user.Status == EStatus.Deleted || user.Status != EStatus.Active)
                    throw new Exception("INACTIVE_USER");

                if (model.AuthorizedGroups != null)
                {
                    if (model.AuthorizedGroups.Count > 0)
                    {
                        var authorizedUserGroups = user.CustomerGroups.Where(x => model.AuthorizedGroups.Any(a => a == x.CustomerGroupId)).ToList();
                        if (!authorizedUserGroups.Any())
                        {
                            throw new Exception("INVALID_CREDENTIALS");
                        }
                    }
                }
                var Source = _workContext.GetSource();

                await CheckExpiryPasswordForUser(user);

                if (!model.ValidateFromAd)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);



                    isPasswordValid = result.Succeeded;
                }
                else
                {

                    isPasswordValid = await ValidateAdIntegration(model.Password, model.Username);

                }

                if (isPasswordValid)
                {
                    await TwoFactorEnabled(user, 0);
                    LoginResponse runTimeObject = new LoginResponse();
                    await _mediator.Publish(new UserSignedIn { UserId = user.Id, SiteId = SiteId });
                    await _userLoginTransactionsService.AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);
                    runTimeObject.Culture = GlobalConfiguration.DefaultCulture;
                    runTimeObject.UserAdditionData = user.UserOptions.Select(x => new UserOptionsVM
                    {
                        PropertyName = x.PropertyName,
                        Value = x.Value,
                        ControlType = x.ControlType,
                        IsEditable = x.IsEditable,
                        IsVisible = x.IsVisible,
                        Id = x.Id
                    }).ToList();
                    runTimeObject.Email = user.Email;
                    runTimeObject.ImageUrl = user.Image;
                    runTimeObject.Role = user.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList();
                    runTimeObject.Token = await _tokenService.CreateToken(user, true);
                    return runTimeObject;
                }

                throw new Exception("INVALID_CREDENTIALS");
            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }

        private async Task<bool> ValidateAdIntegration(string password, string username)
        {
            try
            {
                var credentialsCache = new NetworkCredential(adusername, adpassword);
                var handler = new HttpClientHandler { Credentials = credentialsCache };
                var payload = new loginRequestAdViewModel { Password = password, Username = username };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(AdBaseUrl);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    HttpResponseMessage response = await client.PostAsync("adapi/Identity/ValidateAd", content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var Result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        return Convert.ToBoolean(Result.content);
                    }
                    else
                    {

                        return false;
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<LoginResponse> FingerLogin(FingerVM model)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");


                Site site = await _siteRepository.Query().Where(s => s.Id == SiteId).FirstOrDefaultAsync();

                User user = await _userrepository.Query()
                                              .Include(x => x.UserSites)
                                              .ThenInclude(x => x.Site)
                                              .Include(x => x.CustomerGroups)
                                              .ThenInclude(x => x.CustomerGroup)
                                              .Include(x => x.UserRoles)
                                              .ThenInclude(x => x.Role)
                                              .Include(x => x.UserOptions)
                                              .Where(x => x.Id == model.UserId && x.UserSites.Any(a => a.SiteId == SiteId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new Exception("User Not Foumd ");

                }

                if (user.Status == EStatus.Inactive || user.Status == EStatus.Deleted)
                    throw new Exception("User Is Locked");

                if (string.IsNullOrEmpty(user.FingerSign))
                {

                    throw new Exception("Please Enabled The Finger From Your Profile");


                }
                var publicKeyBytes = Convert.FromBase64String(user.FingerSign);
                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                    var IsVerfiedRSA = rsa.VerifyData(
                         Encoding.UTF8.GetBytes(model.Payload),
                         Convert.FromBase64String(model.Signature),
                         HashAlgorithmName.SHA256,
                         RSASignaturePadding.Pkcs1);

                    if (IsVerfiedRSA)
                    {
                        LoginResponse runTimeObject = new LoginResponse();

                        runTimeObject.Token = await _tokenService.CreateToken(user, true);
                        runTimeObject.Role = user.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList();

                        await _mediator.Publish(new UserSignedIn { UserId = user.Id, SiteId = SiteId });

                        await _userLoginTransactionsService.AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);

                        return runTimeObject;
                    }
                }

                throw new Exception("User UnAuthorized By Biometric");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<LoginResponse> DCMSLoginWithoutPassword(LoginViewModel model, long SiteId, bool isCoreIntegration)
        {
            try
            {
                User user = await _userrepository.Query()
                    .Include(x => x.UserSites)
                    .Include(x => x.CustomerGroups)
                    .ThenInclude(x => x.CustomerGroup)
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                    .Include(x => x.UserOptions)
                    .Where(x => x.NormalizedUserName == model.Username.ToUpper() && x.UserSites.Any(a => a.SiteId == SiteId))
                    .FirstOrDefaultAsync();
                if (user == null)
                    throw new Exception("Something went wrong");
                if (user.UserRoles == null)
                    throw new Exception("Something went wrong");
                if (user.UserSites == null)
                    throw new Exception("Something went wrong");

                if (user.Status == EStatus.Inactive || user.Status == EStatus.Deleted)
                    throw new Exception("Something went wrong");

                if (model.AuthorizedGroups != null)
                {
                    if (model.AuthorizedGroups.Count > 0)
                    {
                        var authorizedUserGroups = user.CustomerGroups.Where(x => model.AuthorizedGroups.Any(a => a == x.CustomerGroupId));
                        if (authorizedUserGroups == null)
                        {
                            throw new Exception("Something went wrong");
                        }
                    }
                }
                LoginResponse runTimeObject = new LoginResponse();
                runTimeObject.Token = await _tokenService.CreateToken(user, true);
                runTimeObject.Role = user.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList();
                runTimeObject.Email = user.Email;
                runTimeObject.ImageUrl = user.Image;
                await _mediator.Publish(new UserSignedIn { UserId = user.Id, SiteId = SiteId });
                await _userLoginTransactionsService.AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);
                return runTimeObject;
            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }
        public async Task<bool> UploadImage(UploadImageDto model)
        {
            var SiteId = _workContext.GetCurrentSiteId();

            long userId = 0;
            if (!string.IsNullOrEmpty(model.Id))
            {
                userId = long.Parse(EncryptDecryptHelper.Decrypt(model.Id));
            }
            else
            {
                userId = await _workContext.GetCurrentUserId();
            }
            var user = _UsersSitesRepository.Query()
                                             .Where(x => x.SiteId == SiteId && x.UserId == userId)
                                             .Include(x => x.User)
                                             .Select(x => x.User)
                                             .FirstOrDefault();
            if (model.UserImage != null)
            {
                var FolderName = "wwwroot\\Resources\\UserImages\\site_" + SiteId;
                var FolderNamePath = "Resources\\UserImages\\site_" + SiteId;
                if (!Directory.Exists(FolderName))
                {
                    Directory.CreateDirectory(FolderName);
                    FolderName = FolderNamePath + "\\";
                }

                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), FolderName.Trim());
                var fileName = ContentDispositionHeaderValue.Parse(model.UserImage.ContentDisposition).FileName.Trim('"');
                var fullPath = Path.Combine(pathToSave.Trim(), fileName.Trim());
                var FilePath = Path.Combine(FolderNamePath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    model.UserImage.CopyTo(stream);
                }
                user.Image = FilePath;

            }
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return true;
            }
            return false;



        }

        public async Task<Core.Models.TokenVM.TokenResponse> GetCrawlerToken(CrawlerViewModel model)
        {
            var siteId = _workContext.GetCurrentSiteId();

            try
            {
                var normalizedUserName = model.UserName.Replace(" ", "")
                                                       .Replace("+", "")
                                                       .Replace("-", "")
                                                       .Replace("_", "")
                                                       .Replace("!", "")
                                                       .Replace(".", "")
                                                       .Replace("/", "")
                                                       .ToUpper();

                string cacheKey = $"CrawlerToken_{normalizedUserName}".ToLower();
                var cacheData = cacheService?.GetData<TokenResponse>(cacheKey);

                if (cacheData != null)
                {
                    if (cacheData.ExpiryDate > DateTime.UtcNow)
                    {
                        return cacheData;
                    }
                }

                var tokenLock = CrawlerTokenLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
                await tokenLock.WaitAsync();
                try
                {
                    cacheData = cacheService?.GetData<TokenResponse>(cacheKey);

                    if (cacheData != null && cacheData.ExpiryDate > DateTime.UtcNow)
                    {
                        return cacheData;
                    }

                    var user = await GetUserByUserName(siteId, normalizedUserName);

                    if (user == null)
                    {
                        user = await CreateNewUser(siteId, model.UserName, normalizedUserName);
                    }
                    else
                    {
                        await EnsureUserHasAccessToSite(user, siteId);
                    }

                    var existingToken = await GetValidCurrentToken(user);
                    if (existingToken != null)
                    {
                        cacheService?.RemoveData(cacheKey);
                        cacheService?.SetData(
                            cacheKey,
                            existingToken,
                            DateTimeOffset.UtcNow.AddDays(
                                double.Parse(configuration.GetSection("CacheConfig:CachePeriod").Value))
                        );

                        return existingToken;
                    }

                    var token = await _tokenService.CreateToken(user, true);
                    cacheService?.RemoveData(cacheKey);
                    cacheService?.SetData(
                        cacheKey,
                        token,
                        DateTimeOffset.UtcNow.AddDays(
                            double.Parse(configuration.GetSection("CacheConfig:CachePeriod").Value))
                    );

                    return token;
                }
                finally
                {
                    tokenLock.Release();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<Core.Models.TokenVM.TokenResponse?> GetValidCurrentToken(User user)
        {
            var currentUser = await _CurrentLoggedInUsers.Query()
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.LoginDate)
                .FirstOrDefaultAsync();

            if (currentUser == null)
                return null;

            if (string.IsNullOrWhiteSpace(currentUser.ApiToken))
                return null;

            var expiryDate = GetTokenExpiry(currentUser.ApiToken);

            if (!expiryDate.HasValue || expiryDate.Value <= DateTime.UtcNow)
                return null;

            return new Core.Models.TokenVM.TokenResponse
            {
                Token = currentUser.ApiToken,
                RefreshToken = currentUser.RefreshTokenHash,
                ExpiryDate = expiryDate.Value
            };
        }

        public DateTime? GetTokenExpiry(string jwtToken)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
                return null;

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(jwtToken))
                return null;

            var token = handler.ReadJwtToken(jwtToken);

            return token.ValidTo;
        }
        public async Task<bool> EditProfile(EditProfileViewModel model, IFormFile file)
        {
            //EditProfileViewModel model = JsonSerializer.Deserialize<EditProfileViewModel>(Request.Form["data"].ToString());

            var SiteId = _workContext.GetCurrentSiteId();
            if (SiteId == 0)
                throw new Exception("Site Id cannot be null");
            var userId = await _workContext.GetCurrentUserId();
            User user = await _userrepository.Query()
                .Where(x => x.Id == userId)
                .Include(x => x.UserRoles)
                .Include(x => x.CustomerGroups)
                .Include(x => x.UserOptions)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return false;
            }

            user.Email = model.Email;
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.TwoFactorEnabled = model.EnableTwoFactorAuthentication.HasValue ? model.EnableTwoFactorAuthentication.Value : false;

            if (model.DateOfBirth != null)
                user.DateOfBirth = new DateTimeOffset(model.DateOfBirth.Value.DateTime, TimeSpan.Zero);
            if (model.Image != null)
                user.Image = model.Image;

            if (file != null)
            {


                var FolderName = "wwwroot\\Resources\\UserImages\\site_" + SiteId;
                var FolderNamePath = "Resources\\UserImages\\site_" + SiteId;
                if (!Directory.Exists(FolderName))
                {
                    Directory.CreateDirectory(FolderName);
                    FolderName = FolderNamePath + "\\";
                }

                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), FolderName.Trim());
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var fullPath = Path.Combine(pathToSave.Trim(), fileName.Trim());
                var FilePath = Path.Combine(FolderNamePath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(stream);
                }
                user.Image = FilePath;

            }
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var userOptions = await _UserOptionsRepository.Query().Where(x => x.UserId == userId).ToListAsync();
                if (user.UserOptions != null && user.UserOptions != null)
                {
                    foreach (var item in model.UserOptions)
                    {
                        if (item.IsEditable)
                        {
                            var itemToUpdate = user.UserOptions.Where(x => x.PropertyName == item.PropertyName).FirstOrDefault();
                            if (itemToUpdate != null)
                            {
                                itemToUpdate.Value = item.Value;
                                _UserOptionsRepository.Update(itemToUpdate);
                            }
                        }
                    }
                    _UserOptionsRepository.SaveChanges();

                    await AddUpdateUserOptions(user.UserName, model.UserOptions);
                }
                return true;
            }
            return false;
        }

        public async Task AddUpdateUserOptions(string RegistrationId, List<UserOptionsVM> userOptions)
        {
            try
            {
                RegistrationId = RegistrationId.Trim().ToLower();

				var user = await _UsersSitesRepository.Query()
                    .Where(x => x.User.UserName.ToLower() == RegistrationId)
                    .Include(x => x.User)
                    .ThenInclude(x => x.UserOptions).FirstOrDefaultAsync();
                if (user != null)
                {
                    await _UserOptionsRepository.ExecuteInTransactionAsync(async () =>
                    {
                        foreach (var option in userOptions)
                        {
                            var selectedOption = user.User.UserOptions.FirstOrDefault(x => x.PropertyName.ToUpper() == option.PropertyName.ToUpper());

                            if (selectedOption != null)
                            {
                                if (selectedOption.IsEditable)
                                {
                                    selectedOption.PropertyName = option.PropertyName;
                                    selectedOption.Value = option.Value;
                                    selectedOption.UserId = user.User.Id;
                                    selectedOption.IsEditable = option.IsEditable;
                                    selectedOption.IsVisible = option.IsVisible;
                                    selectedOption.ModificationDate = DateTimeOffset.Now;
                                    _UserOptionsRepository.Update(selectedOption);
                                }
                            }
                            else
                            {
                                EskaCMS.Entities.Entity.Core.UserOptions updatedUserOption = new EskaCMS.Entities.Entity.Core.UserOptions
                                {
                                    UserId = user.User.Id,
                                    PropertyName = option.PropertyName,
                                    Value = option.Value,
                                    IsEditable = option.IsEditable,
                                    IsVisible = option.IsVisible,
                                    CreationDate = DateTimeOffset.Now
                                };
                                _UserOptionsRepository.Add(updatedUserOption);
                            }

                            _UserOptionsRepository.SaveChanges();
                        }
                        await _UserOptionsRepository.SaveChangesAsync();
                    });

                }
                else
                {
                    throw new Exception("Something went wrong !");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Something went wrong !");
            }
        }

        public async Task<bool> MigrateUserToCustomer(string RegistrationId, string Name, string phone)
        {
            var userId = await _workContext.GetCurrentUserId();

            using var scope = _serviceScopeFactory.CreateScope();

            var userRepository = scope.ServiceProvider
                                      .GetRequiredService<IRepository<User>>();

            var user = await userRepository.Query()
                .Include(x => x.UserRoles)
                .Include(x => x.CustomerGroups)
                .FirstOrDefaultAsync(x => x.Id == userId);
            try
            {


                user.Type = EUsersTypes.Customer;
                user.FullName = Name;
                user.PhoneNumber = phone;



                await userRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    user.Type = EUsersTypes.Customer;
                    user.FullName = Name;
                    user.PhoneNumber = phone;

                    // ✅ Avoid duplicate role
                    if (!user.UserRoles.Any(r => r.RoleId == 6))
                    {
                        user.UserRoles.Add(new UserRole
                        {
                            UserId = userId,
                            RoleId = 6
                        });
                    }

                    // ✅ Avoid duplicate customer group
                    if (!user.CustomerGroups.Any(g => g.CustomerGroupId == 1))
                    {
                        user.CustomerGroups.Add(new CustomerGroupUser
                        {
                            UserId = userId,
                            CustomerGroupId = 1,
                            CreationDate = DateTimeOffset.Now
                        });
                    }

                    await userRepository.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    return false;
                }

                // scope will dispose DbContext automatically
                return false;
            }
        }
        public async Task CheckUserOptionValidation(List<CheckUserOptionValidationVM> validations)
        {
            try
            {
                if (validations != null)
                {
                    foreach (var validation in validations)
                    {
                        var SiteId = _workContext.GetCurrentSiteId();// long.Parse(Request.Headers["siteId"].ToString());
                        long existingUserId = 0;

                        if (validation.Key.ToUpper() == "EMAIL" || validation.Key.ToUpper() == "USERNAME")
                        {
                            var IsUserExist = _userrepository.Query()
                                .Include(x => x.UserRoles).AsNoTracking()
                                .Where(x => x.Status != EStatus.Deleted && x.Type != EUsersTypes.GuestClient && (x.Email.ToLower() == validation.Value.ToLower() ||
                              x.UserName.ToLower() == validation.Value.ToLower())).Select(x => x.Id).FirstOrDefault();
                            if (IsUserExist > 0)
                            {
                                existingUserId = _UsersSitesRepository.Query()
                                    .Include(x => x.User)
                                    .ThenInclude(x => x.UserRoles)
                                    .Where(x => x.SiteId == SiteId && (x.User.Email.ToLower() == validation.Value.ToLower() ||
                                x.User.UserName.ToLower() == validation.Value.ToLower()) && x.User.Type != EUsersTypes.GuestClient)
                                .Select(x => x.User.Id)
                                .FirstOrDefault();

                                if (existingUserId == 0)
                                    throw new Exception("User is already exist, but does not belong to this application, please contact your admin");
                                else
                                    throw new Exception("Email or Username is already used, please use another valid email");
                            }
                        }
                        else
                        {
                            var IsUserExist = _userrepository.Query().AsNoTracking().Include(x => x.UserOptions).Where(x => x.UserOptions.Any(x => x.PropertyName.ToUpper() == validation.Key.ToUpper()
                               && x.Value.ToUpper() == validation.Value.ToUpper())).Select(x => x.Id).FirstOrDefault();
                            if (IsUserExist > 0)
                            {
                                var UserId = await _workContext.GetCurrentUserId();
                                existingUserId = _UsersSitesRepository.Query().AsNoTracking().Include(x => x.User).Where(x => x.User.Status != EStatus.Deleted && x.User.Type != EUsersTypes.GuestClient && x.SiteId == SiteId &&
                                         (x.User.UserOptions.Any(x => x.PropertyName.ToUpper() == validation.Key.ToUpper() && x.Value.ToUpper() == validation.Value.ToUpper()))
                                         && x.UserId != UserId).Select(x => x.User.Id).FirstOrDefault();
                                if (existingUserId > 0)
                                    throw new Exception(String.Format("{0} is already used and cannot be used again.", validation.Key));
                            }
                        }
                    }

                }
                else
                {
                    throw new Exception("Something went wrong !");
                }
            }
            catch (Exception ex)
            {
                //we need it to know if user exists or not;   
                throw new Exception(ex.Message);
            }

        }

        public async Task AddUpdateUserOptions(List<UserOptionsVM> userOptions,long? UserId=null)
        {
            try
            {

                var CurrentUserOptions = await _UserOptionsRepository.Query()
                    .Where(x => x.UserId == _workContext.GetCurrentUserIdValue()).ToListAsync();

                await _UserOptionsRepository.ExecuteInTransactionAsync(async () =>
                {
                    foreach (var option in userOptions)
                    {
                        var selectedOption = CurrentUserOptions.
                            FirstOrDefault(x => x.PropertyName.ToUpper() == option.PropertyName.ToUpper());

                        if (selectedOption != null)
                        {
                            if (selectedOption.IsEditable)
                            {
                                selectedOption.PropertyName = option.PropertyName;
                                selectedOption.Value = option.Value;
                                selectedOption.UserId = _workContext.GetCurrentUserIdValue();
                                selectedOption.IsEditable = option.IsEditable;
                                selectedOption.IsVisible = option.IsVisible;
                                _UserOptionsRepository.Update(selectedOption);
                            }
                        }
                        else
                        {
                            EskaCMS.Entities.Entity.Core.UserOptions updatedUserOption = new EskaCMS.Entities.Entity.Core.UserOptions
                            {
                                UserId = UserId ?? _workContext.GetCurrentUserIdValue(),
                                PropertyName = option.PropertyName,
                                Value = option.Value,
                                IsEditable = option.IsEditable,
                                IsVisible = option.IsVisible
                            };
                            _UserOptionsRepository.Add(updatedUserOption);
                        }

                        _UserOptionsRepository.SaveChanges();
                    }
                    await _UserOptionsRepository.SaveChangesAsync();
                });


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task LogOff()
        {

            var UserId = await _workContext.GetCurrentUserId();
            var IpClient = _workContext.GetIPAddress();
            await _loggedInUserService.RemoveloggedInUser(UserId, IpClient);

            await DeleteAuthCookies();

            await _signInManager.SignOutAsync();

            await HandleUnsubscribeGroup();

            _logger.LogInformation(4, "User logged out.");

        }


        public async Task<bool> HandleUnsubscribeGroup()
        {
            var user = await _workContext.GetCurrentUser();
            if (user != null && !string.IsNullOrEmpty(user.DeviceToken))
            {
                var deviceToken = new List<string>() { user.DeviceToken };

                var usersGroups = _customerGrouprepository.Query().Where(x => x.Users.Any(x => x.UserId == user.Id) && !x.IsDeleted && !string.IsNullOrEmpty(x.FireBaseTopic)).ToList();

                foreach (var groupTopic in usersGroups)
                {

                    var response = await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(deviceToken, groupTopic.FireBaseTopic);

                }

                user.DeviceToken = null;
                await _userManager.UpdateAsync(user);


            }
            return true;
        }

        public async Task<List<string>> GetRoleOptions()
        {
            var currentUserId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();
            var allRoles = await _userrepository.Query()
                                      .Where(x => x.Id == currentUserId &&
                                                 x.UserSites.Any(s => s.SiteId == siteId))
                                      .Include(x => x.UserRoles)
                                      .ThenInclude(x => x.Role)
                                      .ThenInclude(x => x.RoleOptions.Where(t => t.OptionValue == 1))
                                      .ThenInclude(x => x.Option)
                                      .SelectMany(x => x.UserRoles)
                                      .Select(x => x.Role)
                                      .ToListAsync();

            var options = new List<string>();

            foreach (var role in allRoles)
            {
                var Role = await _roleManager.FindByNameAsync(role.Name);
                var Options = Role.RoleOptions.Select(x => x.Option.Code).ToList();
                options.AddRange(Options);
            }

            return options;



        }



        public async Task<IdentityResult> AddRoleToUser(long RoleId)
        {
            var User = await _userManager.FindByIdAsync(_workContext.GetCurrentUserIdValue().ToString());
            var Role = await _roleManager.FindByIdAsync(RoleId.ToString());
            //_userRepository.Query().Where(x=>x.)
            return await _userManager.AddToRoleAsync(User, Role.Name);
        }
        public async Task<bool> ConfirmEmail(string code)
        {
            var User = await _workContext.GetCurrentUser();

            var result = await _userManager.ConfirmEmailAsync(User, code);
            if (result.Succeeded)
                return true;
            else
                return false;

        }

        public async Task ForgotPasswordExternalLink(Core.Entities.ForgotPasswordViewModel model)
        {

            var SiteId = _workContext.GetCurrentSiteId();

            User user = new User();
            if (SiteId > 0)
            {
                user = await _UsersSitesRepository.Query().Include(x => x.User).Where(x => x.User.Email.ToLower() == model.Email.ToLower() && x.SiteId == SiteId).Select(x => x.User).FirstOrDefaultAsync();

            }
            else
            {
                user = await _userrepository.Query().Where(x => x.Email.ToLower() == model.Email.ToLower()).FirstOrDefaultAsync();
            }

            if (user == null)
            {
                // Don't reveal that the user does not exist or is not confirmed
                throw new Exception("Somthing went wrong !");
            }

            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
            // Send an email with this link
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(code);
            string convertedCode = Base64UrlEncode(toEncodeAsBytes);
            //var callbackUrl = Url.Action(Acco "ResetPassword", "AccountController", new { user = user.Id, code = code }, Request.Scheme);

            var callbackUrl = $"{model.Host}/reset-password/user{user.Id}key{convertedCode}";
            var lstemail = new List<string>();
            lstemail.Add(model.Email);

            var EmailBody = $"<html><body><span>Please reset your password by clicking here : </span> <a href='{callbackUrl}'>link</a></body></html>";

            if (model.EmailTemplateId != null)
            {
                var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.EmailTemplateId);

                if (emailTemplateObj != null && emailTemplateObj.EmailBody != null && emailTemplateObj.EmailBody.IndexOf("[callbackUrl]") != -1)
                {
                    EmailBody = emailTemplateObj.EmailBody.Replace("[callbackUrl]", callbackUrl);
                }

            }

            NotificationObject data = new NotificationObject();
            List<long> Ids = new List<long>();
            Ids.Add(user.Id);
            data.UserId = Ids.ToArray();
            data.MessageTitle = "Reset Password";
            data.MessageBody = EmailBody;
            data.SendValue = lstemail;
            data.Severity = 0;
            await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, SiteId);

        }
        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);
            return output;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            // 62nd char of encoding
            output = output.Replace('-', '+');
            // 63rd char of encoding
            output = output.Replace('_', '/');
            // Pad with trailing '='s
            switch (output.Length % 4)
            {
                case 0:
                    // No pad chars in this case
                    break;
                case 2:
                    // Two pad chars
                    output += "=="; break;
                case 3:
                    // One pad char
                    output += "="; break;
                default:
                    throw new InvalidOperationException("Illegal base64url string!");
            }

            // Standard base64 decoder
            return Convert.FromBase64String(output);
        }

        public async Task ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username); ;
                var IsOldPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.OldPassword);
                if (IsOldPasswordCorrect)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var IsValidPassword = await _PasswordPolicy.IsValidPasswordAsync(model.NewPassword, model.Username);

                    IdentityResult result;
                    if (IsValidPassword)
                    {
                        result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                        await _usersPasswordHistory.PasswordHistoryLog(model.NewPassword, model.OldPassword, ChangePasswordHistory.ChangePassword, user.Id);

                        await LastUpdatePasswordEdit(user.Id);

                    }
                    else
                    {
                        throw new Exception("Invalid Password !");
                    }

                    if (!result.Succeeded)
                    {
                        string errors = AddErrors(result);

                        throw new Exception(!string.IsNullOrEmpty(errors) ? errors : "Something wrong!");
                    }


                }
                else
                {
                    throw new Exception("Please Check Your Credential");
                }
            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }
        public async Task<List<GetPasswordHistoryVM>> GetPasswordHistory(long UserId, ChangePasswordHistory? ChangePasswordType)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                return await _UsersPasswordHistoryRepo.Query()
                    .AsNoTracking()
                    .Include(x => x.UsersSites)
                    .Where(x => x.UsersSites.SiteId == SiteId && x.UsersSites.UserId == UserId)
                    .Where(x => ChangePasswordType == null || ChangePasswordType == x.ChangePasswordHistory)

                    .Select(x => new GetPasswordHistoryVM()
                    {
                        ChagneDate = x.Date,
                        EChangePasswordReason = x.ChangePasswordHistory
                    })
                    .OrderByDescending(x => x.ChagneDate)
                    .ToListAsync();

            }
            catch (Exception e)
            {

                throw e;
            }

        }
        public async Task ChangePasswordForAdmin(ChangePasswordForAdminViewModel model)
        {
            try
            {
                //var currentUser = await _workContext.GetCurrentUser();
                //var username = currentUser.UserName;
                //if (await _workContext.IsGuestUser())
                //{
                //    username = currentUser.FullName;
                //}

                //if (currentUser == null || (username != model.Username))
                //{
                //    throw new Exception("Something wrong!");

                //}

                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var IsValidPassword = await _PasswordPolicy.IsValidPasswordAsync(model.NewPassword, model.Username);

                    IdentityResult result;
                    if (IsValidPassword)
                    {
                        result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                        var oldPasswordHashed = user.PasswordHash;
                        await _usersPasswordHistory.PasswordHistoryLog(model.NewPassword, model.OldPassword, ChangePasswordHistory.ChangePassword, user.Id);

                        await LastUpdatePasswordEdit(user.Id);
                    }
                    else
                    {
                        throw new Exception("In Valid Password !");
                    }

                    if (!result.Succeeded)
                    {
                        string errors = AddErrors(result);

                        throw new Exception(!string.IsNullOrEmpty(errors) ? errors : "Something wrong!");
                    }


                }
                else
                {
                    throw new Exception("Something went wrong!");
                }



            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }

        public async Task<bool> LastUpdatePasswordEdit(long Id)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {

                    var _UserRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();


                    var User = await _UserRepo.Query().Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).Where(x => x.Id == Id).FirstOrDefaultAsync();
                    var Days = User.CustomerGroups.Select(x => x.CustomerGroup.PasswordExpiryInDays).FirstOrDefault();
                    User.lastPasswordUpdate = User.lastPasswordUpdate == null ? DateTime.Now.AddDays(Days) : User.lastPasswordUpdate.Value.AddDays(Days);
                    _UserRepo.Update(User);
                    _UserRepo.SaveChanges();


                    return true;


                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task ResetPassword(Core.Entities.ResetPasswordViewModel model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var CurrentUserId = await _workContext.GetCurrentUserId();

                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                string code = string.Empty;

                if (!model.IsResetPassword)
                {
                    byte[] encodedDataAsBytes = Base64UrlDecode(model.Code);
                    code = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
                }
                else
                {
                    code = model.Code;
                }
                var user = await _userrepository.Query()
                                            .Where(x => (x.UserName.ToUpper() == model.Username.ToUpper()))
                                            .Include(x => x.UserSites)
                                            .FirstOrDefaultAsync();

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    throw new Exception("User Not Found");
                }


                var result = await _UserOTPService.VerifyUserTokenAsync(CurrentUserId, SiteId, code, model.Purpose, model.Username, true);

                if (!result)
                {
                    throw new Exception("Invalid OTP !");
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var ResetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                //  await _userrepository.SaveChangesAsync();

                if (ResetResult.Succeeded)
                {

                    var oldPassword = await _UsersPasswordHistoryRepo.Query()
                    .Include(u => u.UsersSites)
                    .Where(x => x.UsersSites.SiteId == SiteId && x.UsersSites.UserId == user.Id)
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefaultAsync();
                    await _usersPasswordHistory.PasswordHistoryLog(model.Password, oldPassword == null ? null : oldPassword.NewPassword, ChangePasswordHistory.forgetPassword, user.UserSites.Where(x => x.SiteId == SiteId).Select(x => x.UserId).FirstOrDefault());


                    await LastUpdatePasswordEdit(user.Id);
                }
                else
                {

                    throw new Exception(ResetResult.Errors.Select(x => x.Description).FirstOrDefault());

                }

            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }
        public async Task SendCode(SendCodeViewModel model)
        {
            try
            {
                //if (configuration.GetValue<bool>("SecuirtyPenetration"))
                //    model.ValidateUser = true;

                if (!string.IsNullOrEmpty(model.Value) && model.ValidateUser)
                    model.Value = _RSA_Helper.Decrypt(model.Value);

                long SiteId = _workContext.GetCurrentSiteId();
                long userId = await _workContext.GetCurrentUserId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");


                if (userId == 0)
                {
                    throw new Exception("Something went wrong!");
                }

                string Code = string.Empty;
                var message = "Your security code is: ";
                Code = Get6DigitsOTP();
                await _UserOTPService.AddUserOTPAsync(userId, SiteId, model, Code);

                message = message + Code;

                if (!string.IsNullOrEmpty(model.EmailTemplateId))
                {
                    var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.EmailTemplateId);

                    if (emailTemplateObj != null && emailTemplateObj.EmailBody != null)
                    {
                        if (emailTemplateObj.EmailBody.IndexOf("[codel]") != -1)
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[codel]", Code);

                        if (emailTemplateObj.EmailBody.IndexOf("[urll]") != -1 && !string.IsNullOrEmpty(model.Url))
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[urll]", model.Url);

                        if (emailTemplateObj.EmailBody.IndexOf("[purposel]") != -1 && !string.IsNullOrEmpty(model.Purpose))
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[purposel]", model.Purpose);
                        else
                        {
                            message += " Purpose: " + model.Purpose;

                        }

                        if (emailTemplateObj.EmailBody.IndexOf("[dateTime]") != -1)
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[dateTime]", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                        if (model.AdditionalInfo != null)
                        {
                            foreach (var info in model.AdditionalInfo)
                            {
                                if (emailTemplateObj.EmailBody.IndexOf(info.Key) != -1 && !string.IsNullOrEmpty(info.Value))
                                {
                                    emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace(info.Key, info.Value);
                                }

                            }

                        }

                    }

                    message = emailTemplateObj.EmailBody;

                }

                if (model.SelectedProvider == E2FAProviders.Email)
                {
                    NotificationObject data = new NotificationObject();
                    List<long> Ids = new List<long>();
                    List<string> SendValues = new List<string>();
                    Ids.Add(userId);
                    SendValues.Add(model.Value);
                    data.UserId = Ids.ToArray();
                    data.MessageTitle = "Security Code";
                    data.MessageBody = message;
                    data.SendValue = SendValues;
                    data.Severity = 0;

                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, SiteId);

                }
                else if (model.SelectedProvider == E2FAProviders.Phone)
                {
                    if (!string.IsNullOrEmpty(model.Value))
                    {
                        NotificationObject data = new NotificationObject();
                        List<long> Ids = new List<long>();
                        List<string> SendValues = new List<string>();
                        Ids.Add(userId);
                        SendValues.Add(model.Value);
                        data.UserId = Ids.ToArray();
                        data.MessageTitle = "OTP";
                        data.MessageBody = message;
                        data.SendValue = SendValues;
                        data.SMSProvider = model.SMSProvider != null ? model.SMSProvider : SMSProviders.SMSMashastra;
                        data.Severity = 0;
                        await _testNotifier.SendMessageAsync(NotificationType.SMS, null, data, SiteId);
                    }
                }
                else if (model.SelectedProvider == E2FAProviders.WhatsApp)
                {
                    if (!string.IsNullOrEmpty(model.Value))
                    {
                        NotificationObject data = new NotificationObject();
                        List<long> Ids = new List<long>();
                        List<string> SendValues = new List<string>();
                        Ids.Add(userId);
                        SendValues.Add(model.Value);
                        data.UserId = Ids.ToArray();
                        data.MessageTitle = "OTP";
                        data.MessageBody = Code;
                        data.SendValue = SendValues;
                        data.UserName = model.Username;
                        data.WhatsAppProvider = model.WhatsAppProvider != null ? model.WhatsAppProvider : WhatsAppProviders.Infoseed;
                        data.Severity = 0;
                        await _testNotifier.SendMessageAsync(NotificationType.WhatsApp, null, data, SiteId);
                    }
                }

            }

            catch (Exception exc)
            {
                throw new Exception(exc.Message);
            }
        }



        private string Get6DigitsOTP()
        {
            var bytes = new byte[4];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            uint random = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return String.Format("{0:D6}", random);
        }
        public async Task VerifyCode(VerifyCodeViewModel model)
        {

            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                bool result;
                result = await _UserOTPService.VerifyUserTokenAsync(await _workContext.GetCurrentUserId(), SiteId, model.Code, model.Purpose, model.Username);

                if (!string.IsNullOrEmpty(model.Username))
                {
                    var user = await _userrepository.Query().Where(x => x.UserName.ToUpper().Equals(model.Username.ToUpper())).FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.Status = EStatus.Active;
                        user.StatusDate = DateTimeOffset.Now;
                        _userrepository.Update(user);
                        await _userrepository.SaveChangesAsync();
                    }
                }

                if (!result)
                    throw new Exception("Invalid code.");
            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }


        public async Task<CypherVM> HandShake(string PublicKey)
        {
            CypherParam CypherParam = new CypherParam();
            CypherParam.AuthId = CypherParam.AuthId;
            CypherParam.CreationDate = DateTimeOffset.Now;
            CypherParam.PublicId = PublicKey;
            CypherParam.MajorKey = await _IAES_Helper.GenerateKey();

            _cypherParamRepo.Add(CypherParam);
            await _cypherParamRepo.SaveChangesAsync();


            CypherParamVM Result = new CypherParamVM()
            {
                PointerId = _RSA_Helper.Encrypt(CypherParam.Id, PublicKey),
                PublicId = _RSA_Helper.getPublicKey(),
                AuthId = _RSA_Helper.Encrypt(CypherParam.AuthId, PublicKey),
                MajorKey = _RSA_Helper.Encrypt(CypherParam.MajorKey, PublicKey),
                CreationDate = CypherParam.CreationDate
            };

            CypherVM Model = new CypherVM()
            {
                CypherParam = Result,
                PointerId = CypherParam.Id

            };

            return Model;
        }


        public async Task<bool> IsValidPointerId(string PointerId)
        {


            var CypherParam = await _cypherParamRepo
                                        .Query()
                                        .Where(c => c.Id == PointerId)
                                        .FirstOrDefaultAsync();

            return CypherParam.CreationDate.AddDays(EXPIRY_LIMIT_IN_DAYS).CompareTo(DateTimeOffset.Now) > 0;


        }

        public async Task<string> TestDecrept(string CypherText)
        {


            return _RSA_Helper.Decrypt(CypherText);
        }



        #region CrawlerHelpers

        private async Task<User> CreateNewUser(long siteId, string userName, string normalizedUserName)
        {
            try
            {
                var newUser = new RegisterViewModel
                {
                    SiteId = siteId,
                    Status = EStatus.Active,
                    FullName = userName,
                    UserName = normalizedUserName,
                    Password = $"Eska@1{normalizedUserName}",
                    RoleId = 20,
                    GroupId = 2,
                    Email = $"{normalizedUserName}@{normalizedUserName}.com"
                };

                await Register(newUser);
                return await GetUserByUserName(siteId, normalizedUserName);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private async Task EnsureUserHasAccessToSite(User user, long siteId)
        {
            try
            {
                if (user.UserSites.All(x => x.SiteId != siteId))
                {
                    var userSite = new UsersSites
                    {
                        IsDefault = true,
                        SiteId = siteId,
                        UserId = user.Id
                    };

                    _UsersSitesRepository.Add(userSite);
                    await _UsersSitesRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<User> GetUserByUserName(long siteId, string normalizedUserName)
        {
            try
            {
                return await _userrepository.Query()
                    .Include(x => x.UserSites).ThenInclude(x => x.Site)
                    .Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup)
                    .Include(x => x.UserRoles).ThenInclude(x => x.Role)
                    .Include(x => x.UserOptions)
                    .Where(x => x.NormalizedUserName == normalizedUserName)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion


        #region Helpers

        private string AddErrors(IdentityResult result)
        {

            List<IdentityError> errorList = result.Errors.ToList();
            var errors = string.Join(Environment.NewLine, errorList.Select(e => e.Description));
            return errors;
        }

        #endregion


        public async Task<EditProfileViewModel> GetProfile()
        {
            var userId = await _workContext.GetCurrentUserId();
            EditProfileViewModel userProfileData = _userrepository.Query()
                .Where(x => x.Id == userId)
                .Include(x => x.UserOptions)
                .Select(x => new EditProfileViewModel
                {
                    FullName = x.FullName,
                    Email = x.Email,
                    Image = x.Image,
                    PhoneNumber = x.PhoneNumber,
                    DateOfBirth = x.DateOfBirth != null ? new DateTimeOffset(x.DateOfBirth.Value.DateTime, TimeSpan.Zero) : null,
                    UserOptions = x.UserOptions.Select(x => new UserOptionsVM
                    {
                        Id = x.Id,
                        ControlType = x.ControlType,
                        PropertyName = x.PropertyName,
                        Value = x.Value,
                        IsEditable = x.IsEditable,
                        IsVisible = x.IsVisible
                    }).ToList(),
                    EnableTwoFactorAuthentication = x.TwoFactorEnabled

                }).FirstOrDefault();

            return userProfileData;
        }
        public async Task<UserTokenResponse> GetUserInfo(string Username)
        {
            var SiteId = _workContext.GetCurrentSiteId();

            if (SiteId == 0)
            {
                throw new Exception("Site Can't be null");
            }
            var UserName = Username.ToUpper();

            // this if statment is a security leak.
            //if (await _workContext.IsGuestUser()) 
            //{


            var user = await _userrepository.Query()
                 .Include(x => x.UserSites)
                 .Where(x => x.NormalizedUserName == UserName && x.UserSites.Any(x => x.SiteId == SiteId))
                 .AsNoTracking()
                 .Select(x => new UserTokenResponse
                 {
                     PhoneNumber = _RSA_Helper.Encrypt(x.PhoneNumber),
                     Email = _RSA_Helper.Encrypt(x.Email),
                     EncodedPhoneNumber = EncodeEmailPhone(E2FAProviders.Phone, x.PhoneNumber),
                     EncodedEmail = EncodeEmailPhone(E2FAProviders.Email, x.Email),
                     DateOfBirth=x.DateOfBirth
                 }).FirstOrDefaultAsync();

            return user;
            //}
            //else
            //{
            //    var UserId = await _workContext.GetCurrentUserId();

            //    var user = await _userrepository.Query()
            //        .Include(x => x.UserSites)
            //        .Where(x => x.Id == UserId
            //         && x.UserSites.Any(x => x.SiteId == SiteId))
            //        .AsNoTracking()
            //        .Select(x => new UserTokenResponse
            //        {
            //            PhoneNumber = _RSA_Helper.Encrypt(x.PhoneNumber),
            //            Email = _RSA_Helper.Encrypt(x.Email),
            //            EncodedPhoneNumber = EncodeEmailPhone(E2FAProviders.Phone, x.PhoneNumber),
            //            EncodedEmail = EncodeEmailPhone(E2FAProviders.Email, x.Email),
            //            Image = x.Image
            //        }).FirstOrDefaultAsync();
            //    return user;
            //}
        }
        private static string EncodeEmailPhone(E2FAProviders type, string value)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                if (type == E2FAProviders.Email)
                {
                    string input = value;
                    string pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
                    result = Regex.Replace(input, pattern, m => new string('*', m.Length));
                }
                else if (type == E2FAProviders.Phone)
                {
                    result = Regex.Replace(value, @"(?<!\()\d(?!\d?$)", m => new string('*', m.Length));
                }
            }
            return result;
        }


        public async Task UpdateLastLogin()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");


                var user = await _userrepository.Query().Include(x => x.UserSites)
                    .Where(x => x.Id == _workContext.GetCurrentUserIdValue() & x.UserSites.Any(a => a.SiteId == SiteId))
                    .FirstOrDefaultAsync();
                user.LastLoginDate = DateTimeOffset.Now;
                var result = await _userManager.UpdateAsync(user);
                await _userLoginTransactionsService.AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);
                if (!result.Succeeded)
                {
                    _userLoginTransactionsService.AddLogUserLogin(user.Id, UserLoggingType.loginFailed);

                    throw new Exception("Something went wrong!");
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<GetUserByUserOptionVm> GetUserByUserOption(string userOptionValue, string userOptionKey)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            if (SiteId == 0)
            {
                throw new Exception("Site Can't be null");
            }
            var user = await _UsersSitesRepository.Query().Include(x => x.User)
                .Where(x => x.User.NormalizedUserName == userOptionValue.ToUpper()
            && SiteId == x.SiteId).Select(x => new GetUserByUserOptionVm
            {
                UserId = x.User.Id,
                FullName = x.User.FullName,
                UserName = x.User.UserName,
                Email = x.User.Email,
                Image = x.User.Image,
                UserOptions = x.User.UserOptions.Select(x => new UserOptionsVM
                {
                    PropertyName = x.PropertyName,
                    Value = x.Value,
                    ControlType = x.ControlType,
                    IsEditable = x.IsEditable,
                    IsVisible = x.IsVisible,
                    Id = x.Id

                }).ToList(),
                PhoneNumber = x.User.PhoneNumber,
                Status = x.User.Status,
                DateOfBirth = x.User.DateOfBirth,
                CreationDate = x.User.CreationDate,
                LastLogin = x.User.LastLoginDate,


            }).FirstOrDefaultAsync();
            if (user == null)
            {
                user = await _UsersSitesRepository.Query().Include(x => x.User).ThenInclude(x => x.UserOptions)
                      .Where(x => x.User.UserOptions.Any(x => x.NormalizedPropertyName == userOptionKey.ToUpper()
                      && x.NormalizedValue == userOptionValue.ToUpper())
                  && x.SiteId == SiteId).Select(x => new GetUserByUserOptionVm
                  {
                      UserId = x.User.Id,
                      FullName = x.User.FullName,
                      UserName = x.User.UserName,
                      Email = x.User.Email,
                      Image = x.User.Image,
                      UserOptions = x.User.UserOptions.Select(x => new UserOptionsVM
                      {
                          PropertyName = x.PropertyName,
                          Value = x.Value,
                          ControlType = x.ControlType,
                          IsEditable = x.IsEditable,
                          IsVisible = x.IsVisible,
                          Id = x.Id

                      }).ToList(),
                      PhoneNumber = x.User.PhoneNumber,
                      Status = x.User.Status,
                      DateOfBirth = x.User.DateOfBirth,
                      CreationDate = x.User.CreationDate,
                      LastLogin = x.User.LastLoginDate,


                  }).FirstOrDefaultAsync();
            }


            return user;
        }

        public async Task<List<EUsersTypes>> GetUserTypes()
        {
            return Enum.GetValues(typeof(EUsersTypes)).Cast<EUsersTypes>().ToList();
        }

        public async Task<CustomerGroupVM> GetUsersByGroupId(long groupId, int pageIndex, int pageSize)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var UserGroupId = await _customerGrouprepository.Query().Where(
                x => x.Id == groupId &&
                (x.SiteId == siteId || x.SiteId == null)
                ).Include(x => x.Users)
                .ThenInclude(x => x.User).ThenInclude(x => x.UserOptions)
                .Select(x => new CustomerGroupVM
                {
                    CustomerGroupId = x.Id,
                    Users = x.Users.Where(x => x.User.Status == EStatus.Active && _UsersSitesRepository.Query().Any(us => us.UserId == x.UserId)).Select(y => new UserViewModel
                    {
                        FullName = y.User.FullName,
                        UserName = y.User.UserName,
                        Email = y.User.Email,
                        Image = y.User.Image,
                        UserOptions = y.User.UserOptions,
                        PhoneNumber = y.User.PhoneNumber,
                        Status = y.User.Status,
                        UserId = y.User.Id
                    }).Skip(pageIndex * pageSize).Take(pageSize).ToList()

                }).FirstOrDefaultAsync();

            return UserGroupId;
        }



        public async Task<bool> SetFavoritePage(long ParentId)
        {
            try
            {
                var user = await _workContext.GetCurrentUserId();
                var UserObj = await _userrepository.Query().Where(x => x.Id == user).FirstOrDefaultAsync();
                UserObj.FavoritePageId = ParentId;
                _userrepository.Update(UserObj);
                await _userrepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<PasswordChangesHistoryVM>> GetPasswordChangesHistory(long UserId, long SiteId)
        {
            ///try
            //{
            //    var PasswordHistoryObj = await _UsersPasswordHistoryRepo.Query()
            //                                    .Include(x => x.UsersSites)
            //                                    .Where(x => x.UsersSites.UserId == UserId
            //                                    && x.UsersSites.SiteId == SiteId).
            //                                    Select(x => new PasswordChangesHistoryVM
            //                                    {
            //                                        Date = x.Date,
            //                                        ChangeReason = x.ChangePasswordHistory

            //                                    }).OrderBy(x=>x.Date).ToListAsync();
            //    return PasswordHistoryObj;
            //}
            //catch(Exception ex)
            //{
            //    throw ex;
            //}
            throw new NotImplementedException();
        }
        //public async Task<List<string>> GetUserTokenByUserId(long UserId)
        //{
        //    try
        //    {
        //      return await _userTokenRepository.Query().Where(x => x.UserId == UserId).Select(x => x.Value).ToListAsync();
        //    }
        //    catch(Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public async Task<UserOTPVM> GetUserTokenByUserIdAndPurpose(string ProviderValue, string Purpose)
        {
            try
            {
                return await _UserOTPService.GetUserOTPByUserIdAndPurpose(ProviderValue, Purpose, _workContext.GetCurrentSiteId());

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public async Task<UserImageVM> GetUserImageById(long UserId)
        //{
        //    try
        //    {

        //        var UserImageDetails = await _userrepository.Query()
        //            .Include(x => x.Thumbnail)
        //            .Where(x => x.Id == UserId)

        //            .Select(x => new UserImageVM
        //            {
        //                ImageId = x.ThumbnailId.Value,
        //                PublicUrl = x.Image
        //            }).FirstOrDefaultAsync();


        //        return UserImageDetails;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        public async Task TwoFactorEnabled(User user, long SiteId = 0)
        {

            try
            {
                if (SiteId == 0)
                {
                    SiteId = _workContext.GetCurrentSiteId();

                }
                var Site = await _siteRepository.Query().Where(x => x.Id == SiteId).FirstOrDefaultAsync();
                if (Site.EnableTwoFactorAuthentication)
                {
                    if (user.CustomerGroups.Any(x => x.CustomerGroup.EnableTwoFactorAuthentication))
                    {
                        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

                        if (user.TwoFactorEnabled)
                        {
                            throw new Exception("Two factor auth is required");

                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task DeleteAccount(DeleteAccountViewModel model)
        {
            try
            {

                var userId = await _workContext.GetCurrentUserId();
                var user = await _userrepository.Query().FirstOrDefaultAsync(x => x.Id == userId);
                var isMatching = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.password);
                if (user == null)
                {
                    throw new Exception("User not found!");
                }

                if ((long)isMatching <= 0)
                {
                    throw new Exception("User not found!");
                }

                user.UserName = user.Id + "Deleted" + user.UserName;
                user.NormalizedUserName = user.Id + "Deleted" + user.NormalizedUserName;

                user.Status = EStatus.Deleted;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.Now.AddYears(200);

                await _userrepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> EnableFingerPrint(FingerPublicVM model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                var ExistSignture = await _userrepository.Query().Where(x => x.FingerSign == model.PublicKey).FirstOrDefaultAsync();

                if (ExistSignture != null)
                {
                    throw new Exception("You Already Enabled The Finger Print");
                }

                User user = await _userrepository.Query().Where(x => x.Id == UserId).FirstOrDefaultAsync();

                user.FingerSign = model.PublicKey;

                await _userrepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> DisableFingerPrint()
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                User user = await _userrepository.Query().Where(x => x.Id == UserId).FirstOrDefaultAsync();

                user.FingerSign = null;

                await _userrepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private async Task DeleteAuthCookies()
        {
            await _workContext.DeleteCookie(configuration["Authentication:UserTokenCookieName"].ToString());
            await _workContext.DeleteCookie(configuration["Authentication:UserRefreshTokenCookieName"].ToString());

        }

        public async Task SetUserCulturePreference()
        {
            try
            {

                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var CultureId = _workContext.GetCurrentCultureId();

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .FirstOrDefault();

                if (UserSiteObj == null || UserId == 0)
                {
                    throw new Exception("User Not Exist");
                }

                UserSiteObj.CultureId = CultureId;

                await _UsersSitesRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task SetWarehousePreference(WarehousePreferenceViewModel model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var siteId = _workContext.GetCurrentSiteId();
                await dcmsLogService.AddInfo(userId, "Warehouse", JsonConvert.SerializeObject(model) + userId + siteId, null, siteId);
                var affected = await _UsersSitesRepository.Query()
                      .Where(x => x.UserId == userId && x.SiteId == siteId)
                      .ExecuteUpdateAsync(s => s
                          .SetProperty(x => x.WarehouseId, model.WarehouseId));
                return;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<long> GetCurrentWarehouse()
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var siteId = _workContext.GetCurrentSiteId();

                var userSite = await _UsersSitesRepository.Query()
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.SiteId == siteId)
                .Select(x => x.WarehouseId)
                .FirstOrDefaultAsync();

                var warehouseId = userSite ?? 0;

                return warehouseId;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    
    }
}
