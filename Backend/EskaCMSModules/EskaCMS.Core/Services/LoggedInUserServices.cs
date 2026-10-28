using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels.LoggedInUser;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using System.Net;

namespace EskaCMS.Core.Services
{
    public class LoggedInUserServices : ILoggedInUserService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();

        private readonly IRepositoryWithTypedId<CurrentLoggedInUsers, string> _CurrentLoggedInUsers;
        private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<CustomerGroup> _CustomerGroupUser;
        private readonly IRepository<UsersSites> _UserSitesRepository;
        private readonly IServiceProvider _serviceProvider;

        public LoggedInUserServices(IRepositoryWithTypedId<CurrentLoggedInUsers, string> CurrentLoggedInUsers,
                                    IRepository<SitesHostNames> SitesHostNameRepository
                                      , IWorkContext workContext
            , IRepository<CustomerGroup> CustomerGroupUser
                                  , IRepository<UsersSites> UsersSitesRepository, IServiceProvider serviceProvider
                                    )
        {
            _CurrentLoggedInUsers = CurrentLoggedInUsers;
            _SitesHostNameRepository = SitesHostNameRepository;
            _workContext = workContext;
            _CustomerGroupUser = CustomerGroupUser;
            _UserSitesRepository = UsersSitesRepository;
            _serviceProvider = serviceProvider;
        }
        
