
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Dashboard.Services.DashboardService.Interfaces;
using EskaCMS.Dashboard.Models.DashboardViewModel;

namespace EskaCMS.Dashboard.Services.DashboardService
{
    public class DashboardService : IDashboard

    {

        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<PageVersions> _pageVersions;
        private readonly IRepository<Posts> _PostRepository;
        private readonly IRepository<Site> _SitesRepository;
        private readonly IRepository<UsersSites> _UserSitesRepository;
        private readonly IRepository<Category> _CategoriesRepository;
        private readonly UserManager<User> _userManager;
        private readonly ISlugServices _slugServices;


        public DashboardService(UserManager<User> userManager, ISlugServices slugServices, IRepository<Category> CategoriesRepository, IRepository<UsersSites> UserSitesRepository, IRepository<Page> pageRepository, IRepository<PageVersions> pageVersions, IRepository<Posts> PostRepository, IRepository<Site> SitesRepository)
        {
            _pageRepository = pageRepository;
            _pageVersions = pageVersions;
            _PostRepository = PostRepository;
            _SitesRepository = SitesRepository;
            _UserSitesRepository = UserSitesRepository;
            _CategoriesRepository = CategoriesRepository;
            _userManager = userManager;
            _slugServices = slugServices;
        }

        public async Task<List<DailyVistsViewModel>> GetSiteGuest(
      long siteId,
      long rangeType)
        {
            var today = DateTime.Today;

            var period = rangeType switch
            {
                2 => today.AddMonths(-1),
                3 => today.AddYears(-1),
                _ => today.AddDays(-7)
            };

            var query = _userManager.Users.AsNoTracking()
                .Where(user =>
                    user.Type == EUsersTypes.GuestClient &&
                    user.CreationDate >= period &&
                    user.UserSites.Any(userSite => userSite.SiteId == siteId))
                .GroupBy(user => user.CreationDate.Date)
                .Select(group => new DailyVistsViewModel
                {
                    date = group.Key,
                    visites = group.Count()
                })
                .OrderBy(result => result.date);

            return await query.ToListAsync();
        }

        public async Task<DashboardViewModel> GetDashboardData(long UserId, long SiteId)
        {
            try
            {
                DashboardViewModel dash = new DashboardViewModel();
                DashboardCountersViewModel Counters = new DashboardCountersViewModel();

                Counters.publishedPages =await _pageRepository.Query().Where(x => (x.ParentId == x.Id || x.ParentId == null) && x.Status == EStatus.Published && x.SiteId == SiteId).CountAsync();

                Counters.unpublishedPages = await _pageRepository.Query().Where(x => x.Status == EStatus.Unpublished && x.SiteId == SiteId && x.ParentId == x.Id).CountAsync();

                Counters.publishedBlogs = await _PostRepository.Query().Where(x=> x.SiteId == SiteId && x.Status == EStatus.Published && x.ParentId == x.Id
				).CountAsync();

                Counters.unpublishedBlogs = await _PostRepository.Query().Where(x => x.SiteId == SiteId && x.Status == EStatus.Unpublished && x.ParentId == x.Id
				 && (x.ExpiryDate == null || x.ExpiryDate.Value > DateTimeOffset.Now) &&
                           x.PublishDate != null && x.PublishDate.Value < DateTimeOffset.Now).CountAsync();

                Counters.ExpiredBlogs = await _PostRepository.Query().Where(y => y.SiteId == SiteId && y.Status != EStatus.Deleted 
                      && y.ExpiryDate!=null&& y.ExpiryDate.Value.Date <= DateTime.Now.Date).CountAsync();


                Counters.sitesNumber = await _SitesRepository.Query().Where(u => u.Status == EStatus.Published).Join(_UserSitesRepository.Query().Where(x => x.UserId == UserId),
                          s => s.Id, u => u.SiteId, (s, u) => s).CountAsync();


                Counters.menuCatesgoriesNumber = await _CategoriesRepository.Query().Where(y => y.Status != EStatus.Deleted && y.CategoryTypeId == (long)ECategoryTypes.Menu).CountAsync();


                dash.lastEditedPages = await _pageRepository.Query().Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted)
                    .Include(x=>x.Parent)
                    .Include(x=>x.SlugInfo)
                                       .OrderByDescending(x => x.ModificationDate)
                                       .Select(x => new Page
                                       {
                                           Id = x.Id,
                                           Name = x.Name,
                                           CreationDate = x.CreationDate,
                                           ModificationDate = x.ModificationDate,
                                           CreatedBy = x.CreatedBy,
                                           ModifiedBy = x.ModifiedBy,
                                           Slug =x.SlugInfo.Slug,
                                           ParentId = x.Parent.ParentId
                                       })
                                       .Take(4).ToListAsync();


                var total = await _pageVersions.Query().Join(_pageRepository.Query().Where(x => x.SiteId == SiteId),
                        s => s.PageId, u => u.Id, (s, u) => s).CountAsync();


                var result1 = (from PageVersion in _pageVersions.Query().AsNoTracking()
                               join Pages in _pageRepository.Query()
                               on PageVersion.PageId equals Pages.Id
                               where Pages.SiteId == SiteId
                               group PageVersion by PageVersion.CreatedById into g
                               select new
                               {
                                   Id = g.Key,
                                   versions = g.Count() * 100 / total
                               }).ToList();

                var result2 = new List<VersionActivitesViewModel>();

                if (result1.Count() > 0)
                {
                    // Was: one DB query per row (N+1) to fetch each user's name.
                    // Now: a single query for all needed users, then map in memory.
                    var creatorIds = result1.Select(x => x.Id).Distinct().ToList();
                    var userNames = _userManager.Users
                        .Where(u => creatorIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FullName })
                        .ToDictionary(u => u.Id, u => u.FullName);

                    result2 = result1.Select(x => new VersionActivitesViewModel
                    {
                        Id = x.Id,
                        user = userNames.TryGetValue(x.Id, out var fullName) ? fullName : null,
                        versions = x.versions
                    }).ToList();
                }






                dash.VersionActivites = result2;
                dash.PageCounters = Counters;
                return dash;

            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


    }
}
