using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Core.Extensions;
using EskaCMS.EskaCoreReports.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.EskaCoreReports.ViewModels.EskaCoreReportVM;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.EskaCoreReports;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Shared;
using Newtonsoft.Json.Linq;

namespace EskaCMS.EskaCoreReports.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly IRepository<Reports> _reportRepository;
        private readonly IRepository<PostsAuther> _postAuthorsRepository;
        private readonly IRepository<ReportHistory> reportHistoryRepository;
        private readonly IRepository<Posts> postsRepository;
        private readonly IRepository<Menu> _MenuRepository;
        private readonly IMenu _MenuService;
        private readonly IBlogArchive _BlogArchiveRepository;
        private readonly IWorkContext _workContext;
        private readonly ISlugServices slugServices;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<EntityTags> _entityTagsRepository;

        public ReportRepository(
            IRepository<Reports> reportRepository,
            IRepository<ReportHistory> ReportHistoryRepository,
            IRepository<Posts> PostsRepository,
            IWorkContext workContext,
            ISlugServices slugServices,
            IRepository<User> UserRepository,
            IRepository<Menu> MenuRepository,
            IMenu MenuService,
            IBlogArchive BlogArchiveRepository,
            IRepository<EntityTags> entityTagsRepository
            )
        {
            _reportRepository = reportRepository;
            reportHistoryRepository = ReportHistoryRepository;
            postsRepository = PostsRepository;
            _workContext = workContext;
            this.slugServices = slugServices;
            userRepository = UserRepository;
            _MenuRepository = MenuRepository;
            _MenuService = MenuService;
            _BlogArchiveRepository = BlogArchiveRepository;
            _entityTagsRepository = entityTagsRepository;
        }

        public async Task<bool> Create(EskaCoreReportVM model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                List<Reports> Reports = new List<Reports>();

                foreach (var postId in model.PostIds)
                {
                    Reports.Add(new Entities.Entity.EskaCoreReports.Reports
                    {


                        MenuId = model.MenuId,
                        Status = model.Status.HasValue ? model.Status.Value : EStatus.Published,
                        CreatedById = userId,
                        CreationDate = DateTimeOffset.Now,
                        PostId = postId


                    });
                }

                _reportRepository.AddRange(Reports);
                await _reportRepository.SaveChangesAsync();

                List<ReportHistory> History = new List<ReportHistory>();
                foreach (var repo in Reports)
                {
                    History.Add(new ReportHistory
                    {
                        CreationDate = DateTimeOffset.Now,
                        CreatedById = userId,
                        ReportId = repo.Id,
                        NewStatus = repo.Status,
                        OldStatus = null



                    });

                }
                reportHistoryRepository.AddRange(History);
                await reportHistoryRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        public async Task<bool> ChangeReportStatus(long Id, EStatus Status)
        {
            try
            {

                var report = await _reportRepository
                                                  .Query()
                                                  .Include(x => x.Post)
                                                  .ThenInclude(x => x.SlugInfo)
                                                  .FirstOrDefaultAsync(x => x.Id == Id);
                var userId = await _workContext.GetCurrentUserId();
                if (report != null)
                {
                    var oldStatus = report.Status;
                    var Post = await postsRepository.Query().FirstOrDefaultAsync(x => x.Id == report.PostId);

                    if (EStatus.Deleted == Status && report.Status != EStatus.Deleted)

                    {
                        report.Status = Status;

                        await _reportRepository.SaveChangesAsync();

                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = Post.SlugInfoId,
                            CultureId = Post.CultureId,

                            Slug = Post.Id + "Deleted" + await slugServices.GetSlugById(Post.SlugInfoId),
                            Entity = EEntity.Post,

                        };
                        Post.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                        Post.Status = Status;
                        await postsRepository.SaveChangesAsync();

                    }
                    else
                    {

                        report.Status = Status;

                        await _reportRepository.SaveChangesAsync();

                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = Post.SlugInfoId,
                            CultureId = Post.CultureId,

                            Slug = Post.Slug.Split("Deleted").LastOrDefault(),
                            Entity = EEntity.Post,

                        };
                        Post.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                        Post.Status = Status;
                        await postsRepository.SaveChangesAsync();

                    }

                    ReportHistory History =
                                         new ReportHistory()
                                         {

                                             CreationDate = DateTimeOffset.Now,
                                             CreatedById = userId,
                                             ReportId = report.Id,
                                             NewStatus = Status,
                                             OldStatus = oldStatus,

                                         };


                    reportHistoryRepository.Add(History);
                    await reportHistoryRepository.SaveChangesAsync();

                }

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ReportVM> GetReportById(long id)
        {
            try
            {
                var Report = await _reportRepository.Query().Include(x => x.Menu)
                                                      .ThenInclude(x => x.Post)
                                                      .ThenInclude(x => x.SlugInfo)
                                                      .Where(x => x.Id == id)
                                                      .Select(x => new ReportVM
                                                      {

                                                          Id = x.Id,
                                                          MenuId = x.MenuId,
                                                          MenuCategoryId = x.Menu.MenuGroup.CategoryId,
                                                          MenuGroupId = x.Menu.MenuGroupId,
                                                          MenuName = x.Menu.Name,
                                                          PostCreationDate = x.Post.CreationDate,
                                                          PostExpiryDate = x.Post.ExpiryDate,
                                                          Slug = x.Post.SlugInfo.Slug,
                                                          Status = x.Post.Status,
                                                          Name = x.Post.Title,
                                                          PostPublishDate = x.Post.PublishDate,
                                                          PostId = x.Post.Id,
                                                          PostsCategories = x.Post.PostsCategories.Select(x => x.Category).SelectMany(c => c.CategoryDetails).Where(p => p.CultureId == x.Post.CultureId).Select(x => x.Name).ToList(),
                                                          ReportPostId = x.Id,
                                                      }).FirstOrDefaultAsync();

                return Report;




            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Update(EskaCoreReportVM model)
        {
            var userId = await _workContext.GetCurrentUserId();
            var Report = await _reportRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
            Report.MenuId = model.MenuId != 0 ? model.MenuId : Report.MenuId;
            Report.ModifiedById = userId;
            Report.ModificationDate = DateTimeOffset.Now;
            Report.Status = model.Status.HasValue ? model.Status.Value : EStatus.Published;
            _reportRepository.Update(Report);
            await _reportRepository.SaveChangesAsync();
            return true;
        }

        public async Task<PagenationReportMenuListVM> GetReportsByMenuId(ReportsByMenuIdSearchVM Model)
        {
            try
            {
                PagenationReportMenuListVM Result = new PagenationReportMenuListVM();

                //    var UserId =await  _workContext.GetCurrentUserId();
                List<long> MenusIds = new List<long>();
                //     var RoleId =await userRepository.Query().Where(x => x.Id == UserId).Include(x => x.UserRoles).SelectMany(x => x.UserRoles).Select(x => x.RoleId).FirstOrDefaultAsync();

                long? MenuId = Model.MenuId;
                Menu? ParentMenu = await _MenuRepository.Query().AsNoTracking().Where(m => m.Id == MenuId).FirstOrDefaultAsync();

                var MenuTree = ParentMenu != null ? await _MenuService.GetMenusByGroupId(ParentMenu.MenuGroupId) : new List<MenuViewModel>();

                var TargetMenuTree = MenuId.HasValue ? await SearchForMenu(MenuId.Value, MenuTree) : null;

                MenusIds = await FillMenusIds(TargetMenuTree, new List<long>());

                var tags = new List<long>();
                if (Model.TagIds != null && Model.TagIds.Count != 0)
                {
                    tags = await _entityTagsRepository.Query().Where(x => Model.TagIds.Contains(x.TagsId)).Select(x => x.EntityId).ToListAsync();
                }

                var Reports = await _reportRepository.Query()
                    .Include(p => p.Post)
                    .ThenInclude(p => p.PostMedias)
                    .ThenInclude(p => p.Media)
                    .Include("Post.Thumbnail")
                    //.Include("Post.PostsTags")
                    .Include("Post.PostsCategories")
                    .Include("Post.SlugInfo")
                    .Include(x => x.CreatedBy)
                    .ThenInclude(x => x.UserRoles)
                    .Include(x => x.Menu)
                    .ThenInclude(x => x.MenuRoles)
                    .Include(c => c.Post).ThenInclude(t => t.PostsAuthers)
                    .Where(x => (MenusIds.Count == 0 || MenusIds.Contains(x.MenuId) || x.Menu.ParentId == TargetMenuTree.Id) &&
                                x.Status != EStatus.Deleted &&
                                x.Post.Status == EStatus.Published &&
                                 (Model.Authors == null || Model.Authors.Count == 0 || x.Post.PostsAuthers.Any(t => Model.Authors.Contains(t.userId))) &&
                                (x.Post.ExpiryDate == null || x.Post.ExpiryDate.Value > DateTimeOffset.Now) &&
                                (x.Post.PublishDate != null && x.Post.PublishDate.Value < DateTimeOffset.Now) &&
                                       (tags == null || tags.Count == 0 || tags.Contains(x.PostId)) &&
                                (Model.CategoryIds == null || Model.CategoryIds.Count == 0 || x.Post.PostsCategories.Any(pc => Model.CategoryIds.Any(CategoryId => pc.CategoryId == CategoryId))) &&
                                (string.IsNullOrEmpty(Model.Title) || x.Post.Title.ToLower().Contains(Model.Title.ToLower()) || x.Post.ShortDescription.ToLower().Contains(Model.Title.ToLower()))
                                )
                    //.Where(x=>x.Menu.MenuRoles.Any(l=>l.RoleId==RoleId))
                    .Select(x => new ReportMenuListVM()
                    {
                        Title = x.Post.Title,
                        ShortDescription = x.Post.ShortDescription,
                        Description = x.Post.Description,
                        Thumbnail = x.Post.Thumbnail.PublicUrl,
                        Slug = x.Post.SlugInfo.Slug,
                        TemplateId = x.Post.PostTemplateId,
                        CreationDate = x.CreationDate,
                        MediaReport = x.Post.PostMedias.Any() ? x.Post.PostMedias.Select(x => new MediaReport
                        {

                            publicUrl = x.Media.PublicUrl,
                            Id = x.Media.Id,


                        }).ToList() : new List<MediaReport>()
                    }).ToListAsync();


                if (Model.FilterBy == null || Model.FilterBy == OrderByEnum.Date)
                {
                    Reports = Reports.OrderBy(x => x.CreationDate).ToList();
                }
                else if (Model.FilterBy == OrderByEnum.AlphabeticalDesc)
                {
                    Reports = Reports.OrderByDescending(x => x.Title).ToList();
                }
                else
                {
                    Reports = Reports.OrderBy(x => x.Title).ToList();
                }

                Result.ReportList = Reports
                                      .Skip(Model.PageSize * Model.PageNumber)
                                      .Take(Model.PageSize)
                                      .ToList();

                Result.TotalCount = Reports.Count();
                Result.PageNumber = Model.PageNumber;
                Result.PageSize = Model.PageSize;


                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<int> GetReportsCount(long siteId)
        {
            try
            {
                //Query To Get All Reports Count By SiteId 
                var query = await _reportRepository
                      .Query()
                      .Include(x => x.Menu)
                      .Include(x => x.Post)

                      .Where(x => x.Menu.SiteId == siteId && x.Status != EStatus.Deleted &&
                                    (x.Post.ExpiryDate == null || x.Post.ExpiryDate.Value > DateTimeOffset.Now) &&
                                    (x.Post.PublishDate != null && x.Post.PublishDate.Value < DateTimeOffset.Now))
                      .ToListAsync();
                return query.Count;
            }
            catch (Exception ex)
            {
                throw ex;

            }

        }

        public async Task<ReportsBreadCrumbVM> GetReportBreadCrumb(string Slug)
        {
            try
            {
                var post = await _BlogArchiveRepository.GetPostBySlug(Slug);
                if (post == null)
                {

                    throw new Exception("Could not Find Post");

                }

                var reportDetails = await _reportRepository.Query().AsNoTracking()
                                                .Where(r => r.PostId == post.Id)
                                                .FirstOrDefaultAsync();

                if (reportDetails == null)
                {
                    throw new Exception("Could not Find Report");
                }

                var result = new ReportsBreadCrumbVM();

                result.MenuBreadCrumb = await _MenuService.GetMenuBreadCrumb(reportDetails.MenuId);
                result.PostTitle = post.Title;
                result.PostId = post.Id;

                return result;

            }
            catch (Exception ex)
            {
                throw ex;

            }

        }


        public async Task<SmartTableResult<ReportDetailsVM>> GetReports(SmartTableParam param, long SiteId)
        {
            try
            {
                var user = await _workContext.CurrentUserInfo();
                var RoleIds = user.UserRoles.Select(x => x.RoleId).ToList();
                var query = _reportRepository
                .Query()
                .Include(r => r.Post).ThenInclude(c => c.PostsCategories)
                .ThenInclude(c => c.Category)
                .ThenInclude(x => x.CategoryDetails)
                .Include(p => p.Post.SlugInfo)
                .Include(p => p.Post.PostsAuthers)
                .Include(x => x.Post).ThenInclude(r => r.PostRoles)
                .Include(r => r.Menu)
                .ThenInclude(m => m.MenuGroup)
                .ThenInclude(m => m.Menus)
                .Include(x => x.Menu)
                .ThenInclude(x => x.MenuRoles)
                .Include(x => x.CreatedBy)
                .Include(x => x.Post)
                .ThenInclude(x => x.PostMedias)
                .ThenInclude(x => x.Media)
                .Where(r => r.Post.SiteId == SiteId && r.Post.PostRoles.Any(p => RoleIds.Contains(p.RoleId)));

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Title != null)
                    {
                        string title = search.Title;
                        query = query.Where(x => x.Post.Title.Contains(title.ToLower()) ||
                        x.Menu.Name.Contains(title.ToLower()) ||
                        x.Post.ShortDescription.Contains(title.ToLower()) ||
                        x.Post.PostsCategories.Any(x => x.Category.CategoryDetails.Any(t => t.Name.Contains(title.ToLower()))));
                    }
                    if (search.Status != null)
                    {
                        EStatus Status = (EStatus)(search.Status);
                        query = query.Where(x => x.Status == Status);

                    }

                    if (search.Authors != null && search.Authors.Count > 0)
                    {
                        JArray Authors = search.Authors;
                        var AuthorIds = Authors.Values<long>().ToList();
                        query = query.Where(x => x.Post.PostsAuthers.Any(t => AuthorIds.Contains(t.userId)));
                    }

                    if (search.MenuId != null)
                    {
                        long MenuId = (long)(search.MenuId);
                        query = query.Where(x => x.MenuId == MenuId || x.Menu.ParentId == MenuId || x.Menu.ParentId == x.MenuId || x.Menu.Parent.ParentId == MenuId);

                    }


                    if (search.Categories != null && search.Categories.Count > 0)
                    {
                        JArray Categories = search.Categories;
                        var CategoryIds = Categories.Values<long>().ToList();
                        query = query.Where(x => x.Post.PostsCategories.Any(t => CategoryIds.Contains(t.CategoryId)));
                    }
                }

                var gridData = query.ToSmartTableResult(
                param,
                r => new ReportDetailsVM()
                {
                    Id = r.Id,
                    MenuId = r.MenuId,
                    Menu = r.Menu,
                    MenuName = r.Menu.Name,
                    MenuCategoryId = r.Menu.MenuGroup.CategoryId,
                    MenuGroupId = r.Menu.MenuGroupId,
                    PostName = r.Post.Title,
                    PostCreationDate = r.Post.CreationDate,
                    PostPublishDate = r.Post.PublishDate,
                    PostExpiryDate = r.Post.ExpiryDate,
                    PostsCategories = r.Post.PostsCategories.Select(x => x.Category).SelectMany(c => c.CategoryDetails).Where(p => p.CultureId == r.Post.CultureId).Select(x => x.Name).ToList(),
                    PostId = r.PostId,
                    Status = r.Status,
                    ShortDescription = r.Post.ShortDescription,
                    PostStatus = r.Post.Status,
                    Author = r.CreatedBy.FullName,
                    MediaReport = r.Post.PostMedias.Any() ? r.Post.PostMedias.Select(x => new MediaReport
                    {

                        publicUrl = x.Media.PublicUrl,
                        Id = x.Media.Id,


                    }).ToList() : new List<MediaReport>(),
                    Slug = r.Post.SlugInfo.Slug,
                    TemplateId = r.Post.PostTemplateId,
                    Customers = string.Join(", ", (r.Post.PostsAuthers.AsQueryable()).Include(x => x.user).Select(t => t.user.FullName).ToList())
                });

                return gridData;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<List<ReportHistoryVM>> GetReportHistory(long id)
        {
            try
            {
                var query = await _reportRepository.Query().AsNoTracking().Where(x => x.Id == id)
                                                     .Include(x => x.ReportHistories)
                                                     .ThenInclude(x => x.CreatedBy)
                                                     .ToListAsync();


                var Result = query.SelectMany(x => x.ReportHistories)
                                                     .Select(x => new ReportHistoryVM
                                                     {
                                                         NewStatus = x.NewStatus,
                                                         ModifyById = x.CreatedBy.Id,
                                                         ModifyBy = x.CreatedBy.UserName,
                                                         ModifyDate = x.CreationDate,
                                                         OldStatus = x.OldStatus,
                                                         NewStatusString = x.NewStatus.GetDisplayName(),
                                                         OldStatusString = x.OldStatus.HasValue ? x.OldStatus.GetDisplayName() : null


                                                     }).ToList();

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<SmartTableResult<ReportDetailsVM>> GetReportsByAuthors(SmartTableParam Param, long SiteId, long UserId)
        {
            var query = _reportRepository
                .Query()
                .Include(x => x.Post)
                 .ThenInclude(c => c.PostsCategories)
                .ThenInclude(c => c.Category)
                .ThenInclude(x => x.CategoryDetails)
                .Include(t => t.Post.PostsAuthers)
                .Include(p => p.Post.SlugInfo)
                .Include(r => r.Menu)
                .ThenInclude(m => m.MenuGroup)
                .ThenInclude(m => m.Menus)
                .Include(x => x.Menu)
                .ThenInclude(x => x.MenuRoles)
                .Include(x => x.CreatedBy)
                .Where(r => r.Post.SiteId == SiteId && r.Post.PostsAuthers.Any(pa => pa.userId == UserId));


            if (Param.Search != null && Param.Search.PredicateObject != null)
            {
                dynamic search = Param.Search.PredicateObject;
                if (search.Title != null)
                {
                    string title = search.Title;
                    query = query.Where(x => x.Post.Title.Contains(title.ToLower()) ||
                    x.Menu.Name.Contains(title.ToLower()) ||
                    x.Post.ShortDescription.Contains(title.ToLower()) ||
                    x.Post.PostsCategories.Any(x => x.Category.CategoryDetails.Any(t => t.Name.Contains(title.ToLower()))));
                }
                if (search.Status != null)
                {
                    EStatus Status = (EStatus)(search.Status);
                    query = query.Where(x => x.Status == Status);

                }

                if (search.MenuId != null)
                {
                    long MenuId = (long)(search.MenuId);
                    query = query.Where(x => x.MenuId == MenuId || (x.Menu.ParentId == MenuId) || x.Menu.Parent.Id == MenuId);

                }

                if (search.Authors != null && search.Authors.Count > 0)
                {
                    JArray Authors = search.Authors;
                    var AuthorIds = Authors.Values<long>().ToList();
                    query = query.Where(x => x.Post.PostsAuthers.Any(t => AuthorIds.Contains(t.userId)));
                }
            }

            var gridData = query.ToSmartTableResult(
                Param,
                r => new ReportDetailsVM()
                {
                    Id = r.Id,
                    MenuId = r.MenuId,
                    Menu = r.Menu,
                    MenuName = r.Menu.Name,
                    MenuCategoryId = r.Menu.MenuGroup.CategoryId,
                    MenuGroupId = r.Menu.MenuGroupId,
                    PostName = r.Post.Title,
                    PostCreationDate = r.Post.CreationDate,
                    PostPublishDate = r.Post.PublishDate,
                    PostExpiryDate = r.Post.ExpiryDate,
                    ShortDescription = r.Post.ShortDescription,
                    PostsCategories = r.Post.PostsCategories.Select(x => x.Category).SelectMany(c => c.CategoryDetails).Where(p => p.CultureId == r.Post.CultureId).Select(x => x.Name).ToList(),
                    PostId = r.PostId,
                    Status = r.Status,
                    PostStatus = r.Post.Status,
                    Author = r.CreatedBy.FullName,
                    Slug = r.Post.SlugInfo.Slug,
                    TemplateId = r.Post.PostTemplateId
                });


            return gridData;
        }



        #region helpers

        private async Task<MenuViewModel> SearchForMenu(long MenuId, List<MenuViewModel> MenuList)
        {
            if (MenuList == null || MenuList.Count == 0)
            {
                return null;
            }
            else
            {
                var TargetMenu = MenuList.Where(m => m.Id == MenuId).FirstOrDefault();
                if (TargetMenu != null)
                {
                    return TargetMenu;
                }
                else if (TargetMenu == null)
                {
                    foreach (var menu in MenuList)
                    {
                        var result = await SearchForMenu(MenuId, menu.Childs);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    return null;
                }
                return null;
            }

        }
        private async Task<List<long>> FillMenusIds(MenuViewModel Menu, List<long> MenuIds)
        {
            if (Menu == null)
            {
                return MenuIds;
            }
            else
            {
                MenuIds.Add(Menu.Id);
                foreach (var menu in Menu.Childs)
                {
                    await FillMenusIds(menu, MenuIds);
                }
                return MenuIds;

            }
        }


        #endregion

    }
}