        public async Task<bool> AddCurrentLoggedInUsers(string token, ETypeLogin Source, string RefreshToken, User user, long SiteId)
        {
            try
            {

                var SiteHostNames = await _SitesHostNameRepository.Query().Where(x => x.SiteId == SiteId).FirstOrDefaultAsync();


                var ClientIpAddress = _workContext.GetIPAddress();
                var sessionLock = SessionLocks.GetOrAdd($"{user.Id}:{ClientIpAddress}", _ => new SemaphoreSlim(1, 1));

                await sessionLock.WaitAsync();
                try
                {
                    await CheckUserMaxSessions(user);
                    await RemoveSessionIfExist(user.Id, ClientIpAddress);
                    CurrentLoggedInUsers CurrentLoggedInUsers;

                    CurrentLoggedInUsers = new CurrentLoggedInUsers
                    {

                        UserId = user.Id,
                        LoginDate = DateTimeOffset.Now,
                        SiteId = SiteId,
                        IsLookedOut = false,
                        HostnameId = SiteHostNames?.Id,
                        ApiToken = token,
                        RefreshTokenHash = RefreshToken,
                        ClientIP = ClientIpAddress,
                        UserType = user.Type,
                        TypeLogin = Source,
                        LoginType = LoginType.DirectLogin,
                        KillSession = false,

                    };

                    _CurrentLoggedInUsers.Add(CurrentLoggedInUsers);
                    await _CurrentLoggedInUsers.SaveChangesAsync();
                }
                finally
                {
                    sessionLock.Release();
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async Task<bool> RemoveSessionIfExist(long UserId, string Ip)
        {
            try
            {
                var Sessions = await _CurrentLoggedInUsers.Query()
                    .Where(x => x.UserId == UserId && x.ClientIP == Ip)
                    .FirstOrDefaultAsync();

                if (Sessions != null)
                {
                    _CurrentLoggedInUsers.Remove(Sessions);
                    try
                    {
                        await _CurrentLoggedInUsers.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return true;
                    }
                }
                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async Task<bool> CheckUserMaxSessions(User user)
        {
            try
            {
                var ClientIp = _workContext.GetIPAddress();
                var loggedInUser = await _CurrentLoggedInUsers.Query().AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

                var MaxSessions = await _CustomerGroupUser.Query().Include(x => x.Users).Where(X => X.Users.Any(y => y.UserId == user.Id)).Select(x => x.MaxSessionsNumber).FirstOrDefaultAsync();

                if (MaxSessions < loggedInUser.Count() + 1)
                {
                    throw new Exception("You Can  Not login Now beacuse You Have Multible login In Many Devices");

                }

                var lastSession = loggedInUser.FirstOrDefault(x => x.ClientIP == ClientIp);
                if (lastSession != null && lastSession.KillSession)
                {
                    throw new Exception("you are not able to login ");
                }
                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    
        public async Task<bool> CheckIfLookedOutUser(long UserId, long SiteId)
        {
            try
            {
                var IsLockout = await _UserSitesRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == UserId && x.SiteId == SiteId)
                    .Select(x=>x.IsLockout)
                    .FirstOrDefaultAsync();

                return IsLockout;

            }
            catch (Exception e)
            {

                throw e;
            }

        }
        public async Task<bool> IsCurrentUserLooked()
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            long UserId = await _workContext.GetCurrentUserId();


            return await CheckIfLookedOutUser(UserId, SiteId);
        }
        public async Task<bool> ChangeLookedOutUserStatus(long Id, bool LookedOut)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserSite = await _UserSitesRepository
                                                .Query()
                                                .Include(x => x.User)
                                                .ThenInclude(x => x.CustomerGroups)
                                                .ThenInclude(x => x.CustomerGroup)
                                                .Where(x => x.UserId == Id && x.SiteId == SiteId)
                                                .FirstOrDefaultAsync();

                if (UserSite == null)
                {
                    throw new Exception("User does not exist on this site");
                }


                UserSite.IsLockout = LookedOut;
                if (UserSite.IsLockout == true)
                {
                    UserSite.User.LockoutEnd = DateTimeOffset.Now + (UserSite.User.CustomerGroups == null || UserSite.User.CustomerGroups.Count == 0 ? (DateTimeOffset.Now.AddDays(7) - DateTimeOffset.Now) : UserSite.User.CustomerGroups.Max(x => x.CustomerGroup.LockoutDuration));
                }
                else
                {
                    UserSite.User.LockoutEnd = DateTimeOffset.Now.AddSeconds(-1);
                }
                await _UserSitesRepository.SaveChangesAsync();

                return true;



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<LoggedInUserVM>> GetAll(SearchParamsVM searchParams)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();

            var TypeLogin = searchParams.TypeLogin.GetValueOrDefault();
            var SearchText = searchParams.SearchText;

            var Query = _CurrentLoggedInUsers.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == SiteId && !x.KillSession);

            if (TypeLogin != 0)
                Query = Query.Where(x => x.TypeLogin == TypeLogin);

            if (!string.IsNullOrEmpty(SearchText))
                Query = Query.Where(x =>
                    EF.Functions.Like(x.User.UserName, $"%{SearchText}%") ||
                    EF.Functions.Like(x.User.Email, $"%{SearchText}%") ||
                    EF.Functions.Like(x.User.FullName, $"%{SearchText}%"));

            return await Query
                .OrderByDescending(x => x.LoginDate)
                .Select(x => new LoggedInUserVM
                {
                    Id = x.Id,
                    UserId = x.User.Id,
                    UserName = x.User.UserName,
                    ClientIP = x.ClientIP,
                    FullName = x.User.FullName,
                    LoginDate = x.LoginDate,
                    UserType = x.UserType,
                    Email = x.User.Email,
                    Source = x.TypeLogin.ToString(),
                    KillSession = x.KillSession,
                    SiteName = x.Site.Name,
                    Roles = x.User.UserRoles.Select(r => new RolesVM
                    {
                        RoleName = r.Role.Name,
                        RoleId = r.Role.Id
                    }).ToList(),
                    IsLookedOut = x.IsLookedOut,
                    LoginType = x.LoginType
                })
                .AsSplitQuery()
                .PaginateAsync(searchParams.PageSize.GetValueOrDefault(10), searchParams.PageNumber.GetValueOrDefault());
        }

        public async Task<List<LoggedInUserVM>> GetLoggedInUsers(long UserId)
        {
            try
            {
                var loggedInUsers = await _CurrentLoggedInUsers.Query().Where(x => x.UserId == UserId).Include(x => x.User)
                                                       .ThenInclude(x => x.UserRoles)
                                                       .ThenInclude(x => x.Role).Select(x => new LoggedInUserVM
                                                       {
                                                           Id = x.Id,
                                                           UserId = x.UserId,
                                                           LoginDate = x.LoginDate,
                                                           LoginType = x.LoginType,
                                                           UserName = x.User.UserName,
                                                           FullName = x.User.FullName,
                                                           Email = x.User.Email,
                                                           UserType = x.UserType,
                                                           KillSession = x.KillSession,
                                                           Roles = x.User.UserRoles.Select(X => new RolesVM
                                                           {
                                                               RoleName = X.Role.Name,
                                                               RoleId = X.Role.Id
                                                           }).ToList()

                                                       }).OrderByDescending(d => d.LoginDate).ToListAsync();
                return loggedInUsers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> RemoveloggedInUser(long UserId, string ClientIp)
        {
            using (var scope = _serviceProvider.CreateScope())
            {

                try
                {

                    var CurrentLoggedInUser = await _CurrentLoggedInUsers.Query().Where(x => x.ClientIP == ClientIp && x.UserId == UserId).ToListAsync();

                    if (CurrentLoggedInUser.Any())
                    {
                        _CurrentLoggedInUsers.RemoveRange(CurrentLoggedInUser);
                        await _CurrentLoggedInUsers.SaveChangesAsync();

                    }

                    return true;

                }
                catch (Exception ex)
                {

                    throw ex;


                }
            }
        }

        public async Task<bool> RemoveloggedInUser(List<long> UsersId, string UserIp)
        {
            using (var scope = _serviceProvider.CreateScope())
            {

                try
                {

                    var CurrentLoggedInUser = await _CurrentLoggedInUsers.Query().Where(x => UsersId.Contains(x.UserId) && x.ClientIP == UserIp).ToListAsync();

                    if (CurrentLoggedInUser.Any())
                    {
                        _CurrentLoggedInUsers.RemoveRange(CurrentLoggedInUser);
                        await _CurrentLoggedInUsers.SaveChangesAsync();

                    }

                    return true;

                }
                catch (Exception ex)
                {

                    throw ex;


                }
            }
        }
        public async Task<bool> CheckUserIsKilled(string ClientIp, long UserId)
        {
            try
            {
                return await _CurrentLoggedInUsers.Query()
                    .AsNoTracking()
                    .Where(x => x.ClientIP == ClientIp && x.UserId == UserId)
                    .Select(x => x.KillSession)
                    .FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
       
        
        
        public async Task<bool> KillSessionsOnCurrentUser(string id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {

                try
                {

                    var query = await _CurrentLoggedInUsers.Query().Where(x => x.Id == id.Trim()).FirstOrDefaultAsync();
                    query.KillSession = true;
                    await _CurrentLoggedInUsers.SaveChangesAsync();

                    return true;

                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }

        #region This Services For User When Logg in And Can Remove His Session
        public async Task<List<SessionsUserViewModel>> GetUserSessions(string UserName)
        {
            try
            {
                var UserNameToLower = UserName.Trim().ToLower();
                var Query = await _CurrentLoggedInUsers.Query().Include(x => x.User).Where(x => x.User.UserName.ToLower() == UserNameToLower).Select(x => new SessionsUserViewModel
                {
                    Channel = x.TypeLogin,
                    Email = x.User.Email,
                    FullName = x.User.FullName,
                    IpAddress = x.ClientIP,
                    Id = x.Id,
                    LoginDate=x.LoginDate
                }).ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }

        public async Task<bool> RemoveSession(string Id)
        {
            try
            {
                var Query = await _CurrentLoggedInUsers.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
             
                if (Query == null)
                {

                    throw new Exception("Not Found This Session");
                }

                _CurrentLoggedInUsers.Remove(Query);

               await _CurrentLoggedInUsers.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        #endregion

        
    }

}

