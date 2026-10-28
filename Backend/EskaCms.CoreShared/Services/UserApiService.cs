
using EskaCms.CoreShared.Helpers;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.CoreShared.ViewModels;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EskaCMS.Core.Models;
using EskaCms.CoreShared.ViewModels;
using EskaCMS.ADIntegration.ViewModels;
using System.Text;
using System.IO;
using EskaCMS.EskaCoreIntegration.Models;
using EskaCMS.Entities.Entity;
using OfficeOpenXml;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Security.Models;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCms.CoreShared;
using Newtonsoft.Json.Linq;
using System.Globalization;
using static EskaCMS.Entities.Enum.EventEnums;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Security.Services.Interfaces;

namespace EskaCMS.CoreShared.Services
{
    public class UserApiService : IUserApiService
    {
        private FileHelper fileHelper;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<CustomerGroup> _groupRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> appSettingRepository;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly UserManager<User> _userManager;
        private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.UserOptions> _userOptionsRepository;
        private readonly ILogger<IRepository<User>> _UserRepoLogger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UserLoginTransactions> _UserLoginTransactions;
        private readonly IRepository<UsersPasswordHistory> _UserPasswordHistory;
        private static bool SyncingUsersStarted = false;
        private readonly RoleManager<Role> _roleManager;
        private readonly IRepository<PostsAuther> _PostsRepository;
        public readonly ILoyaltyWalletService _loyaltyWalletService;
        private readonly IUserSiteService _userSiteService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userServices;
        public UserApiService(
            IWebHostEnvironment webHostEnvironmen,
            IRepository<User> userRepository,
            IRepository<PostsAuther> PostsRepository,
            UserManager<User> userManager,
            IRepository<UsersSites> userSitesRepository,
            IRepository<CustomerGroup> groupRepository,
               IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository,
               IRepositoryWithTypedId<Country, string> countryRepository,
                IRepository<Order> orderRepository,
        IRepository<UsersPasswordHistory> UserPasswordHistory,
            IRepository<EskaCMS.Entities.Entity.Core.UserOptions> userOptionsRepository,
            IRepository<UserLoginTransactions> UserLoginTransactions,
            ILogger<IRepository<User>> logger,
            IServiceScopeFactory serviceScopeFactory,
              RoleManager<Role> roleManager,
        IWorkContext workContext,
        ILoyaltyWalletService loyaltyWalletService,
        IUserSiteService userSiteService,
        IHttpClientFactory httpClientFactory,
        IUserService userServices

            )
        {
            fileHelper = new FileHelper(webHostEnvironmen);
            _userRepository = userRepository;
            _userManager = userManager;
            _UserPasswordHistory = UserPasswordHistory;
            _userSitesRepository = userSitesRepository;
            _userOptionsRepository = userOptionsRepository;
            _groupRepository = groupRepository;
            appSettingRepository = _appSettingRepository;
            _UserRepoLogger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _workContext = workContext;
            _PostsRepository = PostsRepository;
            _UserLoginTransactions = UserLoginTransactions;
            _roleManager = roleManager;
            _loyaltyWalletService = loyaltyWalletService;
            _countryRepository = countryRepository;
            _userSiteService = userSiteService;
            _orderRepository = orderRepository;
            _httpClientFactory = httpClientFactory;
            _userServices = userServices;
        }

        public async Task<UserBasicInfoVM> GetBasicinfo(long userId)
        {
            var user = _userRepository
                                        .Query().AsNoTracking()
                                        .Where(x => x.Id == userId).FirstOrDefault();

            if (user == null)
                throw new Exception("User Not found");

            var basicInfo = new UserBasicInfoVM()
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth ?? null,
            };

            return basicInfo;


        }

        public async Task<bool> UpdateBasicinfo(UserBasicInfoVM model, long userId)
        {
            try
            {
                var user = _userRepository
                                        .Query()
                                        .Where(x => x.Id == userId)
                                        .FirstOrDefault();

                if (user == null)
                    throw new Exception("User Not found");


                user.DateOfBirth = model.DateOfBirth;
                user.Email = model.Email;
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.NormalizedEmail = model.Email.ToUpper();

                await _userRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }



        }
        public async Task<long> Create(UserForm model, long siteId)
        {


            CheckIfDublicatePhoneNumberAndEmail(model.Email, model.PhoneNumber);
            if (_userManager.Users.Any(x => x.UserName == model.UserName))
            {
                throw new ValidationException("UserName Already Exists");
            }
            User user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                VendorId = model.VendorId,
                TwoFactorEnabled = model.EnableTwoFactorAuthentication.HasValue ? model.EnableTwoFactorAuthentication.Value : false,
                CreatedById = model.CreatedById,
                IsADAuthRequired = model.IsADAuthRequired,
                ThumbnailId = model.ThumbnailId,
                Status = model.Status,
                ParentId=model.ParentId.HasValue &&model.ParentId!=0 ? model.ParentId : null
            };

            if (model.UserImage != null)
            {
                user.Image = fileHelper.PostFile(model.UserImage, siteId);
            }
            if (model.RoleIds != null)
            {
                user.UserRoles = AddUserRoles(model.RoleIds);
            }

            if (model.CustomerGroupIds != null)
            {
                user.CustomerGroups = AddUserGroups(model.CustomerGroupIds);
            }
            IdentityResult result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _loyaltyWalletService.CreateLoyaltyWallet(user.Id);

                var GenratedCode = Guid.NewGuid().ToString("N").Substring(0, 4);
                string userCode = $"{user.Id}{GenratedCode}";
                user.Code = userCode;

                await _userManager.UpdateAsync(user);



                await _loyaltyWalletService.ProcessRegistrationLoyaltySetups(user.Id, model.ReferrerCode);
                UsersSites UserToSite = new UsersSites { UserId = user.Id, SiteId = Convert.ToInt64(siteId), IsDefault = true };
                _userSitesRepository.Add(UserToSite);
                _userSitesRepository.SaveChanges();
                AddUserOptions(model.UserOptions, user.Id);

