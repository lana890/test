using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Helpers;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity;
using EskaCMS.UserDashboard.Helpers;
using EskaCMS.Core.Events;
using MediatR;

using Microsoft.Extensions.Configuration;
using EskaCMS.UserDashboard;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.UserDashboard.Services
{
    public class UserDashboardService : IUserDashboardService 
    {
        public IRepository<UserVisits> _UserVisitRepo { get; set; }
        public IRepository<User> _UserRepo { get; set; }
        
        public IRepository<UserNavigations> _UserNavigationRepo { get; set; }
        public IRepositoryWithCustomId<BrowserType> _BrowserTypeRepo { get; set; }
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMediator _mediator;
        private IWorkContext _workContext;
        private readonly IConfiguration _configuration;
        private static readonly double MAX_ESTIMATION_TIME_PER_VISITE = 15; // in minutes 
        private readonly string OUTSOURCE = "OutSource";
        public UserDashboardService(IRepository<UserVisits> UserVisitRepo,
            IServiceScopeFactory serviceScopeFactory,
            IRepository<UserNavigations> UserNavigationRepo,
            IRepository<User> UserRepo,
             IMediator mediator,
            IWorkContext workContext
            , IConfiguration Configuration, IRepositoryWithCustomId<BrowserType> browserTypeRepo)
        {
            _UserVisitRepo = UserVisitRepo;
            _serviceScopeFactory = serviceScopeFactory;
            _UserNavigationRepo = UserNavigationRepo;
            _workContext = workContext;
            _configuration = Configuration;
            _UserRepo = UserRepo;
            _mediator = mediator;
            _BrowserTypeRepo = browserTypeRepo;
        }


        public async Task<bool> ExecuteTakeNavigationSnapShot(UserNavigationVm userNavigationVm, SlugsTypes SlugType,long? ProductId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                var IPAdress = _workContext.GetIPAddress();
                await TakeNavigationSnapShot(userNavigationVm, IPAdress, SlugType, UserId, SiteId);
                if (SlugType == SlugsTypes.Product)
                {
                    await _mediator.Publish(new EntityViewed { EntityId = ProductId.HasValue ? ProductId.Value : 0, EntityTypeId = "Product" });
                }


                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }



        public async Task<List<long>> CheckLastFlowUser(long userId,string IpAddress)
        {
            try
            {
                var TimeNow = DateTimeOffset.Now;
                var PeriodExpiry = int.Parse(_configuration.GetSection("LoggedInUserConfig:LoggedInUserPeriod").Value);


                var query = await _UserNavigationRepo.Query().AsNoTracking()
                                                     .Where(x => userId == x.UserId
                                                      && x.CreationDate.AddMinutes(PeriodExpiry) < TimeNow)
                                                     .OrderByDescending(x => x.Id)
                                                     .ToListAsync();

                var result = query
                    .Where(x => string.Equals(x.UserIp, IpAddress, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.UserId)
                    .ToList();

                if (result.Count > 0)
                {
                    return result;
                }
                else
                {
                    return new List<long>();
                }
            }
            catch (Exception ex)
            {

                return new List<long>();
            }     
        }

        public async Task<List<string>> GetBrowsers()
        {
           return await _BrowserTypeRepo.Query().Select(x=>x.Name).Distinct().ToListAsync();
        }

        public async Task<GetDashBoardDataVM> GetNewAndReturningUsers(GetDashBoardDataVM data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!data.FromDate.HasValue || !data.ToDate.HasValue)
                throw new ArgumentException("FromDate and ToDate are required.");

            var fromDate = data.FromDate.Value.Date;
            var toDateExclusive = data.ToDate.Value.Date.AddDays(1);
            var siteId = data.SiteId;

            var result = new GetDashBoardDataVM
            {
                FromDate = data.FromDate,
                ToDate = data.ToDate
            };

            // New users
            result.NumberOfNewUsers = await _UserRepo.Query()
                .AsNoTracking()
                .Where(u =>
                    u.CreationDate >= fromDate &&
                    u.CreationDate < toDateExclusive &&
                    u.UserSites.Any(us => us.SiteId == siteId))
                .CountAsync();

            // Returning users
            result.NumberOfReturningUsers = await _UserNavigationRepo.Query()
                .AsNoTracking()
                .Where(n =>
                    n.SiteId == siteId &&
                    n.CreationDate >= fromDate &&
                    n.CreationDate < toDateExclusive &&
                    n.UserId != null)
                .GroupBy(n => n.UserId)
                .Select(g => g.Key)
                .CountAsync();

            return result;
        }
        public async Task<Dictionary<string, UsersFlowVm>> GetUsersFlow(PeriodVM Period)
        {
            try
            {
                var Result = new Dictionary<string, UsersFlowVm>();

                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var UsersNavigations = await _UserNavigationRepo
                    .Query()
                    .AsNoTracking()
                    .Where(v => v.SiteId == SiteId
                                && (Period.UserId == null || v.UserId == Period.UserId)
                                &&(Period.RoleId==0||v.User.UserRoles.Any(x=>x.RoleId==Period.RoleId))
                                && (v.CreationDate.Date >= Period.FromDate.Value.Date)
                                && (v.CreationDate.Date <= Period.ToDate.Value.Date)
                                && (string.IsNullOrEmpty(Period.BrowserType) || v.BrowserName.ToLower() == Period.BrowserType.ToLower()))
                    .ToListAsync();

                UsersNavigations = UsersNavigations
                    .Where(v => !string.IsNullOrWhiteSpace(v.PreviousSlug) &&
                                !string.Equals(v.PreviousSlug, OUTSOURCE, StringComparison.OrdinalIgnoreCase))
                    .ToList();


                // Precompute the distinct-user count per (Slug, PreviousSlug) transition ONCE.
                // Previously this was recomputed by re-scanning the whole list inside the loop
                // (O(n^2)); this dictionary gives the identical counts in O(n).
                var distinctUsersByTransition = UsersNavigations
                    .GroupBy(x => new { x.Slug, x.PreviousSlug })
                    .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

                foreach (var item in UsersNavigations)
                {
                    var key = item.PreviousSlug + '&' + item.Slug;
                    if (Result.ContainsKey(key))
                    {
                        
                        Result[key].Transactions = Result[key].Transactions + 1;
                        
                    }
                    else
                    {
                        Result.Add(key, new UsersFlowVm
                        {
                            Transactions = 1,
                            Users = distinctUsersByTransition[new { item.Slug, item.PreviousSlug }]
                        }
                                  );

                   
                    }
                    
                }

                return Result;  // await BuildGraphAsync(UsersNavigations);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public async Task<List<UserVisitVM>> GetUserVisits(long UserId)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                return await  _UserVisitRepo
                                .Query()
                                .Where(v => v.SiteId == SiteId && v.UserId == UserId)
                                .Include(v=>v.UserNavigations)
                                .Select(v=>new UserVisitVM()
                                {
                                    CreationDate=v.CreationDate,
                                    Id=v.Id,
                                    NavigationsCount = v.UserNavigations.Count
                                })
                                .OrderByDescending(v => v.Id)
                                .Take(10)
                                .ToListAsync();
                 
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        


        public async Task<List<UserNavigations>> GetUserFlowByVisitId(long VisitId )
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserNavigations = await _UserNavigationRepo
                                    .Query()
                                    .Where(v => v.SiteId == SiteId && v.VisitId == VisitId)
                                    .OrderBy(v => v.Id)
                                    .ToListAsync();

                return UserNavigations;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<List<UserNavigationResultVM>>GetUserNavigations(UserNavigationVM userNavigationVM)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            var userList = await _UserNavigationRepo.Query().AsNoTracking()
                           .Include(u => u.User)
                           .ThenInclude(u => u.UserRoles)
                           .ThenInclude(u => u.Role)
                           .Where(n => n.SiteId == SiteId &&
                                      n.UserId == userNavigationVM.UserId)
                           .ToListAsync();

            var filteredUsers = userList.FilterSlug(new UsersByVisitVM { 
                                   FromSlug= userNavigationVM.PreviousSlug, 
                                   ToSlug= userNavigationVM.Slug,
                                   SingleSlug= userNavigationVM.SingleSlug,
                                   FromDate= userNavigationVM.FromDate,
                                   ToDate= userNavigationVM.ToDate}).ToList();
          
           

            var UserNavigationTree =await BuildUserNavigation(filteredUsers, userNavigationVM);
            return UserNavigationTree;

        }

        public async Task<List<NavigationsByUserAndVisitResultVM>> GetNavigationsByUserAndVisit(NavigationsByUserAndVisitVM Model)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            var navigations = await _UserNavigationRepo
                                .Query()
                                .Where(n => n.SiteId == SiteId &&
                                        (Model.UserId == 0 || Model.UserId ==  n.UserId ) &&
                                        (n.CreationDate.Date >= Model.FromDate.Value.Date &&
                                            n.CreationDate.Date <= Model.ToDate.Value.Date))
                                .ToListAsync();

            return navigations
                                .Where(n => string.Equals(n.Slug, Model.ToSlug, StringComparison.OrdinalIgnoreCase) &&
                                            string.Equals(n.PreviousSlug, Model.FromSlug, StringComparison.OrdinalIgnoreCase))
                                .Select(n => new NavigationsByUserAndVisitResultVM()
                                {
                                    NavigationDate=n.CreationDate,
                                    NavigationId = n.Id,
                                })
                                .OrderByDescending(u => u.NavigationDate)
                                .Skip(Model.PageNumber * Model.PageSize)
                                .Take(Model.PageSize)
                                .ToList();
        }

       
        public async Task<GetUserNavigationPaginationVM> GetUsersByNavigation(UsersByVisitVM Model)
        {
            try
            {

         
            long SiteId = await _workContext.GetCurrentSiteIdAsync();


            GetUserNavigationPaginationVM Result = new();

            var query = await _UserNavigationRepo
                                 .Query().AsNoTracking()
                                .Where(n => n.SiteId == SiteId&&
                                       (Model.UserId==0 || Model.UserId==n.UserId))
                                .Include(u => u.User)
                                .ThenInclude(u => u.UserRoles)
                                .ThenInclude(u => u.Role)
                                .ToListAsync();

            var filteredNavigations = query.FilterSlug(Model).ToList();

            var UserNavigations = filteredNavigations.Select(n => new UsersByVisitResultVM()
                                 {
                                     Email = n.User.Email,
                                     UserId = n.UserId,
                                     UserName = n.User.UserName,
                                     UserRoleIds = n.User.UserRoles==null?new List<long>(): n.User.UserRoles.Select(u => u.RoleId).ToList(),
                                     UserRoleName = n.User.UserRoles==null?new List<string>(): n.User.UserRoles.Select(u => u.Role.Name).ToList(),
                                     ClientIp=n.UserIp,
                                     FullName = n.User.FullName,
                                     BrowserName=n.BrowserName,
                                     PeriodTime=n.Period

                                 }).ToList();


            UserNavigations = UserNavigations
                   .Where(u =>
                           (string.IsNullOrEmpty(Model.SearchText) ||
                          u.Email.ToLower().Contains(Model.SearchText.ToLower()) ||
                          u.UserName.ToLower().Contains(Model.SearchText.ToLower())
                          || u.FullName.ToLower().Contains(Model.SearchText.ToLower())
                        )
                        &&
                        (
                          Model.SearchByUserRole == 0 ||
                          u.UserRoleIds.Contains(Model.SearchByUserRole)
                        )
                        && (string.IsNullOrEmpty(Model.searchByBrowserName) ||
                            u.BrowserName.ToLower().Contains(Model.searchByBrowserName.ToLower())))
                  .DistinctBy(u => u.UserId).ToList();

            UserNavigations = await SortNavigations(UserNavigations, Model.sortType);
  


            Result.TotalUsersVisites = UserNavigations.Count;

            UserNavigations = UserNavigations.Skip(Model.PageNumber * Model.PageSize)
                  .Take(Model.PageSize).ToList();

            Result.usersByVisitResultVMs = UserNavigations;    



            return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        #region Helper Functions For BGS
        private async Task<List<UsersByVisitResultVM>> SortNavigations(List<UsersByVisitResultVM> UsersByVisitList,SortType sortType)
        {
            if (sortType == SortType.UserName)
            {
                    UsersByVisitList = UsersByVisitList.OrderBy(x => x.UserName).ToList();
            }
            if (sortType == SortType.BrowserType)
            {
                UsersByVisitList = UsersByVisitList.OrderBy(x => x.BrowserName).ToList();
            }
            if (sortType == SortType.Role)
            {
                UsersByVisitList = UsersByVisitList.OrderBy(x => x.UserRoleName.FirstOrDefault()).ToList();
            }
            if (sortType == SortType.PeriodTime)
            {
                UsersByVisitList = UsersByVisitList.OrderBy(x => x.PeriodTime).ToList();
            }
       
            return UsersByVisitList;
        }
        private async Task<bool> TakeNavigationSnapShot(UserNavigationVm userNavigationVm, string UserIP, SlugsTypes SlugType, long UserId, long SiteId)
        {
            try
            {


                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _UserVisitRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserVisits>>();
                    var _UserNavigationRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserNavigations>>();
                    var _BrowsersRepo = scope.ServiceProvider.GetRequiredService<IRepositoryWithCustomId<BrowserType>>();
                    var _UserSitesRepository = scope.ServiceProvider.GetRequiredService<IRepository<UsersSites>>();
                    var _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var _workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();

                   
                  

                    long LastUserVisitId = 0;

                    var LastSnapShot = await _UserNavigationRepo.Query()
                        .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                        .OrderByDescending(u => u.Id)
                        .FirstOrDefaultAsync();




                    var cultureId = _workContext.GetCurrentCultureId();

                    var userSite = await _UserSitesRepository.Query()
                                                             .Where(x => x.SiteId == SiteId && x.UserId == UserId)
                                                             .FirstOrDefaultAsync();

                    if (userSite is null)
                    {
                        return false;
                    }
                    userSite.CultureId = cultureId;

                    _UserSitesRepository.Update(userSite);

                    await _UserSitesRepository.SaveChangesAsync();


                    if (LastSnapShot == null)
                    {
                        UserVisits UserVisitObj = await AddUserVisit(UserId, SiteId, _UserVisitRepo);
                        LastUserVisitId = UserVisitObj.Id;
                    }
                    else
                    {
                        TimeSpan EstimationTime = DateTime.Now - LastSnapShot.CreationDate;

                        if (EstimationTime.TotalMinutes > MAX_ESTIMATION_TIME_PER_VISITE)
                        {
                            LastSnapShot.Period = TimeSpan.FromMinutes(MAX_ESTIMATION_TIME_PER_VISITE);

                            UserVisits UserVisitObj = await AddUserVisit(UserId, SiteId, _UserVisitRepo);
                            LastUserVisitId = UserVisitObj.Id;
                            LastSnapShot = null; 

                        }
                        else
                        {
                            LastSnapShot.Period = EstimationTime;

                            LastUserVisitId = LastSnapShot.VisitId;


                        }

                    }

                    BrowserType browser =  await _BrowsersRepo.Query()
                        .Where(b => b.Name.ToLower() == userNavigationVm.BrowserName.ToLower() &&
                                   b.Version.ToLower() == userNavigationVm.BrowserVersion.ToLower())
                        .FirstOrDefaultAsync();
                    if(browser == null)
                    {
                        browser = new BrowserType()
                        {
                            Name = userNavigationVm.BrowserName,
                            Version = userNavigationVm.BrowserVersion,
                        };
                        _BrowsersRepo.Add(browser);
                        await _BrowsersRepo.SaveChangesAsync();
                    }

                    var UserNavigation = new UserNavigations()
                    {
                        SiteId = SiteId,
                        
                        BrowserName= browser.Name,
                        BrowserVersion = browser.Version,
                        UserId = UserId,
                        Slug = userNavigationVm.Slug,
                        CreationDate = DateTime.Now,
                        VisitId = LastUserVisitId,
                        UserIp = UserIP,
                        SlugsType = SlugType,
                        PreviousSlug = LastSnapShot != null ?  LastSnapShot.Slug : OUTSOURCE,
                        PreviousSlugsType = LastSnapShot != null ?  LastSnapShot.SlugsType : null,

                    };

                    _UserNavigationRepo.Add(UserNavigation);
                    await _UserNavigationRepo.SaveChangesAsync();


                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }

        }

        private async Task<UserVisits> AddUserVisit(long UserId, long SiteId, IRepository<UserVisits> _UserVisitRepo)
        {
            try
            {
                var UserVisit = new UserVisits()
                {
                    CreationDate = DateTime.Now,
                    SiteId = SiteId,
                    UserId = UserId,


                };

                _UserVisitRepo.Add(UserVisit);

                await _UserVisitRepo.SaveChangesAsync();
                return UserVisit;
            }
            catch (Exception E)
            {
                throw E;
            }
        }
        #endregion


        #region Helper Functions

        //private async Task<GraphNode> BuildGraphAsync(List<IGrouping<dynamic,UserNavigations>> UsersVisits)
        //{

        //    UserFlowGraph userFlow = new UserFlowGraph();


        //    //foreach (var Visit in UsersVisits)
        //    //{
        //    //    for (int index = 0; index < Visit.Count; index++)
        //    //    {
        //    //        var UserVisit = index == 0 ? UserFlowGraph.OutSource : Visit[index - 1].Slug;
        //    //        userFlow.InsertNavigation(UserVisit, Visit[index].Slug);
        //    //    }

        //    //}


        //    return userFlow.GetGraph();
        //}
        private async Task<List<UserNavigationResultVM>> BuildUserNavigation(List<UserNavigations> UserList, UserNavigationVM userNavigationVM)
        {
            List<UserNavigationResultVM> users= new List<UserNavigationResultVM>();  
            users=UserList.Select(u => new UserNavigationResultVM()
            { 
                FullName=u.User.FullName,
                FromDate=u.CreationDate,
                ToDate=u.CreationDate.Add(u.Period),
                UserName=u.User.UserName,
                FromSlug=u.Slug,
                LastLoginDate = u.User.UserRoles.Select(x=>x.Role.Name).Where(A=>A.ToLower()== "Customer".ToLower()).FirstOrDefault() !=null? u.User.LastLoginDate :null ,
                ToSlug=u.PreviousSlug,
                Period=u.Period,
                BrowserName=u.BrowserName,
                ClientIp=u.UserIp
            }).Skip(userNavigationVM.PageNumber * userNavigationVM.PageSize)
            .Take(userNavigationVM.PageSize)
            .ToList();
            return users;
        }




        #endregion

    }
}
