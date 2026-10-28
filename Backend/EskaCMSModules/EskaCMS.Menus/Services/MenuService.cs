
using EskaCMS.Entities.Entity;
using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Entities.Enum;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Elements.ViewModels;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Menus.Services
{
    public class MenuService : IMenu
    {

        private readonly IRepository<Menu> _MenuRepository;
        private readonly IRepository<MenuGroup> _MenuGroupRepository;
        //    private readonly IRepository<MenuSettings> _MenuSettingsRepository;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<Posts> _PostRepository;
        private readonly IRepository<Category> _CategoriesRepository;
        private readonly IRepository<CategoryDetails> categoriesDetailsRepository;
        private readonly IRepository<ProductCategory> productCategoryRepository;
        private readonly IBlogArchive _BlogArchiveService;
        private readonly IUserRoleService _UserRoleService;
        private readonly IMenuRole _MenuRoleService;
        private readonly IUserSiteService userSiteService;
        private readonly ISlugServices slugServices;
        private readonly IRepository<Category> categoryrepository;
        private readonly IRepository<MenusRoles> _MenuRolesRepository;
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _Context;
        private readonly IRepository<Params> _paramsRepository;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _Configration;
        public MenuService(IRepository<Posts> PostRepository,
                            IBlogArchive BlogArchiveService,
                            IRepository<Page> pageRepository,
                            IRepository<Menu> menuRepository,
                            IRepository<Category> CategoriesRepository,
                            IRepository<CategoryDetails> CategoriesDetailsRepository,
                            IRepository<ProductCategory> ProductCategoryRepository,
                            IRepository<MenuGroup> MenuGroupRepository,
                            IUserRoleService UserRoleService,
                            IMenuRole MenuRoleService, IUserSiteService userSiteService,

                            ISlugServices SlugServices,
                            IRepository<Category> Categoryrepository,
                             IRepository<MenusRoles> MenuRolesRepository,
                            IWorkContext workContext,
                            EskaDCMSDbContext Context,
                            IRepository<Params> ParamsRepository,
                            ICacheService cacheService,
                            IConfiguration Configration
                           )
        {
            _MenuRepository = menuRepository;
            // _MenuSettingsRepository = menuSettingsRepository;
            _CategoriesRepository = CategoriesRepository;
            categoriesDetailsRepository = CategoriesDetailsRepository;
            productCategoryRepository = ProductCategoryRepository;
            _pageRepository = pageRepository;
            _PostRepository = PostRepository;
            _BlogArchiveService = BlogArchiveService;
            _UserRoleService = UserRoleService;
            _MenuRoleService = MenuRoleService;
            this.userSiteService = userSiteService;
            slugServices = SlugServices;
            categoryrepository = Categoryrepository;
            _MenuRolesRepository = MenuRolesRepository;
            _workContext = workContext;
            _MenuGroupRepository = MenuGroupRepository;
            _Context = Context;
            _paramsRepository = ParamsRepository;
            _cacheService = cacheService;
            _Configration = Configration;
        }

        public MenuService()
        {
        }

        public async Task<MenuViewModel> GetMenuByIdWithChilds(long id)
        {
            try
            {
                // get published static menu
                var Menu = await _MenuRepository.Query().Where(y => y.Id == id)
                    .Include(x => x.Thumbnail)
                    //  .Include(x => x.MenuSettings)
                    .Include(x => x.MenuGroup)
                    .Include(x => x.Post)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Page)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Params)

                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           CategoryId = x.CategoryId,
                           ProductId = x.ProductId,
                           BrandId = x.BrandId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId,
                           Slug = GetSlug(x) + AddParamsToSlug(x),
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           // Content = x.MenuSettings.Settings,
                           Order = x.Order,
                           ExternalSlug = x.ExternalSlug
                           //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,

                       }
                      ).FirstOrDefaultAsync();

                Menu.Childs = GetChilds(Menu.Id, EStatus.Published);

                return Menu;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        private static string AddParamsToSlug(Menu menu)
        {
            try
            {
                if (menu?.Params != null && menu.Params.Count() > 0)
                {
                    var Params = menu.Params.Select(x => x.Key + "=" + x.Value).ToList();

                    var Querystring = string.Join("&", Params);



                    return "?" + Querystring;
                }

                return "";
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<MenuViewModel> GetMenuById(long id)
        {
            try
            {
                // get published static menu
                var Menu = await _MenuRepository.Query().Where(y => y.Id == id)
                    .Include(x => x.Thumbnail)
                    .Include(x => x.MenuGroup)
                    //.Include(x => x.MenuSettings)
                    .Include(x => x.Post)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Page)
                    .ThenInclude(x => x.SlugInfo)
                   .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Params)
                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           CategoryId = x.CategoryId,
                           ProductId = x.ProductId,
                           BrandId = x.BrandId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                           Slug = GetSlug(x),
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           MenuGroupId = x.MenuGroupId,
                           Params = x.Params == null ? new List<Models.ParamsViewModel>() : x.Params.Select(x => new Models.ParamsViewModel { Id = x.Id, Key = x.Key, Order = x.Order, Value = x.Value }).ToList(),

                           //  Content = x.MenuSettings.Settings,
                           Order = x.Order,

                           ExternalSlug = x.ExternalSlug
                           // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,

                       }
                      ).FirstOrDefaultAsync();

                return Menu;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<MenuBreadCrumbVM>> GetMenuBreadCrumb(long id)
        {
            try
            {
                var TargetMenu = await _MenuRepository.Query()
                                            .Where(y => y.Id == id)
                                            .Include(x => x.Thumbnail)
                                            .Include(x => x.MenuGroup)
                                            .Include(x => x.Post)
                                            .ThenInclude(x => x.SlugInfo)
                                            .Include(x => x.Page)
                                            .ThenInclude(x => x.SlugInfo)
                                           .Include(x => x.Product)
                                            .ThenInclude(x => x.ProductDetails)
                                            .ThenInclude(x => x.SlugInfo)
                                            .Include(x => x.Category)
                                            .ThenInclude(x => x.CategoryDetails)
                                            .ThenInclude(x => x.SlugInfo)
                                                .Include(x => x.Brand)
                                            .ThenInclude(x => x.BrandDetails)
                                            .ThenInclude(x => x.SlugInfo)
                                            .Include(x => x.Params)
                                            .Select(x =>
                                                       new MenuBreadCrumbVM
                                                       {
                                                           Id = x.Id,
                                                           Name = x.Name,
                                                           Icon = x.Icon,
                                                           Title = x.Title,
                                                           ThumbnailURL = x.Thumbnail.PublicUrl,
                                                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                                                           Slug = GetSlug(x),// x.Slug,
                                                           ParentId = x.ParentId,
                                                           MenuGroupId = x.MenuGroupId,
                                                       })
                                            .FirstOrDefaultAsync();

                var Menus = await _MenuRepository
                    .Query()
                    .Where(y => y.MenuGroupId == TargetMenu.MenuGroupId &&
                                y.Status != EStatus.Deleted)
                   .Include(x => x.Thumbnail)
                    .Include(x => x.MenuGroup)
                    .Include(x => x.Post)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Page)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Params)
                    .Select(x =>
                       new MenuBreadCrumbVM
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                           Slug = GetSlug(x) + AddParamsToSlug(x),
                           ParentId = x.ParentId,
                           MenuGroupId = x.MenuGroupId,
                           MenuType = x.MenuType

                       }
                      ).ToListAsync();


                var Result = await GetMenuBreadCrumb(TargetMenu, Menus, new List<MenuBreadCrumbVM>());
                Result.Reverse();
                return Result;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<MenuBreadCrumbVM>> GetMenuBreadCrumb(MenuBreadCrumbVM TargetMenu, List<MenuBreadCrumbVM> MenuList, List<MenuBreadCrumbVM> MenuBreadCrumb)
        {

            if (TargetMenu == null)
            {
                return MenuBreadCrumb;
            }
            else
            {
                var TargetMenuParent = MenuList.FirstOrDefault(m => m.Id == TargetMenu.ParentId);
                MenuBreadCrumb.Add(TargetMenu);
                return await GetMenuBreadCrumb(TargetMenuParent, MenuList, MenuBreadCrumb);
            }
        }

        public async Task<MenuViewModel> GetParentMenus(long MenuId)
        {
            try
            {
                var Menu = await GetMenuById(MenuId);
                var MenuBreadCrumb = await GetMenuBreadCrumb(MenuId);
                var ParentIds = MenuBreadCrumb.Select(x => x.Id).ToList();
                var MenusInSameGroup = await GetMenusByGroupId(Menu.MenuGroupId);
                var result = MenusInSameGroup.Where(x => ParentIds.Contains(x.Id)).FirstOrDefault();

                return result;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }



        public async Task<IList<MenuViewModel>> GetMenusBySiteIdAndMenuType(long siteId, EMenuTypes Type)
        {
            try
            {
                // get not deleted static menu
                var Menus = await _MenuRepository.Query()
                    .Where(y => y.MenuType == Type && y.Status != EStatus.Deleted && y.SiteId == siteId)
                    .Include(x => x.Thumbnail)
                      // .Include(x => x.MenuSettings)
                      .Include(x => x.Post)
                     .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.MenuGroup)
                    .Include(x => x.Page)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Params)
                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           BrandId = x.BrandId,
                           CategoryId = x.CategoryId,
                           ProductId = x.ProductId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId,
                           Slug = GetSlug(x) + AddParamsToSlug(x),
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           //   Content = x.MenuSettings.Settings,
                           Order = x.Order,

                           ExternalSlug = x.ExternalSlug
                           //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                       }
                      ).ToListAsync();

                return Menus;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<List<MenuViewModel>> GetMenusByCategoryId(long CatId, EStatus status, long userId, SourceMenuType sourceType)
        {
            try
            {
                bool isGuest = await _workContext.IsGuestUser();
                var CultureId = _workContext.GetCurrentCultureId();
                List<MenuViewModel> MenuItems = new List<MenuViewModel>();
                var query = _MenuRepository.Query();
                switch (sourceType)
                {
                    case SourceMenuType.Admin:
                        query = query.Include(q => q.MenuGroup).Where(x => x.MenuGroup.CategoryId == CatId);
                        break;
                    case SourceMenuType.Web:
                        var securityMenuList = await _MenuRolesRepository.Query().Select(x => x.MenuId)
            .Distinct().ToListAsync();
                        if (!isGuest)
                        {
                            // If normal user --> get authorized and all public menu for this category
                            var RoleObj = await _UserRoleService.GetRoleByUserId(userId);
                            var userRoleIds = RoleObj.Select(x => x.RoleId).ToList();
                            if (userRoleIds != null && userRoleIds.Count > 0)
                            {
                                query = query
                                    .Include(x => x.MenuGroup)
                                    .Include(x => x.MenuRoles)
                                    .Where(x =>
                                    (
                                        (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) && x.MenuGroup.CategoryId == CatId) ||
                                        (!securityMenuList.Contains(x.Id) && x.MenuGroup.CategoryId == CatId)
                                    ));
                            }
                        }
                        else
                        {
                            // If guest user --> get public menu for this category
                            query = query.Include(x => x.MenuGroup).Where(x => x.MenuGroup.CategoryId == CatId && !securityMenuList.Contains(x.Id));
                        }
                        break;
                }

                var items = await query.Where(x => ((x.Status == status) || (status == EStatus.All && x.Status != EStatus.Deleted))
                          && x.ParentId == null)
                         .Include(x => x.Post)
                         .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Page)
                         .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Parent)
                         .Include(x => x.MenuGroup)

                           .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails.Where(x => x.CultureId == CultureId))
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails.Where(x => x.CultureId == CultureId))
                    .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Params)

                         .ToListAsync();

                MenuItems = items.Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      ProductId = x.ProductId,
                                      PostId = x.PostId,
                                      BrandId = x.BrandId,
                                      CategoryId = x.CategoryId,
                                      MenuType = x.MenuType,
                                      ThumbnailId = x.ThumbnailId,
                                      Title = x.Title,
                                      ThumbnailURL = x.Thumbnail == null ? "" : x.Thumbnail.PublicUrl,
                                      Slug = x.MenuType == EMenuTypes.None ? "" : GetSlug(x) + AddParamsToSlug(x), //x.MenuType == EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,

                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      ParentId = x.ParentId,
                                      ExternalSlug = x.ExternalSlug
                                      //Content = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.Settings).FirstOrDefault(),
                                      //AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                                  }
                                 ).OrderBy(x => x.Order).Distinct().ToList();

                foreach (var item in MenuItems)
                {
                    item.Childs = GetChilds(item.Id, status);
                }
                return MenuItems;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<Menu>> GetMenuByCategoryCultureParentId(long cultureParentId)
        {
            try
            {
                var RelatedCategories = await _CategoriesRepository.Query()
                    .AsNoTracking()
                    .Where(p => p.ParentId == cultureParentId && p.ParentId != null)
                    .Select(s => s.Id)
                    .ToListAsync();

                return await _MenuRepository.Query()
                    .AsNoTracking()
                    .Where(m => (m.CategoryId.HasValue && RelatedCategories.Contains(m.CategoryId.Value)))
                    .Include(m => m.MenuGroup)
                    .ToListAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }



        }
        public static string GetSlug(Menu menu)
        {
            if (menu == null)
            {
                return string.Empty;
            }

            switch (menu.MenuType)
            {
                case EMenuTypes.Page:
                    return menu.Page?.SlugInfo?.Slug ?? string.Empty;
                case EMenuTypes.Post:
                    return menu.Post?.SlugInfo?.Slug ?? string.Empty;
                case EMenuTypes.Product:
                    return menu.Product?.ProductDetails?.Select(x => x.SlugInfo?.Slug).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
                case EMenuTypes.URL:
                    return menu.ExternalSlug ?? string.Empty;
                case EMenuTypes.None:
                    return string.Empty;
                case EMenuTypes.Category:
                    return menu.Category?.CategoryDetails?.Select(x => x.SlugInfo?.Slug).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

                case EMenuTypes.Brand:
                    return menu.Brand?.BrandDetails?.Select(x => x.SlugInfo?.Slug).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
                default:
                    return string.Empty;
            }
        }
        public async Task<List<MenuViewModel>> GetMenusByCategoryIdForWebAsync(long CatId)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();

                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                var CurrentUserId = await _workContext.GetCurrentUserId();

                List<Menu> Query = new List<Menu>(); // for top level only
                List<Menu> AllSubMenus = new List<Menu>();

                List<MenuViewModel> MenuItems = new List<MenuViewModel>();
                var RoleObj = await _UserRoleService.GetRoleByUserId(CurrentUserId);
                var userRoleIds = RoleObj.Select(x => x.RoleId).ToList();

                // If normal user --> get authorized and all public menu for this category
                Query = await _MenuRepository.Query()
                       .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.CategoryId == CatId
                        && x.Status == EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId == null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                        .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Post)
                        .ThenInclude(x => x.SlugInfo)

                      .Include(x => x.Product)
                       .ThenInclude(x => x.ProductDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Category)
                       .ThenInclude(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Brand)
                       .ThenInclude(x => x.BrandDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                       .Include(x => x.MenuRoles).AsNoTracking().ToListAsync();



                AllSubMenus = await _MenuRepository.Query()
                      .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.CategoryId == CatId
                        && x.Status == EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId != null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Product)
                       .ThenInclude(x=>x.ProductDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Category)
                       .ThenInclude(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Brand)
                       .ThenInclude(x=>x.BrandDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Params)
                       .Include(x => x.MenuRoles)
                       .Include(x => x.MenuGroup)
                       .AsNoTracking()
                       .ToListAsync();


                MenuItems = Query.Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      CategoryId = x.CategoryId,
                                      ProductId = x.ProductId,
                                      BrandId = x.BrandId,
                                      PostId = x.PostId,
                                      MenuType = x.MenuType,
                                      ThumbnailURL = x.Thumbnail.PublicUrl,
                                      Title = x.Title,
                                      //  Image = x.Image,
                                      Slug = GetSlug(x) + AddParamsToSlug(x), //x.MenuType == EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,
                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                      ParentId = x.ParentId,
                                      ExternalSlug = x.ExternalSlug
                                      //Content = query.
                                      // AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                                  }
                                 ).OrderBy(x => x.Order).Distinct().ToList();

                //foreach (var item in MenuItems)
                //{
                //    item.Childs = GetChildsForWeb(item.Id);
                //}
                return MenuItems;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public List<TabViewModel> GetTabsByCategoryId(long CatId)
        {
            try
            {
                List<TabViewModel> tabs = _MenuRepository.Query()
                    .Include(x => x.MenuGroup)
                    .Where(x => x.MenuGroup.CategoryId == CatId && x.Status == EStatus.Published && x.ParentId == null)
                    .Include(x => x.Thumbnail)
                     .Include(x => x.Post)
                         .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Page)
                         .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Params)

                    .Select(t => new TabViewModel
                    {
                        Id = t.Id,
                        ThumbnailURL = t.Thumbnail.PublicUrl,
                        ThumbnailId = t.ThumbnailId.HasValue ? t.ThumbnailId.Value : null,
                        Name = t.Name,
                        Slug = GetSlug(t) + AddParamsToSlug(t),
                    }).ToList();
                return tabs;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        //Checked with Rami, this action is not used at all //Firas
        //public async Task<SubMenuViewModel> GetSubMenu(long catId, long id, string idType)
        //{
        //    try
        //    {
        //        var subMenuVM = new SubMenuViewModel();

        //        if (idType == "1")
        //        {
        //            subMenuVM.currentMenu = await _MenuRepository.Query()
        //                .Where(y => y.CategoryId == catId && y.Status == EStatus.Published
        //                && y.MenuType == EMenuTypes.Page
        //                && y.PageId == id)
        //                .Include(x => x.Thumbnail)
        //                .Include(x=>x.Parent)
        //                .Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId=x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0,

        //             }).FirstOrDefaultAsync();
        //            subMenuVM.parentMenu = subMenuVM.currentMenu.Parent.Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId = x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefault();

        //            subMenuVM.menuSiblings = await _MenuRepository.Query().Where(y => y.CategoryId == catId && y.ParentId == subMenuVM.currentMenu.ParentId && y.Status == EStatus.Published).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId = x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }
        //              ).ToArrayAsync();

        //            if (subMenuVM.menuSiblings.Count > 0)
        //            {
        //                foreach (var item in subMenuVM.menuSiblings)
        //                {
        //                    if (item.Type == EMenuTypes.Page)
        //                    {
        //                        item.Slug = _pageRepository.Query().Where(y => y.Id == item.PageId).FirstOrDefault().Slug;
        //                    }

        //                    item.Childs = GetChilds(item.Id, EStatus.Published);
        //                }
        //            }

        //            return subMenuVM;

        //        }
        //        else if (idType == "2")
        //        {
        //            subMenuVM.currentMenu = await _MenuRepository.Query().Where(y => y.Id == id).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 Type = x.Type,
        //                 Url = x.Url,
        //                 Title = x.Title,
        //                 Image = x.Image,
        //                 Slug = x.Slug,
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 ParentId = x.ParentId,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefaultAsync();
        //            subMenuVM.parentMenu = await _MenuRepository.Query().Where(y => y.Id == subMenuVM.currentMenu.ParentId).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 Type = x.Type,
        //                 Url = x.Url,
        //                 Title = x.Title,
        //                 Image = x.Image,
        //                 Slug = x.Slug,
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefaultAsync();
        //            subMenuVM.menuSiblings = await _MenuRepository.Query().Where(y => y.CategoryId == catId && y.ParentId == subMenuVM.currentMenu.ParentId && y.Status == EStatus.Published).Select(x =>
        //              new MenuViewModel
        //              {
        //                  Id = x.Id,
        //                  Name = x.Name,
        //                  Icon = x.Icon,
        //                  PageId = x.PageId,
        //                  Type = x.Type,
        //                  Url = x.Url,
        //                  Title = x.Title,
        //                  Image = x.Image,
        //                  Slug = x.Slug,
        //                  TemplateId = x.TemplateId,
        //                  SiteId = x.SiteId,
        //                  Status = x.Status,
        //                  CategoriesId = x.CategoryId ?? 0
        //              }
        //              ).ToArrayAsync();

        //            if (subMenuVM.menuSiblings.Count > 0)
        //            {
        //                foreach (var item in subMenuVM.menuSiblings)
        //                {
        //                    if (item.Type == EMenuTypes.Page)
        //                    {
        //                        item.Slug = _pageRepository.Query().Where(y => y.Id == item.PageId).FirstOrDefault().Slug;
        //                    }
        //                    item.Childs = GetChilds(item.Id, EStatus.Published);
        //                }
        //            }
        //            return subMenuVM;

        //        }
        //        else if (idType == "3")
        //        {
        //            var blog = _PostRepository.Query().Where(y => y.Id == id).FirstOrDefault();

        //            subMenuVM.currentMenu = new MenuViewModel
        //            {
        //                Id = blog.Id,
        //                Name = blog.Title,
        //                Slug = blog.Slug
        //            };

        //            //var blogCat = await _CategoriesService.GetCategoriesById(blog.PostCategoryId);// _PostCategoriesRepository.Query().Where(y => y.Id == blog.PostCategoryId).FirstOrDefault();
        //            //subMenuVM.parentMenu = new MenuViewModel
        //            //{
        //            //    Id = blogCat.Id,
        //            //    Name = blogCat.Name,
        //            //};


        //            dynamic blogs = await _BlogArchiveService.GetPosts(blog.SiteId, -1, 1, 100);

        //            subMenuVM.menuSiblings = new List<MenuViewModel>();

        //            foreach (var item in blogs.PostsList)
        //            {
        //                var s = new MenuViewModel();
        //                s.Id = item.Id;
        //                s.Name = item.Title;
        //                s.Slug = item.Slug;
        //                s.Type = EMenuTypes.Page;

        //                subMenuVM.menuSiblings.Add(s);
        //            }

        //        }



        //        return subMenuVM;

        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
        public List<MenuViewModel> GetChildsForWeb(long ParentId, List<Menu> AllMenuItems)
        {
            try
            {


                var ChildMenuItems = AllMenuItems
                    .Where(x => x.ParentId == ParentId)
                    .Select(x => new MenuViewModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Icon = x.Icon,
                        PageId = x.PageId,
                        MenuType = x.MenuType,
                        CategoryId = x.CategoryId,
                        ProductId = x.ProductId,
                        PostId = x.PostId,
                        BrandId = x.BrandId,
                        Title = x.Title,
                        Slug = GetSlug(x) + AddParamsToSlug(x),
                        TemplateId = x.TemplateId,
                        SiteId = x.SiteId,
                        Status = x.Status,
                        MenuGroupId = x.MenuGroupId,
                        Order = x.Order,
                        Childs = GetChildsForWeb(x.Id, AllMenuItems),
                        ParentId = x.ParentId,
                        ThumbnailId = x.ThumbnailId,
                        ThumbnailURL = x.Thumbnail?.PublicUrl,
                        ExternalSlug = x.ExternalSlug,
                        Expanded = true,
                        Params = x.Params.Select(x => new EskaCMS.Menus.Models.ParamsViewModel()
                        {
                            Id = x.Id,
                            Value = x.Value,
                            Key = x.Key,
                        }).ToList()
                    }
                  ).OrderBy(x => x.Order).DistinctBy(x => x.Id).ToList();

                return ChildMenuItems;


            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public List<MenuViewModel> GetChilds(long parentId, EStatus status, List<MenusRoles> MenuRoles = null)
        {
            try
            {

                List<MenuViewModel> AuthorizedMenuList = new List<MenuViewModel>();
                if (MenuRoles?.Count > 0)
                {
                    var AuthrizedMenuItems = _MenuRepository.Query()
                        .Where(x => MenuRoles.Select(y => y.MenuId).ToList().Contains(x.Id)
                        && x.ParentId == parentId
                        && ((x.Status == status) || (status == EStatus.All && x.Status != EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(p => p.Page)
                        .ThenInclude(x => x.SlugInfo)

                        .Include(p => p.Post)
                        .ThenInclude(x => x.SlugInfo)

                         .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                        .Include(x => x.Parent)
                        //  .Include(x=>x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            CategoryId = x.CategoryId,
                            ProductId = x.ProductId,
                            BrandId = x.BrandId,
                            PostId = x.PostId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,

                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x) + AddParamsToSlug(x),
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            // CategoriesId = x.CategoryId ?? 0,
                            MenuGroupId = x.MenuGroupId,
                            Order = x.Order,
                            ParentId = parentId,
                            ExternalSlug = x.ExternalSlug
                            // Content = x.MenuSettings.Settings,
                            // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                      ).OrderBy(x => x.Order).Distinct().ToList();
                    foreach (var item in AuthrizedMenuItems)
                    {
                        item.Childs = GetChilds(item.Id, status);
                        AuthorizedMenuList.Add(item);
                    }

                    return AuthorizedMenuList.Distinct().ToList();
                }
                else
                {
                    var Menus = _MenuRepository.Query()
                        .Where(y => y.ParentId == parentId
                        && ((y.Status == status) || (status == EStatus.All && y.Status != EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(p => p.Page)
                        .ThenInclude(x => x.SlugInfo)
                         .Include(p => p.Post)
                        .ThenInclude(x => x.SlugInfo)
                         .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                        // .Include(x=>x.MenuSettings)
                        .Select(x =>
                          new MenuViewModel
                          {
                              MenuGroupId = x.MenuGroupId,
                              Id = x.Id,
                              Name = x.Name,
                              Icon = x.Icon,
                              PageId = x.PageId,
                              CategoryId = x.CategoryId,
                              ProductId = x.ProductId,
                              BrandId = x.BrandId,
                              PostId = x.PostId,
                              MenuType = x.MenuType,
                              ThumbnailId = x.ThumbnailId,
                              Title = x.Title,

                              ThumbnailURL = x.Thumbnail.PublicUrl,
                              Slug = GetSlug(x) + AddParamsToSlug(x),
                              TemplateId = x.TemplateId,
                              SiteId = x.SiteId,
                              Status = x.Status,
                              // CategoriesId = x.CategoryId ?? 0,
                              Order = x.Order,
                              ParentId = parentId
                              //   Content = x.MenuSettings.Settings,
                              // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                          }
                         ).OrderBy(x => x.Order).ToList();
                    if (Menus.Count > 0)
                    {
                        foreach (var item in Menus)
                        {
                            item.Childs = GetChilds(item.Id, status);
                        }
                    }
                    else
                        Menus = new List<MenuViewModel>();
                    return Menus;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MenuViewModel> GetChilds(long parentId, EStatus status, SourceMenuType source, List<MenusRoles> MenuRoles = null)
        {
            try
            {
                var securedMenu = _MenuRolesRepository.Query().Select(x => x.MenuId)
                     .Distinct().ToList();
                List<MenuViewModel> AuthorizedMenuList = new List<MenuViewModel>();
                if (MenuRoles?.Count > 0)
                {

                    var AuthrizedMenuItems = _MenuRepository.Query()
                        .Where(x => MenuRoles.Select(y => y.MenuId).ToList().Contains(x.Id)
                        && x.ParentId == parentId
                        && ((x.Status == status) || (status == EStatus.All && x.Status != EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(p => p.Page)
                        .ThenInclude(x => x.SlugInfo)
                        .Include(p => p.Post)
                        .ThenInclude(x => x.SlugInfo)
                          .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                        // .Include(x => x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            PostId = x.PostId,
                            ProductId = x.ProductId,

                            CategoryId = x.CategoryId,
                            BrandId = x.BrandId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,
                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x) + AddParamsToSlug(x),
                            ExternalSlug = GetSlug(x) + AddParamsToSlug(x),
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            //CategoriesId = x.CategoryId ?? 0,
                            MenuGroupId = x.MenuGroupId,
                            Order = x.Order,
                            //   Content = x.MenuSettings.Settings,
                            //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                      ).Distinct().ToList();
                    foreach (var item in AuthrizedMenuItems)
                    {
                        item.Childs = GetChilds(item.Id, status);
                        AuthorizedMenuList.Add(item);
                    }

                    return AuthorizedMenuList.Distinct().ToList();
                }
                else
                {
                    var Menus = _MenuRepository.Query()
                        .Where(y => y.ParentId == parentId
                    && !securedMenu.Contains(y.Id)
                    && ((y.Status == status) || (status == EStatus.All && y.Status != EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(x => x.MenuGroup)
                         .Include(p => p.Page)
                        .ThenInclude(x => x.SlugInfo)

                        .Include(p => p.Post)
                        .ThenInclude(x => x.SlugInfo)

                         .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                     .Include(x => x.Brand)
                    .ThenInclude(x => x.BrandDetails)
                    .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                        // .Include(x => x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            CategoryId = x.CategoryId,
                            ProductId = x.ProductId,
                            PostId = x.PostId,
                            BrandId = x.BrandId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,
                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x) + AddParamsToSlug(x),
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            ExternalSlug = x.ExternalSlug,
                            CategoriesId = x.MenuGroup.CategoryId ?? 0,
                            Order = x.Order,
                            //   Content = x.MenuSettings.Settings,
                            //   AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                         ).ToList();
                    if (Menus.Count > 0)
                    {
                        foreach (var item in Menus)
                        {
                            item.Childs = GetChilds(item.Id, status);
                        }
                    }
                    else
                        Menus = new List<MenuViewModel>();
                    return Menus;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        public async Task<long> AddMenu(MenuViewModel menu)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();
                long UserId = await _workContext.GetCurrentUserId();
                IList<Params> Params = new List<Params>();
                if (!menu.Order.HasValue || menu.Order == 0)
                {
                    var maximumOrder = await GetMaximumOrder(menu.ParentId, SiteId, menu.MenuGroupId);

                    if (maximumOrder != null)
                    {
                        menu.Order = maximumOrder + 1;
                    }
                    else
                    {
                        menu.Order = 1;
                    }
                }
                var m = new Menu();

                m.Name = menu.Name;
                m.Icon = menu.Icon;
                m.PageId = menu.PageId;
                m.MenuType = menu.MenuType;
                m.Title = menu.Title;
                m.CategoryId = menu.CategoryId;
                m.PostId = menu.PostId;
                m.BrandId = menu.BrandId;
                m.ProductId = menu.ProductId;
                m.ThumbnailId = menu.ThumbnailId;
                m.ExternalSlug = menu.ExternalSlug;
                m.CreatedById = menu.UserId;
                m.SiteId = menu.SiteId;
                m.Status = EStatus.Unpublished;
                m.MenuGroupId = menu.MenuGroupId;
                m.ParentId = menu.ParentId;
                m.TemplateId = menu.TemplateId;
                m.Order = menu.Order;
                switch (menu.MenuType)
                {

                    case EMenuTypes.Page:
                        m.PageId = menu.SlugTargetId;
                        break;
                    case EMenuTypes.Post:
                        m.PostId = menu.SlugTargetId;
                        break;
                    case EMenuTypes.Product:
                        m.ProductId = menu.SlugTargetId;
                        break;
                    case EMenuTypes.Category:
                        m.CategoryId = menu.SlugTargetId;
                        break;

                    case EMenuTypes.Brand:
                        m.BrandId = menu.SlugTargetId;
                        break;
                }

                _MenuRepository.Add(m);
                await _MenuRepository.SaveChangesAsync();

                foreach (var param in menu.Params)
                {
                    var Eparam = new Params();
                    Eparam.Key = param.Key;
                    Eparam.Value = param.Value;
                    Eparam.CreatedById = UserId;
                    Eparam.CreationDate = DateTimeOffset.Now;
                    Eparam.SiteId = SiteId;
                    Eparam.MenuId = m.Id;
                    Eparam.Order = param.Order;

                    Params.Add(Eparam);

                }
                if (Params.Count() > 0)
                {

                    _paramsRepository.AddRange(Params);
                    await _paramsRepository.SaveChangesAsync();


                }

                return m.Id;

            }
            catch (Exception exc)
            {
                throw;
            }
        }



        public async Task<bool> AddEditMenu(List<MenuViewModel> menus, long SiteId, long UserId)
        {
            var strategy = _Context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var scope = await _Context.Database.BeginTransactionAsync();
                try
                {
                    menus = menus.OrderByDescending(m => m.Id).ToList();




                    foreach (var menu in menus)
                    {
                        Menu Menu;


                        if (menu.Id == 0)
                        {
                            Menu = new Menu()
                            {
                                CreationDate = DateTimeOffset.Now,
                                Icon = menu.Icon,
                                MenuGroupId = menu.MenuGroupId,
                                CreatedById = UserId,
                                SiteId = SiteId,
                                Description = menu.Description,
                                TemplateId = menu.TemplateId,
                                Title = menu.Title,

                                ThumbnailId = menu.ThumbnailId,
                                ExternalSlug = menu.ExternalSlug,
                                ParentId = menu.ParentId,
                                Status = menu.Status,
                                ModifiedById = UserId,
                                Name = menu.Name,

                                ProductId = menu.ProductId,
                                SlugTargetId = menu.SlugTargetId,
                                PostId = menu.PostId,
                                Order = menu.Order,

                            };

                            if (menu.Order == null || menu.Order == 0)
                            {
                                var MaxOrder = await GetMaximumOrder(menu.ParentId, SiteId, menu.MenuGroupId);
                                Menu.Order = MaxOrder.HasValue ? MaxOrder.Value + 1 : 0;
                            }
                            else
                            {
                                Menu.Order = menu.Order;
                            }

                            Menu.MenuType = menu.MenuType;
                            switch (menu.MenuType)
                            {
                                case EMenuTypes.Page:
                                    Menu.PageId = menu.PageId;

                                    break;
                                case EMenuTypes.Post:
                                    Menu.PostId = menu.PostId;

                                    break;
                                case EMenuTypes.Product:
                                    Menu.ProductId = menu.ProductId;

                                    break;
                                case EMenuTypes.None:

                                    break;
                                case EMenuTypes.Category:
                                    Menu.CategoryId = menu.CategoryId;

                                    break;

                                case EMenuTypes.Brand:
                                    Menu.BrandId = menu.SlugTargetId;

                                    break;
                                default:
                                    break;
                            }

                            _MenuRepository.Add(Menu);
                        }
                        else
                        {
                            Menu = await _MenuRepository.Query().Include(x => x.MenuGroup).Where(y => y.Id == menu.Id).FirstOrDefaultAsync();
                            if (Menu == null)
                            {
                                throw new Exception("Could not find menu item");
                            }

                            Menu.MenuType = menu.MenuType;
                            switch (menu.MenuType)
                            {
                                case EMenuTypes.Page:
                                    Menu.PageId = menu.SlugTargetId;

                                    break;
                                case EMenuTypes.Post:
                                    Menu.PostId = menu.SlugTargetId;

                                    break;
                                case EMenuTypes.Product:
                                    Menu.ProductId = menu.SlugTargetId;

                                    break;
                                case EMenuTypes.None:

                                    break;
                                case EMenuTypes.Category:
                                    Menu.CategoryId = menu.SlugTargetId;

                                    break;
                                case EMenuTypes.Brand:
                                    Menu.BrandId = menu.SlugTargetId;

                                    break;
                                default:
                                    break;
                            }
                        }


                    }


                    await _MenuRepository.SaveChangesAsync();

                    await scope.CommitAsync();


                    return true;

                }
                catch (Exception exc)
                {
                    await scope.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> EditMenu(int userId, long menuId, MenuViewModel EditedMenu)
        {
            try
            {
                var Menu = await _MenuRepository.Query().Include(x => x.Params).Where(y => y.Id == menuId).FirstOrDefaultAsync();
                IList<Params> Params = new List<Params>();

                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserId = await _workContext.GetCurrentUserId();
                Menu.Name = EditedMenu.Name;
                Menu.Icon = EditedMenu.Icon;
                // Menu.PageId = EditedMenu.PageId;

                Menu.Title = EditedMenu.Title;
                Menu.ThumbnailId = EditedMenu.ThumbnailId;
                Menu.ExternalSlug = EditedMenu.ExternalSlug;
                Menu.ModifiedById = userId;
                Menu.MenuGroupId = EditedMenu.MenuGroupId;
                Menu.ModificationDate = DateTimeOffset.Now;
                Menu.TemplateId = EditedMenu.TemplateId;
                if (!(EditedMenu.ParentId == 0 && Menu.ParentId == null) || Menu.ParentId != EditedMenu.ParentId)
                {
                    Menu.ParentId = EditedMenu.ParentId == 0 ? null : EditedMenu.ParentId;
                    if (!EditedMenu.Order.HasValue && EditedMenu.Order == 0)
                    {
                        var maximumOrder = await GetMaximumOrder(EditedMenu.ParentId, SiteId, Menu.MenuGroupId);

                        Menu.Order = maximumOrder == null ? 0 : maximumOrder + 1;
                    }
                    else
                    {
                        Menu.Order = EditedMenu.Order;
                    }
                }
                Menu.MenuType = EditedMenu.MenuType;
                switch (EditedMenu.MenuType)
                {

                    case EMenuTypes.Page:
                        Menu.PageId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Post:
                        Menu.PostId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Product:
                        Menu.ProductId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Category:
                        Menu.CategoryId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Brand:
                        Menu.BrandId = EditedMenu.SlugTargetId;

                        break;
                }
                await _MenuRepository.SaveChangesAsync();
                var ParamsRemove = await _paramsRepository.Query().Where(x => x.MenuId == menuId).ToListAsync();
                _paramsRepository.RemoveRange(ParamsRemove);
                await _paramsRepository.SaveChangesAsync();
                foreach (var param in EditedMenu.Params)
                {
                    var Eparam = new Params();
                    Eparam.Key = param.Key;
                    Eparam.Value = param.Value;
                    Eparam.CreatedById = UserId;
                    Eparam.CreationDate = DateTimeOffset.Now;
                    Eparam.SiteId = SiteId;
                    Eparam.MenuId = menuId;
                    Eparam.Order = param.Order;

                    Params.Add(Eparam);

                }
                if (Params.Count() > 0)
                {

                    _paramsRepository.AddRange(Params);
                    await _paramsRepository.SaveChangesAsync();


                }
                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> EditMenuStatus(int userId, long id, int statusId)
        {
            try
            {
                if ((EStatus)statusId == EStatus.Deleted)
                {
                    await DeleteMenu(userId, id);
                }
                else
                {
                    var menu = await _MenuRepository.Query().Where(y => y.Id == id).FirstOrDefaultAsync();
                    menu.Status = (EStatus)statusId;
                    menu.ModifiedById = userId;
                    await _MenuRepository.SaveChangesAsync();

                }


                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> publishAllMenus(publishMenuViewModel menuObj, long UserId, long SiteId)
        {
            try
            {
                foreach (long MenuId in menuObj.MenuIds)
                {
                    var menuList = await _MenuRepository.Query().Where(x => x.Id == MenuId && x.SiteId == SiteId).Select(x => x).FirstOrDefaultAsync();
                    menuList.Status = menuObj.status;
                    menuList.ModifiedById = UserId;
                }
                await _MenuRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        //end bulk update,delete,publish

        public async Task<bool> DeleteMenu(long userId, long menuId)
        {
            try
            {
                var MenuList = await _MenuRepository.Query().Where(y => y.Id == menuId || y.ParentId == menuId).ToListAsync();
                var SelectedMenu = MenuList.FirstOrDefault(y => y.Id == menuId);
                SelectedMenu.Status = EStatus.Deleted;
                SelectedMenu.ModifiedById = userId;
                SelectedMenu.ModificationDate = DateTimeOffset.Now;
                _MenuRepository.Update(SelectedMenu);


                foreach (var MenuItem in MenuList)
                {
                    MenuItem.ParentId = SelectedMenu.ParentId;
                    _MenuRepository.Update(MenuItem);
                }
                await _MenuRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<long> DuplicateMenuCategory(MenuViewModel menu)
        {
            var oldId = menu.Id;
            var NewCat = await GetCategoriesById(menu.Id);
            NewCat.Id = 0;
            var newCatId = await UpsertCategory(NewCat, menu.Name, menu.UserId);// await AddMenuCategory(siteId, userId, menu.Name);
            var copiedMenu = await GetMenusByCategoryId(oldId, EStatus.All, menu.UserId, SourceMenuType.Admin);

            if (copiedMenu != null && copiedMenu.Count > 0)
            {
                foreach (var item in copiedMenu)
                {
                    var m = item;
                    m.CategoriesId = newCatId;
                    m.UserId = menu.UserId;
                    var newMenu = await AddMenu(m);
                    if (item.Childs != null && item.Childs.Count > 0)
                    {
                        await addChildesMenuAsync(menu.SiteId, menu.UserId, item, newMenu);

                    }
                }
            }

            return newCatId;
        }

        private async Task addChildesMenuAsync(long siteId, long userId, MenuViewModel m, long parentId)
        {

            if (m.Childs != null && m.Childs.Count > 0)
            {
                foreach (var item in m.Childs)
                {
                    item.UserId = userId;
                    item.ParentId = parentId;
                    var newMenu = await AddMenu(item);
                    var Menu = await GetMenuById(newMenu);
                    Menu.ParentId = parentId;
                    if (item.Childs.Count > 0)
                    {
                        await addChildesMenuAsync(siteId, userId, item, Menu.Id);
                    }
                }
            }


        }



        //added by Mahmoud to save order of the menu

        public void menuSwapHandler(List<Menu> menuList)
        {
            long? tempparentid = menuList[0].ParentId;
            long? temporder = (menuList[0].Order != null) ? menuList[0].Order : menuList[0].Id;
            menuList[0].ParentId = menuList[1].ParentId;
            menuList[0].Order = (menuList[1].Order != null) ? menuList[1].Order : menuList[1].Id;
            menuList[1].ParentId = tempparentid;
            menuList[1].Order = temporder;
        }

        public void moveMenu(List<Menu> menuList, MenuOrderViewModel menuOrder)
        {
            var sourceItem = menuList.Find(x => x.Id == menuOrder.sourceID);

            sourceItem.ParentId = menuOrder.destinationParentId;
            sourceItem.Order = menuOrder.newOrder;
        }

        public async Task<bool> SwapAndAddMenu(MenuOrderViewModel menuOrder, long siteId)
        {
            try
            {

                var result = false;
                var menuList = await _MenuRepository.Query().Where(y => y.Id == menuOrder.sourceID || y.Id == menuOrder.destinationID).ToListAsync();
                if (menuOrder.sourceParentId == 0) menuOrder.sourceParentId = null;
                if (menuOrder.destinationParentId == 0) menuOrder.destinationParentId = null;
                if (menuList.Count > 0)
                {
                    if (menuOrder.type == "swap")
                    {
                        List<Menu> childList = new List<Menu>();
                        childList = await _MenuRepository.Query()
                                                    .Include(x => x.MenuGroup)
                                                    .Where(y => y.ParentId == menuOrder.destinationParentId
                                                                && y.Status != EStatus.Deleted
                                                                && menuOrder.categoryId == y.MenuGroup.CategoryId
                                                                && siteId == y.SiteId)
                                                    .OrderBy(x => x.Order)
                                                    .ToListAsync();

                        if (childList != null)
                        {
                            var destIndex = childList.FindIndex(x => x.Id == menuOrder.destinationID);
                            var sourceIndex = childList.FindIndex(x => x.Id == menuOrder.sourceID);

                            if (destIndex != -1 && sourceIndex != -1 && menuOrder.destinationParentId == menuOrder.sourceParentId && destIndex - sourceIndex == 1)
                            {
                                menuSwapHandler(menuList);
                            }
                            else
                            {
                                for (int i = destIndex; i < childList.Count; i++)
                                {
                                    if (i == destIndex)
                                    {
                                        var menuitem = menuList.Find(item => item.Id == menuOrder.sourceID);
                                        var destItem = childList[destIndex];
                                        var firstItem = childList.FirstOrDefault();
                                        var previousItem = childList.ElementAtOrDefault(i - 1);

                                        menuitem.ParentId = destItem.ParentId;

                                        if ((firstItem.Id == destItem.Id && childList[destIndex].Order != 1) || (firstItem.Id != destItem.Id && childList[destIndex].Order - 1 > previousItem.Order))
                                            menuitem.Order = destItem.Order - 1;
                                        else
                                        {
                                            menuitem.Order = destItem.Order;
                                            destItem.Order = destItem.Order + 1;
                                        }

                                    }
                                    else
                                    {
                                        var previousItem = childList.ElementAtOrDefault(i - 1);
                                        if (previousItem != null && childList[i].Order == previousItem.Order)
                                        {
                                            childList[i].Order = childList[i].Order + 1;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else if (menuOrder.type == "add")
                    {
                        var sourceItem = menuList.Find(item => item.Id == menuOrder.sourceID);
                        sourceItem.ParentId = menuOrder.destinationID;
                        sourceItem.Order = menuOrder.newOrder;
                    }
                    await _MenuRepository.SaveChangesAsync();
                    result = true;
                }
                else
                {
                    result = false;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Menu>> GetMenuByParentPageId(long ParentPageId)
        {
            var RelatedPagesIds = await _pageRepository.Query()
                .AsNoTracking()
                .Where(p => p.ParentId == ParentPageId && p.ParentId != null)
                .Select(s => s.Id)
                .ToListAsync();

            return await _MenuRepository.Query()
                .AsNoTracking()
                .Where(m => (m.PageId.HasValue && RelatedPagesIds.Contains(m.PageId.Value)))
                .Include(m => m.MenuGroup)
                .ToListAsync();


        }




        public async Task<List<Menu>> ChangeMenuSort(MenuGroupWithSortVM Model)
        {
            return await _MenuRepository.ExecuteInTransactionAsync(async () =>
            {
                var MenusList = await _MenuRepository
                            .Query()
                            .Where(m => m.MenuGroupId == Model.MenuGroupId && m.Status != EStatus.Deleted)
                            .ToListAsync();

                foreach (var menu in MenusList)
                {
                    var selectedMenu = Model.SortedList.Where(i => i.Id == menu.Id).FirstOrDefault();
                    if (selectedMenu == null)
                    {
                        continue;
                    }
                    menu.Order = selectedMenu.Order;
                    menu.ParentId = selectedMenu.ParentId == 0 ? null : selectedMenu.ParentId;
                }


                await _MenuRepository.SaveChangesAsync();

                return MenusList.OrderBy(m => m.Order).ToList();
            });

        }

        public async Task<List<MenuViewModel>> GetMenusByGroupId(long MenuGroupId)
        {
            try
            {
                string CacheKey = "menu" + MenuGroupId.ToString();
                var CultureId = _workContext.GetCurrentCultureId();
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                SourceMenuType Source = (SourceMenuType)_workContext.GetSource();
                long CurrentUserId = await _workContext.GetCurrentUserId();
                IList<UserRole> RoleObj = await _UserRoleService.GetRoleByUserId(CurrentUserId);
                List<long> userRoleIds = RoleObj.Select(x => x.RoleId).OrderBy(x => x).ToList();
                CacheKey += String.Join(",", userRoleIds);


                var cacheData = _cacheService?.GetData<List<MenuViewModel>>(CacheKey);

                if (cacheData != null && Source == SourceMenuType.Web)
                {
                    return cacheData;
                }

                List<Menu> Query = new List<Menu>();
                // List<Menu> Menu = new List<Menu>(); // for top level only
                List<Menu> AllSubMenus = new List<Menu>();

                List<MenuViewModel> MenuItems = new List<MenuViewModel>();

                // If normal user --> get authorized and all public menu for this category
                Query = await _MenuRepository.Query()
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.Id == MenuGroupId
                        && x.Status == EStatus.Published
                        && (x.MenuRoles == null || x.MenuRoles.Count == 0 || x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)))
                        )
                       .Where(x=>(x.MenuType == EMenuTypes.Post  && x.Post != null && x.Post.Status == EStatus.Published) ||
                                    (x.MenuType == EMenuTypes.Page && x.Page != null && x.Page.Status == EStatus.Published) ||
                                    (x.MenuType == EMenuTypes.Category && x.Category != null && x.Category.Status == EStatus.Published) ||
                                    (x.MenuType == EMenuTypes.Brand && x.Brand != null && x.Brand.Status == EStatus.Published) ||
                                    (x.MenuType == EMenuTypes.Product && x.Product != null && x.Product.Status == EStatus.Published)||
                                     x.MenuType ==EMenuTypes.None ||
                                     x.MenuType == EMenuTypes.URL)
                        .Include(x => x.Thumbnail)
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Product)
                       .ThenInclude(x => x.ProductDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Category)
                       .ThenInclude(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Brand)
                       .ThenInclude(x => x.BrandDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Params)
                       .Include(x => x.MenuRoles)
                       .Include(x => x.MenuGroup)
                       
                       .OrderBy(x => x.Order)
                       
                       .AsNoTracking()
                       
                       .ToListAsync();




                MenuItems = Query.Where(x => x.ParentId == null).Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      BrandId = x.BrandId,
                                      MenuType = x.MenuType,
                                      CategoryId = x.CategoryId,
                                      ProductId = x.ProductId,
                                      PostId = x.PostId,
                                      MenuGroupId = x.MenuGroupId,
                                      ThumbnailURL = x.Thumbnail?.PublicUrl,
                                      Title = x.Title,
                                      //  Image = x.Image,
                                      Slug = GetSlug(x) + AddParamsToSlug(x),
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,
                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      Childs = GetChildsForWeb(x.Id, Query.Where(m => m.ParentId != null).ToList()),
                                      ParentId = x.ParentId,
                                      ExternalSlug = x.ExternalSlug,
                                      Expanded = true,
                                      Params = x.Params.Select(x => new EskaCMS.Menus.Models.ParamsViewModel()
                                      {
                                          Id = x.Id,
                                          Value = x.Value,
                                          Key = x.Key,
                                      }).ToList()
                                            }
                                 ).OrderBy(x => x.Order).DistinctBy(x => x.Id).ToList();



               


                if (_cacheService != null
                    && double.TryParse(_Configration.GetSection("CacheConfig:CachePeriod").Value, out var cachePeriod))
                {
                    _cacheService.SetData<List<MenuViewModel>>(CacheKey, MenuItems, DateTimeOffset.Now.AddDays(cachePeriod));
                }


                return MenuItems;
            }
            catch (Exception exc)
            {
                throw;
            }
        }

      

        public async Task<List<MenuViewModel>> GetMenusForWeb(long MenuGroupId, long TargetMenuId, EMenuRelation Relation)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                var CurrentUserId = await _workContext.GetCurrentUserId();
                var CultureId =  _workContext.GetCurrentCultureId();
                List<Menu> AllMenuItems = new List<Menu>();
                List<Menu> AllSubMenus = new List<Menu>();
                List<Menu> AllParentMenus = new List<Menu>();
                List<Menu> SublilingMenus = new List<Menu>();
                List<MenuViewModel> MenuItems = new List<MenuViewModel>();

                Menu TargetMenu = await _MenuRepository
                                            .Query().AsNoTracking()
                                            .Where(x => x.Id == TargetMenuId)
                                            .FirstOrDefaultAsync();


                var RoleObj = await _UserRoleService.GetRoleByUserId(CurrentUserId);
                List<long> userRoleIds = new List<long>();
                if (RoleObj != null)
                {
                    userRoleIds = RoleObj.Select(x => x.RoleId).ToList();
                }


                // If normal user --> get authorized and all public menu for this category
                AllMenuItems = await _MenuRepository.Query()
                       .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.Id == MenuGroupId
                        && x.Status == EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)
                            .Include(x => x.Product)
                       .ThenInclude(x => x.ProductDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Category)
                       .ThenInclude(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Post)
                       .ThenInclude(x => x.SlugInfo)

                       .Include(x => x.Brand)
                       .ThenInclude(x => x.BrandDetails.Where(x => x.CultureId == CultureId))
                       .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.Params)
                       .Include(x => x.MenuRoles)
                       .OrderBy(x => x.Order)
                       .ToListAsync();


                AllSubMenus = AllMenuItems
                                        .Where(x => x.ParentId != null)
                                        .OrderBy(x => x.Order)
                                        .ToList();

                switch (Relation)
                {

                    case EMenuRelation.Tree:

                        return AllMenuItems.Where(x =>
                                (TargetMenu.ParentId == null && TargetMenu.Id == x.Id) ||
                                (TargetMenu.ParentId != null && TargetMenu.ParentId == x.Id)
                        )
                       .Select(x =>
                          new MenuViewModel
                          {
                              Id = x.Id,
                              Name = x.Name,
                              Icon = x.Icon,
                              PageId = x.PageId,
                              MenuType = x.MenuType,
                              CategoryId = x.CategoryId,
                              ProductId = x.ProductId,
                              BrandId = x.BrandId,
                              PostId = x.PostId,
                              MenuGroupId = x.MenuGroupId,
                              ThumbnailURL = x.Thumbnail?.PublicUrl,
                              Title = x.Title,
                              Slug = GetSlug(x) + AddParamsToSlug(x),
                              TemplateId = x.TemplateId,
                              SiteId = x.SiteId,
                              Status = x.Status,
                              CategoriesId = x.MenuGroup.CategoryId ?? 0,
                              Order = x.Order,
                              Childs = GetChildsForWeb(x.Id, AllSubMenus),
                              ParentId = x.ParentId,
                              ExternalSlug = x.ExternalSlug,
                              Expanded = true
                          }
                         ).OrderBy(x => x.Order)
                         .Distinct()
                         .ToList();

                    case EMenuRelation.Subilings:

                        return AllMenuItems.Where(x => x.ParentId == TargetMenu.ParentId)
                            .Select(x =>
                               new MenuViewModel
                               {
                                   Id = x.Id,
                                   Name = x.Name,
                                   Icon = x.Icon,
                                   PageId = x.PageId,
                                   MenuType = x.MenuType,
                                   CategoryId = x.CategoryId,
                                   ProductId = x.ProductId,
                                   BrandId = x.BrandId,
                                   PostId = x.PostId,
                                   MenuGroupId = x.MenuGroupId,
                                   ThumbnailURL = x.Thumbnail?.PublicUrl,
                                   Title = x.Title,
                                   Slug = GetSlug(x) + AddParamsToSlug(x),
                                   TemplateId = x.TemplateId,
                                   SiteId = x.SiteId,
                                   Status = x.Status,
                                   CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                   Order = x.Order,
                                   Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                   ParentId = x.ParentId,
                                   ExternalSlug = x.ExternalSlug,
                                   Expanded = true
                               }
                              ).OrderBy(x => x.Order)
                              .Distinct()
                              .ToList();


                    case EMenuRelation.Children:



                        return AllMenuItems
                                        .Where(x => x.ParentId == TargetMenu.Id)
                                        .Select(x =>
                                        new MenuViewModel
                                        {
                                            Id = x.Id,
                                            Name = x.Name,
                                            Icon = x.Icon,
                                            PageId = x.PageId,
                                            MenuType = x.MenuType,
                                            CategoryId = x.CategoryId,
                                            ProductId = x.ProductId,
                                            PostId = x.PostId,
                                            BrandId = x.BrandId,
                                            MenuGroupId = x.MenuGroupId,
                                            ThumbnailURL = x.Thumbnail?.PublicUrl,
                                            Title = x.Title,
                                            Slug = GetSlug(x) + AddParamsToSlug(x),
                                            TemplateId = x.TemplateId,
                                            SiteId = x.SiteId,
                                            Status = x.Status,
                                            CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                            Order = x.Order,
                                            Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                            ParentId = x.ParentId,
                                            ExternalSlug = x.ExternalSlug,
                                            Expanded = true
                                        })
                                        .OrderBy(x => x.Order)
                                        .Distinct()
                                        .ToList();


                    case EMenuRelation.All:
                    default:

                        AllParentMenus = AllMenuItems
                                            .Where(x => x.ParentId == null)
                                            .OrderBy(x => x.Order)
                                            .ToList();



                        return AllParentMenus.Select(x =>
                                new MenuViewModel
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    Icon = x.Icon,
                                    PageId = x.PageId,
                                    MenuType = x.MenuType,
                                    CategoryId = x.CategoryId,
                                    ProductId = x.ProductId,
                                    PostId = x.PostId,
                                    BrandId = x.BrandId,
                                    MenuGroupId = x.MenuGroupId,
                                    ThumbnailURL = x.Thumbnail?.PublicUrl,
                                    Title = x.Title,
                                    Slug = GetSlug(x) + AddParamsToSlug(x),
                                    TemplateId = x.TemplateId,
                                    SiteId = x.SiteId,
                                    Status = x.Status,
                                    CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                    Order = x.Order,
                                    Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                    ParentId = x.ParentId,
                                    ExternalSlug = x.ExternalSlug,
                                    Expanded = true
                                }
                                ).OrderBy(x => x.Order)
                                .Distinct()
                                .ToList();

                }



                return MenuItems;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        #region MenuGroups

        public async Task<List<MenuGroupVM>> GetMenuGroupsList(long SiteId, long CategoryId = 0)
        {
            var MenuGroupsList = await _MenuGroupRepository
                .Query()
                .AsNoTracking()
                .Where(m => m.SiteId == SiteId
                            && m.Status != EStatus.Deleted
                            && (CategoryId == 0 || m.CategoryId == CategoryId))
                .Include(x => x.Menus)
                .Include("Menus.MenuRoles")
                .Include("Menus.Params")
                .Include("Menus.Thumbnail")
                .Include("Menus.Page.SlugInfo")
                .Include("Menus.Post.SlugInfo")
                .Include("Menus.Product")
                .Include("Menus.Product.ProductDetails")
                .Include("Menus.Product.ProductDetails.SlugInfo")
                .Include("Menus.Brand")
                .Include("Menus.Brand.BrandDetails")
                .Include("Menus.Brand.BrandDetails.SlugInfo")
                .Include("Menus.Category")
                .Include("Menus.Category.CategoryDetails")
                .Include("Menus.Category.CategoryDetails.SlugInfo")
                .Include("Menus.Category.CategoryDetails.Culture")
                .Select(m => new MenuGroupVM
                {
                    Category = m.Category,
                    CultureCode = m.Category.CategoryDetails.Select(X => X.SlugInfo.Culture.CultureId).FirstOrDefault(),
                    Id = m.Id,
                    Name = m.Name,
                    SiteId = m.SiteId,
                    MenuItemsCount = m.Menus.Where(m => m.SiteId == SiteId && (m.Status != EStatus.Deleted)).ToList().Count,
                    CategoryId = m.CategoryId.HasValue ? m.CategoryId.Value : 0,
                    Status = m.Status,
                    Menus = m.Menus.Where(m => m.SiteId == SiteId && (m.Status != EStatus.Deleted)).ToList()

                }).ToListAsync();

            return MenuGroupsList;
        }

        public async Task<MenuGroupVM> GetMenuGroupById(long GroupId)
        {
            var MenuGroupsItem = await _MenuGroupRepository
                .Query()
                .AsNoTracking()
                .Where(m => m.Id == GroupId)
                .Include(m => m.Menus)
                .Include("Menus.MenuRoles")
                .Include("Menus.Params")
                .Include("Menus.Thumbnail")
                .Include("Menus.Page.SlugInfo")
                .Include("Menus.Post.SlugInfo")
                 .Include("Menus.Product")
                .Include("Menus.Product.ProductDetails")
                .Include("Menus.Product.ProductDetails.SlugInfo")
                .Include("Menus.Brand")
                .Include("Menus.Brand.BrandDetails")
                .Include("Menus.Brand.BrandDetails.SlugInfo")
                .Include("Menus.Category")
                .Include("Menus.Category.CategoryDetails")
                .Include("Menus.Category.CategoryDetails.SlugInfo")
                .Include("Menus.Category.CategoryDetails.Culture")
                .Select(m => new MenuGroupVM
                {
                    Category = m.Category,
                    CultureCode = m.Category.CategoryDetails.Select(X => X.SlugInfo.Culture.CultureId).FirstOrDefault(),
                    Id = m.Id,
                    Name = m.Name,
                    SiteId = m.SiteId,
                    MenuItemsCount = m.Menus.Count,
                    CategoryId = m.CategoryId.HasValue ? m.CategoryId.Value : 0,
                    Status = m.Status,
                    Menus = m.Menus.ToList()
                }).FirstOrDefaultAsync();

            return MenuGroupsItem;
        }


        public async Task<MenuGroupVM> UpsertMenuGroup(UpsertMenuGroupVM Model, long SiteId, long UserId)
        {
            bool IsEditMood = Model.Id != 0;
            MenuGroup GroupItem = new MenuGroup();
            if (IsEditMood)
            {
                GroupItem = await _MenuGroupRepository
                               .Query()
                               .Where(m => m.Id == Model.Id)
                               .FirstOrDefaultAsync();
            }


            GroupItem.Name = Model.Name;
            GroupItem.CategoryId = Model.CategoryId;

            if (!IsEditMood)
            {
                GroupItem.Status = EStatus.Published;
                GroupItem.SiteId = SiteId;
                GroupItem.CreatedById = UserId;
                GroupItem.CreationDate = DateTime.Now;

                _MenuGroupRepository.Add(GroupItem);
            }
            else
            {
                GroupItem.ModificationDate = DateTime.Now;
                GroupItem.ModifiedById = UserId;

            }

            await _MenuGroupRepository.SaveChangesAsync();


            return await GetMenuGroupById(GroupItem.Id);
        }


        public async Task<MenuGroupVM> CloneMenuGroup(CloneMenuGroupVM model, long UserId, long SiteId)
        {
            var strategy = _Context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _Context.Database.BeginTransactionAsync();
                try
                {
                    var SourceGroup = await _MenuGroupRepository
                      .Query()
                      .Where(g => g.Id == model.SourceGroupId)
                      .Include(g => g.Menus)
                      .ThenInclude(m => m.MenuRoles)


                    .Include(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .Include("Category.CategoryDetails.SlugInfo")
                    .Include("Category.CategoryDetails.Culture")
                      .FirstOrDefaultAsync();

                    MenuGroup newGroup = new MenuGroup()
                    {
                        CategoryId = SourceGroup.CategoryId,
                        CreatedById = UserId,
                        CreationDate = DateTime.Now,
                        SiteId = SiteId,
                        Name = model.Name,
                        Status = EStatus.Published

                    };

                    _MenuGroupRepository.Add(newGroup);
                    await _MenuGroupRepository.SaveChangesAsync();

                    await CloneMenuItemsAndRoles(newGroup, SourceGroup, UserId, SiteId);



                    await transaction.CommitAsync();

                    return await GetMenuGroupById(newGroup.Id);
                }
                catch (Exception E)
                {
                    await transaction.RollbackAsync();

                    throw;
                }
            });

        }

        public async Task<Menu> AddMenuItemToGroup(AddMenuItemToGroupVM Model, long UserId)
        {

            var MenuItem = await _MenuRepository
                .Query()
                .Where(m => m.Id == Model.MenuItemId)
                .FirstOrDefaultAsync();

            MenuItem.ModificationDate = DateTime.Now;
            MenuItem.ModifiedById = UserId;
            MenuItem.MenuGroupId = Model.MenuGroupId;

            await _MenuGroupRepository.SaveChangesAsync();



            return MenuItem;
        }


        public async Task<MenuGroup> UpdateMenuGroupStatus(long GroupId, EStatus NewStatus, long UserId)
        {

            MenuGroup GroupItem = await _MenuGroupRepository
                                .Query()
                                .Where(m => m.Id == GroupId)
                                .Include(m => m.Menus)
                                .FirstOrDefaultAsync();

            if (GroupItem.Menus.Where(x => x.Status != EStatus.Deleted).Count() > 0 && NewStatus == EStatus.Deleted)
            {
                throw new Exception("Could Not Delete , this Group Has Menu Items Attached To It");
            }

            GroupItem.ModificationDate = DateTime.Now;
            GroupItem.ModifiedById = UserId;
            GroupItem.Status = NewStatus;
            await _MenuGroupRepository.SaveChangesAsync();



            return GroupItem;
        }

        #endregion


        #region HelperFunctions

        private async Task<MenuGroup> CloneMenuItemsAndRoles(MenuGroup newGroup, MenuGroup SourceGroup, long UserId, long SiteId)
        {
            List<KeyValuePair<Menu, Menu>> MenuMapper = new List<KeyValuePair<Menu, Menu>>(); // < Source , NewMenu >


            foreach (var MenuItem in SourceGroup.Menus)
            {
                Menu NewMenuItem = new Menu()
                {
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    Description = MenuItem.Description,
                    ExternalSlug = MenuItem.ExternalSlug,
                    Icon = MenuItem.Icon,
                    IsLoginRequired = MenuItem.IsLoginRequired,
                    MenuGroupId = newGroup.Id,
                    MenuType = MenuItem.MenuType,
                    Name = MenuItem.Name,
                    Title = MenuItem.Title,
                    ThumbnailId = MenuItem.ThumbnailId,
                    TemplateId = MenuItem.TemplateId,
                    Status = MenuItem.Status,
                    ProductId = MenuItem.ProductId,
                    BrandId = MenuItem.BrandId,
                    PageId = MenuItem.PageId
                   ,
                    CategoryId = MenuItem.CategoryId,
                    Order = MenuItem.Order,
                    PostId = MenuItem.PostId,
                    SiteId = SiteId,
                    ParentId = null, // ParentId will Be Null now but Menu Mapper Will Take care of it
                };

                _MenuRepository.Add(NewMenuItem);
                await _MenuRepository.SaveChangesAsync();


                NewMenuItem.MenuRoles = await CloneMenuRoles(MenuItem.MenuRoles.ToList(), NewMenuItem.Id, UserId);

                MenuMapper.Add(new KeyValuePair<Menu, Menu>(MenuItem, NewMenuItem));
            }

            foreach (var MapperItem in MenuMapper)
            {
                if (MapperItem.Key.ParentId == null)
                {
                    MapperItem.Value.ParentId = null;
                    continue;
                }

                var Parent = MenuMapper.Where(x => x.Key.Id == MapperItem.Key.ParentId).FirstOrDefault();
                MapperItem.Value.ParentId = Parent.Value.Id;

            }

            await _MenuRepository.SaveChangesAsync();

            return newGroup;
        }

        private async Task<long?> GetMaximumOrder(long? ParentId, long SiteId, long GroupId)
        {
            return await _MenuRepository.Query()
                        .Include(x => x.MenuGroup)
                        .Where(x => x.ParentId == ParentId
                        && x.SiteId == SiteId
                        && x.MenuGroupId == GroupId
                        && x.Status != EStatus.Deleted)
                        .MaxAsync(y => y.Order);
        }
        private async Task<List<MenusRoles>> CloneMenuRoles(List<MenusRoles> SourceRoles, long NewMenuItemId, long UserId)
        {
            List<MenusRoles> NewMenuRoles = new List<MenusRoles>();

            foreach (var RoleItem in SourceRoles)
            {
                MenusRoles NewRoleItem = new MenusRoles()
                {
                    RoleId = RoleItem.RoleId,
                    MenuId = NewMenuItemId,
                    CreationDate = DateTime.Now,
                    CreatedById = UserId

                };

                NewMenuRoles.Add(NewRoleItem);

            }

            _MenuRolesRepository.AddRange(NewMenuRoles);
            await _MenuRolesRepository.SaveChangesAsync();

            return NewMenuRoles;
        }
        public async Task<CategoryElementViewModel> GetCategoriesById(long CategoryId)
        {
            try
            {
                var Result = await categoryrepository
                                        .Query()
                                        .Where(x => x.Id == CategoryId)
                                        .Include(x => x.CategoryDetails)
                                        .Include(x => x.ThumbnailImage)
                                        .Include("CategoryDetails.SlugInfo")
                                        .Select(Category => new CategoryElementViewModel
                                        {
                                            Id = Category.Id,
                                            IncludeInMenu = Category.IncludeInMenu,
                                            ParentId = Category.ParentId,
                                            DisplayOrder = Category.DisplayOrder,
                                            Status = Category.Status,
                                            CategoryTypeId = (ECategoryTypes)Category.CategoryTypeId,
                                            MediaCategory = Category.ThumbnailImage == null ? new Elements.ViewModels.MediaViewModel() : new Elements.ViewModels.MediaViewModel
                                            {
                                                FileName = Category.ThumbnailImage.Filename,
                                                Id = Category.ThumbnailImageId,
                                                PublicUrl = Category.ThumbnailImage.PublicUrl
                                            },

                                            MetaDescription = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().MetaDescription,
                                            MetaKeywords = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().MetaKeywords,
                                            MetaTitle = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().MetaTitle,
                                            CategoryTemplateId = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CategoryTemplateId == null ? 0 : Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CategoryTemplateId.Value,
                                            Slug = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo == null ? String.Empty : Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo.Slug,

                                            Description = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description,

                                            CultureCode = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Culture == null ? "" : Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Culture.CultureId,
                                            CultureId = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CultureId,

                                        }).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                return Result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<long> UpsertCategory(CategoryElementViewModel category, string Name, long UserId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                if (category.Id == 0)
                {
                    Category ObjCategory = new Category();
                    ObjCategory.CreationDate = DateTimeOffset.Now;
                    ObjCategory.CreatedById = UserId;
                    ObjCategory.CategoryTypeId = (long)category.CategoryTypeId;

                    ObjCategory.ParentId = category.ParentId;
                    ObjCategory.Status = EStatus.Active;
                    ObjCategory.StatusDate = DateTimeOffset.Now;
                    ObjCategory.IncludeInMenu = category.IncludeInMenu;
                    ObjCategory.SiteId = SiteId;
                    ObjCategory.DisplayOrder = category.DisplayOrder;
                    ObjCategory.ParentId = category.ParentId != 0 ? category.ParentId : null;
                    categoryrepository.Add(ObjCategory);
                    await categoryrepository.SaveChangesAsync();

                    await InsertCategoryDetails(category, Name, ObjCategory.Id);

                    return ObjCategory.Id;
                }

                else

                {
                    var updateObj = await categoryrepository.Query().Include(x => x.CategoryDetails).Where(x => x.Id == category.Id).FirstOrDefaultAsync();
                    updateObj.CategoryTypeId = (long)category.CategoryTypeId;
                    updateObj.ModificationDate = DateTimeOffset.Now;
                    updateObj.ModifiedById = UserId;



                    updateObj.Status = category.Status;
                    updateObj.StatusDate = DateTimeOffset.Now;
                    updateObj.IncludeInMenu = category.IncludeInMenu;

                    updateObj.ParentId = category.ParentId;
                    updateObj.SiteId = SiteId;
                    updateObj.DisplayOrder = category.DisplayOrder;
                    updateObj.ParentId = category.ParentId != 0 ? category.ParentId : null;
                    categoryrepository.Update(updateObj);
                    await categoryrepository.SaveChangesAsync();

                    await UpdateCategoryDetails(category, Name, updateObj.Id);

                }


                return 0;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        private async Task<bool> InsertCategoryDetails(CategoryElementViewModel categoryDetails, string Name, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();



                var Category = new CategoryDetails();

                var slugInfo = new SlugManagementVm
                {

                    CultureId = categoryDetails.CultureId,
                    Entity = EEntity.Category,
                    SiteId = SiteId,
                    Slug = categoryDetails.Slug


                };
                if (Category.SlugInfoId != 0 || Category.SlugInfoId != null)

                {
                    slugInfo.Id = Category.SlugInfoId.Value;

                }



                Category.CategoryId = id;
                Category.Name = Name;
                Category.Description = categoryDetails.Description;
                Category.MetaTitle = categoryDetails.MetaTitle;
                Category.MetaDescription = categoryDetails.MetaDescription;
                Category.Description = categoryDetails.Description;
                Category.CategoryTemplateId = categoryDetails.CategoryTemplateId == 0 || categoryDetails.CategoryTemplateId == null ? null : categoryDetails.CategoryTemplateId;
                Category.CultureId = categoryDetails.CultureId;
                Category.SlugInfoId = await slugServices.UpsertAndGetId(slugInfo);


                categoriesDetailsRepository.Add(Category);

                await categoriesDetailsRepository.SaveChangesAsync();


                return true;

            }






            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> UpdateCategoryDetails(CategoryElementViewModel categoryDetail, string Name, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();


               
                    var Category = await categoriesDetailsRepository.Query().Where(x => x.Id == id).ToListAsync();

                foreach (var category in Category)
                {
                   
                        if (categoryDetail.CultureId == category.CultureId)

                        {
                            var slugInfo = new SlugManagementVm
                            {

                                CultureId = category.CultureId,
                                Entity = EEntity.Category,
                                SiteId = SiteId,
                                Slug = categoryDetail.Slug


                            };
                            if (category.SlugInfoId != 0 || category.SlugInfoId != null)

                            {
                                slugInfo.Id = category.SlugInfoId.Value;
                                category.SlugInfoId = await slugServices.UpsertAndGetId(slugInfo);

                            }



                            category.CategoryId = id;
                            category.Name = Name;
                            category.Description = categoryDetail.Description;
                            category.MetaTitle = categoryDetail.MetaTitle;
                            category.MetaDescription = categoryDetail.MetaDescription;
                            category.Description = categoryDetail.Description;
                            category.CategoryTemplateId = categoryDetail.CategoryTemplateId == 0 || categoryDetail.CategoryTemplateId == null ? null : categoryDetail.CategoryTemplateId;
                            category.CultureId = categoryDetail.CultureId;


                            await categoriesDetailsRepository.SaveChangesAsync();

                        }

                    
                }



                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<List<MenuViewModel>> ReturnCategoriesByCountry(List<MenuViewModel> menuItems)
        {
            try
            {
                var filteredMenu = menuItems
                    .Where(menu => menu.MenuType !=EMenuTypes.Category)
                    .ToList();

                var categoryMenuIdsWithMenutypecategory = menuItems
                    .Where(category => category.MenuType==EMenuTypes.Category)
                    .Select(category => category.Id)
                    .ToList();

                var productCategories = await productCategoryRepository.Query()
                    .Include(pc => pc.Product)
                    .Where(pc => categoryMenuIdsWithMenutypecategory.Contains(pc.Id))
                    .ToListAsync();

                var categoryIdsWithForignPrice = productCategories
                          .Where(pc => pc.Product.ForeignPrice.HasValue)
                          .Select(pc => pc.CategoryId)
                          .Distinct()
                          .ToList();

                filteredMenu.AddRange(filteredMenu.Where(category => categoryIdsWithForignPrice.Contains(category.Id)));

                return filteredMenu;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
     

            #endregion

        }
}
