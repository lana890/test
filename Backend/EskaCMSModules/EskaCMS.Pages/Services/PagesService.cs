using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Elements;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Pages.Models;
using EskaCMS.Pages.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCMS.Pages.Services
{
    public class PagesService : IPages
    {
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<PagePreviewCodes> _pagePreviewCodeRepository;
        private readonly IRepository<PageComponentSettings> _pageComponentSettings;
        private readonly IRepository<TemplateVersions> templateVersionsRepository;
        private readonly IRepository<PageVersions> _pageVersions;
        private readonly IRepository<PageTemplates> _pageTemplatesRepository;
        private readonly IRepository<Category> categoryRepository;
        private readonly IRepository<CategoryDetails> categoryDetailsRepository;
        private readonly IRepository<TemplateVersions> _templateVersionsRepository;
        private readonly IRepository<PagesRoles> _PagesRolesRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<TemplateImportedFiles> _TemplateImportedFilesRepository;
        private readonly IRepository<Site> _SitesRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<SitesHostNames> _SitesHostNamesRepo;
        private readonly ISlugServices _slugServices;
        private readonly IRepository<ShareableElement> _SharedElemantRepo;
        private readonly IRepository<SlugInfo> _SlugInfoRepo;
        private readonly IRepository<FavouritePages> _favouritepageRepository;
        private readonly IRepository<PagesRolesVersions> _pageRoleVersionRepository;
        private readonly ISearchRepository<NaviagtableSearchModel> _searchRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ICacheService cacheService;
        private readonly IRepository<Posts> postRepository;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly IRepository<BrandDetails> brandDetailsRepository;
        private readonly IClientCacheService clientCacheService;
        private readonly IRepository<TemplateRoleVersions> _templateRoleVersionsRepository;
        private readonly RoleManager<Role> _roleManager;
        private readonly ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> _meiliRepo;
        private readonly IConfiguration _config;


        public PagesService(
            IRepository<TemplateVersions> templateVersionsRepository,
            IRepository<PagePreviewCodes> pagePreviewCodeRepository,
            IRepository<PageComponentSettings> PageComponentSettingsRepository,
            IRepository<TemplateVersions> TemplateVersionsRepository,

            IRepository<Page> pageRepository,
            IRepository<PageVersions> pageVersions,
            IRepository<SlugInfo> SlugInfoRepo,
            IRepository<PageTemplates> pageTemplates, IRepository<Category> CategoryRepository,
            IRepository<CategoryDetails> CategoryDetailsRepository,
            IRepository<PagesRoles> PagesRolesRepository,
            IRepository<ShareableElement> SharedElemantRepo,
          IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
          IRepository<TemplateImportedFiles> TemplateImportedFilesRepository,
          IRepository<Site> SitesRepository,
            EskaDCMSDbContext context,
            IRepository<SitesHostNames> SitesHostNamesRepo,
          IWorkContext WorkContext,
          ISlugServices slugServices,
          IRepository<FavouritePages> favouritepageRepository,
          RoleManager<Role> roleManager,
          IRepository<TemplateRoleVersions> templateRoleVersionsRepository,
          IRepository<PagesRolesVersions> pageRoleVersionRepository,
          ISearchRepository<NaviagtableSearchModel> searchRepository,
          IRepository<User> userRepository,
             ICacheService cacheService,
          IRepository<Posts> PostRepository,
          IRepository<ProductDetails> ProductDetailsRepository,
          IRepository<BrandDetails> BrandDetailsRepository,
          IClientCacheService clientCacheService,
         ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> meiliRepo,
         IConfiguration config

          )
        {
            _SlugInfoRepo = SlugInfoRepo;
            _pageRepository = pageRepository;
            _pageVersions = pageVersions;
            _SharedElemantRepo = SharedElemantRepo;
            _pageTemplatesRepository = pageTemplates;
            categoryRepository = CategoryRepository;
            categoryDetailsRepository = CategoryDetailsRepository;
            _templateVersionsRepository = templateVersionsRepository;
            _siteSettingsRepository = siteSettingsRepository;
            _context = context;
            _PagesRolesRepository = PagesRolesRepository;
            _TemplateImportedFilesRepository = TemplateImportedFilesRepository;
            _SitesRepository = SitesRepository;
            _WorkContext = WorkContext;
            _SitesHostNamesRepo = SitesHostNamesRepo;
            _slugServices = slugServices;
            _favouritepageRepository = favouritepageRepository;
            _pagePreviewCodeRepository = pagePreviewCodeRepository;
            _pageComponentSettings = PageComponentSettingsRepository;
            this.templateVersionsRepository=TemplateVersionsRepository;
            _roleManager = roleManager;
            _pageRoleVersionRepository = pageRoleVersionRepository;
            _searchRepository = searchRepository;
            _userRepository = userRepository;
            this.cacheService = cacheService;
            postRepository = PostRepository;
            productDetailsRepository = ProductDetailsRepository;
            brandDetailsRepository = BrandDetailsRepository;
            this.clientCacheService = clientCacheService;
            _templateRoleVersionsRepository = templateRoleVersionsRepository;
            _meiliRepo = meiliRepo;
            _config = config;
        }

        public async Task<string> GetPreviewCode(long pageVersionId)
        {
            try
            {
                if (!_pageVersions.Query().Any(x => x.Id == pageVersionId))
                    throw new Exception("Page Version Not Found");


                var GenratedCode = Guid.NewGuid().ToString("N").Substring(0, 12);


                var NewCode = new PagePreviewCodes() { PageVersionId = pageVersionId, Code = GenratedCode };
                _pagePreviewCodeRepository.Add(NewCode);
                await _pagePreviewCodeRepository.SaveChangesAsync();
                return NewCode.Code;

            }
            catch (Exception e)
            {

                throw e;
            }

        }
        public async Task<List<RelatedPageVM>> GetRelatedPages(long ParentId, long SiteId)
        {

            var pages = await _pageRepository.Query()
               .Where(p => p.ParentId == ParentId && p.SiteId == SiteId)
                 .Include(p => p.SlugInfo)
                 .Include(p => p.CurrentVersion)
                 .Include(p => p.Culture)
                 .Include(p => p.PageCategory)
                 .Include(p => p.PageRoles)
                 .Include(p => p.Thumbnail)
                 .Include("ParentLoginPage.Children.SlugInfo")
                 .Select(p => new RelatedPageVM()
                 {
                     Id = p.Id,
                     AdditionalContent = p.AdditionalContent,
                     AddtionalMetaTags = p.AddtionalMetaTags,
                     SlugInfo = p.SlugInfo,
                     Slug = p.SlugInfo.Slug,
                     CreatedById = p.CreatedById,
                     CreationDate = p.CreationDate,
                     Culture = p.Culture,
                     CultureId = p.CultureId,
                     CurrentVersion = p.CurrentVersion,
                     CurrentVersionId = p.CurrentVersionId,
                     Description = p.Description,
                     IsHidden = p.IsHidden,
                     IsLoginRequired = p.IsLoginRequired,
                     Keywords = p.Keywords,
                     PageCategory = p.PageCategory,
                     ModificationDate = p.ModificationDate,
                     ModifiedById = p.ModifiedById,
                     ParentId = p.ParentId,
                     NavigationTitle = p.NavigationTitle,
                     PageTemplate = p.PageTemplate,
                     Name = p.Name,
                     PageCategoryId = p.PageCategoryId,
                     PageOrder = p.PageOrder,
                     PageRoles = p.PageRoles,
                     PageTemplateId = p.PageTemplateId,
                     Route = p.Route,
                     SiteId = p.SiteId,
                     ThumbnailId = p.ThumbnailId,
                     SlugInfoId = p.SlugInfoId,
                     Status = p.Status,
                     StatusDate = p.StatusDate,
                     Thumbnail = p.Thumbnail,
                     IsCacheable = p.IsCacheable,
                     LoginPage = p.ParentLoginPage != null ? p.ParentLoginPage.Children.Where(x => x.CultureId == p.CultureId).Select(l => new LoginPageDetails()
                     {
                         Id = l.Id,
                         Slug = l.SlugInfo.Slug,
                         Name = l.Name,
                         ParentId = l.ParentId.Value
                     }).FirstOrDefault() : null
                 })
                 .ToListAsync();

            return pages;
        }

        public async Task<List<PageCultureDetailsVM>> GetPageCultures(long ParentPageId, long SiteId)
        {
            var RealatedPages = await GetRelatedPages(ParentPageId, SiteId);
            var Result = RealatedPages.Select(c => new PageCultureDetailsVM()
            {
                PageCultureStatus = c.Status,
                PageId = c.Id,
                ParentId = c.ParentId,
                CultureCode = c.Culture.CultureId,
                CultureId = c.CultureId


            }).ToList();

            return Result;
        }

        public async Task<List<PageIdAndCultureIdVM>> AddEditPage(long UserId, long SiteId, List<PagesViewModel> Pages)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    List<PageIdAndCultureIdVM> result = new List<PageIdAndCultureIdVM>();
                    Pages = Pages.OrderByDescending(p => p.Id).ToList();
                    bool IsEditMode = Pages[0].Id != 0;

                    if (!IsEditMode)
                    {
                        Pages = Pages.OrderByDescending(p => p.Name).ToList();
                    }
                    else
                    {
                        Pages = Pages.OrderByDescending(p => (int)p.Status).ThenByDescending(p => p.Id).ToList();
                    }

                    long ParentId = 0;

                    foreach (var Page in Pages)
                    {
                        //testing data need to be deleted
                        //Page.RolesIds = new List<long>();
                        //Page.RolesIds.Add(1);
                        //Page.RolesIds.Add(4);


                        if (Page.Status == EStatus.Unpublished && Page.Id == 0)
                        {
                            result.Add(new PageIdAndCultureIdVM()
                            {
                                CultureId = Page.CultureId,
                                PageId = Page.Id
                            });
                            continue;
                        }


                        var ObjPage = Page.Id == 0
                                ? new Page()
                                : await _pageRepository.Query()
                                .Where(x => x.Id == Page.Id)
                                .Include(x => x.PageRoles)
                                .Include(x => x.SlugInfo)
                                .FirstOrDefaultAsync();

                        SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPage.SlugInfoId,
                            Slug = Page.Slug,
                            CultureId = Page.CultureId,
                            SiteId = SiteId,
                            Entity = EEntity.Page,

                        };

                        if (Page.PageCategoryId == 0)
                        {
                            var newCat = new CategoryViewModel();
                            newCat.Name = Page.PageCategoryName;
                            newCat.UserId = UserId;
                            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
                            newCat.SiteId = Page.SiteId;
                            newCat.CultureId = Page.CultureId;
                            newCat.CategoryTypeId = ECategoryTypes.Page;
                            newCat.CategoryTemplateId = Page.PageTemplateId;
                            Page.PageCategoryId = await AddCategory(newCat);
                        }



                        ObjPage.Name = string.IsNullOrEmpty(Page.Name) ? Pages[0].Name : Page.Name;

                        ObjPage.SlugInfoId = (long)await _slugServices.UpsertAndGetId(slugManagementVm);


                        ObjPage.Status = Page.Status;
                        ObjPage.StatusDate = DateTimeOffset.Now;
                        ObjPage.NavigationTitle = Page.Slug;
                        ObjPage.PageCategoryId = Page.PageCategoryId;
                        ObjPage.SiteId = SiteId;
                        ObjPage.Route = Page.Route;
                        ObjPage.Description = Page.Description;

                        ObjPage.CultureId = Page.CultureId;

                        ObjPage.Keywords = Page.Keywords;
                        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
                        ObjPage.AdditionalContent = Page.AdditionalContent;
                        ObjPage.IsLoginRequired = Page.IsLoginRequired;
                        ObjPage.ParentLoginPageId = Page.LoginPageId;
                        ObjPage.ThumbnailId = Page.ThumbnailId;
                        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
                        ObjPage.PublishDate = Page.PublishDate;
                        ObjPage.ExpiryDate = Page.ExpiryDate;
                        ObjPage.IsCacheable = Pages.Any(x => x.IsCacheable);
                        await ValidateStatusAndPublishDate_ExpireDate(ObjPage.Status, ObjPage.PublishDate, ObjPage.ExpiryDate);

                        if (ObjPage.Id == 0)
                        {
                            ObjPage.CreatedById = UserId;
                            ObjPage.CreationDate = DateTimeOffset.Now;
							ObjPage.ModificationDate = DateTimeOffset.Now;

							_pageRepository.Add(ObjPage);
                            ObjPage.CurrentVersion = new PageVersions
                            {
                                CreatedById = ObjPage.CreatedById,
                                CreationDate = DateTimeOffset.Now,
                                SettingsBody = "{}",
                                VersionId = 1,
                                Alias = "Initial"

                            };
                            await _pageRepository.SaveChangesAsync();
                            ObjPage.CurrentVersion.PageId = ObjPage.Id;
                        }
                        else
                        {

                            ObjPage.ModificationDate = DateTimeOffset.Now;
                            ObjPage.ModifiedById = UserId;
                            cacheService.RemoveByPrefix("Slug" + ObjPage.SlugInfo.Slug + ObjPage.CultureId + SiteId);
                            await clientCacheService.RemoveCacheFile(ObjPage.SlugInfo.Slug, ObjPage.SiteId, ObjPage.CultureId);
                            await _pageRepository.SaveChangesAsync();

                        }

                        if (Page.DuplicatedOfId != 0) // incase of duplicate page
                        {
                            var OrigenalPageVersion = await _pageRepository
                                                       .Query()
                                                       .Where(p => p.ParentId == Page.DuplicatedOfId &&
                                                              p.CultureId == Page.CultureId)
                                                       .Include(p => p.CurrentVersion)
                                                       .Select(p => p.CurrentVersion)
                                                       .FirstOrDefaultAsync();

                            ObjPage.CurrentVersion.PageComponents = OrigenalPageVersion.PageComponents;
                            ObjPage.CurrentVersion.SettingsBody = OrigenalPageVersion.SettingsBody;

                            await _pageRepository.SaveChangesAsync();

                        }

                        ObjPage.ParentId = ParentId == 0 ? ObjPage.Id : ParentId;
                        ParentId = ParentId == 0 ? ObjPage.Id : ParentId;
                        result.Add(new PageIdAndCultureIdVM()
                        {
                            CultureId = ObjPage.CultureId,
                            PageId = ObjPage.Id,
                            Slug = ObjPage.NavigationTitle,

                        });
                        // await UpsertPageRolesAsync(ObjPage, Page.RolesIds, UserId);
                        await _pageRepository.SaveChangesAsync();

                    }
                    await _pageRepository.SaveChangesAsync();

                //    await _meiliRepo.AddDocument(EskaCMS.Infrastructure.Enums.EEntity.Page, Pages);
                    //if there is no page added before set the first page as defult just for the first time
                    SetDefaultParentPageIdForSite(SiteId, ParentId);

                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception exc)
                {
                    await transaction.RollbackAsync();
                    if (exc.InnerException?.Message.Contains("Duplicate entry") == true)
                    {
                        throw new Exception("Slug is already used");
                    }
                    throw exc;

                }
            });
        }

        private async Task<long> AddCategory(CategoryViewModel newCat)
        {
            try
            {
                long UserId = await _WorkContext.GetCurrentUserId();
                long SiteId = _WorkContext.GetCurrentSiteId();

                var Category = new Category();

                Category.SiteId = SiteId;
                Category.CategoryTypeId = Convert.ToInt64(newCat.CategoryTypeId);
                Category.CreatedById = UserId;
                Category.IncludeInMenu = newCat.IncludeInMenu;
                Category.StatusDate = DateTime.Now;
                Category.CreationDate = DateTime.Now;
                Category.ParentId = newCat.ParentId == 0 ? null : newCat.ParentId;
                Category.Status = EStatus.Published;
                categoryRepository.Add(Category);
                await categoryRepository.SaveChangesAsync();
                var SlugInfo = new SlugManagementVm
                {
                    CultureId = newCat.CultureId,
                    Entity = EEntity.Page,
                    SiteId = SiteId,
                    Slug = newCat.Slug

                };
                var CategoryDetails = new CategoryDetails();
                CategoryDetails.CategoryId = Category.Id;
                CategoryDetails.CategoryTemplateId = newCat.CategoryTemplateId;
                CategoryDetails.CultureId = newCat.CultureId;
                CategoryDetails.Name = newCat.Name;
                CategoryDetails.SlugInfoId = await _slugServices.UpsertAndGetId(SlugInfo);

                categoryDetailsRepository.Add(CategoryDetails);

                await categoryDetailsRepository.SaveChangesAsync();
                return Category.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Page> GetPageBySlug(string slug)
        {
            try
            {
                return await _pageRepository.Query().Include(x => x.SlugInfo).Where(x => x.SlugInfo.Slug.ToLower() == slug && x.Status == EStatus.Published).FirstOrDefaultAsync();

            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool SetDefaultParentPageIdForSite(long siteId, long parentId)
        {
            try
            {
                var site = _SitesRepository.Query().Where(x => x.Id == siteId).FirstOrDefault();
                if (site.DefaultParentPageId == 0)
                {
                    site.DefaultParentPageId = parentId;
                    _SitesRepository.SaveChanges();
                }

                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> UpdatePagesStatus(string PagesIds, int Status, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query()
                    .Include(x => x.SlugInfo)
                    .Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId)
                    .Select(x => x)
                    .ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            SlugManagementVm slugManagementVm = slugManagementVm = new()
                            {
                                Id = pageList[i].SlugInfoId,
                                CultureId = pageList[i].CultureId,
                                Entity = EEntity.Page,
                                SiteId = SiteId


                            };

                            pageList[i].Status = (EStatus)Status;
                            pageList[i].ModifiedById = UserId;

                            await ValidateStatusAndPublishDate_ExpireDate(pageList[i].Status, pageList[i].PublishDate, pageList[i].ExpiryDate);

                            if (Status == (long)EStatus.Deleted)
                            {
                                slugManagementVm.Slug = pageList[i].Id + "Deleted" + await _slugServices.GetSlugById(pageList[i].SlugInfoId);
                            }
                            else
                            {
                                var SlugName = await _slugServices.GetSlugById(pageList[i].SlugInfoId);
                                slugManagementVm.Slug = SlugName.Split("Deleted").LastOrDefault();
                            }

                            pageList[i].SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);

                            await _pageRepository.SaveChangesAsync();
                            var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
                                if (IsMeliEnabled&&(Status == (long)EStatus.Deleted || Status == (long)EStatus.Unpublished))
                            {
                                if (pageList[i].Id == pageList[i].ParentId)
                                {
                                    //var elasticId = $"{pageList[i].Id}-{nameof(EEntity.Page)}";
                                    //await _searchRepository.DeleteDocuemnt(elasticId);

                                    //MeiliSearch
                                    var MeiliId = $"{pageList[i].Id}-{nameof(EEntity.Page)}";
                                    await _meiliRepo.DeleteDocuemnt(MeiliId, nameof(EEntity.Page));
                                }
                            }
                        }
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<bool> DeletePages(string PagesIds, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query().Include(x => x.SlugInfo).Where(x => x.Status != EStatus.Deleted
                          && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            pageList[i].Status = EStatus.Deleted;
                            pageList[i].ModifiedById = UserId;
                            SlugManagementVm slugManagementVm = slugManagementVm = new()
                            {
                                Id = pageList[i].SlugInfoId,
                                CultureId = pageList[i].CultureId,
                                Entity = EEntity.Page,
                                SiteId = pageList[i].SiteId,
                                Slug = pageList[i].Id + "Deleted" + _slugServices.GetSlugById(pageList[i].SlugInfoId),

                            };
                            pageList[i].SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);

                            await _pageRepository.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        //End of bulk update and delete
        public async Task<List<PageResultVM>> DublicatePage(long ParentId, long SiteId, long UserId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var originalPages = await GetRelatedPages(ParentId, SiteId);
                    var DublicatePages = new List<Page>();
                    long DublicateParentId = 0;

                    foreach (var Page in originalPages)
                    {
                        var ObjPage = new Page();
                        ObjPage.Name = Page.Name + " (Duplicated)";

                        var Slug = await _slugServices.GetSlugById(Page.SlugInfoId);

                        SlugManagementVm slugManagementVm = null;

                        slugManagementVm = new()
                        {
                            CultureId = Page.CultureId,

                            Entity = EEntity.Page,

                            SiteId = Page.SiteId,


                            Slug = Slug + "-duplicated " + Guid.NewGuid().ToString().Substring(1, 2),

                        };
                        ObjPage.SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);
                        ObjPage.CreatedById = UserId;
                        ObjPage.CreationDate = DateTimeOffset.Now;
                        ObjPage.Status = Page.Status;
                        ObjPage.StatusDate = DateTimeOffset.Now;
                        ObjPage.NavigationTitle = slugManagementVm.Slug + "-duplicated";
                        ObjPage.PageCategoryId = Page.PageCategoryId;
                        ObjPage.SiteId = SiteId;
                        ObjPage.Route = Page.Route;
                        ObjPage.Description = Page.Description;
                        ObjPage.Keywords = Page.Keywords;
                        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
                        ObjPage.ThumbnailId = Page.ThumbnailId;
                        ObjPage.AdditionalContent = Page.AdditionalContent;
                        ObjPage.CultureId = Page.CultureId;
                        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
                        ObjPage.PageCategory = Page.PageCategory;

                        _pageRepository.Add(ObjPage);


                        var pageContent = Page.CurrentVersion;
                        var Settings = pageContent != null ? pageContent.SettingsBody : "{}";
                        var PageComponents = pageContent != null ? pageContent.PageComponents : "";

                        ObjPage.CurrentVersion = new PageVersions
                        {
                            CreatedById = UserId,
                            CreationDate = DateTime.Now,
                            SettingsBody = Settings,
                            VersionId = 1,
                            PageComponents = PageComponents
                        };

                        _pageRepository.SaveChanges();


                        List<long> PageRolesIdList = Page.PageRoles.Select(r => r.RoleId).ToList();

                        //await UpsertPageRolesAsync(ObjPage, PageRolesIdList, UserId);

                        ObjPage.CurrentVersion.PageId = ObjPage.Id;

                        ObjPage.ParentId = DublicateParentId == 0 ? ObjPage.Id : DublicateParentId;

                        DublicateParentId = DublicateParentId == 0 ? ObjPage.Id : DublicateParentId;



                        slugManagementVm = new()
                        {
                            CultureId = ObjPage.CultureId,
                            Entity = EEntity.Page,
                            Slug = await _slugServices.GetSlugById(ObjPage.SlugInfoId) + "-duplicated " + ObjPage.Id,
                            SiteId = SiteId

                        };
                        ObjPage.SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);
                        DublicatePages.Add(ObjPage);
                    }

                    await _pageRepository.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return BuildPagesTree(DublicatePages);
                }
                catch (Exception exc)
                {
                    await transaction.RollbackAsync();
                    throw exc;
                }
            });
        }
        public void UpdatePageVersion(long? pageId, long versionId)
        {
            Page page = _pageRepository.Query().FirstOrDefault(v => v.Id == pageId);
            page.CurrentVersionId = versionId;
            _pageRepository.SaveChanges();


        }

        public async Task<IList<PageVersionVM>> SavePageSettings(SavePageSettingsViewModel SavePageSettingsModel, long siteId)
        {
            try
            {
                SavePageSettingsModel.UserId = await GetValidatedCurrentUserId(SavePageSettingsModel.UserId);
                var strategy = _context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = _pageRepository.BeginTransaction())
                    {
                        var ObjPageVersions = new PageVersions();
                        ObjPageVersions.PageId = SavePageSettingsModel.PageId;
                        ObjPageVersions.VersionId = GetLatestVersionId(SavePageSettingsModel.PageId) + 1;
                        using var doc = JsonDocument.Parse(SavePageSettingsModel.Settings);
                        ObjPageVersions.SettingsBody = System.Text.Json.JsonSerializer.Serialize(doc.RootElement);
                        ObjPageVersions.CreatedById = SavePageSettingsModel.UserId;
                        ObjPageVersions.CreationDate = DateTimeOffset.Now;
                        ObjPageVersions.Alias = SavePageSettingsModel.Alias;

                        if (SavePageSettingsModel.PageComponents == null)
                        {
                            SavePageSettingsModel.PageComponents = new List<string>();
                        }

                        ObjPageVersions.PageComponents = JsonConvert.SerializeObject(SavePageSettingsModel.PageComponents);

                        _pageVersions.Add(ObjPageVersions);
                        await _pageVersions.SaveChangesAsync();
                        if (!SavePageSettingsModel.IsDraft)
                        {
                            UpdatePageVersion(ObjPageVersions.PageId, ObjPageVersions.Id);
                        }
                        var ObjPage = await _pageRepository.Query().Where(x => x.Id == SavePageSettingsModel.PageId).Include(x => x.PageVersions).FirstOrDefaultAsync();
                        long latestVersionId = ObjPage.PageVersions.Max(x => x.VersionId);

                        ObjPage.ModifiedById = SavePageSettingsModel.UserId;
                        ObjPage.ModificationDate = DateTimeOffset.Now;



                        string tempMaxPageVersionCount = GetSiteSettingByKey("MaxPageVersionCount", siteId);
                        long MaxPageVersionCount = 50;

                        if (tempMaxPageVersionCount != "")
                        {
                            MaxPageVersionCount = long.Parse(tempMaxPageVersionCount);
                        }


                        var allOldVersions = _pageVersions.Query().Where(x => x.PageId == SavePageSettingsModel.PageId && ObjPage.CurrentVersionId != x.Id).OrderBy(v => v.CreationDate).ToList();

                        var deletedPageVersions = new List<long>();

                        if (allOldVersions.Count >= MaxPageVersionCount)
                        {
                            var diff = allOldVersions.Count - MaxPageVersionCount;
                            for (int i = 0; i < diff; i++)
                            {
                                _pageVersions.Remove(allOldVersions[i]);
                                deletedPageVersions.Add(allOldVersions[i].Id);

                            }
                            if (deletedPageVersions.Any())
                            {
                                await DeletePageRoleVersions(deletedPageVersions);
                                await DeletePagePreviewCodes(deletedPageVersions);
                                await DeletePageComponentSettings(deletedPageVersions);
                            }
                        }
                        await _pageVersions.SaveChangesAsync();
                        await _pageRepository.SaveChangesAsync();

                        var Versions = await GetAllPageVersions(ObjPage.Id);


                        await InsertPageSettingsForRoles(SavePageSettingsModel, siteId, ObjPageVersions.Id, ObjPageVersions.PageComponents);


                        transaction.Commit();
                        return Versions;
                    }
                });
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<bool> InsertPageSettingsForRoles(SavePageSettingsViewModel SavePageSettingsModel, long siteId, long versionId, string pageComponents)
        {
            var userId = SavePageSettingsModel.UserId;
            var roleIds = _roleManager.Roles.Where(x => x.SiteId == siteId || x.SiteId == null).Select(x => x.Id).ToList();

            if (roleIds.Any())
            {
                List<PagesRolesVersions> Result = new();
                foreach (var RoleId in roleIds)
                {
                    var serealizedObject = JToken.Parse(SavePageSettingsModel.Settings);
                    var filterdSettings = (await SettingsFilteration(serealizedObject, RoleId)).ToString();
                    Result.Add(new PagesRolesVersions
                    {
                        RoleId = RoleId,
                        PageId = SavePageSettingsModel.PageId,
                        CreationDate = DateTimeOffset.Now,
                        PageVersionId = versionId,
                        SettingsBody = filterdSettings,
                        CreatedById = userId,
                        PageComponents = pageComponents,

                    });
                }
                foreach (var setting in Result)
                {
                    _pageRoleVersionRepository.Add(setting);
                    _pageRoleVersionRepository.SaveChanges();
                }

                return true;
            }

            return false;
        }

        private async Task<long> GetValidatedCurrentUserId(long fallbackUserId)
        {
            var currentUserId = await _WorkContext.GetCurrentUserId();
            var userId = currentUserId > 0 ? currentUserId : fallbackUserId;

            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Current user is not valid. Please sign in again.");
            }

            var userExists = await _userRepository.Query()
                .AsNoTracking()
                .AnyAsync(x => x.Id == userId);

            if (!userExists)
            {
                throw new UnauthorizedAccessException("Current user was not found. Please sign in again.");
            }

            return userId;
        }










        public async Task<bool> DeletePageRoleVersions(List<long> versionIds)
        {
            var userId = await _WorkContext.GetCurrentUserId();
            var pageRoleVersions = _pageRoleVersionRepository.Query()
                                                    .Where(x => versionIds.Contains(x.PageVersionId))
                                                    .ToList();

            if (pageRoleVersions.Any())
            {
                _pageRoleVersionRepository.RemoveRange(pageRoleVersions);
                _pageRoleVersionRepository.SaveChanges();
                return true;
            }

            return false;
        }

        public async Task<bool> DeletePagePreviewCodes(List<long> versionIds)
        {
            var pagePreviewCodes = _pagePreviewCodeRepository.Query()
                                                    .Where(x => versionIds.Contains(x.PageVersionId))
                                                    .ToList();

            if (pagePreviewCodes.Any())
            {
                _pagePreviewCodeRepository.RemoveRange(pagePreviewCodes);
                await _pagePreviewCodeRepository.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> DeletePageComponentSettings(List<long> versionIds)
        {
            var pageComponentSettings = _pageComponentSettings.Query()
                                                    .Where(x => versionIds.Contains(x.PageVersionId))
                                                    .ToList();

            if (pageComponentSettings.Any())
            {
                _pageComponentSettings.RemoveRange(pageComponentSettings);
                await _pageComponentSettings.SaveChangesAsync();
                return true;
            }

            return false;
        }


        public async Task<PageResultWithTotalPagesVm> GetAllPages(GetAllPagesSearchParamsVM getAllPagesSearchParamsVM, long SiteId)
        {
            try
            {
                var pageList = await _pageRepository.Query()
                    .Include(y => y.PageTemplate)
                    .Include(x => x.PageCategory)
                    .ThenInclude(x => x.CategoryDetails)
                    .Include(x => x.Culture)
                    .Include(x => x.Site)
                    .Include(x => x.Thumbnail)
                    .Include(x => x.SlugInfo)
                    .Include("ParentLoginPage.Children.SlugInfo")
                    .Where(x => (
                    ((x.Status != EStatus.Deleted && x.Status != EStatus.Inactive &&
                    (!getAllPagesSearchParamsVM.Status.HasValue || getAllPagesSearchParamsVM.Status.Value == 0))
                    || ((x.Status == getAllPagesSearchParamsVM.Status) && (getAllPagesSearchParamsVM.Status.HasValue && getAllPagesSearchParamsVM.Status.Value != 0)))
                    && x.SiteId == SiteId
                    && ((x.PageCategoryId == getAllPagesSearchParamsVM.PageCategory) || getAllPagesSearchParamsVM.PageCategory == null
                    || getAllPagesSearchParamsVM.PageCategory == 0)
                    && (string.IsNullOrEmpty(getAllPagesSearchParamsVM.CultureCode) || x.Culture.CultureId == getAllPagesSearchParamsVM.CultureCode)
                    && ((!string.IsNullOrEmpty(getAllPagesSearchParamsVM.PageName) && x.Name.ToLower().Equals(getAllPagesSearchParamsVM.PageName.ToLower()))
                    || string.IsNullOrEmpty(getAllPagesSearchParamsVM.PageName)
                    || getAllPagesSearchParamsVM.PageName.Equals("0"))
                    && (getAllPagesSearchParamsVM.CultureId == 0 || x.CultureId == getAllPagesSearchParamsVM.CultureId)
                    )).ToListAsync();

                if (!getAllPagesSearchParamsVM.Status.HasValue || getAllPagesSearchParamsVM.Status.Value == 0)

                {
                    pageList = pageList.Where(x => x.Status == EStatus.Published || x.Status == EStatus.Unpublished).ToList();
                }

                var PageTree = BuildPagesTree(pageList);

                var Pages = new PageResultWithTotalPagesVm();


                PageTree = PageTree.
                    Where(x => (string.IsNullOrEmpty(getAllPagesSearchParamsVM.SearchText) ||
                            x.PageCultureDetails.Any(p => p.Name.ToLower().Contains(getAllPagesSearchParamsVM.SearchText.ToLower())
                                                       || p.Slug.ToLower().Contains(getAllPagesSearchParamsVM.SearchText.ToLower())
                                                       ))).ToList();
                Pages.TotalPages = PageTree.Count();

                PageTree = PageTree.Skip(getAllPagesSearchParamsVM.PageNumber * getAllPagesSearchParamsVM.PageSize)
                        .Take(getAllPagesSearchParamsVM.PageSize)
                        .ToList();
                Pages.Pages = PageTree;
                return Pages;



            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<IList<PageResultVM>> GetAllPagesFlat(string PageName = null, long? PageCategory = null, EStatus? Status = null, long siteId = 0, long cultureId = 0)
        {

            var pageList = await _pageRepository.Query()
                .Include(y => y.PageTemplate)
                .Include(x => x.PageCategory)
                .ThenInclude(x => x.CategoryDetails)
                .Include(x => x.Culture)
                .Include(x => x.Site)
                .Include(x => x.Thumbnail)
                .Include(x => x.SlugInfo)
                 .Where(x => (
                   ((x.Status != EStatus.Deleted && x.Status != EStatus.Inactive &&
                    (!Status.HasValue || Status.Value == 0))
                    || ((x.Status == Status) && (Status.HasValue && Status.Value != 0)))
                    && x.SiteId == siteId
                    && ((x.PageCategoryId == PageCategory) || PageCategory == null || PageCategory == 0)
                    && ((!string.IsNullOrEmpty(PageName) && x.Name.ToLower().Equals(PageName.ToLower())) || string.IsNullOrEmpty(PageName) || PageName.Equals("0"))
                    && (cultureId == 0 || x.CultureId == cultureId)))

                 .Select(page => new PageResultVM
                 {
                     PageId = page.Id,
                     Name = page.Name,
                     PageStatus = page.Status,
                     PageCategoryId = page.PageCategoryId,
                     PageCategoryName = page.PageCategory.CategoryDetails.Where(i => i.CultureId == page.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                     CreationDate = page.CreationDate,
                     ParentPageId = page.ParentId,
                     TemplateName = page.PageTemplate == null ? "No Template" : page.PageTemplate.Name,
                     TemplateId = page.PageTemplate == null ? 0 : page.PageTemplate.Id,
                     ParentPageSlug = page.SlugInfo.Slug,
                     IsDefaultPage = page.Site == null ? false : page.Site.DefaultParentPageId == page.ParentId,

                 }).ToListAsync();

            return pageList;


        }

        public async Task<IList<PageSiteMapResultVM>> GetAllPagesBysiteId(long siteId = 0, EStatus Status = EStatus.Active, long? PageCategory = null)
        {
            try
            {


                var pageList = await _pageRepository.Query()
                   .Include(x => x.Culture)
                   .Include(x => x.Site)
                   .Include(x => x.SlugInfo)
                   .Where(x => ((x.Status != EStatus.Deleted && Status == EStatus.Active) || (Status != EStatus.Active && x.Status == Status)) /*when status is active  will return not deleted pages*/
                   && x.SiteId == siteId).Select(x => new PageSiteMapResultVM
                   {
                       ParentPageId = x.ParentId.Value,
                       CultureId = x.CultureId,
                       PageSlug = x.SlugInfo.Slug,
                       Name = x.Name,
                       Culture = x.Culture.Culture.Name,
                       LastModification = x.ModificationDate
                   }).ToListAsync();




                return pageList;


            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public async Task<Page> ClonePages(long TargetPageId, long SourcePageId, long SiteId)
        {
            try
            {
                var RealatedPages = await _pageRepository.Query()
                                .Where(p => p.SiteId == SiteId && (p.Id == TargetPageId || p.Id == SourcePageId))
                                .Include(p => p.CurrentVersion)
                                .ToListAsync();

                var SourcePage = RealatedPages.Where(p => p.Id == SourcePageId).FirstOrDefault();
                var TargetPage = RealatedPages.Where(p => p.Id == TargetPageId).FirstOrDefault();

                var ClonedVersion = new PageVersions()
                {
                    PageId = TargetPage.Id,
                    CreatedById = SourcePage.CurrentVersion.CreatedById,
                    CreationDate = DateTime.Now,
                    PageComponents = SourcePage.CurrentVersion.PageComponents,
                    SettingsBody = SourcePage.CurrentVersion.SettingsBody,
                    Alias = SourcePage.CurrentVersion.Alias,


                };

                _pageVersions.Add(ClonedVersion);

                _pageVersions.SaveChanges();

                ClonedVersion.VersionId = ClonedVersion.Id;
                TargetPage.CurrentVersionId = ClonedVersion.Id;

                _pageVersions.SaveChanges();

                TargetPage.CurrentVersion = ClonedVersion;
                return TargetPage;

            }
            catch (Exception E)
            {
                throw E;
            }
        }

        public async Task<bool> updatePageRoles(long ParentId, List<long> RolesIdList, long SiteId, long UserId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var Transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var RelatedPages = await GetRelatedPages(ParentId, SiteId);
                    foreach (var Page in RelatedPages)
                    {
                        await UpsertPageRolesAsync(Page, RolesIdList, UserId);
                    }
                    await _pageRepository.SaveChangesAsync();
                    await _PagesRolesRepository.SaveChangesAsync();

                    await Transaction.CommitAsync();
                    return true;

                }
                catch (Exception E)
                {
                    await Transaction.RollbackAsync();

                    throw E;
                }
            });
        }

        public async Task<PagesViewModel> GetPageSettings(long pageId)
        {
            try
            {
                var pageEntity = await _pageRepository.Query()
                    .AsNoTracking()
                    .IgnoreAutoIncludes()
                    .AsSplitQuery()
                    .Where(x => x.Id == pageId && x.Status != EStatus.Deleted)

                    .Include(x => x.CurrentVersion)
                        .ThenInclude(x => x.CreatedBy)

                    .Include(x => x.PageTemplate)
                        .ThenInclude(x => x.CurrentVersion)
                            .ThenInclude(x => x.CreatedBy)

                    .Include(x => x.PageTemplate)
                        .ThenInclude(x => x.CurrentVersion)
                            .ThenInclude(x => x.ImportedFiles)
                                .ThenInclude(x => x.Media)

                    .Include(x => x.Thumbnail)

                    .Include(x => x.Culture)

                    .Include(x => x.SlugInfo)

                    .Include(x => x.PageCategory)
                        .ThenInclude(x => x.CategoryDetails)

                    .FirstOrDefaultAsync();

                if (pageEntity == null)
                {
                    return null;
                }

                var currentVersion = pageEntity.CurrentVersion;
                var pageTemplate = pageEntity.PageTemplate;
                var templateCurrentVersion = pageTemplate?.CurrentVersion;

                var pageCategoryName = pageEntity.PageCategory?.CategoryDetails?
                    .Where(i => i.CultureId == pageEntity.CultureId)
                    .Select(x => x.Name)
                    .FirstOrDefault() ?? string.Empty;

                var page = new PagesViewModel
                {
                    CultureId = pageEntity.CultureId,
                    ParentId = pageEntity.ParentId,
                    AdditionalContent = pageEntity.AdditionalContent,
                    AddtionalMetaTags = pageEntity.AddtionalMetaTags,

                    CurrentVersion = currentVersion != null
                        ? new PageVersionVM
                        {
                            CreatedById = currentVersion.CreatedById,
                            CreatedByName = currentVersion.CreatedBy != null
                                ? currentVersion.CreatedBy.FullName
                                : string.Empty,

                            CreationDate = currentVersion.CreationDate,
                            Id = currentVersion.Id,
                            SettingsBody = currentVersion.SettingsBody,
                            VersionId = currentVersion.VersionId,
                            PageComponents = currentVersion.PageComponents,
                            Alias = currentVersion.Alias
                        }
                        : null,

                    CurrentVersionId = pageEntity.CurrentVersionId,
                    Description = pageEntity.Description,
                    Id = pageEntity.Id,
                    ThumbnailId = pageEntity.ThumbnailId,

                    ThumbnailURL = pageEntity.Thumbnail != null
                        ? pageEntity.Thumbnail.PublicUrl
                        : null,

                    IsLoginRequired = pageEntity.IsLoginRequired,
                    LoginPageId = pageEntity.ParentLoginPageId,
                    Keywords = pageEntity.Keywords,
                    Name = pageEntity.Name,

                    PageCategoryId = pageEntity.PageCategoryId,
                    PageCategoryName = pageCategoryName,

                    PageTemplateId = pageEntity.PageTemplateId.HasValue
                        ? pageEntity.PageTemplateId.Value
                        : 0,

                    PageTemplate = pageTemplate != null
                        ? new PageTemplateVM
                        {
                            Description = pageTemplate.Description,
                            Name = pageTemplate.Name,
                            AddtionalMetaTags = pageTemplate.AddtionalMetaTags,

                            CurrentVersion = templateCurrentVersion != null
                                ? new TemplateVersionVM
                                {
                                    Id = templateCurrentVersion.Id,
                                    CreatedById = templateCurrentVersion.CreatedById,

                                    CreatedByName = templateCurrentVersion.CreatedBy != null
                                        ? templateCurrentVersion.CreatedBy.FullName
                                        : string.Empty,

                                    CreationDate = templateCurrentVersion.CreationDate,
                                    Css = templateCurrentVersion.Css,
                                    Alias = templateCurrentVersion.Alias,

                                    ImportFilesList = templateCurrentVersion.ImportedFiles != null
                                        ? templateCurrentVersion.ImportedFiles
                                            .Select(i => new ImportedFilesVM
                                            {
                                                Id = i.Id,
                                                FileId = i.Media != null ? i.Media.Id : 0,
                                                FileName = i.Media != null ? i.Media.Filename : string.Empty,
                                                FileUrl = i.Media != null ? i.Media.PublicUrl : string.Empty,
                                                SourceId = pageTemplate.CurrentVersionId
                                            })
                                            .ToList()
                                        : new List<ImportedFilesVM>(),

                                    Scripts = templateCurrentVersion.Scripts,
                                    SettingsBody = templateCurrentVersion.SettingsBody,
                                    VersionId = templateCurrentVersion.VersionId,
                                    TemplateComponents = templateCurrentVersion.TemplateComponents
                                }
                                : null
                        }
                        : null,

                    Route = pageEntity.Route,

                    Slug = pageEntity.SlugInfo != null
                        ? pageEntity.SlugInfo.Slug
                        : string.Empty,

                    SiteId = pageEntity.SiteId,
                    Status = pageEntity.Status,
                    StatusDate = pageEntity.StatusDate
                };

                return page;
            }
            catch
            {
                throw;
            }
        }


        public async Task<IList<PageVersionVM>> GetAllPageVersions(long pageId)
        {
            IList<PageVersionVM> pageVersions = await _pageVersions.Query()
                .Where(x => x.PageId == pageId)
                .Include(x => x.CreatedBy)
                .Select(v => new PageVersionVM
                {
                    // CreatedById = v.CreatedById,
                    CreatedByName = v.CreatedBy.FullName,
                    CreationDate = v.CreationDate,
                    Id = v.Id,
                    IsPublished = v.Page.CurrentVersionId == v.Id ? true : false,
                    Alias = v.Alias,
                    //PageId = v.PageId,
                    //  SettingsBody = v.SettingsBody,
                    VersionId = v.VersionId
                }).OrderByDescending(x => x.CreationDate).ToListAsync();
            return pageVersions;
        }

        public async Task<bool> DeletePageByID(long PageId, long UserId)

        {
            try
            {
                var SiteId = _WorkContext.GetCurrentSiteId();


                await DeletePages(PageId.ToString(), UserId, SiteId);

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public async Task<string> GetSpecificSettingForPage(string slug, string componentName, string settingKey)

        {
            try
            {
                var SiteId = _WorkContext.GetCurrentSiteId();

                var cultureId = _WorkContext.GetCurrentCultureId();

                var lowerSlug = slug.ToLower();

                var targetPage = await _pageRepository.Query()
                                                       .Where(x => x.SiteId == SiteId && x.SlugInfo.Slug.ToLower() == lowerSlug && x.CultureId == cultureId)
                                                       .Include(x => x.CurrentVersion)
                                                       .FirstOrDefaultAsync();

                if (targetPage is null) // to be removed after new aghati mobile build.
                {
                    targetPage = await _pageRepository.Query()
                                                       .Where(x => x.SiteId == SiteId && x.Name.ToLower() == lowerSlug)
                                                       .Include(x => x.CurrentVersion)
                                                       .FirstOrDefaultAsync();
                }

                var settings = JToken.Parse(targetPage.CurrentVersion.SettingsBody);
                var data = await GetSpecificKey(settings, componentName, settingKey);
                if (data is not null)
                {
                    var result = data.ToString();

                    return result;

                }

                return string.Empty;



            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public  async Task ReplacePageComponent(ReplaceComponentBody model)
        {
            try
            {
                if (model.PageId.HasValue)
                {
                    var CurrentVersionId = await _pageRepository.Query().Where(x => x.Id==model.PageId).AsNoTracking().Select(x => x.CurrentVersionId).FirstOrDefaultAsync();
                    var pageVersions = await _pageVersions.Query().Where(x => x.Id==CurrentVersionId).FirstOrDefaultAsync();
                    if (string.IsNullOrWhiteSpace(pageVersions.SettingsBody))
                        throw new InvalidOperationException("Page JSON is empty.");

                    JObject root;
                    try { root = JObject.Parse(pageVersions.SettingsBody); }
                    catch (Exception ex) { throw new System.Text.Json.JsonException("Stored page JSON is invalid.", ex); }

                    JObject newComponent;
                    try { newComponent = JObject.Parse(model.NewComponentJson); }
                    catch (Exception ex) { throw new System.Text.Json.JsonException("NewComponentJson is not valid JSON.", ex); }

                    newComponent["componentSettingsId"] = model.ComponenetSettingsId;

                    var replaced = ReplaceComponentByComponentSettingsId(root, model.ComponenetSettingsId, newComponent);

                    if (!replaced)
                        throw new Exception("the json not update");

                    pageVersions.SettingsBody = root.ToString(Formatting.None);
                    var newSettings = new SavePageSettingsViewModel {
                        IsDraft=false,
                        PageId=model.PageId.Value,
                        UserId=await _WorkContext.GetCurrentUserId(),
                        PageComponents= System.Text.Json.JsonSerializer.Deserialize<List<string>>(pageVersions.PageComponents),
                        Settings=pageVersions.SettingsBody


                    };
                    await SavePageSettings(newSettings, _WorkContext.GetCurrentSiteId());

                }
                else if (model.PageTemplateId.HasValue)
                {
                    var CurrentVersionId = await _pageTemplatesRepository.Query().Where(x => x.Id==model.PageTemplateId).AsNoTracking().Select(x => x.CurrentVersionId).FirstOrDefaultAsync();
                    var pageVersions = await _templateVersionsRepository.Query().Where(x => x.Id==CurrentVersionId).FirstOrDefaultAsync();
                    if (string.IsNullOrWhiteSpace(pageVersions.SettingsBody))
                        throw new InvalidOperationException("Page JSON is empty.");

                    JObject root;
                    try { root = JObject.Parse(pageVersions.SettingsBody); }
                    catch (Exception ex) { throw new System.Text.Json.JsonException("Stored page JSON is invalid.", ex); }

                    JObject newComponent;
                    try { newComponent = JObject.Parse(model.NewComponentJson); }
                    catch (Exception ex) { throw new System.Text.Json.JsonException("NewComponentJson is not valid JSON.", ex); }

                    newComponent["componentSettingsId"] = model.ComponenetSettingsId;

                    var replaced = ReplaceComponentByComponentSettingsId(root, model.ComponenetSettingsId, newComponent);

                    if (!replaced)
                        throw new Exception("the json not update");

                    pageVersions.SettingsBody = root.ToString(Formatting.None);
                    var newSettings = new TemplateSettingsModel
                    {
                        IsDraft = false,
                        TemplateComponents=System.Text.Json.JsonSerializer.Deserialize<List<string>>(pageVersions.TemplateComponents),
                        Settings=pageVersions.SettingsBody,
                        TemplateId=model.PageTemplateId.Value
                    };
                    await SaveTemplatesSettings(newSettings, _WorkContext.GetCurrentSiteId());
                    await _templateVersionsRepository.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private static bool ReplaceComponentByComponentSettingsId(JToken token, string targetId, JObject newComponent)
        {
            if (token is JObject obj)
            {
                var id = obj["componentSettingsId"]?.Value<string>();
                if (string.Equals(id, targetId, StringComparison.Ordinal))
                {
                    var clone = (JObject)newComponent.DeepClone();

                    switch (obj.Parent)
                    {
                        case JProperty prop:
                            prop.Value = clone;
                            break;
                        case JArray arr:
                            var idx = arr.IndexOf(obj);
                            if (idx >= 0) arr[idx] = clone;
                            break;
                        case JObject parentObj:
                            var propToReplace = parentObj.Properties()
                                .FirstOrDefault(p => ReferenceEquals(p.Value, obj));
                            if (propToReplace != null) propToReplace.Value = clone;
                            break;
                        default:
                            obj.RemoveAll();
                            foreach (var p in clone.Properties())
                                obj[p.Name] = p.Value;
                            break;
                    }
                    return true;
                }
            }

            foreach (var child in token.Children())
                if (ReplaceComponentByComponentSettingsId(child, targetId, newComponent))
                    return true;

            return false;
        }


        /**************************************** Page Template *********************************/
        #region Page Templates

        public async Task<bool> UpdatePagesTemplate(string PagesIds, int TemplateId, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query().Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            pageList[i].PageTemplateId = TemplateId;
                            pageList[i].ModifiedById = UserId;
                            await _pageRepository.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public void UpdateTemplateVersion(long? templateId, long versionId)
        {
            PageTemplates page = _pageTemplatesRepository.Query().FirstOrDefault(v => v.Id == templateId);
            page.CurrentVersionId = versionId;
            _pageRepository.SaveChanges();
        }


        public int ChangePageAndTemplatesVersions()
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var transaction = _context.Database.BeginTransaction();
                var pages = _pageRepository.Query().Include(x => x.PageVersions);
                foreach (var page in pages)
                {
                    if (page.CurrentVersionId == null && page.PageVersions.Count > 1)
                    {
                        var pageVersion = page.PageVersions.OrderBy(e => e.VersionId).Last();
                        page.CurrentVersionId = pageVersion.Id;
                    }
                    else if (page.CurrentVersionId == null)
                    {
                        page.CurrentVersion = new PageVersions
                        {
                            CreatedById = page.CreatedById,
                            CreationDate = DateTime.Now,
                            PageId = page.Id,
                            SettingsBody = "{}",
                            VersionId = 1
                        };
                    }
                }
                _pageRepository.SaveChanges();
                transaction.Commit();
            });
            strategy.Execute(() =>
            {
                using var transaction = _context.Database.BeginTransaction();
                var pagesTemplates = _pageTemplatesRepository.Query().Include(x => x.PageTemplateVersions);
                foreach (var pageTemplate in pagesTemplates)
                {
                    if (pageTemplate.CurrentVersionId == 0 && pageTemplate.PageTemplateVersions.Count > 1)
                    {
                        var pageVersion = pageTemplate.PageTemplateVersions.OrderBy(e => e.VersionId).Last();
                        pageTemplate.CurrentVersionId = pageVersion.Id;
                    }
                    else if (pageTemplate.CurrentVersionId == 0)
                    {
                        pageTemplate.CurrentVersion = new TemplateVersions
                        {
                            CreatedById = pageTemplate.CreatedById,
                            CreationDate = DateTime.Now,
                            Css = "",
                            Scripts = "",
                            SettingsBody = "{}",
                            VersionId = 1,
                            TemplateId = pageTemplate.Id
                        };
                    }
                }
                _pageTemplatesRepository.SaveChanges();
                transaction.Commit();
            });
            return 0;

        }


        public async Task<IList<PageTemplates>> GetAllPageTemplates(long SiteId, EStatus? Status, ETemplateTypes? Type = null)
        {
            try
            {
                var PageTemplates = await _pageTemplatesRepository.Query().AsNoTracking().Where(x => x.SiteId == SiteId
                && (Type == null || x.Type == Type)
                && (
                (Status != null && x.Status == Status)  // if Status not null check template status
                || (Status == null && x.Status == EStatus.Published)  // if status is null get published
                || (Status == EStatus.All) // if status = ALL get All Template
                )).Select(x => x).ToListAsync();
                return PageTemplates;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<PageTemplates> AddPageTemplates(PageTemplates pageTemplate, long SiteId, long UserId)
        {
            try
            {

                pageTemplate.Status = EStatus.Published;
                pageTemplate.SiteId = SiteId;
                pageTemplate.CreatedById = UserId;
                pageTemplate.CurrentVersion = new TemplateVersions
                {
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    Css = "",
                    Scripts = "",
                    SettingsBody = "{}",
                    VersionId = 1,
                    Alias = "Initial"
                };

                _pageTemplatesRepository.Add(pageTemplate);
                await _pageTemplatesRepository.SaveChangesAsync();
                pageTemplate.CurrentVersion.TemplateId = pageTemplate.Id;
                await _pageTemplatesRepository.SaveChangesAsync();
                return pageTemplate;

            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<PageTemplates> EditPageTemplates(PageTemplates pageTemplate, long UserId, long PageId)
        {
            try
            {
                var PT = await _pageTemplatesRepository.Query().Where(x => x.Id == PageId).FirstOrDefaultAsync();

                PT.ModifiedById = UserId;
                PT.ModificationDate = DateTimeOffset.Now;
                PT.Name = pageTemplate.Name;
                PT.Description = pageTemplate.Description;
                PT.Type = pageTemplate.Type;
                PT.Status = pageTemplate.Status;
                PT.AddtionalMetaTags = pageTemplate.AddtionalMetaTags;

                await RemoveTemplateCaching(PT.Id, PT.Type);

                await _pageTemplatesRepository.SaveChangesAsync();

                return PT;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        private async Task RemoveTemplateCaching(long id, ETemplateTypes Type)
        {
            try
            {
                var siteId = await _WorkContext.GetCurrentSiteIdAsync();

                if (Type == ETemplateTypes.Page)
                {
                    var objPages = await _pageRepository.Query().Where(x => x.Status != EStatus.Deleted).Include(x => x.SlugInfo).Where(x => x.PageTemplateId == id).AsNoTracking().ToListAsync();

                    cacheService.RemoveByPrefix(objPages.Select(page => page.SlugInfo.Slug + page.CultureId + page.SiteId).ToList());
                    await clientCacheService.RemoveCacheFile(objPages.Select(x => new SlugCacheVM
                    {
                        CultureId = x.CultureId,
                        Slug = x.SlugInfo.Slug
                    }).ToList(), siteId);
                }
                else if (Type == ETemplateTypes.Post)
                {

                    var objPosts = await postRepository.Query().Where(x => x.Status != EStatus.Deleted).Include(x => x.SlugInfo).Where(x => x.PostTemplateId == id).AsNoTracking().ToListAsync();

                    cacheService.RemoveByPrefix(objPosts.Select(post => post.SlugInfo.Slug + post.CultureId + post.SiteId).ToList());

                    await clientCacheService.RemoveCacheFile(objPosts.Select(x => new SlugCacheVM
                    {
                        CultureId = x.CultureId,
                        Slug = x.SlugInfo.Slug
                    }).ToList(), siteId);
                }
                else if (Type == ETemplateTypes.Brand)
                {

                    var objBrands = await brandDetailsRepository.Query().Include(x => x.Brand).Where(x => x.Brand.Status != EStatus.Deleted).Include(x => x.SlugInfo).Where(x => x.BrandTemplateId == id).AsNoTracking().ToListAsync();

                    cacheService.RemoveByPrefix(objBrands.Select(brand => brand.SlugInfo.Slug + brand.CultureId + siteId).ToList());
                    await clientCacheService.RemoveCacheFile(objBrands.Select(x => new SlugCacheVM
                    {
                        CultureId = x.CultureId,
                        Slug = x.SlugInfo.Slug
                    }).ToList(), siteId);
                }
                else if (Type == ETemplateTypes.Product)
                {

                    var objProducts = await productDetailsRepository.Query().Include(x => x.Product).Where(x => x.Product.Status != EStatus.Deleted).Include(x => x.SlugInfo).Where(x => x.ProductTemplateId == id).AsNoTracking().ToListAsync();

                    cacheService.RemoveByPrefix(objProducts.Select(product => product.SlugInfo.Slug + product.CultureId + siteId).ToList());
                    await clientCacheService.RemoveCacheFile(objProducts.Select(x => new SlugCacheVM
                    {
                        CultureId = x.CultureId,
                        Slug = x.SlugInfo.Slug
                    }).ToList(), siteId);
                }
                else if (Type == ETemplateTypes.Category)
                {

                    var objCategories = await categoryDetailsRepository.Query().Include(x => x.SlugInfo).Include(x => x.SlugInfo)
                        .Where(x => x.Category.Status != EStatus.Deleted && x.SlugInfoId.HasValue && x.CultureId.HasValue)
                        .Where(x => x.CategoryTemplateId == id).AsNoTracking().ToListAsync();
                    cacheService.RemoveByPrefix(objCategories.Select(category => category.SlugInfo.Slug + (category.CultureId.HasValue ? category.CultureId : "0") + siteId).ToList());



                    await clientCacheService.RemoveCacheFile(objCategories.Select(x => new SlugCacheVM
                    {
                        CultureId = x.CultureId ?? 0,
                        Slug = x.SlugInfo.Slug
                    }).ToList(), siteId);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> DeleteTemplateByID(long ID, long UserId)
        {

            try
            {
                var PT = await _pageTemplatesRepository.Query().Where(x => x.Id == ID).Select(x => x).FirstOrDefaultAsync();
                PT.Status = EStatus.Deleted;
                PT.ModifiedById = UserId;
                PT.ModificationDate = DateTimeOffset.Now;
                await _pageRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<IList<VersionsViewModel>> SaveTemplatesSettings(TemplateSettingsModel templateSettingsModel, long siteId)
        {
            try
            {

                TemplateVersions tv = new TemplateVersions();

                tv.TemplateId = templateSettingsModel.TemplateId;
                tv.SettingsBody = templateSettingsModel.Settings;
                tv.Css = templateSettingsModel.Css;
                tv.Alias = templateSettingsModel.Alias;
                if (templateSettingsModel.TemplateComponents == null)
                {
                    templateSettingsModel.TemplateComponents = new List<string>();
                }

                tv.TemplateComponents = JsonConvert.SerializeObject(templateSettingsModel.TemplateComponents);
                tv.CreatedById = templateSettingsModel.UserId;
                tv.CreationDate = DateTimeOffset.Now;
                tv.VersionId = GetLatestTemplateVersionId(templateSettingsModel.TemplateId) + 1;
                _templateVersionsRepository.Add(tv);

                await _templateVersionsRepository.SaveChangesAsync();

                if (!templateSettingsModel.IsDraft)
                {
                    UpdateTemplateVersion(tv.TemplateId, tv.Id);

                }


                string tempMaxPageVersionCount = GetSiteSettingByKey("MaxPageTemplateVersionCount", siteId);
                long MaxPageVersionCount = 15;

                if (tempMaxPageVersionCount != "")
                {
                    MaxPageVersionCount = long.Parse(tempMaxPageVersionCount);
                }

                var template = _pageTemplatesRepository.Query().Where(x => x.Id == templateSettingsModel.TemplateId).FirstOrDefault();

                var allOldVersions = _templateVersionsRepository.Query().Where(x => template.CurrentVersionId != x.Id && x.TemplateId == templateSettingsModel.TemplateId && x.VersionId < tv.VersionId - MaxPageVersionCount)
                    .Include(t => t.ImportedFiles)
                    .Include(t => t.TemplateRoleVersions)
                    .ToList();


                var oldVersionToBeRemoved = new List<TemplateVersions>();

                if (allOldVersions.Count >= MaxPageVersionCount)
                {
                    var diff = allOldVersions.Count - MaxPageVersionCount;
                    for (int i = 0; i < diff; i++)
                    {
                        _TemplateImportedFilesRepository.RemoveRange(allOldVersions[i].ImportedFiles);
                        _TemplateImportedFilesRepository.SaveChanges();
                        _templateRoleVersionsRepository.RemoveRange(allOldVersions[i].TemplateRoleVersions);
                        _templateRoleVersionsRepository.SaveChanges();
                        oldVersionToBeRemoved.Add(allOldVersions[i]);

                    }

                }

                _templateVersionsRepository.RemoveRange(oldVersionToBeRemoved);

                await _templateVersionsRepository.SaveChangesAsync();

                await InsertTemplateSettingsForRoles(templateSettingsModel, siteId, tv.Id, tv.TemplateComponents);

                var Versions = _templateVersionsRepository.Query().Where(x => x.TemplateId == templateSettingsModel.TemplateId)
                    .Include(x => x.Template).Select(x => new VersionsViewModel
                    {
                        Id = x.Id,
                        VersionId = x.VersionId,
                        CreatedByName = x.CreatedBy.FullName,
                        CreationDate = x.CreationDate,
                        CreatedById = x.CreatedById,
                        IsPublished = x.Template.CurrentVersionId == x.Id,
                        Alias = x.Alias,
                    }
                 ).OrderByDescending(x => x.CreationDate).Take(100).ToList();

                return Versions;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public async Task<bool> InsertTemplateSettingsForRoles(TemplateSettingsModel SaveTemplateSettingsModel, long siteId, long versionId, string templateComponents)
        {
            var userId = await _WorkContext.GetCurrentUserId();
            var roleIds = _roleManager.Roles.Where(x => x.SiteId == siteId || x.SiteId == null).Select(x => x.Id).ToList();

            if (roleIds.Any())
            {
                List<TemplateRoleVersions> Result = new();
                foreach (var RoleId in roleIds)
                {
                    var serealizedObject = JToken.Parse(SaveTemplateSettingsModel.Settings);
                    var filterdSettings = (await SettingsFilteration(serealizedObject, RoleId)).ToString();
                    Result.Add(new TemplateRoleVersions
                    {
                        RoleId = RoleId,
                        PageTemplateId = SaveTemplateSettingsModel.TemplateId,
                        CreationDate = DateTimeOffset.Now,
                        PageTemplateVersionId = versionId,
                        SettingsBody = filterdSettings,
                        CreatedById = userId,
                        TemplateComponents = templateComponents,

                    });
                }

                foreach (var item in Result)
                {
                    _templateRoleVersionsRepository.Add(item);
                    _templateRoleVersionsRepository.SaveChanges();
                }
                return true;
            }

            return false;
        }


        public async Task<PageTemplateVM> GetPageTemplateSettings(long TemplateId)
        {
            var pageTemplate = await _pageTemplatesRepository.Query()
                .AsNoTracking()
                .Where(x => x.Id == TemplateId)
                .Select(x => new PageTemplateVM
                {
                    Description = x.Description,
                    Name = x.Name,
                    Type = (long)x.Type,

                    CurrentVersion = x.CurrentVersion == null
                        ? null
                        : new TemplateVersionVM
                        {
                            Id = x.CurrentVersionId,
                            CreatedById = x.CurrentVersion.CreatedById,
                            CreatedByName = x.CurrentVersion.CreatedBy != null
                                ? x.CurrentVersion.CreatedBy.FullName
                                : string.Empty,

                            CreationDate = x.CurrentVersion.CreationDate,
                            Css = x.CurrentVersion.Css,
                            Alias = x.CurrentVersion.Alias,
                            Scripts = x.CurrentVersion.Scripts,
                            SettingsBody = x.CurrentVersion.SettingsBody,
                            VersionId = x.CurrentVersion.VersionId,

                            // Do not load this inside the main projection.
                            // It causes OUTER APPLY in MySQL/Pomelo.
                            ImportFilesList = new List<ImportedFilesVM>()
                        }
                })
                .FirstOrDefaultAsync();

            if (pageTemplate == null || pageTemplate.CurrentVersion == null)
                return pageTemplate;

            var currentVersionId = pageTemplate.CurrentVersion.Id;

            pageTemplate.CurrentVersion.ImportFilesList = await _templateVersionsRepository.Query()
                .AsNoTracking()
                .Where(v => v.Id == currentVersionId)
                .SelectMany(v => v.ImportedFiles.Select(i => new ImportedFilesVM
                {
                    Id = i.Id,
                    FileId = i.Media != null ? i.Media.Id : 0,
                    FileName = i.Media != null ? i.Media.Filename : string.Empty,
                    FileUrl = i.Media != null ? i.Media.PublicUrl : string.Empty,
                    SourceId = currentVersionId
                }))
                .ToListAsync();

            return pageTemplate;
        }



        public async Task<PageTemplates> DuplicateTemplate(PageTemplates pageTemplate, long TemplateId, long UserId)
        {
            try
            {
                var orgTemp = await _pageTemplatesRepository.Query().Where(x => x.Id == TemplateId).Include(x => x.CurrentVersion).FirstOrDefaultAsync();

                var result = await AddPageTemplates(pageTemplate, orgTemp.SiteId, UserId);

                TemplateSettingsModel ts = new TemplateSettingsModel();

                ts.Css = orgTemp.CurrentVersion.Css;
                ts.Settings = orgTemp.CurrentVersion.SettingsBody;
                ts.UserId = UserId;
                ts.TemplateId = result.Id;

                await SaveTemplatesSettings(ts, orgTemp.SiteId);

                return result;
            }

            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<Page> GetPageByIdAsync(int id, long siteId)
        {
            try
            {
                var page = await _pageRepository.Query().Include(x => x.PageCategory).Where(x => x.Id == id).Select(x => x).FirstOrDefaultAsync();
                return page;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> SetTemplate(TemplateSettingsModel template)
        {
            try
            {
                var Template = _pageTemplatesRepository.Query()
                                .Where(x => x.Id == template.TemplateId)
                                .Include(x => x.CurrentVersion)
                                .ThenInclude(i => i.ImportedFiles)
                                .ThenInclude(i => i.Media)
                                .FirstOrDefault();

                Template.CurrentVersion.Css = template.Css;
                Template.CurrentVersion.Scripts = template.Scripts;
                await _pageTemplatesRepository.SaveChangesAsync();

                foreach (var Item in template.ImportFilesList)
                {

                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<TemplateVersionVM> GetTemplateData(long TemplateId)
        {
            try
            {
                // ChangesToBeTestedByYazan
                var TemplateData = _pageTemplatesRepository.Query()
                    .Where(x => x.Id == TemplateId)
                    .Include(x => x.CurrentVersion)
                    .ThenInclude(x => x.ImportedFiles)
                    .ThenInclude(x => x.Media)
                    .Select(x => new TemplateVersionVM()
                    {
                        TemplateId = x.Id,
                        Css = x.CurrentVersion.Css,
                        ImportFilesList = x.CurrentVersion.ImportedFiles
                            .Select(i => new ImportedFilesVM()
                            {
                                Id = i.Id,
                                FileId = i.Media == null ? 0 : i.Media.Id,
                                FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                SourceId = x.CurrentVersionId
                            })
                            .ToList(),
                        Scripts = x.CurrentVersion.Scripts,

                    })
                    .FirstOrDefault();


                return TemplateData;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        // this function for updating a part of template
        public async Task<bool> SetTemplate(TemplateSettingsModel template, UpdateTemplateEnum Target)
        {
            try
            {
                var Template = _pageTemplatesRepository.Query().Where(x => x.Id == template.TemplateId).Include(x => x.CurrentVersion).FirstOrDefault();
                switch (Target)
                {
                    case UpdateTemplateEnum.CSS:

                        Template.CurrentVersion.Css = template.Css;
                        break;

                    case UpdateTemplateEnum.ImportFiles:

                        var Files = template.ImportFilesList.Where(f => f.Id == 0).Select(i => new TemplateImportedFiles()
                        {
                            MediaId = i.FileId,
                            TemplateVersionId = Template.CurrentVersionId

                        }).ToList();

                        _TemplateImportedFilesRepository.AddRange(Files);
                        await _TemplateImportedFilesRepository.SaveChangesAsync();
                        break;

                    case UpdateTemplateEnum.Scripts:

                        Template.CurrentVersion.Scripts = template.Scripts;
                        break;
                }



                await _pageTemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<VersionsViewModel>> GetTemplatesVersions(long TemplateId)
        {
            try
            {
                return await _templateVersionsRepository.Query().Where(v => v.TemplateId == TemplateId).Select(x => new VersionsViewModel
                {
                    Id = x.Id,
                    VersionId = x.VersionId,
                    CreatedByName = x.CreatedBy.FullName,
                    CreationDate = x.CreationDate,
                    CreatedById = x.CreatedById,
                    IsPublished = x.Template.CurrentVersionId == x.Id ? true : false,
                    Alias = x.Alias

                }).OrderByDescending(x => x.CreationDate).Take(100).ToListAsync();

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> DeleteImportedFile(long Id)
        {
            try
            {
                var ImportedFile = await _TemplateImportedFilesRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                _TemplateImportedFilesRepository.Remove(ImportedFile);
                await _TemplateImportedFilesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }
        #endregion


        #region Helper_Functions

        private async Task<JToken> SettingsFilteration(JToken Settings, long RoleId)
        {
            if (Settings.Type == JTokenType.Object)
            {
                JObject SettingObject = (JObject)Settings;
                if (SettingObject.ContainsKey("rolesAndAsigend") && (JArray)SettingObject["rolesAndAsigend"] != null)// 
                {

                    JArray Roles = (JArray)SettingObject["rolesAndAsigend"];

                    bool CanAccesThisComponent = Roles.Any(id => (long)id == RoleId);
                    if (CanAccesThisComponent)
                    {
                        return Settings;

                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    foreach (JProperty property in SettingObject.Properties())
                    {
                        //string pn= property.Name.ToString();
                        JToken propertyValue = (JToken)property.Value;
                        var Result = await SettingsFilteration(propertyValue, RoleId);
                        if (Result is null)
                        {
                            return null;
                        }
                        property.Value = Result;
                    }

                    return SettingObject;
                }
            }
            else if (Settings.Type == JTokenType.Array)
            {
                JArray SettingArray = (JArray)Settings;
                JArray Result = new JArray();
                foreach (JToken SettingItem in SettingArray)
                {
                    var ResultItem = await SettingsFilteration(SettingItem, RoleId);
                    if (ResultItem is not null)
                    {
                        Result.Add(ResultItem);
                    }
                }
                return Result;
            }
            else
            {
                return Settings;

            }

        }



        private async Task<JToken> GetSpecificKey(JToken settings, string componentName, string settingKey)
        {

            if (settings.Type == JTokenType.Object)
            {
                JObject obj = (JObject)settings;

                if (obj.ContainsKey("componentName") &&
                    obj["componentName"]?.ToString() == componentName)
                {
                    var st = obj["setting"];

                    if (st != null && st.Type == JTokenType.Object)
                    {
                        JObject settingObj = (JObject)st;

                        // return if key exists
                        if (!string.IsNullOrWhiteSpace(settingKey) &&
                            settingObj.ContainsKey(settingKey))
                        {
                            return settingObj[settingKey];
                        }

                        // return whole object if no key requested
                        if (string.IsNullOrWhiteSpace(settingKey))
                            return settingObj;
                    }

                    // ❗ IMPORTANT FIX:
                    // If "setting" is string OR
                    // the key does not exist → skip this component
                    // and continue searching others
                }

                // Recursively search children
                foreach (var prop in obj.Properties())
                {
                    var r = await GetSpecificKey(prop.Value, componentName, settingKey);
                    if (r != null)
                        return r;
                }

                return null;
            }

            if (settings.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)settings)
                {
                    var r = await GetSpecificKey(item, componentName, settingKey);
                    if (r != null)
                        return r;
                }
            }

            return null;


        }

        private static PageVersionVM GetCurrentPageVersion(ICollection<PageVersions> pageVersions, long pageId)
        {
            var pageVersion = pageVersions.Where(version => version.PageId == pageId).OrderBy(e => e.VersionId).Last();
            PageVersionVM pageLatestVersion = new PageVersionVM
            {
                CreatedById = pageVersion.CreatedById,
                CreatedByName = pageVersion.CreatedBy.FullName,
                CreationDate = pageVersion.CreationDate,
                Id = pageVersion.Id,
                SettingsBody = pageVersion.SettingsBody,
                VersionId = pageVersion.VersionId
            };
            return pageLatestVersion;
        }


        private static TemplateVersionVM GetCurrentTemplateVersion(ICollection<TemplateVersions> templateVersions, long templateId)
        {
            var templateVersion = templateVersions
                                    .Where(version => version.TemplateId == templateId)
                                    .OrderBy(e => e.VersionId)
                                    .Last();

            TemplateVersionVM templateLatestVersion = new TemplateVersionVM
            {
                CreatedById = templateVersion.CreatedById,
                CreatedByName = templateVersion.CreatedBy.FullName,
                CreationDate = templateVersion.CreationDate,
                Css = templateVersion.Css,
                // ImportFilesList = templateVersion.ImportFiles,
                Scripts = templateVersion.Scripts,
                SettingsBody = templateVersion.SettingsBody,
                VersionId = templateVersion.VersionId
            };
            return templateLatestVersion;
        }


        private long GetLatestVersionId(long PageId)

        {
            var page = _pageRepository.Query().Where(x => x.Id == PageId).Select(x => x).FirstOrDefault();
            if (page != null)
            {
                return page.CurrentVersionId.GetValueOrDefault();
            }

            return 0;

        }

        private long GetLatestTemplateVersionId(long TemplateId)
        {
            var temp = _pageTemplatesRepository.Query().Where(x => x.Id == TemplateId).Select(x => x).FirstOrDefault();
            if (temp != null)
            {
                return temp.CurrentVersionId;
            }

            return 0;
        }


        private string GetSiteSettingByKey(string key, long siteId)
        {
            var setting = _siteSettingsRepository.Query().Where(x => x.SiteId == siteId && x.Id == key).Select(x => x.Value).FirstOrDefault();
            if (setting == null)
                return "";
            else
                return setting;
        }


        private async Task<bool> UpsertPageRolesAsync(RelatedPageVM SelectedPage, List<long> RolesId, long UserId)
        {

            foreach (var id in RolesId)
            {
                var Role = SelectedPage.PageRoles?.FirstOrDefault(x => x.RoleId == id && x.PageId == SelectedPage.Id);
                if (Role == null)
                {
                    Role = new PagesRoles()
                    {
                        RoleId = id,
                        PageId = SelectedPage.Id,
                        CreatedById = UserId
                    };
                    _PagesRolesRepository.Add(Role);

                }
                else
                {
                    Role.RoleId = id;
                    Role.CreatedById = UserId;
                    _PagesRolesRepository.Update(Role);


                }

            }

            var DeletedRoles = SelectedPage.PageRoles.Where(p => !RolesId.Contains(p.RoleId)).ToList();
            if (DeletedRoles.Count != 0)
            {
                _PagesRolesRepository.RemoveRange(DeletedRoles);
            }
            await _PagesRolesRepository.SaveChangesAsync();
            return true;
        }




        private List<PageResultVM> BuildPagesTree(List<Page> pageList)
        {
            List<Page> parentPages = new List<Page>();
            List<PageResultVM> pageResults = new List<PageResultVM>();

            parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

            foreach (var page in parentPages)
            {
                PageResultVM pageResultVM = new PageResultVM();

                pageResultVM.Name = page.Name;
                pageResultVM.IsRequiredLogin = page.IsLoginRequired;
                pageResultVM.PageStatus = pageList.Any(x => x.ParentId == page.Id && x.Status == EStatus.Published) ? EStatus.Published : EStatus.Unpublished;
                pageResultVM.PageCategoryId = page.PageCategoryId;
                pageResultVM.PageCategoryName = page.PageCategory.CategoryDetails.Where(i => i.CultureId == page.CultureId).Select(x => x.Name).FirstOrDefault() ?? "";
                pageResultVM.CreationDate = page.CreationDate;
                pageResultVM.ParentPageId = page.Id;
                pageResultVM.TemplateName = page.PageTemplate == null ? "No Template" : page.PageTemplate.Name;
                pageResultVM.TemplateId = page.PageTemplate == null ? 0 : page.PageTemplate.Id;
                pageResultVM.ParentPageSlug = page.SlugInfo.Slug;
                pageResultVM.IsDefaultPage = page.Site == null ? false : page.Site.DefaultParentPageId == page.ParentId;
                pageResultVM.PublishDate = page.PublishDate;
                pageResultVM.ExpiryDate = page.ExpiryDate;
                pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
                    .Select(c => new PageCultureDetailsVM()
                    {
                        Name = c.Name,
                        PageCultureStatus = c.Status,
                        PageId = c.Id,
                        ParentPageId = c.ParentId,
                        CultureCode = c.Culture.CultureId,
                        CultureId = c.CultureId,
                        Thumbnail = c.Thumbnail == null ? "" : c.Thumbnail.PublicUrl,
                        Slug = c.SlugInfo.Slug,
                        PageCategoryId = c.PageCategoryId,
                        PageCategoryName = c.PageCategory.CategoryDetails.Where(i => i.CultureId == c.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                        TemplateName = c.PageTemplate == null ? "No Template" : c.PageTemplate.Name,
                        TemplateId = c.PageTemplate == null ? 0 : c.PageTemplate.Id,
                        LoginPage = c.ParentLoginPage != null ? c.ParentLoginPage.Children.Where(x => x.CultureId == c.CultureId).Select(l => new LoginPageDetails()
                        {
                            Id = l.Id,
                            Slug = l.SlugInfo.Slug,
                            Name = l.Name,
                            ParentId = l.ParentId.Value
                        }).FirstOrDefault() : null
                    })
                    .ToList();


                pageResults.Add(pageResultVM);
            }

            return pageResults.OrderByDescending(p => p.CreationDate).ToList();
        }



        //private List<PageSiteMapResultVM> BuildSiteMapTree(List<Page> pageList)
        //{
        //    List<Page> parentPages = new List<Page>();
        //    List<PageSiteMapResultVM> pageResults = new List<PageSiteMapResultVM>();

        //    parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

        //    foreach (var page in parentPages)
        //    {
        //        PageSiteMapResultVM pageResultVM = new PageSiteMapResultVM();

        //        pageResultVM.Name = page.Name;
        //        pageResultVM.CreationDate = page.CreationDate;
        //        pageResultVM.ParentPageId = page.Id;
        //        pageResultVM.PageSlug = page.Slug;
        //        pageResultVM.CultureId = page.CultureId;
        //        //pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
        //        //.Select(c => new PageCultureDetailsVM()
        //        //{
        //        //    PageCultureStatus = c.Status,
        //        //    PageId = c.Id,
        //        //    ParentPageId = c.ParentId,
        //        //    CultureCode = c.Culture.CultureId,
        //        //    CultureId = c.CultureId,
        //        //    Thumbnail = c.Thumbnail == null ? "" : c.Thumbnail.PublicUrl,
        //        //    Slug = c.Slug
        //        //}).ToList();


        //        pageResults.Add(pageResultVM);
        //    }

        //    return pageResults;
        //}

        #endregion

        #region Old_And_Not_Used_Apis


        //public async Task<Page> AddNewPage(PagesViewModel Page)
        //{
        //    try
        //    {
        //        if (Page.PageCategoryId == 0)
        //        {
        //            var newCat = new CategoryViewModel();
        //            newCat.Name = Page.PageCategoryName;
        //            newCat.UserId = Page.UserId;
        //            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
        //            newCat.SiteId = Page.SiteId;
        //            newCat.CategoryTypeId = ECategoryTypes.Page;
        //            Page.PageCategoryId = await _CategoriesService.AddCategory(newCat);
        //        }
        //        Page ObjPage = new Page();
        //        ObjPage.Name = Page.Name;
        //        ObjPage.Slug = Page.Slug;
        //        ObjPage.CreatedById = Page.UserId;
        //        ObjPage.CreationDate = DateTimeOffset.Now;
        //        ObjPage.Status = Page.Status;
        //        ObjPage.StatusDate = DateTimeOffset.Now;
        //        ObjPage.NavigationTitle = Page.Slug;
        //        ObjPage.PageCategoryId = Page.PageCategoryId;
        //        ObjPage.SiteId = Page.SiteId;
        //        ObjPage.Route = Page.Route;
        //        ObjPage.Description = Page.Description;
        //        ObjPage.Keywords = Page.Keywords;
        //        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
        //        ObjPage.AdditionalContent = Page.AdditionalContent;
        //        ObjPage.IsLoginRequired = Page.IsLoginRequired;
        //        ObjPage.ThumbnailId = Page.ThumbnailId;

        //        ObjPage.CultureId = Page.CultureId;

        //        long? noTemplate = null;
        //        ObjPage.PageTemplateId = Page.PageTemplateId != 0 ? Page.PageTemplateId : noTemplate;
        //        _pageRepository.Add(ObjPage);
        //        ObjPage.CurrentVersion = new PageVersions
        //        {
        //            CreatedById = ObjPage.CreatedById,
        //            CreationDate = DateTime.Now,
        //            SettingsBody = "{}",
        //            VersionId = 1
        //        };
        //        await _pageRepository.SaveChangesAsync();
        //        ObjPage.CurrentVersion.PageId = ObjPage.Id;
        //        await _pageRepository.SaveChangesAsync();
        //        return ObjPage;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}


        //public async Task<Page> EditPage(PagesViewModel Page)
        //{
        //    try
        //    {

        //        var ObjPage = await _pageRepository.Query().Where(x => x.Id == Page.Id).FirstOrDefaultAsync();
        //        if (Page.PageCategoryId == 0)
        //        {
        //            var newCat = new CategoryViewModel();
        //            newCat.Name = Page.PageCategoryName;
        //            newCat.UserId = Page.CreatedById;
        //            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
        //            newCat.SiteId = Page.SiteId;
        //            newCat.CategoryTypeId = ECategoryTypes.Page;
        //            Page.PageCategoryId = await _CategoriesService.AddCategory(newCat);
        //        }
        //        ObjPage.Name = Page.Name;
        //        ObjPage.Slug = Page.Slug;
        //        ObjPage.CreatedById = ObjPage.CreatedById;
        //        ObjPage.CreationDate = ObjPage.CreationDate;
        //        ObjPage.ModifiedById = Page.UserId;
        //        ObjPage.ModificationDate = DateTimeOffset.Now;
        //        ObjPage.Status = Page.Status;
        //        ObjPage.StatusDate = DateTimeOffset.Now;
        //        ObjPage.NavigationTitle = Page.Slug;
        //        ObjPage.PageCategoryId = Page.PageCategoryId;
        //        ObjPage.SiteId = Page.SiteId;
        //        ObjPage.Route = Page.Route;
        //        ObjPage.Description = Page.Description;
        //        ObjPage.Keywords = Page.Keywords;
        //        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
        //        ObjPage.IsLoginRequired = Page.IsLoginRequired;
        //        ObjPage.ThumbnailId = Page.ThumbnailId;
        //        ObjPage.AdditionalContent = Page.AdditionalContent;
        //        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
        //        //ObjPage.PageTemplateId = Page.CultureId;
        //        ObjPage.Slug = Page.Slug;

        //        await _pageRepository.SaveChangesAsync();
        //        return ObjPage;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}
        //Bulk update and delete
        #endregion

        public async Task<Dictionary<string, object>> GetComponentsSettings(long PageId, string ComponentId, string Culture, string IdentifierCode, bool? isComponentShared)
        {
            try
            {
                long SiteId = _WorkContext.GetCurrentSiteId();
                long CultureId = _WorkContext.GetCurrentCultureId();
                var Result = new Dictionary<string, object>();

                var pageContent = await _pageVersions.Query().Where(x => x.PageId == PageId).OrderByDescending(x => x.VersionId).FirstOrDefaultAsync();
                if (pageContent != null)
                {
                    Result = await GetSettingsIdByPage(pageContent, ComponentId);



                    if (Result != null)
                    {
                        return Result;

                    }
                }


                var PageTemplateContent = await _pageRepository
                                             .Query()
                                             .Where(x => x.Id == PageId)
                                             .Include(x => x.PageTemplate)
                                             .ThenInclude(x => x.CurrentVersion)
                                             .Select(x => x.PageTemplate.CurrentVersion)
                                             .FirstOrDefaultAsync();
                if (PageTemplateContent != null)
                {
                    Result = await GetSettingsIdByTemplate(PageTemplateContent, ComponentId);

                    if (Result != null)
                    {
                        return Result;

                    }
                }


                var PageTemplate = await _pageTemplatesRepository.Query()
                 .Where(x => x.Id == PageId).Include(x => x.PageTemplateVersions)
                 .Select(x => x.CurrentVersion).FirstOrDefaultAsync();
                if (PageTemplate != null)
                {
                    Result = await GetSettingsIdByTemplate(PageTemplate, ComponentId);

                    if (Result != null)
                    {
                        return Result;

                    }
                }


                var SharableElements = await _SharedElemantRepo.Query()
                                                                 .Where(x =>
                                                                 (string.IsNullOrEmpty(IdentifierCode) || x.IdentifierCode.Trim().Equals(IdentifierCode.Trim()))
                                                                 && x.SiteId == SiteId
                                                                  && (x.CultureId == null || x.CultureId == CultureId)
                                                                  && x.Settings.Contains(ComponentId
                                                                  ))
                                                                 .Select(x => x.Settings)
                                                                 .ToListAsync();
                if (SharableElements != null)

                {
                    foreach (var Shareable in SharableElements)
                    {

                        JObject jo = string.IsNullOrEmpty(Shareable) ? null : JObject.Parse(Shareable);


                        Result = jo.ToObject<Dictionary<string, object>>();



                        if (Result != null)
                        {
                            return Result;
                        }
                    }


                }




                return Result;





            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        public async Task<Dictionary<string, object>> GetSettingsIdByPage(PageVersions pageContent, string ComponentId)
        {

            try
            {

                JObject jo = JObject.Parse(pageContent.SettingsBody);

                return await GetComponentsSettingsId(jo, ComponentId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Dictionary<string, object>> GetSettingsIdByTemplate(TemplateVersions pageTemplateContent, string ComponentId)
        {

            try
            {
                JObject jo = JObject.Parse(pageTemplateContent.SettingsBody);


                return await GetComponentsSettingsId(jo, ComponentId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //public async Task<Dictionary<string, object>> GetSettingsBySharedElemant(string SettingsBody, string ComponentId)
        //{

        //    try
        //    {
        //        JObject jo;
        //        try
        //        {

        //            jo = JObject.Parse(SettingsBody);
        //        }
        //        catch (Exception)
        //        {

        //            return null;
        //        }


        //        return await GetComponentsSettingsId(jo, ComponentId);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}
        // public async Task<Dictionary<string, object>> GetSettingsIdByShareableElemant(ShareableElement shareableElement, string ComponentId)
        //{

        //    try
        //    {
        //        JObject jo = JObject.Parse(shareableElement.Settings);


        //        return await GetComponentsSettingsId(jo, ComponentId);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        private async Task<Dictionary<string, object>> GetComponentsSettingsId(JObject jo, string ComponentId)
        {
            try
            {
                var sett = jo.FindTokens("setting");
                foreach (var item in sett)
                {
                    var componentSettingsId = item.FindTokens("componentSettingsId");
                    if (componentSettingsId.Count > 0)
                    {

                        if (componentSettingsId.FirstOrDefault()?.ToString() == ComponentId)
                        {
                            Dictionary<string, object> dictObj = item.ToObject<Dictionary<string, object>>();
                            return dictObj;
                        }

                    }


                }
                return null;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        //by sondos
        public async Task<IEnumerable<Page>> GetAllPagesByCultures(long[] ids)
        {
            try
            {
                long siteId = await _WorkContext.GetCurrentSiteIdAsync();
                if (ids != null && ids.Any())
                    return await _pageRepository.Query().Where(p => ids.Contains(p.CultureId)
                     && p.SiteId == siteId).ToListAsync();
                else
                    return await _pageRepository.Query().Where(p => p.SiteId == siteId).ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }


        }

        // by sondos
        public async Task<bool> AddComponentSettings(UpsertComponentSettingsVM model)
        {
            try
            {
                long siteId = await _WorkContext.GetCurrentSiteIdAsync();
                var existingComponent = await _pageComponentSettings.Query().Where(c => c.ComponentSettingsId == model.ComponentSettingsId).FirstOrDefaultAsync();

                if (existingComponent != null)
                {
                    existingComponent.Settings = model.Settings;
                    existingComponent.ComponentName = model.ComponentName;
                    existingComponent.CreationDate = DateTime.Now;
                    existingComponent.Alias = model.Alias;
                    existingComponent.PageVersionId = model.PageVersionId;
                    existingComponent.SiteId = siteId;

                }
                else
                {
                    var newComponentSettings = new PageComponentSettings()
                    {
                        ComponentName = model.ComponentName,
                        Settings = model.Settings,
                        CreationDate = DateTime.Now,
                        ComponentSettingsId = model.ComponentSettingsId,
                        Alias = model.Alias,
                        IsLazyUi = model.LazyUi,
                        PageVersionId = model.PageVersionId,
                        SiteId = siteId
                    };
                    _pageComponentSettings.Add(newComponentSettings);
                }
                await _pageComponentSettings.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //by sondos
        public async Task<bool> DuplicateComponentSettings(DuplicateComponentSettingsVM model)
        {
            try
            {

                var existingComponent = await _pageComponentSettings.Query().Where(c => c.ComponentSettingsId == model.previousId).FirstOrDefaultAsync();

                if (existingComponent != null)
                {

                    var newComponentSettings = new PageComponentSettings()
                    {
                        ComponentName = existingComponent.ComponentName,
                        Settings = existingComponent.Settings,
                        CreationDate = DateTime.Now,
                        ComponentSettingsId = model.newId,
                        Alias = existingComponent.Alias,
                        IsLazyUi = existingComponent.IsLazyUi,
                        PageVersionId = existingComponent.PageVersionId
                    };
                    _pageComponentSettings.Add(newComponentSettings);
                }
                await _pageComponentSettings.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        //by sondos
        public async Task<IEnumerable<PageComponentSettings>> FilterComponentsSettings(FilterComponentsSettingsVM model)
        {


            long siteId = await _WorkContext.GetCurrentSiteIdAsync();

            if (model.PageIds.Any() && model.CultureIds.Any())
            {
                var pageIdsForFilteredPages = await _pageRepository.Query().Where(p => model.PageIds.Contains(p.Id) &&
                model.CultureIds.Contains(p.CultureId) && p.SiteId == siteId).Select(p => p.Id)
                .ToListAsync();


                var pageVersionsIds = await _pageVersions.Query().Where(v => v.PageId.HasValue && pageIdsForFilteredPages
                .Contains(v.PageId.Value)).Select(p => p.Id).ToListAsync();


                var filteredComponentsSettings = await _pageComponentSettings.Query().Where(c => pageVersionsIds.
                Contains(c.PageVersionId) && c.ComponentName == model.ComponentName && c.IsLazyUi == true).ToListAsync();

                return filteredComponentsSettings;

            }


            else if (model.PageIds.Any())
            {
                var pageIdsForFilteredPages = await _pageRepository.Query().Where(p => model.PageIds.Contains(p.Id)
                && p.SiteId == siteId).Select(p => p.Id)
                    .ToListAsync();


                var pageVersionsIds = await _pageVersions.Query().Where(v => v.PageId.HasValue &&
                pageIdsForFilteredPages.Contains(v.PageId.Value)).
                Select(p => p.Id).ToListAsync();


                var filteredComponentsSettings = await _pageComponentSettings.Query().Where(c => pageVersionsIds.
                Contains(c.PageVersionId) && c.ComponentName == model.ComponentName && c.IsLazyUi == true).ToListAsync();

                return filteredComponentsSettings;
            }

            else if (model.CultureIds.Any())
            {
                var pageIdsForFilteredPages = await _pageRepository.Query().Where(p => model.CultureIds.Contains(p.CultureId)
                && p.SiteId == siteId)
                    .Select(p => p.Id)
                    .ToListAsync();


                var pageVersionsIds = await _pageVersions.Query().Where(v => v.PageId.HasValue &&
                pageIdsForFilteredPages.Contains(v.PageId.Value)).
                Select(p => p.Id).ToListAsync();


                var filteredComponentsSettings = await _pageComponentSettings.Query().Where(c => pageVersionsIds.
                Contains(c.PageVersionId) && c.ComponentName == model.ComponentName && c.IsLazyUi == true).ToListAsync();

                return filteredComponentsSettings;
            }


            return await _pageComponentSettings.Query().Where(c => c.SiteId == siteId && c.ComponentName == model.ComponentName && c.IsLazyUi == true).ToListAsync();




        }






        //by sondos 
        public async Task<string> GetComponentSettingsById(string SettingsId)
        {

            try
            {
                long siteId = await _WorkContext.GetCurrentSiteIdAsync();
                string settings = await _pageComponentSettings.Query().Where(x => x.ComponentSettingsId == SettingsId && x.SiteId == siteId).Select(x => x.Settings).FirstOrDefaultAsync();
                return settings ?? "{}";
            }
            catch (Exception)
            {

                throw;
            }



        }



        public async Task<bool> ChangeParentByCultureId(long siteId, long cultureId)
        {
            try
            {
                var Query = _pageRepository.Query().Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted);

                var AllPages = await Query.ToListAsync();
                var PagesEqualCulture = await Query.Where(x => x.CultureId == cultureId).ToListAsync();


                foreach (var page in PagesEqualCulture)
                {

                    var RelatedPages = AllPages.Where(x => x.ParentId == page.ParentId).ToList();

                    foreach (var CultureItem in RelatedPages)
                    {
                        CultureItem.ParentId = page.Id;
                    }


                }

                await _pageRepository.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> AddFavouritePageFroUser(long UserId, long PageId)
        {
            try
            {
                var isPageExist = await _favouritepageRepository.Query().Where(x => x.UserId == UserId && x.PageId == PageId).FirstOrDefaultAsync();
                FavouritePages favouritePages = new FavouritePages();
                bool Success = false;
                if (isPageExist == null)
                {
                    favouritePages.UserId = UserId;
                    favouritePages.PageId = PageId;
                    _favouritepageRepository.Add(favouritePages);
                    await _favouritepageRepository.SaveChangesAsync();
                    Success = true;
                }
                return Success;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<FavouritePageVM>> GetAllFavouritePage()
        {
            var UserId = await _WorkContext.GetCurrentUserId();
            var FavPage = await _favouritepageRepository.Query().Where(x => x.UserId == UserId).Include(x => x.Page).ThenInclude(x => x.SlugInfo).Select(x => new FavouritePageVM
            {
                Name = x.Page.Name,
                PageId = x.PageId,
                slugInfoId = x.Page.SlugInfoId,
                Slug = x.Page.Slug
            }).ToListAsync();
            return FavPage;
        }
        //by sondos
        public async Task<IEnumerable<string>> GetAllPagesByHostname(GetAllPagesByHostnameVM model)
        {
            var siteId = await _SitesHostNamesRepo.Query().Where(sh =>
            sh.HostName == model.Hostname).Select(s => s.SiteId).FirstOrDefaultAsync();
            var pages = await _pageRepository.Query().Include(page => page.Site)
                .Where(p => p.SiteId == siteId && p.Status == EStatus.Published &&
                p.CultureId == model.CultureId).Select(p => p.NavigationTitle).ToListAsync();
            return pages;
        }
        public async Task<bool> RemoveFavouritePage(long favouritePgaeId)
        {
            try
            {
                var favPage = await _favouritepageRepository.Query().Where(x => x.Id == favouritePgaeId).FirstOrDefaultAsync();
                if (favPage != null)
                {
                    _favouritepageRepository.Remove(favPage);
                    await _favouritepageRepository.SaveChangesAsync();
                }
                return favPage != null;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<bool> SetLoginPage(SetLoginPageVM model)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                long UserId = await _WorkContext.GetCurrentUserId();
                //long cultureId = await _WorkContext.GetCurrentCultureId();

                var pages = _pageRepository.Query().Where(x => model.ParentsPageId.Contains(x.ParentId.Value) && x.SiteId == SiteId).ToList();


                foreach (var page in pages)
                {
                    page.ParentLoginPageId = model.LoginPageId;
                    page.ModificationDate = DateTimeOffset.Now;
                    page.ModifiedById = UserId;
                    page.IsLoginRequired = model.LoginPageId.HasValue;

                }



                await _pageRepository.SaveChangesAsync();

                return true;


            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public async Task<PageVersionVM> GetVersionSettings(long verisonId)
        {
            try
            {

                PageVersionVM page = await _pageVersions.Query()
                    .Where(x => x.Id == verisonId && x.Page.Status != EStatus.Deleted)
                     .Include(x => x.CreatedBy)
                     .Include(x => x.Page)
                     .Select(v => new PageVersionVM
                     {
                         CreatedById = v.CreatedById,
                         CreatedByName = v.CreatedBy.FullName,
                         CreationDate = v.CreationDate,
                         Id = v.Id,
                         SettingsBody = v.SettingsBody,
                         VersionId = v.VersionId,
                         PageComponents = v.PageComponents,
                         PageId = v.PageId.Value,
                         IsPublished = v.Page.CurrentVersionId == v.Id ? true : false,
                         Alias = v.Alias,

                     })
                    .FirstOrDefaultAsync();

                if (page == null)
                    throw new Exception("Version Not Found");

                return page;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<TemplateVersionVM> GetTemplateVersionSettings(long templateVersionId)
        {
            // MySQL does not support OUTER APPLY, which EF Core generates when a nested
            // .Select().ToList() appears inside an outer .Select() projection. Materialise
            // the entity first, then project to the ViewModel in memory.
            var entity = await _templateVersionsRepository.Query().AsNoTracking()
                .Where(x => x.Id == templateVersionId)
                .Include(x => x.CreatedBy)
                .Include(x => x.ImportedFiles)
                    .ThenInclude(x => x.Media)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            return new TemplateVersionVM
            {
                Id = entity.Id,
                CreatedById = entity.CreatedById,
                CreatedByName = entity.CreatedBy?.FullName,
                CreationDate = entity.CreationDate,
                Alias = entity.Alias,
                Css = entity.Css,
                Scripts = entity.Scripts,
                SettingsBody = entity.SettingsBody,
                TemplateComponents = entity.TemplateComponents,
                TemplateId = entity.TemplateId.Value,
                VersionId = entity.VersionId,
                ImportFilesList = entity.ImportedFiles?.Select(i => new ImportedFilesVM
                {
                    Id = i.Id,
                    FileId = i.Media == null ? 0 : i.Media.Id,
                    FileName = i.Media == null ? string.Empty : i.Media.Filename,
                    FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                    SourceId = entity.TemplateId.Value,
                    contentType = i.Media?.ContentType,
                }).ToList() ?? new List<ImportedFilesVM>()
            };
        }

        public async Task<IList<PageVersionVM>> UpdatePageVersionAlias(EditAliasViewModel model)
        {

            var pageVersion = _pageVersions.Query().Where(x => x.Id == model.Id).FirstOrDefault();
            if (pageVersion == null)
                throw new Exception("Page Version Not Found");
            pageVersion.Alias = model.Alias;

            await _pageVersions.SaveChangesAsync();

            var Versions = await GetAllPageVersions(pageVersion.PageId.Value);



            return Versions;




        }

        public async Task<IList<VersionsViewModel>> UpdateTemplateVersionAlias(EditAliasViewModel model)
        {
            var templateVersion = _templateVersionsRepository.Query().Where(x => x.Id == model.Id).FirstOrDefault();
            if (templateVersion == null)
                throw new Exception("Template Version Not Found");
            templateVersion.Alias = model.Alias;

            await _templateVersionsRepository.SaveChangesAsync();

            var Versions = _templateVersionsRepository.Query().Where(x => x.TemplateId == templateVersion.TemplateId)
                    .Include(x => x.Template).Select(x => new VersionsViewModel
                    {
                        Id = x.Id,
                        VersionId = x.VersionId,
                        CreatedByName = x.CreatedBy.FullName,
                        CreationDate = x.CreationDate,
                        CreatedById = x.CreatedById,
                        IsPublished = x.Template.CurrentVersionId == x.Id,
                        Alias = x.Alias,
                    }
                 ).OrderByDescending(x => x.CreationDate).Take(100).ToList();

            return Versions;







        }


        private async Task<bool> ValidateStatusAndPublishDate_ExpireDate(EStatus status, DateTimeOffset? publishDate, DateTimeOffset? unPublishDate)
        {

            if (status == EStatus.Published && (unPublishDate.HasValue && unPublishDate.Value < DateTimeOffset.Now))
            {
                throw new Exception("Please Add a valid date for unpublish date");
            }

            if (status == EStatus.Unpublished && (publishDate.HasValue && publishDate.Value < DateTimeOffset.Now))
            {
                throw new Exception("Please Add a valid date for publish date");
            }

            if ((publishDate.HasValue && unPublishDate.HasValue) && (unPublishDate.Value < publishDate.Value || unPublishDate.Value == publishDate.Value))
            {
                throw new Exception("Please Add a valid range for publish and unpublish date");
            }

            return true;
        }

    }


}