                return user.Id;
            }
            else
            {
                throw new ValidationException("User creation failed");
            }

        }

        private void CheckIfDublicatePhoneNumberAndEmail(string email, string phoneNumber)
        {
            try
            {
                // Both settings fetched in ONE round trip instead of two separate queries.
                var duplicateSettings = appSettingRepository.Query().AsNoTracking()
                    .Where(x => x.Id.Contains("DublicateEmail") || x.Id.Contains("DublicatePhoneNumber"))
                    .ToList();
                var IsDublicateEmail = Convert.ToBoolean(Convert.ToInt32(duplicateSettings.FirstOrDefault(x => x.Id.Contains("DublicateEmail"))?.Value));
                var IsDublicatePhoneNumber = Convert.ToBoolean(Convert.ToInt32(duplicateSettings.FirstOrDefault(x => x.Id.Contains("DublicatePhoneNumber"))?.Value));
                if (!IsDublicatePhoneNumber)
                {

                    if (_userManager.Users.Any(x => x.PhoneNumber == phoneNumber))
                    {
                        throw new ValidationException("Phone Number Already Exists");
                    }


                }
                if (!IsDublicateEmail)
                {

                    if (_userManager.Users.Any(x => x.Email == email))
                    {
                        {
                            throw new ValidationException("Email Already Exists");
                        }


                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private static List<UserRole> AddUserRoles(IList<long> rolesIds)
        {
            List<UserRole> userRoles = new List<UserRole>();

            foreach (var roleId in rolesIds)
            {
                UserRole userRole = new UserRole
                {
                    RoleId = roleId,

                };

                userRoles.Add(userRole);
            }
            return userRoles;
        }
        private static List<CustomerGroupUser> AddUserGroups(IList<long> groupIds)
        {
            List<CustomerGroupUser> userGroups = new List<CustomerGroupUser>();
            foreach (var customergroupId in groupIds)
            {
                var userCustomerGroup = new CustomerGroupUser
                {
                    CustomerGroupId = customergroupId
                };

                userGroups.Add(userCustomerGroup);
            }
            return userGroups;
        }
        private void AddUserOptions(List<UserOptionsVM> userOptions, long userId)
        {
            if (userOptions != null)
            {
                foreach (var item in userOptions)
                {
                    EskaCMS.Entities.Entity.Core.UserOptions objUserOpt = new EskaCMS.Entities.Entity.Core.UserOptions();

                    objUserOpt.PropertyName = item.PropertyName;
                    objUserOpt.Value = item.Value;
                    objUserOpt.ControlType = item.ControlType;
                    objUserOpt.IsEditable = item.IsEditable;
                    objUserOpt.IsVisible = item.IsVisible;
                    objUserOpt.UserId = userId;
                    _userOptionsRepository.Add(objUserOpt);
                    _userOptionsRepository.SaveChanges();
                }
            }
        }
        public async Task<long> Update(UserForm model, long siteId, long userId, IFormFileCollection files)
        {
            try
            {
                User user = await _userRepository.Query().Include(x => x.UserRoles)
                                                         .ThenInclude(x => x.Role)
                                                         .Include(x => x.CustomerGroups)
                                                         .Where(x => x.Id == userId).FirstOrDefaultAsync();
                user.Email = model.Email;
                user.UserName = model.UserName;
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.VendorId = model.VendorId;
                user.Type = model.UserType;
                user.Status = model.Status;
                user.ParentId=model.ParentId.HasValue &&model.ParentId!=0 ? model.ParentId : null;

                user.IsADAuthRequired = model.IsADAuthRequired;
                user.ThumbnailId = model.ThumbnailId;
                user.TwoFactorEnabled = model.EnableTwoFactorAuthentication.HasValue ? model.EnableTwoFactorAuthentication.Value : false;
                AddOrDeleteRoles(model, user);
                AddOrDeleteCustomerGroups(model, user);
                if (model.UserOptions != null)
                {
                    await _userServices.AddUpdateUserOptions(model.UserOptions, user.Id);
                  
                    
                }
                if (files.Count > 0)
                {
                    user.Image = fileHelper.UpdateFile(files[0], user.Image, siteId);
                }
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {

                    List<UsersSites> UserToSite = _userSitesRepository.Query().Where(x => x.UserId == userId).ToList();
                    var UserSiteIds = UserToSite.Select(x => x.Id).ToList();
                    var LoginTransactions = await _UserLoginTransactions.Query().Where(u => UserSiteIds.Contains(u.UserSiteId)).ToListAsync();

                    _UserLoginTransactions.RemoveRange(LoginTransactions);

                    await _UserLoginTransactions.SaveChangesAsync();

                    _userSitesRepository.RemoveRange(UserToSite);

                    var UserHistory = await _UserPasswordHistory.Query().Where(u => UserSiteIds.Contains(u.UsersSitesId)).ToListAsync();

                    _UserPasswordHistory.RemoveRange(UserHistory);

                    await _UserPasswordHistory.SaveChangesAsync();


                    for (int i = 0; i < model.AssignedSites.Count; i++)
                    {
                        bool IsDefault = false;

                        if (i == 0)
                            IsDefault = true;
                        else
                            IsDefault = false;

                        var AddUserToSite = new UsersSites { UserId = user.Id, SiteId = model.AssignedSites[i], IsDefault = IsDefault };
                        _userSitesRepository.Add(AddUserToSite);
                    }

                    _userSitesRepository.SaveChanges();


                    return user.Id;
                }
                else
                {
                    throw new ValidationException("Something went wrong");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        private async void AddOrDeleteRoles(UserForm model, User user)
        {
            foreach (var roleId in model.RoleIds)
            {
                if (user.UserRoles.Any(x => x.RoleId == roleId))
                {
                    continue;
                }

                var userRole = new UserRole
                {
                    RoleId = roleId,

                    User = user
                };
                user.UserRoles.Add(userRole);
            }

            var deletedUserRoles =
                user.UserRoles.Where(userRole => !model.RoleIds.Contains(userRole.RoleId))
                    .ToList();

            foreach (var deletedUserRole in deletedUserRoles)
            {
                if (deletedUserRole.Role.Name.ToLower().Contains("Auther".ToLower()))
                {
                    var PostAuthor = _PostsRepository.Query().Where(x => x.userId == deletedUserRole.UserId).FirstOrDefault();
                    if (PostAuthor != null)
                    {
                        _PostsRepository.Remove(PostAuthor);
                        _PostsRepository.SaveChanges();
                    }
                }


                deletedUserRole.User = null;
                user.UserRoles.Remove(deletedUserRole);
            }
        }
        private void AddOrDeleteCustomerGroups(UserForm model, User user)
        {
            if (model.CustomerGroupIds != null)
            {
                foreach (var customergroupId in model.CustomerGroupIds)
                {
                    if (user.CustomerGroups.Any(x => x.CustomerGroupId == customergroupId))
                    {
                        continue;
                    }

                    var userCustomerGroup = new CustomerGroupUser
                    {
                        CustomerGroupId = customergroupId,
                        User = user
                    };
                    user.CustomerGroups.Add(userCustomerGroup);
                }
            }


            var deletedUserCustomerGroups = user.CustomerGroups.ToList();
            if (model.CustomerGroupIds != null)
            {
                deletedUserCustomerGroups = user.CustomerGroups.Where(userCustomerGroup => !model.CustomerGroupIds.Contains(userCustomerGroup.CustomerGroupId))
            .ToList();
            }

            foreach (var deletedUserCustomerGroup in deletedUserCustomerGroups)
            {
                deletedUserCustomerGroup.User = null;
                user.CustomerGroups.Remove(deletedUserCustomerGroup);
            }
        }
        public async Task EditUserOption(UserOptionsVM userOptionsVM)
        {
            var userOptions = await _userOptionsRepository.Query().FirstOrDefaultAsync(x => x.Id == userOptionsVM.Id);
            userOptions.ControlType = userOptionsVM.ControlType;
            userOptions.IsEditable = userOptionsVM.IsEditable;
            userOptions.IsVisible = userOptionsVM.IsVisible;
            userOptions.PropertyName = userOptionsVM.PropertyName;
            userOptions.Value = userOptionsVM.Value;
            await _userOptionsRepository.SaveChangesAsync();
        }

        public async Task<List<GetUsersWithOptionsVM>> GetAllUsersOptions(GetUserOptionByPropertyVM model)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var upperPropertyName = model.PropertyName.ToUpper();

            var result = await _userSitesRepository.Query()
                                             .Where(x => x.SiteId == siteId && !x.User.UserRoles.Any(y => y.Role.NormalizedName == "GUEST"))
                                             .Include(x => x.User)
                                             .ThenInclude(x => x.UserOptions)
                                              .Where(x => x.User.Status!=EStatus.Deleted)
                                             .Select(x => new GetUsersWithOptionsVM

                                             {
                                                 UserName = x.User.UserName,
                                                 Email = x.User.Email,
                                                 UserId = x.UserId,
                                                 UserOption = x.User.UserOptions.Where(y => y.NormalizedPropertyName == upperPropertyName)
                                                                                .Select(x => new UserOptionsVM
                                                                                {
                                                                                    Id = x.Id,
                                                                                    ControlType = x.ControlType,
                                                                                    IsEditable = x.IsEditable,
                                                                                    IsVisible = x.IsVisible,
                                                                                    PropertyName = x.PropertyName,
                                                                                    Value = x.Value
                                                                                }).FirstOrDefault()
                                             }).ToListAsync();

            return result;
        }

        public async Task EditUserCurrencyAndCulture(EditUserCurrencyVM Model)
        {
            try
            {

                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = await _workContext.GetCurrentSiteIdAsync();

                var UserSiteObj = _userSitesRepository
                                    .Query()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .FirstOrDefault();

                if (UserSiteObj == null || UserId == 0)
                {
                    throw new Exception("User Not Exist");
                }

                UserSiteObj.CurrencyId = Model.CurrencyId;
                UserSiteObj.CultureId = Model.CultureId;

                await _userSitesRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> EditUserCountry(EditUserCountry Model)
        {
            var UserId = await _workContext.GetCurrentUserId();
            var SiteId = await _workContext.GetCurrentSiteIdAsync();

            var UserSiteObj = _userSitesRepository
                                .Query()
                                .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                .FirstOrDefault();

            if (UserSiteObj == null || UserId == 0)
            {
                return false;
            }

            UserSiteObj.CountryId = Model.CountryId;


            await _userSitesRepository.SaveChangesAsync();

            return true;

        }

        public async Task<UserCountryVM> GetUserCountry()
        {


            var result = await _userSiteService.GetUserCountry();

            return result;



        }


        public async Task<List<UserDDLVM>> GetUsersDDL(UserSearchTableParam<UserGetddlVM> param)
        {
            string search = param.Search.SearchText;
            long RoleId = param.Search.RoleId.HasValue ? param.Search.RoleId.Value : 0;
            long SiteId = _workContext.GetCurrentSiteId();

            var UserQuery = await _userManager.Users.Include(x => x.UserRoles)
                .AsNoTracking()
                .Where(user =>
                    user.UserSites.Any(us => us.SiteId == SiteId) &&
                    (string.IsNullOrEmpty(search) || user.FullName.ToUpper().Contains(search.ToUpper())) &&
                    user.Status != EStatus.Deleted &&
                   (RoleId == 0 || user.UserRoles.Any(x => x.RoleId == RoleId)))
                .Select(x => new UserDDLVM
                {
                    FullName = x.FullName,
                    Id = x.Id
                })
                .OrderBy(x => x.FullName)
                .Skip(param.Pagination.Number * (param.Pagination.Start - 1))
                .Take(param.Pagination.Number)
                .ToListAsync();


            return UserQuery;
        }



        public async Task DeleteUserOption(long id)
        {
            var userOption = await _userOptionsRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (userOption == null)
            {
                throw new ValidationException("User option not found");
            }

            _userOptionsRepository.Remove(userOption);
            await _userOptionsRepository.SaveChangesAsync();
        }
        public async Task Delete(long id)
        {
            User user = await _userRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                throw new ValidationException("User not found");
            }

            user.UserName = user.Id + "Deleted" + user.UserName;
            user.NormalizedUserName = user.Id + "Deleted" + user.NormalizedUserName;

            user.Status = EStatus.Deleted;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.Now.AddYears(200);

            UserDetailsVM model = await GetById(id);

            await _userRepository.SaveChangesAsync();
        }

        public async Task<string> GetUserCode(long id)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userSite = await _userSitesRepository.Query().AsNoTracking()
                                        .Where(x => x.UserId == id && x.SiteId == siteId)
                                        .Include(x => x.User)
                                        .FirstOrDefaultAsync();
            if (userSite == null)
                return String.Empty;

            var code = userSite.User.Code;
            return code;


        }

        public async Task<bool> CheckReferrerCode(ReferrerCodeVM code)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var referrerCode = _userSitesRepository.Query().AsNoTracking()
                               .Where(x => x.User.Code == code.ReferrerCode && x.User.Status == EStatus.Active && x.SiteId == siteId)
                               .FirstOrDefault();
            if (referrerCode == null)
                return false;


            return true;


        }



        public async Task<UserDetailsVM> GetById(long id)
        {

            UserDetailsVM user = await _userManager.Users
                .Include(x => x.UserRoles)
                .Include(x => x.CustomerGroups)
                .Include(x => x.UserOptions)
                .Include(x => x.Thumbnail)
                .Where(x => x.Id == id).Select(user => new UserDetailsVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    VendorId = user.VendorId,
                    EnableTwoFactorAuthentication = user.TwoFactorEnabled,
                    UserType = user.Type,
                    Status = user.Status,
                    UserId = user.Id,
                    ReferrerCode = user.Code,
                    Image = user.Thumbnail.PublicUrl,
                    IsADAuthRequired = user.IsADAuthRequired,
                    CreationDate = user.CreationDate,
                    RoleIds = user.UserRoles.Select(x => x.RoleId).ToList(),
                    CustomerGroupIds = user.CustomerGroups.Select(x => x.CustomerGroupId).ToList(),
                    UserOptions = user.UserOptions.Select(x => new UserOptionsVM
                    {
                        // Id = x.Id,
                        ControlType = x.ControlType,
                        IsEditable = x.IsEditable,
                        IsVisible = x.IsVisible,
                        PropertyName = x.PropertyName,
                        Value = x.Value
                    }).ToList(),
                    DefaultShippingAddressId = user.DefaultShippingAddressId
                }).FirstOrDefaultAsync();

            return user;


        }
        public async Task<UserGridOutputVM<UserViewModel>> GetList(
       UserSearchTableParam<UserListSearchVM> param,
       Entities.Enum.GeneralEnums.FillterType? fillterType)
        {
            UserListSearchVM search = param.Search;
            long siteId = _workContext.GetCurrentSiteId();
            long cultureId = _workContext.GetCurrentCultureId();

            // Get user IDs first
            var userIds = await GetUserIdsBasedOnOptions(search);

            if (!string.IsNullOrEmpty(search.Name))
            {
                search.Name = search.Name.ToUpper();
            }

            // Build base query
            IQueryable<User> baseQuery = _userManager.Users.AsNoTracking();

            // Apply user IDs filter
            if (userIds != null && userIds.Any())
            {
                baseQuery = baseQuery.Where(u => userIds.Contains(u.Id));
            }

            // Apply search filters without joins
            if (!string.IsNullOrEmpty(search.Name))
            {
                baseQuery = baseQuery.Where(u =>
                    u.FullName.ToUpper().Contains(search.Name) ||
                    u.UserName.ToUpper().Contains(search.Name) ||
                    u.Email.ToUpper().Contains(search.Name));
            }

            // Role and group filtering happen HERE, before the count. They used to live inside the
            // switch below - after CountAsync - so TotalRecord reported the UNFILTERED total: a
            // group matching 12 users still reported every user, and the grid paged through empty
            // pages. Filtering here also means a role or group filter applies whatever the sort is;
            // previously choosing "sort by name" silently dropped the filter entirely.
            if (search.RoleId != null && search.RoleId.Any())
            {
                baseQuery = baseQuery.Where(u => u.UserRoles.Any(ur => search.RoleId.Contains(ur.RoleId)));
            }

            // Group conventions match QueryableExtensions.FilterUsers: a null or 0 GroupId means
            // "no group filter". GroupIds allows filtering on several groups at once; it was
            // declared on UserListSearchVM but never honoured here.
            if (search.GroupId.HasValue && search.GroupId != 0)
            {
                baseQuery = baseQuery.Where(u => u.CustomerGroups.Any(cg => cg.CustomerGroupId == search.GroupId));
            }

            if (search.GroupIds != null && search.GroupIds.Any())
            {
                baseQuery = baseQuery.Where(u => u.CustomerGroups.Any(cg => search.GroupIds.Contains(cg.CustomerGroupId)));
            }

            // Count after filtering, before pagination
            var totalRecord = await baseQuery.CountAsync();

            IQueryable<User> sortedQuery;

            // This switch now only chooses the ORDER - every filter is already applied above.
            switch (fillterType)
            {
                case FillterType.Name:
                    sortedQuery = baseQuery
                        .OrderBy(u => u.FullName)
                        .ThenBy(u => u.Id);
                    break;

                case FillterType.UserRole:
                    if (search.RoleId != null && search.RoleId.Any())
                    {
                        sortedQuery = baseQuery
                            .OrderBy(u => u.UserRoles
                                .Where(ur => search.RoleId.Contains(ur.RoleId))
                                .Min(ur => (long?)ur.RoleId) ?? long.MaxValue)
                            .ThenBy(u => u.Id);
                    }
                    else
                    {
                        sortedQuery = baseQuery
                            .OrderBy(u => u.Id);
                    }
                    break;

                case FillterType.Usergroup:
                    sortedQuery = baseQuery
                        .OrderBy(u => u.CustomerGroups
                            .Min(cg => (long?)cg.CustomerGroupId) ?? long.MaxValue)
                        .ThenBy(u => u.Id);
                    break;

                default:
                    sortedQuery = baseQuery
                        .OrderByDescending(u => u.Id);
                    break;
            }

            // Get paginated user IDs only
            var paginatedUserIds = await sortedQuery
                .Skip(param.Pagination.Number * (param.Pagination.Start - 1))
                .Take(param.Pagination.Number)
                .Select(u => u.Id)
                .ToListAsync();

            if (!paginatedUserIds.Any())
            {
                return new UserGridOutputVM<UserViewModel>
                {
                    Items = new List<UserViewModel>(),
                    TotalRecord = totalRecord
                };
            }

            // Load only paginated users with related data
            // Important: Includes are done AFTER pagination IDs.
            // This avoids loading all users into memory.
            var pagedUsers = await _userManager.Users
                .AsNoTracking()
                .Where(u => paginatedUserIds.Contains(u.Id))
                .Include(u => u.Thumbnail)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CustomerGroups)
                    .ThenInclude(cg => cg.CustomerGroup)
                        .ThenInclude(cg => cg.customerGroupDetails)
                .Include(u => u.UserSites)
                .AsSplitQuery()
                .ToListAsync();

            // Keep same order as paginatedUserIds
            var userLookup = pagedUsers.ToDictionary(u => u.Id);

            var orderedUsers = paginatedUserIds
                .Where(id => userLookup.ContainsKey(id))
                .Select(id => userLookup[id])
                .ToList();

            var result = new UserGridOutputVM<UserViewModel>
            {
                Items = orderedUsers.Select(user => new UserViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Email = user.Email,
                    CreationDate = user.CreationDate,
                    Status = user.Status,
                    StatusDesc = user.Status.ToString(),
                    PublicURL = user.Thumbnail != null
                        ? user.Thumbnail.PublicUrl
                        : null,

                    UserGroup = user.CustomerGroups != null
                        ? user.CustomerGroups
                            .Select(cg =>
                                cg.CustomerGroup != null &&
                                cg.CustomerGroup.customerGroupDetails != null
                                    ? cg.CustomerGroup.customerGroupDetails
                                        .Where(cgd => cultureId == 0 || cgd.CultureId == cultureId)
                                        .Select(cgd => cgd.Name)
                                        .FirstOrDefault()
                                    : null)
                            .Where(groupName => !string.IsNullOrEmpty(groupName))
                            .ToList()
                        : new List<string>(),

                    RoleName = user.UserRoles != null
                        ? user.UserRoles
                            .Where(ur => ur.Role != null)
                            .Select(ur => ur.Role.Name)
                            .Where(roleName => !string.IsNullOrEmpty(roleName))
                            .ToList()
                        : new List<string>(),

                    RoleId = user.UserRoles != null
                        ? user.UserRoles
                            .Select(ur => ur.RoleId)
                            .ToList()
                        : new List<long>(),

                    IsLockout = user.UserSites != null &&
                                user.UserSites.Any(us =>
                                    us.SiteId == siteId &&
                                    us.IsLockout)

                }).ToList(),

                TotalRecord = totalRecord
            };

            return result;
        }
        public async Task<PaginationResult<UserListAdvanceVM>> GetListWithAdvancedSearch(UserSearchTableParam<UserListAdvanceSearchVM> param)
        {
            UserListAdvanceSearchVM search = param.Search;

            long SiteId = _workContext.GetCurrentSiteId();
            long cultureId = _workContext.GetCurrentCultureId();


            var userIds = new List<long>();

            if (search.ProductIds.Any())
            {
                var previousOrder = _orderRepository.Query()
                                                    .Where(x => x.OrderItems.Any(x => search.ProductIds.Contains(x.ProductId)))
                                                    .AsTracking()
                                                    .Select(x => x.CustomerId)
                                                    .ToList();
                userIds.AddRange(previousOrder);
            }
            string NameToUpper = "";
            if (!string.IsNullOrEmpty(search.SearchText))
            {
                NameToUpper = search.SearchText;
            }
            var UserQuery = _userManager.Users
                .Where(x => x.Status==EStatus.Active)
                 .Where(user =>
                user.UserSites.Any(us => us.SiteId == SiteId)
                && (search.RoleIds.Any() ? user.UserRoles.Any(r => search.RoleIds.Contains(r.RoleId)) : true)
                && (!string.IsNullOrEmpty(NameToUpper) ? (user.NormalizedUserName.Contains(NameToUpper) || user.FullName.Contains(NameToUpper) || user.NormalizedEmail.Contains(NameToUpper)) : true)
                && (search.GroupIds.Any() ? user.CustomerGroups.Any(r => search.GroupIds.Contains(r.CustomerGroupId)) : true)
                && (userIds.Any() ? userIds.Contains(user.Id) : true)
                  && ((search.Birthday != null && search.Birthday.HasValue) ? (user.DateOfBirth.Value.Month == search.Birthday.Value.Month && user.DateOfBirth.Value.Day == search.Birthday.Value.Day) : true)
                && ((search.CreatedAfter.HasValue ? user.CreationDate > search.CreatedAfter.Value : true) && (search.CreatedBefore.HasValue ? user.CreationDate < search.CreatedBefore.Value : true)))
                .Include(x => x.UserRoles)

                .ThenInclude(x => x.Role)
                .Include(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .ThenInclude(x => x.customerGroupDetails)
                .Include(x => x.UserGeoLocation)
                .Where(x => string.IsNullOrEmpty(param.Search.Country) || x.UserGeoLocation.Any(u => u.Country.Contains(param.Search.Country)))
                .Where(x => string.IsNullOrEmpty(param.Search.City) || x.UserGeoLocation.Any(u => u.City.Contains(param.Search.City)))
                .Where(x => string.IsNullOrEmpty(param.Search.Area) || x.UserGeoLocation.Any(u => u.Area.Contains(param.Search.Area)))
                .AsNoTracking();





            if (!UserQuery.Any())
            {
                return new PaginationResult<UserListAdvanceVM>(new List<UserListAdvanceVM>(), 0, 0);
            }

            var result = await UserQuery.Select(x => new UserListAdvanceVM
            {
                Id = x.Id,
                UserName = x.UserName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                FullName = x.FullName,
                UserGroup = x.CustomerGroups.SelectMany(x => x.CustomerGroup.customerGroupDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList(),
                RoleName = x.UserRoles.Select(x => x.Role).Select(x => x.Name).ToList(),
                addressGeoLocations = x.UserGeoLocation.Count() > 0 ? x.UserGeoLocation

               .Select(x => new Core.Models.GeoLocation.AddressGeoLocation
               {
                   City = x.City,
                   Area = x.Area,
                   Country = x.Country
               }).ToList() : new List<Core.Models.GeoLocation.AddressGeoLocation>()

            }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);


            return result;
        }
        public async Task<List<ExportUsersData>> ExportUsers(UserSearchTableParam<UserListSearchVM> param, Entities.Enum.GeneralEnums.FillterType? fillterType)
        {
            UserListSearchVM search = param.Search;

            long SiteId = _workContext.GetCurrentSiteId();

            var userIds = await GetUserIdsBasedOnOptions(search);

            var UserQuery = _userManager.Users
                .AsNoTracking()
                .Include(x => x.UserSites)
                .Include(x => x.Thumbnail)
                .Include(x => x.UserOptions)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Include(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .Where(user =>
                user.UserSites.Any(us => us.SiteId == SiteId) &&
                (string.IsNullOrEmpty(search.Name) || (user.FullName.ToUpper().Contains(search.Name.ToUpper()) || user.UserName.ToUpper().Contains(search.Name.ToUpper())))
                && ((search.GroupId == null || search.GroupId == 0) || user.CustomerGroups.Any(r => r.CustomerGroupId == search.GroupId))
                && ((search.RoleId.Count == null || search.RoleId.Count == 0) || user.UserRoles.Any(r => search.RoleId.Contains(r.RoleId)))
                && ((search.Status != null && search.Status.Any()) ? search.Status.Contains(user.Status) : true)
                && ((search.UserOptions != null && search.UserOptions.Any()) ? userIds.Contains(user.Id) : true)
                 ).AsNoTracking();



            if (fillterType == FillterType.Name)
            {
                UserQuery = UserQuery.OrderBy(x => x.FullName.ToLower());
            }

            else if (fillterType == null)
            {
                UserQuery = UserQuery.OrderByDescending(x => x.CreationDate);

            }




            var Users = await UserQuery.Select(x => new ExportUsersData
            {

                FullName = x.FullName,

                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                Blood_Type = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "BLOODTYPE").FirstOrDefault().Value,
                Nationality = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "NATIONALITY").FirstOrDefault().Value,
                National_Id = x.UserName,
                University = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "UNIVERSITY").FirstOrDefault().Value,
                TShirt_Size = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "TSHIRTSIZE").FirstOrDefault().Value,
                Gender = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "GENDER").FirstOrDefault().Value,
                Address = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "ADDRESS").FirstOrDefault().Value,
                Worked_With_Us_Before = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "ISWORKEDWITHUS").FirstOrDefault().Value,
                Register_Date = x.CreationDate.ToString("yyyy-MM-dd"),
                Day_Of_Birth = x.DateOfBirth.HasValue ? x.DateOfBirth.Value.Day : 0,
                Moth_Of_Birth = x.DateOfBirth.HasValue ? x.DateOfBirth.Value.Day : 0,
                YearOfBirth = x.DateOfBirth.HasValue ? x.DateOfBirth.Value.Day : 0,
                Event_Name = x.UserOptions.Where(y => y.UserId == x.Id && y.NormalizedPropertyName == "EVENT_NAME").FirstOrDefault().Value


            })
            //.Skip(param.Pagination.Number * (param.Pagination.Start - 1)).Take(param.Pagination.Number)
            .ToListAsync();
            //var Users = UserQuery.Skip(param.Pagination.Number * (param.Pagination.Start - 1)).Take(param.Pagination.Number).ToList();


            return Users;
        }



        public async Task<List<UserGetAllVM>> GetAll()
        {
            List<UserGetAllVM> UsersQuery = await _userManager.Users
                .Where(user => user.Status == EStatus.Active).OrderByDescending(x => x.CreationDate).Select(x => new UserGetAllVM
                {
                    Id = x.Id,
                    Name = x.FullName,
                    PhoneNumber = x.PhoneNumber,
                    UserName = x.UserName
                }).ToListAsync();

            return UsersQuery;
        }
        public async Task<PaginationResult<QuickSearchVM>> QuickSearch(UserSearchOption searchOption, long siteId)
        {
            long cultureId = _workContext.GetCurrentCultureId();
            EUsersTypes userTypes = searchOption.UserType;

            var query = _userSitesRepository.Query().Include(x => x.User)
                                                    .Include("User.UserRoles")
                                                    .Include("User.UserRoles.Role")
                                                    .Include("User.CustomerGroups")
                                                    .Include("User.CustomerGroups.CustomerGroup")
                                                    .Include("User.CustomerGroups.CustomerGroup.customerGroupDetails")
                                      .Where(x => x.SiteId == siteId && (x.User.Status != EStatus.Deleted));
            query = query.Where(x => (string.IsNullOrEmpty(searchOption.Name) ||
                      x.User.FullName.ToLower().Contains(searchOption.Name.ToLower())
                    || x.User.UserName.ToLower().Contains(searchOption.Name.ToLower())
                    || x.User.Email.ToLower().Contains(searchOption.Name.ToLower()))
                    && (x.User.Type == searchOption.UserType || searchOption.UserType == 0));



            var users = await query.Select(x => new QuickSearchVM
            {

                Id = x.Id,
                UserId = x.UserId,
                Email = x.User.Email,
                FullName = x.User.FullName,
                UserName = x.User.UserName,
                PhoneNumber = x.User.PhoneNumber,
                LastLoginDate = (DateTimeOffset)x.User.LastLoginDate,
                Roles = x.User.UserRoles.Select(x => x.Role.Name).ToList(),
                UserGroup = x.User.CustomerGroups.SelectMany(x => x.CustomerGroup.customerGroupDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList(),
            }).PaginateAsync(searchOption.PageSize, searchOption.PageNumber);

            return users;
        }


        public async Task<bool> SyncADUsers()
        {
            try
            {
                await SyncActiveDirectoryUsers();


                return true;
            }
            catch
            {
                return false;
            }
        }



        private async Task<List<ActiveDirectoryViewModel>> GetActiveDirectoryUsers()
        {
            try
            {
                var credentialsCache = new NetworkCredential("w.abuaishe", "Eska123");
                var handler = new HttpClientHandler { Credentials = credentialsCache };
                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("http://eska-intranet:2021/");
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    //GET Method  
                    HttpResponseMessage response = await client.GetAsync("api/Identity/GetDomainUsers");
                    string responseContent = await response.Content.ReadAsStringAsync();
                    EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>> returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>>>(responseContent);
                    return returnResponse.content;
                }
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        private async Task SyncActiveDirectoryUsers()
        {
            if (SyncingUsersStarted == true)
                return;

            try
            {
                SyncingUsersStarted = true;
                // This using statment is used for retreving disposed objects
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var UserRepoService = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
                    var groupRepo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerGroup>>();

                    List<ActiveDirectoryViewModel> adUsers = await GetActiveDirectoryUsers();

                    foreach (var adUser in adUsers)
                    {
                        User user = UserRepoService.Query().Include(x => x.UserRoles).Where(x => x.UserName == adUser.samAccountName).FirstOrDefault();
                        HREmployee employee = await GetEmployee(adUser.samAccountName);
                        if (employee != null)
                        {
                            adUser.voiceTelephoneNumber = employee.MOBILE;
                        }
                        else
                        {
                            Random generator = new Random();
                            String r = generator.Next(0, 1000000).ToString("D6");
                            adUser.voiceTelephoneNumber = "0781" + r;
                        }
                        var userRoles = user?.UserRoles;
                        try
                        {
                            if (user == null)
                            {
                                await CreateUser(adUser, groupRepo, UserRepoService);
                            }
                            else
                            {

                                List<HREmployee> employees = await GetEmployees(adUser.samAccountName);
                                if (user.UserRoles.Count <= 0)
                                {
                                    user.UserRoles = AddUserRoles(6);
                                    UserRepoService.SaveChanges();
                                }

                                foreach (var empl in employees)
                                {
                                    if (!string.IsNullOrEmpty(empl.USERNAME))
                                    {
                                        User emp = UserRepoService.Query().Where(x => x.UserName == empl.USERNAME).FirstOrDefault();
                                        emp.ParentId = user.Id;
                                        UserRepoService.SaveChanges();
                                        string adUserDepartment = adUser.distinguishedName.Split(',')[1];
                                        string department = adUserDepartment.Split('=')[1];
                                        //CustomerGroup customerGroup = groupRepo.Query().Where(x => x.Name == department).FirstOrDefault();

                                        //if (customerGroup != null)
                                        //{
                                        //    //customerGroup.Description=
                                        //}
                                    }
                                }
                            }
                        }
                        catch (Exception Exep)
                        {
                            if (user != null) user.UserRoles = userRoles;
                            continue;
                        }
                    }
                }

            }
            catch (Exception e)
            {

                // nothing to do here 
            }
            SyncingUsersStarted = false;
        }



        private async Task<HREmployee> GetEmployee(string username)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                List<HREmployee> respone = new List<HREmployee>();
                client.BaseAddress = new Uri("http://imsserver/AdministrationAPI/JsonServer/Intranet/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                //GET Method  
                HttpResponseMessage response = await client.GetAsync("GetEmployees?CompanyId=1&PageNumber=1&RowsCountPerPage=&EmployeeName=&EmployeeNo=&nBranch=1&nDepartment=&EmployeeStatus=2");
                string responseContent = await response.Content.ReadAsStringAsync();
                CoreResponse<List<HREmployee>> returnResponse = JsonSerializer.Deserialize<CoreResponse<List<HREmployee>>>(responseContent);
                HREmployee employee = returnResponse.content.Where(x => x.USERNAME == username).FirstOrDefault();

                return employee;
            }

        }
        private async Task<List<HREmployee>> GetEmployees(string username)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                List<HREmployee> respone = new List<HREmployee>();
                client.BaseAddress = new Uri("http://imsserver/AdministrationAPI/JsonServer/Intranet/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                //GET Method  
                HttpResponseMessage response = await client.GetAsync("GetEmployees?CompanyId=1&PageNumber=1&RowsCountPerPage=&EmployeeName=&EmployeeNo=&nBranch=1&nDepartment=&EmployeeStatus=2");
                string responseContent = await response.Content.ReadAsStringAsync();
                CoreResponse<List<HREmployee>> returnResponse = JsonSerializer.Deserialize<CoreResponse<List<HREmployee>>>(responseContent);
                //Core.Models.HREmployee employee = returnResponse.Data.Where(x => (x.USERNAME != null) && (x.USERNAME.ToUpper() == username.ToUpper())).FirstOrDefault();
                HREmployee employee = returnResponse.content.Where(x => x.USERNAME == username).FirstOrDefault();
                if (employee != null)
                {
                    List<HREmployee> employees = returnResponse.content.Where(x => x.MANAGER_ID == employee.ID).ToList();
                    return employees;
                }
                return respone;
            }

        }

        private async Task CreateUser(ActiveDirectoryViewModel adUser, IRepository<CustomerGroup> groupRepository, IRepository<User> userRepository)
        {


            try
            {
                User user = new User
                {
                    UserName = adUser.samAccountName,
                    Email = adUser.emailAddress,
                    FullName = adUser.displayName,
                    Type = Entities.Enum.GeneralEnums.EUsersTypes.Customer,
                    CreatedById = 1,
                    IsADAuthRequired = true,
                    PhoneNumber = adUser.voiceTelephoneNumber,
                    Status = Entities.Enum.GeneralEnums.EStatus.Active,
                    ConcurrencyStamp = "05286794-6c65-4c3e-8de2-e6a10b85d050",
                    PasswordHash = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
                    PhoneNumberConfirmed = false,
                    SecurityStamp = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw=="
                };
                string adUserDepartment = adUser.distinguishedName.Split(',')[1];
                string department = adUserDepartment.Split('=')[1];
                //  CustomerGroup customerGroup = groupRepository.Query().Where(x => x.Name == department).FirstOrDefault();
                //user.UserRoles = AddUserRoles(6);
                //if (customerGroup == null)
                //{
                //    long groupId = CreateGroup(department, groupRepository);
                //    user.CustomerGroups = AddUserGroups(groupId);
                //}
                //else
                //{
                //    user.CustomerGroups = AddUserGroups(customerGroup.Id);
                //}
                userRepository.Add(user);
                userRepository.SaveChanges();
                UsersSites UserToSite = new UsersSites { UserId = user.Id, SiteId = 1, IsDefault = true };
                _userSitesRepository.Add(UserToSite);
                _userSitesRepository.SaveChanges();

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        private static List<CustomerGroupUser> AddUserGroups(long groupId)
        {
            List<CustomerGroupUser> userGroups = new List<CustomerGroupUser>();
            CustomerGroupUser userCustomerGroup = new CustomerGroupUser
            {
                CustomerGroupId = groupId
            };

            userGroups.Add(userCustomerGroup);
            return userGroups;
        }

        private static List<UserRole> AddUserRoles(long roleId)
        {
            List<UserRole> userRoles = new List<UserRole>();
            UserRole userRole = new UserRole
            {
                RoleId = roleId
            };
            userRoles.Add(userRole);
            return userRoles;
        }

        //private long CreateGroup(string name, IRepository<CustomerGroup> groupRepository)
        //{
        //    CustomerGroup group = new CustomerGroup
        //    {
        //        CreatedOn = DateTime.Now,
        //        Description = name,
        //        IsActive = true,
        //        IsDeleted = false,
        //        LatestUpdatedOn = DateTime.Now,
        //        Name = name,
        //        SiteId = 1,
        //        FireBaseTopic = "ADDepartment",
        //        Email = "f.sirhan@eskadenia.com",
        //        MaxSessionsNumber = 4,
        //        PasswordExpiryInDays = 30
        //    };

        //    groupRepository.Add(group);
        //    groupRepository.SaveChanges();

        //    return group.Id;
        //}
        private async Task<List<User>> ImportFile(IFormFile file, UploadUsers uploadUsers)
        {
            List<User> List = new List<User>();
            var SiteId = _workContext.GetCurrentSiteId();
            long UserId = await _workContext.GetCurrentUserId();


            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelPackage.LicenseContext = LicenseContext.Commercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var IsNull = worksheet.Cells[row, 1];

                        if (string.IsNullOrWhiteSpace(IsNull.Value?.ToString()))
                        {
                            continue;
                        }

                        List.Add(new User
                        {
                            Type = (EUsersTypes)Convert.ToInt32(worksheet.Cells[row, 4].Value.ToString()),
                            CreatedById = UserId,
                            FullName = worksheet.Cells[row, 1].Value.ToString().Trim().Split("@")[0],
                            CreationDate = DateTimeOffset.Now,
                            Status = EStatus.Active,
                            StatusDate = DateTimeOffset.Now,
                            UserName = worksheet.Cells[row, 2].Value.ToString().Trim(),
                            Email = worksheet.Cells[row, 2].Value.ToString().Trim(),
                            PasswordHash = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
                            SecurityStamp = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
                            PhoneNumber = worksheet.Cells[row, 3].Value.ToString().Trim(),
                            TwoFactorEnabled = false,
                            LockoutEnabled = false,
                            UserRoles = AddUserRoles(uploadUsers.RoleIds),
                            CustomerGroups = AddUserGroups(uploadUsers.CustomerGroupIds),
                            PhoneNumberConfirmed = false,
                            lastPasswordUpdate = DateTime.Now,

                        }
                          );
                    }
                }
            }
            return List;
        }

        public async Task<bool> UploadUsers(IFormFile formFile, UploadUsers uploadUsers)
        {
            try
            {
                var Users = await ImportFile(formFile, uploadUsers);


                _userRepository.AddRange(Users);

                await _userRepository.SaveChangesAsync();

                await UploadUsersToSite(uploadUsers.SiteIds.FirstOrDefault());


                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private async Task<bool> UploadUsersToSite(long siteId)
        {
            try
            {
                var UserSite = new List<UsersSites>();
                var Users = await _userRepository.Query().Include(x => x.UserSites).Where(y => !y.UserSites.Any(x => x.UserId == y.Id)).ToListAsync();

                foreach (var user in Users)
                {
                    UserSite.Add(new UsersSites { UserId = user.Id, IsDefault = true, SiteId = siteId });

                }

                _userSitesRepository.AddRange(UserSite);
                await _userSitesRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async Task<List<long>> GetUserIdsBasedOnOptions(UserListSearchVM search)
        {
            var userIds = new List<long>();
            if (search.UserOptions is not null && search.UserOptions.Any())
            {

                var dataForEachKey = new Dictionary<string, List<long>>();
                var uppercasedOptions = search.UserOptions
                                                .Select(option => new
                                                {
                                                    Key = option.Key.ToUpper(),
                                                    Value = option.Value.ToUpper()
                                                })
                                                .ToList();


                foreach (var useroption in uppercasedOptions)
                {

                    var query = _userOptionsRepository.Query()
                                                      .Where(u => u.NormalizedPropertyName.Contains(useroption.Key) && u.NormalizedValue.Contains(useroption.Value))
                                                      .AsNoTracking();

                    var userIdsBasedOnOptions = await query.GroupBy(x => x.UserId)
                                                           .Select(x => x.Key)
                                                           .ToListAsync();

                    dataForEachKey.Add(useroption.Key, userIdsBasedOnOptions);

                }

                if (dataForEachKey.Count > 0)
                {
                    userIds = dataForEachKey.Values.First();
                    foreach (var userIDs in dataForEachKey.Values.Skip(1))
                    {
                        userIds = userIds.Intersect(userIDs).ToList();
                    }
                }


            }
            return userIds;
        }
        public async Task<PaginationResult<DepartmentEmployeeViewModel>> GetAllEmployees(FilterUserVM model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var cultureId = _workContext.GetCurrentCultureId();
            var groupTypeId = await appSettingRepository.Query()
                .Where(x => x.Id == "DepartmentsGroupTypeId" && x.SiteId == siteId)
                .Select(x => x.Value).FirstOrDefaultAsync();

            string username = model.Username?.ToUpper() ?? string.Empty;
            int duration = model.Duration ?? 30;

            var today = DateTime.Today;
            var toDate = today.AddDays(duration);
            var fromDate = today.AddDays(-duration);

            var data = await _groupRepository.Query()
                .AsNoTracking()
                .Include(x => x.customerGroupDetails)
                .Include(x => x.Users)
                    .ThenInclude(u => u.User)
                        .ThenInclude(u => u.UserOptions)
                .Where(x => x.GroupTypeId == Convert.ToInt32(groupTypeId))
                .ToListAsync();

            var allEmployees = data

                .SelectMany(group => group.Users.Select(u =>
                {
                    var options = u.User.UserOptions;
                    string GetOption(string key) =>
                        options.FirstOrDefault(o => o.NormalizedPropertyName == key)?.Value;

                    var StartDate = ParseFlexibleDate(GetOption("START_DATE"));
                    var birthDate = ParseFlexibleDate(GetOption("BIRTHDATE"));

                    return new DepartmentEmployeeViewModel
                    {
                        DepartmentCode = JObject.Parse(group.AdditionalInfo ?? "{}")["DepartmentId"]?.ToString(),
                        DepartmentId = group.Id,
                        DepartmentName = group.customerGroupDetails.FirstOrDefault(d => d.CultureId == cultureId)?.Name,
                        FullName = u.User.FullName,
                        Username = u.User.UserName,
                        Email = u.User.Email,
                        CompanyEmail = u.User.Email,
                        Mobile = u.User.PhoneNumber,
                        EmployeeRank = GetOption("EMPLOYEE_RANK"),
                        JobTitleName = GetOption("JOB_TITLE_NAME"),
                        EmployeeId = GetOption("EMPLOYEEID"),
                        PhotoPath = GetOption("PHOTO_PATH"),
                        ManagerId = int.TryParse(GetOption("MANAGER_ID"), out var mangId) ? mangId : (int?)null,
                        Name2 = GetOption("NAME2"),
                        Phone2 = GetOption("PHONE2"),
                        AvailabilityStatus = int.TryParse(GetOption("AVAILABILITYSTATUS"), out var avail) ? avail : (int?)null,
                        StartDate = StartDate,
                        DateOfBirth = birthDate,
                        Status = u.User.Status
                    };
                }))
                .Where(x => x.Status == EStatus.Active)
                .Where(x =>
                    (!model.DepatmentId.HasValue || x.DepartmentCode == model.DepatmentId.ToString()) &&
                    (string.IsNullOrEmpty(username) || x.Username.ToUpper().Contains(username) || x.FullName.ToUpper().Contains(username)))
                .OrderBy(x => x.StartDate)
                .ToList();

            IEnumerable<DepartmentEmployeeViewModel> filtered = allEmployees;

            switch (model.SearchType)
            {
                case ESearchType.NewEmployees:
                    filtered = filtered
                        .Where(e => e.StartDate.HasValue)
                        .Where(e =>
                            e.StartDate.Value.Date >= fromDate &&
                            e.StartDate.Value.Date <= today)
                        .OrderByDescending(x => x.StartDate)
;
                    break;

                case ESearchType.AnniversaryEmployees:
                    filtered = filtered
                        .Where(e => e.StartDate.HasValue)
                        .Where(e =>
                        {
                            var start = e.StartDate.Value.Date;
                            var firstAnniversary = start.AddYears(1);

                            return firstAnniversary >= today && firstAnniversary <= today.AddDays(duration);
                        });
                    break;


                case ESearchType.BirthDate:
                    filtered = filtered
                    .Where(e => e.DateOfBirth.HasValue)
                    .Where(e =>
                    {
                        var dob = e.DateOfBirth.Value;
                        var birthdayThisYear = new DateTime(today.Year, dob.Month, dob.Day);

                        if (birthdayThisYear < today)
                            birthdayThisYear = birthdayThisYear.AddYears(1);

                        return birthdayThisYear >= today && birthdayThisYear <= toDate;
                    })
                  .OrderBy(e => e.DateOfBirth.Value.Month)
                  .ThenBy(e => e.DateOfBirth.Value.Day)
                    ;
                    break;
            }


            var totalCount = filtered.Count();
            var paged = filtered
                .Skip((model.Pagenumber - 1) * model.Pagesize)
                .Take(model.Pagesize)
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)model.Pagesize);
            return new PaginationResult<DepartmentEmployeeViewModel>(paged, totalPages, totalCount);
        }

        private static readonly string[] EmployeeDateFormats =
        {
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fffffff"
        };

        /// <summary>
        /// Parses a stored user-option date. These are calendar dates (birth date, start date),
        /// not instants, so the result carries a zero offset - attaching the server's local
        /// offset would shift the date by a day once it is serialized or converted to UTC.
        /// </summary>
        private static DateTimeOffset? ParseFlexibleDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim();

            // ignore invalid defaults
            if (raw == "0" || raw.StartsWith("0001"))
                return null;

            // Try exact formats
            if (DateTime.TryParseExact(raw, EmployeeDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt1))
            {
                return new DateTimeOffset(dt1.Date, TimeSpan.Zero);
            }

            // Fallback to general parsing
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt2))
                return new DateTimeOffset(dt2.Date, TimeSpan.Zero);

            return null;
        }

        public async Task<List<UserDetailsVM>> GetUserChildren(long id)
        {
            try
            {

                var user = await _userManager.Users
                    .Include(x => x.UserRoles)
                    .Include(x => x.CustomerGroups)
                    .Include(x => x.UserOptions)
                    .Include(x => x.Thumbnail)
                    .AsNoTracking()
                    .Where(x => x.ParentId == id).Select(user => new UserDetailsVM
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        UserName = user.UserName,
                        PhoneNumber = user.PhoneNumber,
                        VendorId = user.VendorId,
                        EnableTwoFactorAuthentication = user.TwoFactorEnabled,
                        UserType = user.Type,
                        Status = user.Status,
                        UserId = user.Id,
                        ReferrerCode = user.Code,
                        Image = user.Thumbnail.PublicUrl,
                        IsADAuthRequired = user.IsADAuthRequired,
                        CreationDate = user.CreationDate,
                        RoleIds = user.UserRoles.Select(x => x.RoleId).ToList(),
                        CustomerGroupIds = user.CustomerGroups.Select(x => x.CustomerGroupId).ToList(),
                        UserOptions = user.UserOptions.Select(x => new UserOptionsVM
                        {
                            ControlType = x.ControlType,
                            IsEditable = x.IsEditable,
                            IsVisible = x.IsVisible,
                            PropertyName = x.PropertyName,
                            Value = x.Value
                        }).ToList(),
                        DefaultShippingAddressId = user.DefaultShippingAddressId
                    }).ToListAsync();

                return user;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }

}
