using AutoMapper;
using Elastic.Clients.Elasticsearch.Security;
using EskaCMS.BlogArchive.Models;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.ViewModels;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Pages.Models;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using EskaCMS.Sites.Services.Model;
using EskaCMS.Theming.Services.Interfaces;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Drawing.Drawing2D;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Services
{
    public class SitesService : ISites
    {
        private readonly IRepository<Entities.Entity.Core.Site> _SitesRepository;
        private readonly IRepository<UsersSites> _UserSitesRepository;
        private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        private readonly IRepository<SitesCultures> _SitesCulturesRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly ITheme _ThemeService;
        private readonly ISiteCultureService _siteCultureService;
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithTypedId<CurrentLoggedInUsers, string> _currentLoggedInRepository;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<Posts> _PostRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IdcmsLogService dcmsLogService;

        //private readonly EskaDCMSDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ISlugServices _slugServices;
        private readonly IMapper _mapper;
        private readonly IRepository<SitesSecurityPolicy> _SitesSecurityPolicyRespository;
        private readonly ICurrencyService _currencyService;
        private readonly IUserDashboardService _UserDashboardService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _Configration;
        private readonly RoleManager<Entities.Entity.Core.Role> _roleManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly EskaCMS.Infrastructure.Enums.EProvider _provider;


        public SitesService(
            IRepository<SitesCultures> SitesCulturesRepository,
            IRepository<Entities.Entity.Core.Site> SitesRepository,
            IRepository<SitesHostNames> SitesHostNameRepository,
            IRepository<UsersSites> UserSitesRepository,
            ITheme ThemeService,
            IWorkContext workContext,
            ISlugServices slugServices,
            IRepositoryWithTypedId<CurrentLoggedInUsers, string> CurrentLoggedInRepository,
            IRepository<Page> pageRepository,
            IRepository<Posts> PostRepository,
            EskaDCMSDbContext context, IdcmsLogService dcmsLogService,

            //  EskaDCMSDbContext context,
            IConfiguration configuration,
            IMapper mapper,
            IRepository<SitesSecurityPolicy> SitesSecurityPolicyRespository,
            ISiteCultureService siteCultureService,
            IRepositoryWithTypedId<SiteSettings,
                string> siteSettingsRepository,
            IRepository<ProductDetails> productDetailsRepository,
            IUserDashboardService UserDashboardService,
            ICurrencyService currencyService,
            ICacheService cacheService,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration Configration, RoleManager<Entities.Entity.Core.Role> roleManager)
        {
            _SitesRepository = SitesRepository;
            _SitesHostNameRepository = SitesHostNameRepository;
            _UserSitesRepository = UserSitesRepository;
            _ThemeService = ThemeService;
            _SitesSecurityPolicyRespository = SitesSecurityPolicyRespository;
            _workContext = workContext;
            _slugServices = slugServices;
            _currentLoggedInRepository = CurrentLoggedInRepository;
            _pageRepository = pageRepository;
            _PostRepository = PostRepository;
            _context = context;
            this.dcmsLogService=dcmsLogService;
            _SitesCulturesRepository = SitesCulturesRepository;
            _configuration = configuration;
            _mapper = mapper;
            _siteSettingsRepository = siteSettingsRepository;
            this.productDetailsRepository = productDetailsRepository;
            _siteCultureService = siteCultureService;
            _currencyService = currencyService;
            _UserDashboardService = UserDashboardService;
            _cacheService = cacheService;
            _Configration = Configration;
            _roleManager = roleManager;
            _serviceScopeFactory = serviceScopeFactory;
            _provider = DbProviderHelper.ParseProvider(
                configuration.GetSection("ConnectionStrings:DatabaseProvider").Value);
        }

        public async Task<long> UpsertSiteInfo(SitesAddVM Site)
        {
            long resultSiteId = 0;
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    long UserId = await _workContext.GetCurrentUserId();

                    bool IsInAddMode = (Site.SiteId == null || Site.SiteId == 0);

                    Entities.Entity.Core.Site ObjSite = new Entities.Entity.Core.Site();

                    if (!IsInAddMode)
                    {
                        ObjSite = await _SitesRepository.Query().Where(s => s.Id == Site.SiteId).FirstOrDefaultAsync();
                        if (ObjSite == null)
                        {
                            throw new Exception("Site Is not Found");
                        }

                        ObjSite.ModifiedById = UserId;
                        ObjSite.ModificationDate = DateTimeOffset.Now;

                    }

                    // var ObjUserSite = new UsersSites();


                    ObjSite.Name = Site.Name;
                    ObjSite.Description = Site.Description;
                    ObjSite.Status = EStatus.Published;
                    //ObjSite.Css = Site.Css;
                    //  ObjSite.SiteContentSettings = Site.Settings;
                    ObjSite.StatusDate = DateTimeOffset.Now;
                    ObjSite.ThumbnailId = Site.ThumbnailId;
                    ObjSite.IsADAuthRequired = Site.IsADAuthRequired;
                    ObjSite.OrganizationId = Site.OrganizationId;
                    ObjSite.CoreBranchId = Site.CoreBranchId;
                    ObjSite.CountryId = Site.CountryId;
                    ObjSite.EnableTwoFactorAuthentication = Site.EnableTwoFactorAuthentication.HasValue ? Site.EnableTwoFactorAuthentication.Value : false;

                    if (IsInAddMode)
                    {
                        ObjSite.CreatedById = UserId;
                        ObjSite.CreationDate = DateTimeOffset.Now;

                        _SitesRepository.Add(ObjSite);

                    }
                    await _SitesRepository.SaveChangesAsync();

                    //ObjUserSite.SiteId = ObjSite.Id;
                    //ObjUserSite.UserId = UserId;
                    if (IsInAddMode)
                    {
                        await CreateUserSiteAsync(ObjSite.Id, UserId, Site.IsDefault);
                        await AddDefultSiteSettingsAsync(ObjSite.Id);
                        await AddDefaultPasswordPolicyAsync(ObjSite.Id, UserId);
                    }

                    await transaction.CommitAsync();
                    resultSiteId = ObjSite.Id;

                }
                catch (Exception Ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return resultSiteId;
        }

        public async Task<long> CompleteAddSite(SitesAddVM Site)
        {
            Entities.Entity.Core.Site ObjSite = await _SitesRepository.Query().Where(x => x.Id == Site.SiteId).FirstOrDefaultAsync();
            ObjSite.Css = Site.Css;
            ObjSite.DefaultParentPageId = Site.DefaultParentPageId;
            await _SitesRepository.SaveChangesAsync();

            return ObjSite.Id;

        }
        public async Task<long> AddSiteHostName(SitesHostNamesViewModel HostName)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                bool isDuplicateHostName = await _SitesHostNameRepository.Query().AsNoTracking().AnyAsync(x => x.HostName == HostName.HostName);
                if (isDuplicateHostName)
                {
                    throw new Exception("This host name already exist");
                }
                SitesHostNames sitesHost = new SitesHostNames
                {
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    CultureId = HostName.CultureId,
                    IsActive = HostName.IsActive,
                    SiteId = HostName.SiteId,
                    HostName = HostName.HostName,
                    AddtionalMetaTags = HostName.AddtionalMetaTags
                };
                _SitesHostNameRepository.Add(sitesHost);
                await _SitesHostNameRepository.SaveChangesAsync();

                return sitesHost.Id;

            }
            catch (Exception exc)
            {
                throw;
            }

        }
        public async Task<bool> AddSiteCultures(List<SitesCulturesViewModel> Cultures)
        {
            long UserId = await _workContext.GetCurrentUserId();
            List<SitesCultures> mappedCultures = new List<SitesCultures>();
            foreach (var culture in Cultures)
            {
                if (await RestoreSiteCultureIfExist(culture.SiteId, UserId, culture.CultureCodeId))
                {
                    continue;
                }
                else
                {

                    SitesCultures sitesCulture = new SitesCultures
                    {
                        CreationDate = DateTimeOffset.Now,
                        CultureId = culture.CultureCodeId,
                        SiteId = culture.SiteId,
                        Status = EStatus.Published,
                        CreatedById = UserId
                    };
                    _SitesCulturesRepository.Add(sitesCulture);
                }

            }
            await _SitesCulturesRepository.SaveChangesAsync();

            return true;
        }
        public async Task AddDefaultPasswordPolicyAsync(long SiteId, long UserId)
        {
            try
            {
                List<SitesSecurityPolicy> sitesSecurityPolicy = new List<SitesSecurityPolicy>
                {
                    new SitesSecurityPolicy{RegularExpression=null,StatusDate=DateTime.Now,NoRepeatedPassword=100,MinUpperCaseLetters=1,MinSpecialCharacters=0,MinLength=6,MinLowerCaseLetters=1,Status=EStatus.Published,SiteId=SiteId, MinDigits =1, CreationDate = DateTimeOffset.Now, CreatedById = UserId},
                };
                _SitesSecurityPolicyRespository.AddRange(sitesSecurityPolicy);
                await _SitesSecurityPolicyRespository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<DefaultSlugDto> GetDefaultSlug()
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var cultureId = _workContext.GetCurrentCultureId();

                var pageId = await _SitesRepository.Query()
                    .Where(x => x.Id == siteId)
                    .Select(x => x.DefaultParentPageId)
                    .FirstOrDefaultAsync();

                var siteHosts = await _SitesHostNameRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.SiteId == siteId && x.CultureId == cultureId)
                    .ToListAsync();

                var slug = await _pageRepository.Query()
                    .Where(x =>
                        x.SiteId == siteId &&
                        (pageId == 0 || x.ParentId == pageId) &&
                        x.CultureId == cultureId)
                    .Select(x => x.SlugInfo.Slug)
                    .FirstOrDefaultAsync();

                var prefix = siteHosts
                    .Where(x => !string.IsNullOrWhiteSpace(x.Prefix))
                    .Select(x => x.Prefix)
                    .FirstOrDefault();

                return new DefaultSlugDto
                {
                    PageId = pageId,
                    Slug = slug,
                    Prefix = prefix 
                };
            }
            catch
            {
                throw;
            }
        }


        public async Task<bool> SetDefaultPage(long SiteId, long UserId, long ParentPageId)
        {
            var site = _SitesRepository.Query().Where(s => s.Id == SiteId).FirstOrDefault();
            if (site == null)
            {
                return false;
            }
            site.DefaultParentPageId = ParentPageId;
            site.ModifiedById = UserId;

            await _SitesRepository.SaveChangesAsync();

            return true;
        }
        public async Task<bool> EditSiteHostName(SitesHostNamesViewModel HostName)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                bool isDuplicateHostName = await _SitesHostNameRepository.Query().AsNoTracking().AnyAsync(x => x.HostName == HostName.HostName && x.SiteId != HostName.SiteId);
                if (isDuplicateHostName)
                {
                    throw new Exception("This host name is already exist");
                }
                long id = HostName.Id.Value;
                SitesHostNames sitesHost = await _SitesHostNameRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
                sitesHost.HostName = HostName.HostName;
                sitesHost.IsActive = HostName.IsActive;
                sitesHost.CultureId = HostName.CultureId;
                sitesHost.ModificationDate = DateTime.Now;
                sitesHost.ModifiedById = UserId;
                sitesHost.AddtionalMetaTags = HostName.AddtionalMetaTags;

                _SitesHostNameRepository.Update(sitesHost);
                await _SitesHostNameRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw;
            }
        }
        public async Task<bool> AddSiteSettings(SiteSettingsVM settings)
        {
            try
            {
                SiteSettings SiteSettings = new SiteSettings(settings.Name);
                SiteSettings.Value = settings.Value;
                SiteSettings.SiteId = (long)settings.SiteId;
                SiteSettings.IsVisibleInCommonSettingPage = true;
                SiteSettings.Module = settings.Module;
                _siteSettingsRepository.Add(SiteSettings);
                await _siteSettingsRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<bool> EditSiteSettings(SiteSettingsVM settings)
        {
            try
            {
                SiteSettings SiteSettings = _siteSettingsRepository.Query().FirstOrDefault(x => x.Id == settings.Name && x.SiteId == settings.SiteId);
                SiteSettings.Value = settings.Value;
                SiteSettings.SiteId = (long)settings.SiteId;
                SiteSettings.IsVisibleInCommonSettingPage = true;
                SiteSettings.Module = settings.Module;
                _siteSettingsRepository.Update(SiteSettings);
                await _siteSettingsRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<bool> DeleteSiteSettings(string name)
        {
            try
            {
                SiteSettings SiteSettings = _siteSettingsRepository.Query().FirstOrDefault(x => x.Id == name);
                _siteSettingsRepository.Remove(SiteSettings);
                await _siteSettingsRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<SiteGridOutputVM<SitesViewModel>> GetList(SiteSearchTableParam<SiteListSearchVM> param)
        {
            SiteListSearchVM search = param.Search;
            long userId = await _workContext.GetCurrentUserId();

            string searchName = search?.Name?.Trim();

            var query = _UserSitesRepository.Query()
                .AsNoTracking()
                .Where(s =>
                    s.UserId == userId &&
                    (!search.Status.HasValue || s.Site.Status == search.Status) &&
                    s.Site.Status != EStatus.Deleted &&
                    (!search.CreationDateFrom.HasValue || s.Site.CreationDate >= search.CreationDateFrom) &&
                    (!search.CreationDateTo.HasValue || s.Site.CreationDate <= search.CreationDateTo));

            /*
                Important:
                Do NOT search on s.Site.Description here because Description is NCLOB in Oracle.
                Do NOT select full Site with Distinct because Site contains NCLOB columns.
            */
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                string upperSearchName = searchName.ToUpper();

                query = query.Where(s =>
                    s.Site.Name.ToUpper().Contains(upperSearchName) ||
                    s.Site.CreatedBy.FullName.ToUpper().Contains(upperSearchName)
                );
            }

            var siteRows = await query
                .Select(s => new
                {
                    SiteId = s.Site.Id,
                    Name = s.Site.Name,
                    Status = s.Site.Status,
                    CreatedBy = s.Site.CreatedBy.FullName,
                    CreationDate = s.Site.CreationDate
                })
                .Distinct()
                .OrderByDescending(x => x.CreationDate)
                .ToListAsync();

            long defaultSiteId = await _UserSitesRepository
                .Query()
                .AsNoTracking()
                .Where(u => u.IsDefault && u.UserId == userId)
                .Select(u => u.SiteId)
                .FirstOrDefaultAsync();

            var siteIds = siteRows.Select(x => x.SiteId).ToList();

            /*
                Load Description separately.
                Selecting NCLOB alone is okay.
                The problem is filtering/sorting/distinct/grouping on NCLOB.
            */
            var descriptions = await _UserSitesRepository.Query()
                .AsNoTracking()
                .Where(s => s.UserId == userId && siteIds.Contains(s.SiteId))
                .Select(s => new
                {
                    SiteId = s.SiteId,
                    Description = s.Site.Description
                })
                .ToListAsync();

            var descriptionLookup = descriptions
                .GroupBy(x => x.SiteId)
                .ToDictionary(x => x.Key, x => x.FirstOrDefault()?.Description);

            var result = siteRows
                .Select(p => new SitesViewModel
                {
                    SiteId = p.SiteId,
                    Name = p.Name,
                    Description = descriptionLookup.ContainsKey(p.SiteId)
                        ? descriptionLookup[p.SiteId]
                        : null,
                    Status = p.Status,
                    CreatedBy = p.CreatedBy,
                    CreationDate = p.CreationDate,
                    IsDefault = p.SiteId == defaultSiteId
                })
                .ToList();

            /*
                Optional:
                If you MUST search inside Description also,
                do it here in memory after loading Description.
            */
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                string upperSearchName = searchName.ToUpper();

                result = result
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.Name) &&
                            x.Name.ToUpper().Contains(upperSearchName)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) &&
                            x.Description.ToUpper().Contains(upperSearchName)) ||
                        (!string.IsNullOrWhiteSpace(x.CreatedBy) &&
                            x.CreatedBy.ToUpper().Contains(upperSearchName))
                    )
                    .ToList();
            }

            var totalRecord = result.Count;

            var items = result
                .Skip(param.Pagination.Number * (param.Pagination.Start - 1))
                .Take(param.Pagination.Number)
                .ToList();

            return new SiteGridOutputVM<SitesViewModel>
            {
                Items = items,
                TotalRecord = totalRecord
            };
        }

        public async Task<List<SitesViewModel>> GetAllSites(long UserId)
        {
            try
            {
                return _UserSitesRepository.Query()
                     .Where(x => x.UserId == UserId && x.Site.Status != EStatus.Deleted)
                     .Include(x => x.Site).ThenInclude(x => x.SitesCultures).Include(x => x.Site).Include(x => x.Site.Thumbnail)
					 .Select(x => new SitesViewModel
                     {
                         Name = x.Site.Name,
                         Description = x.Site.Description,
                         ThumbnailId = x.Site.ThumbnailId,
                         Css = x.Site.Css,
                         CreatedBy = x.Site.CreatedBy.FullName,
                         CreationDate = x.Site.CreationDate,
                         Status = x.Site.Status,
                         StatusDate = x.Site.StatusDate,
                         SiteId = x.Site.Id,
                         IsDefault = x.IsDefault,
                         SiteImage = x.Site.Thumbnail.PublicUrl,
                         SiteCultures = x.Site.SitesCultures.Select(x =>new siteCultureObj { Id=x.Id , name = x.Culture.Name,code = x.CultureId}).ToList()
                     }).ToList();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<ExpandoObject> GetAssignedUnassignedSites(long UserId)
        {
            try
            {
                dynamic newObj = new ExpandoObject();
                newObj.AssignSites = await GetAllSites(UserId);


                var userSites = await _UserSitesRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == UserId)
                    .Select(x => x.SiteId)
                    .ToListAsync();
                var UnassignSites = await _SitesRepository.Query()
                    .AsNoTracking()
                    .Where(u => u.Status != EStatus.Deleted && !userSites.Contains(u.Id))
                    .Select(s => new SitesViewModel
                    {
                        Name = s.Name,
                        Description = s.Description,
                        ThumbnailId = s.ThumbnailId,
                        Css = s.Css,
                        CreationDate = s.CreationDate,
                        Status = s.Status,
                        StatusDate = s.StatusDate,
                        //    CompanyId = s.CompanyId,
                        SiteId = s.Id,
                        //IsDefault = s.user,
                    })
                   .ToListAsync();
                newObj.UnassignSites = UnassignSites;
                return newObj;


            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<SiteBaseDataViewModel> GetSiteId(string HostName)
        {
            SitesHostNames last = await _SitesHostNameRepository.Query().AsNoTracking().Where(x => x.HostName.Contains(HostName)).Include(x => x.Site).FirstOrDefaultAsync();
            //SitesHostNames last = SitesHostnames.Last();
            return new SiteBaseDataViewModel
            {
                CoreBranchId = last.Site.CoreBranchId,
                IsADAuthRequired = last.Site.IsADAuthRequired,
                Id = last.SiteId,
                EnableTwoFactorAuthentication = last.Site.EnableTwoFactorAuthentication,
            };
        }

        public async Task<SitesDetailsViewModel> GetSiteIdByHostName(string HostName)
        {
            try
            {

                string Host = HostName;
                var data = new SitesDetailsViewModel();
                var SitesHostnames = await _SitesHostNameRepository.Query()
                     .Where(x => x.HostName.ToLower().StartsWith(HostName.Substring(0, 1).ToLower()))
                     .Include(x => x.Site)
                     .Select(x => new { x.HostName, x.CultureId, x.Site.Id, HostNameId = x.Id, x.Site.Name, x.Site, x.Site.SiteSettings }).ToListAsync();
                // var SitesHostnames = await _SitesHostNameRepository.Query().Where(x=>x.HostName.StartsWith().Include(x => x.Site).ToListAsync();

                bool res = true;
                string resURL = "";
                long resId = 0;
                bool result = false;
                var last = SitesHostnames.Last();
                while (res && !string.IsNullOrEmpty(Host))
                {
                    for (int i = 0; i < SitesHostnames.Count; i++)
                    {
                        result = SitesHostnames[i].HostName.Equals(Host.Replace(" ", "/"));
                        if (SitesHostnames[i].Equals(last))
                        {
                            if (result)
                            {
                                resURL = Host;
                                resId = SitesHostnames[i].HostNameId;
                                res = false;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (result)
                            {
                                resURL = Host;
                                resId = SitesHostnames[i].HostNameId;
                                res = false;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    if (!result)
                    {
                        var uri = Host;
                        var newSegments = uri.Split("/").ToArray();
                        newSegments = newSegments.SkipLast(1).ToArray();
                        Host = string.Join("/", newSegments);
                    }

                }

                if (!string.IsNullOrEmpty(resURL))
                {
                    var hostObj = SitesHostnames.Where(x => x.HostNameId == resId).LastOrDefault();
                    data.Culture = hostObj.CultureId;
                    data.hostname = hostObj.HostName;

                    data.Site = new SiteVM
                    {
                        Id = hostObj.Id,
                        Name = hostObj.Name,
                        ThumbnailId = hostObj.Site.ThumbnailId,
                        DefaultParentPageId = hostObj.Site.DefaultParentPageId,
                        LoginType = (LoginType)Enum.Parse(typeof(LoginType), hostObj.SiteSettings.Where(x => x.Id == "LoginType")?.FirstOrDefault().Value),
                        IsADAuthRequired = hostObj.Site.IsADAuthRequired,
                        CoreBranchId = hostObj.Site.CoreBranchId,
                        OrganizationId = hostObj.Site.OrganizationId
                    };
                    var UserCultureId = await _workContext.GetCurrentUserCultureId(data.Site.Id);
                    data.Culture = UserCultureId == 0 ? data.Culture : UserCultureId;


                    data.DefaultTheme = (await _ThemeService.GetThemesAsync(hostObj.Id))
                        .Where(x => x.IsDefault == true)
                        .Select(x => new ThemeVM
                        {

                            Name = x.Name,
                            Colors = x.Colors,
                            Classes = x.Classes,
                            Fonts = x.Fonts,
                            //   Styles = x.Styles,
                            Scripts = x.Scripts,
                            ImportFiles = x.ImportedFiles.Select(i => new ImportedFilesVM()
                            {
                                SourceId = x.Id,
                                Id = i.Id,
                                FileId = i.MediaId,
                                FileUrl = i.Media.PublicUrl,
                                FileName = i.Media.MediaDetails.FirstOrDefault(c => c.CultureId == data.Culture)?.Title

                            }).ToList(),
                        }).FirstOrDefault();



                    string DefaultSlug = _pageRepository.Query()
                                         .Include(x => x.SlugInfo)
                                        .Where(p => p.ParentId == hostObj.Site.DefaultParentPageId && p.CultureId == data.Culture)
                                        .Select(p => p.SlugInfo.Slug)
                                        .FirstOrDefault();

                    data.Site.DefaultSlug = DefaultSlug;


                    // getting User Currency 
                    var UserCurrency = await _currencyService.GetCurrentUserCurrency(data.Site.Id);

                    data.CurrencyId = UserCurrency != null ? UserCurrency.Id : data.CurrencyId;


                    return data;
                }
                else
                    throw new Exception("Can not find site for this host (" + Host + ")");
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<SitesDetailsViewModel> GetSiteIdByHostNameSP(string HostName)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();

            try
            {

                var cacheData = _cacheService?.GetData<SitesDetailsViewModel>(HostName);

                if (cacheData != null)
                {
                    return cacheData;
                }
                var data = await spGetSiteIdByHostName(HostName);

                // Public, site-level guest role id so the SSR/SSG-cached storefront can
                // resolve guest visibility deterministically even for a brand-new anonymous
                // browser that has no guest token yet. Resolved via the SAME name-based lookup
                // guests are assigned with (GuestController.CreateDumyUser ->
                // AddToRoleAsync(ERoleTypes.Guest.ToString())), so it always equals the RoleIds
                // claim inside a guest token. No user/token/identity data — cache-safe.
                if (data?.Site != null)
                {
                    try
                    {
                        var guestRole = await _roleManager.FindByNameAsync(ERoleTypes.Guest.ToString());
                        data.Site.GuestRoleId = guestRole?.Id ?? 0;
                    }
                    catch
                    {
                        // Best-effort: a guest-role lookup failure must never break site
                        // resolution (SSR critical path). GuestRoleId stays 0 -> frontend
                        // safely falls back to public-only visibility.
                    }
                }

              



                _cacheService.SetData<SitesDetailsViewModel>(HostName, data, DateTimeOffset.Now.AddDays(double.Parse(_Configration.GetSection("CacheConfig:CachePeriod").Value)));
                return data;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<HostWithCultrueVM>> GetSiteHostNamesWithCulture(long SiteId)
        {
            var Hostnames = await _SitesHostNameRepository.Query()
                  .Where(x => x.SiteId == SiteId)
                  .Include(x => x.Site)
                  .Select(x => new HostWithCultrueVM()
                  {
                      HostName = GetSplittedHostName(x.HostName),
                      CultureCode = x.Culture.CultureId
                  }).ToListAsync();

            return Hostnames.GroupBy(h => new { h.HostName, h.CultureCode }).Select(h => h.FirstOrDefault()).ToList();

        }

        public async Task<IList<SiteUsedCulture>> UpsertHostNamesWithCultures(List<SiteHostNamesWithCulture> model, long UserId)
        {
            IList<SiteUsedCulture> result = null;
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long SiteId = 0;
                    foreach (var HostNameGroup in model)
                    {
                        SiteId = HostNameGroup.SiteId;
                        SitesCultures _SitesCulture = new SitesCultures()
                        {
                            Status = HostNameGroup.CultureStatus,
                            CultureId = HostNameGroup.CultureCode,
                            Id = HostNameGroup.CultureId
                        };

                        HostNameGroup.CultureId = await UpsertSiteCultures(_SitesCulture, SiteId, UserId);


                        foreach (var HostName in HostNameGroup.HostNames)
                        {

                            await UpsertHostNames(HostName, HostNameGroup.CultureId, SiteId, UserId);
                        }

                    }

                    await _SitesHostNameRepository.SaveChangesAsync();

                    await transaction.CommitAsync();
                    result = await GetUsedSiteCultures(SiteId);

                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    if (e.InnerException?.Message.Contains("IX_Core_SitesHostNames_HostName_Prefix") == true)
                    {
                        throw new Exception($"Duplicate hostname . Please ensure all hostnames are unique.");
                    }
                    throw;
                }
            });

            return result;
        }

        public async Task<HostNamesValidationResultVM> ValidateHostNameExistence(HostNamesValidationRequestVM model, long userId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
              
                var Result = new HostNamesValidationResultVM();
                var HostName = await _SitesHostNameRepository.Query()
                                                            .AsNoTracking()
                                                            .Where(x => x.SiteId == siteId).ToListAsync();
                if (model.HostName == null)
                {
                    return new HostNamesValidationResultVM
                    {
                        AlternativeHostName = "",
                        IsHostNameAvailable = false

                    };
                
                }
                var splitHostName = model.HostName.Split("/");

                if (splitHostName.Length == 1)
                {

                    Result.AlternativeHostName = "";
                    Result.IsHostNameAvailable = HostName.Exists(x => x.HostName.Contains(splitHostName.FirstOrDefault()));
                  
                    return Result;
                }
                else if(splitHostName.Length == 2)
                {
                    Result.AlternativeHostName = "";
                    Result.IsHostNameAvailable = HostName.Exists(x => x.HostName + "/" + x.Prefix == model.HostName);

                    if (!Result.IsHostNameAvailable)
                    {
                        Result.AlternativeHostName = HostName
                            .Where(x => x.HostName.Contains(splitHostName[0]))
                            .Select(x => x.HostName)
                            .FirstOrDefault();
                    }

                    return Result;  
                }

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

     


        public async Task<PageTemplateVM> GetTemplateSettingsBySlugSP(string Slug)
        {
            long SiteId = _workContext.GetCurrentSiteId();

            long cultureId = _workContext.GetCurrentCultureId();

            var userId = await _workContext.GetCurrentUserId();

            var roleId = await GetUserRoles(userId);

            if (string.IsNullOrEmpty(Slug) || Slug == "0")
            {
                Slug = string.Empty;
            }
            var cacheData = _cacheService?.GetData<PageTemplateVM>(Slug + cultureId + SiteId + roleId);

            if (cacheData != null)
            {
                return cacheData;
            }
            var Template = await spGetTemplateSettingsBySlug(Slug, cultureId, SiteId, (long)EStatus.Published, roleId);
            _cacheService.SetData<PageTemplateVM>(Slug + cultureId + SiteId + roleId, Template, DateTimeOffset.Now.AddMinutes(double.Parse(_Configration.GetSection("CacheConfig:CachePeriod").Value)));
            return Template;
        }

        public async Task<SitesViewModel> GetSiteDetails(long SiteId)
        {
            try
            {
                if (SiteId == 0)
                    throw new Exception("Site Id is missing");
                else
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    long cultureId = _workContext.GetCurrentCultureId();

                    var Site = await _SitesRepository.Query()

                        .Include(x => x.UserSites)
                        .Include(x => x.SiteSettings)
                        .Include(x => x.SitesHostNames)
                        .Include(x => x.Thumbnail)
                        .Include(x => x.SitesCultures)
                        .Include(x => x.Country)
                        .ThenInclude(x => x.CountryDetails)
                        .Where(y => y.Id == SiteId)
                        .Select(x =>
                             new SitesViewModel
                             {
                                 Name = x.Name,
                                 Description = x.Description,
                                 CreatedBy = x.CreatedBy.FullName,
                                 CreationDate = x.CreationDate,
                                 ModifiedById = x.ModifiedById,
                                 Status = x.Status,
                                 Css = x.Css,
                                 ThumbnailId = x.ThumbnailId,
                                 StatusDate = x.StatusDate,
                                 CountryName = x.Country == null ? "" : x.Country.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                 CountryId = x.CountryId,
                                 HasDefaultCountry = x.Country != null ? true : false,
                                 CoreBranchId = x.CoreBranchId,
                                 IsADAuthRequired = x.IsADAuthRequired,
                                 IsDefault = x.UserSites.Select(x => x.IsDefault).FirstOrDefault(),
                                 OrganizationId = x.OrganizationId,
                                 SiteId = x.Id,
                                 EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                                 ImageUrl = x.Thumbnail.PublicUrl,
                                 AppSettings = x.SiteSettings.Where(x => x.IsVisibleInCommonSettingPage).Select(y =>
                                     new SiteSettingsVM
                                     {
                                         Name = y.Id,
                                         Value = y.Value,
                                         Module = y.Module

                                     }
                                 ).ToList(),
                                 HostNames = x.SitesHostNames.Select(y =>
                                    new SitesHostNamesViewModel
                                    {
                                        Id = y.Id,
                                        IsActive = y.IsActive,
                                        HostName = y.HostName,
                                        CultureId = y.CultureId,
                                        CultureName = y.Culture.CultureId,
                                        Prefix = y.Prefix
                                    }
                                 ).ToList(),
                                 ModificationDate = x.ModificationDate,
                                 SiteCulturesList = x.SitesCultures.Select(s => new SitesCulturesGetAllVM
                                 {
                                     Id = s.Id,
                                     CreationDate = s.CreationDate,
                                     SiteId = s.SiteId,
                                     CultureCodeId = s.CultureId,
                                     CreatedById = s.CreatedById,
                                     Culture = s.CultureId
                                 }).ToList()
                             }


                       ).FirstOrDefaultAsync();

                    // Site.SiteCulturesList = await _siteCultureService.GetSiteCultures(SiteId);

                    return Site;
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> EditSite(SitesViewModel Site, long UserId)
        {
            try
            {

                var ObjSite = await _SitesRepository.Query().Where(x => x.Id == Site.SiteId).FirstOrDefaultAsync();
                ObjSite.Name = Site.Name;
                ObjSite.Description = Site.Description;
                ObjSite.ThumbnailId = Site.ThumbnailId;
                ObjSite.Css = Site.Css;





                ObjSite.IsADAuthRequired = Site.IsADAuthRequired;
                ObjSite.OrganizationId = Site.OrganizationId;

                if (Site.IsDefault)
                {
                    var UsersSites = await _UserSitesRepository.Query().Where(x => x.UserId == UserId).ToListAsync();

                    foreach (var item in UsersSites)
                    {
                        item.IsDefault = false;
                        _UserSitesRepository.Update(item);
                    }
                    // Flush all default-flag resets in one round-trip instead of one SaveChanges
                    // per row; final state is identical (every row set false here, then the
                    // matching site set true below and persisted on the shared context).
                    await _UserSitesRepository.SaveChangesAsync();

                    var UsersSitesDefault = await _UserSitesRepository.Query().Where(x => x.UserId == UserId && x.SiteId == Site.SiteId).FirstOrDefaultAsync();
                    if (UsersSitesDefault != null)
                        UsersSitesDefault.IsDefault = true;


                }
                else if (!Site.IsDefault)
                {
                    var UsersSitesDefault = await _UserSitesRepository.Query().Where(x => x.UserId == UserId && x.SiteId == Site.SiteId).FirstOrDefaultAsync();
                    if (UsersSitesDefault != null)
                        UsersSitesDefault.IsDefault = false;
                }
                ObjSite.ModifiedById = UserId;
                ObjSite.ModificationDate = DateTimeOffset.Now;
                ObjSite.Status = Site.Status;
                ObjSite.StatusDate = DateTimeOffset.Now;
                ObjSite.DefaultParentPageId = Site.DefaultParentPageId;
                await _SitesRepository.SaveChangesAsync();


                if (Site.AppSettings != null && Site.AppSettings.Count() > 0)
                {
                    var oldAppSettings = _siteSettingsRepository.Query().Where(x => x.SiteId == Site.SiteId && x.IsVisibleInCommonSettingPage).ToList();

                    foreach (var item in oldAppSettings)
                    {
                        _siteSettingsRepository.Remove(item);
                        await _siteSettingsRepository.SaveChangesAsync();
                    }


                    foreach (var item in Site.AppSettings)
                    {
                        var SiteSettings = new SiteSettings(item.Name);
                        SiteSettings.Value = item.Value;
                        SiteSettings.SiteId = Site.SiteId;
                        SiteSettings.IsVisibleInCommonSettingPage = true;
                        SiteSettings.Module = item.Module;
                        _siteSettingsRepository.Add(SiteSettings);
                        await _siteSettingsRepository.SaveChangesAsync();
                    }
                }



                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> DeleteSite(long SiteId)
        {
            try
            {
                var ObjSite = await _SitesRepository.Query().Where(x => x.Id == SiteId).FirstOrDefaultAsync();
                ObjSite.Status = EStatus.Deleted;
                ObjSite.StatusDate = DateTimeOffset.Now;
                await _SitesRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }
        public async Task<bool> DeleteSiteHost(long Id)
        {
            try
            {
                SitesHostNames ObjSite = await _SitesHostNameRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                _SitesHostNameRepository.Remove(ObjSite);
                await _SitesHostNameRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        public List<long> GetSiteCultures(long siteId)
        {
            return _SitesHostNameRepository.Query()
                .AsNoTracking()
                .Where(y => y.SiteId == siteId)
                .Select(y => y.CultureId)
                .Distinct()
                .ToList();
        }


        public List<string> GetSiteCultureCodes(long siteId)
        {
            return _SitesCulturesRepository.Query()
                .AsNoTracking()
                .Where(y => y.SiteId == siteId)
                .Select(y => y.CultureId)
                .Distinct()
                .ToList();
        }

        public async Task<bool> SetSiteAsDefault(long SiteId, long UserId)
        {
            var UsersSites = await _UserSitesRepository.Query().Where(x => x.UserId == UserId).ToListAsync();
            foreach (var item in UsersSites)
            {
                item.IsDefault = false;
                _UserSitesRepository.Update(item);
            }
            var UsersSitesDefault = await _UserSitesRepository.Query().Where(x => x.UserId == UserId && x.SiteId == SiteId).FirstOrDefaultAsync();
            UsersSitesDefault.IsDefault = true;
            await _UserSitesRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetSiteCSS(SitesViewModel Site)
        {
            try
            {
                var ObjSite = await _SitesRepository.Query().Where(x => x.Id == Site.SiteId).FirstOrDefaultAsync();

                ObjSite.ModifiedById = Site.ModifiedById;
                ObjSite.Css = Site.Css;
                string Folder = "wwwroot/Resources/styles/siteId_" + ObjSite.Id;
                var minifiedprimerystyle = FileHelper.MinifyCssFile(cssContent: Site.Css);
                FileHelper.UpsertFile(Content: minifiedprimerystyle, Name: "primaryStyle", Path: Folder, type: "css");
                await _SitesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<string> GetSiteCss(long SiteId)
        {
            try
            {
                // save it in file on the server 
                var ObjSite = await _SitesRepository.Query().Where(x => x.Id == SiteId).FirstOrDefaultAsync();

                return ObjSite.Css;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<string> GetThemeVariableStyle(long SiteId)
        {
            try
            {
                string folderPath = Path.Combine("wwwroot", "Resources", "styles", $"siteId_{SiteId}");
                string filePath = Path.Combine(folderPath, "themeStyle.css");

                if (!File.Exists(filePath))
                    return null;

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task setThemeVariableStyle(string Css, long SiteId)
        {
            try
            {
                string Folder = "wwwroot/Resources/styles/siteId_" + SiteId;
                var minifiedthemestyle = FileHelper.MinifyCssFile(cssContent: Css);
                FileHelper.UpsertFile(Content: minifiedthemestyle, Name: "themeStyle", Path: Folder, type: "css");
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<string> CheckSiteHosts(long SiteId, string host)
        {

            try
            {
                var HostNames = await _SitesHostNameRepository.Query().Where(x => x.SiteId != SiteId && x.Site.Status != EStatus.Deleted).ToListAsync();
                foreach (var item in HostNames)
                {
                    if (item.HostName.Equals(host))
                    {
                        return "This host already Used ( " + item.HostName + " ) , please change it";
                    }
                }

                return "ok";

            }
            catch (Exception)
            {
                return "ok";
            }



        }
        public async Task<IList<SiteUsedCulture>> GetUsedSiteCultures(long? SiteId)
        {
            try
            {

                return await _SitesCulturesRepository.Query().Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted).Select(x => new SiteUsedCulture
                {
                    Id = x.Id,
                    Name = x.CultureId,
                    Status = x.Status

                }).ToListAsync();


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<IList<StatusSiteCulturesVM>> GetUsedSiteCulturesByStatus(long? SiteId, long? Status)
        {
            try
            {
                // Create variable For Query
                var Query = _SitesCulturesRepository.Query();

                //Check If Type Value Null Or Empty If empty ,Query Will Search For Delete Status else Will Search For Publish Status
                if (!Status.HasValue)
                {
                    Query = Query
                        .Where(x => x.SiteId == SiteId && x.Status == EStatus.Deleted);
                }
                else
                {
                    Query = Query
                        .Where(x => x.SiteId == SiteId && (long)x.Status == Status.Value);

                }

                return await Query
                    .Select(x => new StatusSiteCulturesVM
                    {
                        Id = x.Id,
                        Name = x.CultureId,


                    }).ToListAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<string> GetMetaTages(string Slug)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                string EntityMetaTags = string.Empty;
                string TemplateMetaTags = string.Empty;
                var ClosestMetaTagesObject = new MetaTagsViewModel();

                // Slugs are matched lower-case (GetSlugDataBySlug compares against
                // x.Slug.ToLower()), and the cache read must use the same key as the
                // SetData at the end, so normalise once, up front.
                Slug = (Slug ?? string.Empty).ToLower();

                var SiteMetaTags = await _SitesHostNameRepository
                                                         .Query()
                                                         .AsNoTracking()
                                                         .Where(h => h.SiteId == siteId && CultureId == h.CultureId)
                                                         .FirstOrDefaultAsync();

                var cacheKey = "Meta" + SiteMetaTags?.HostName + Slug + CultureId;
                var cacheData = _cacheService?.GetData<string>(cacheKey);
                if (cacheData != null)
                {
                    return cacheData;
                }

                // Entity and template tags are best-effort: an unknown slug, a missing
                // entity or one without a template must not throw away the site tags.
                try
                {
                    var SlugInfo = await _slugServices.GetSlugDataBySlug(Slug);

                    if (SlugInfo?.Entity == EEntity.Page)
                    {
                        var Page = _pageRepository.Query()
                                        .Include(x => x.SlugInfo)
                                        .Where(p => p.SlugInfo.Slug.ToLower() == Slug && p.CultureId == CultureId)
                                        .Include(p => p.PageTemplate)
                                        .AsNoTracking()
                                        .FirstOrDefault();

                        EntityMetaTags = Page?.AddtionalMetaTags;
                        TemplateMetaTags = Page?.PageTemplate?.AddtionalMetaTags;
                    }
                    else if (SlugInfo?.Entity == EEntity.Post)
                    {
                        var Post = _PostRepository.Query()
                                      .Include(x => x.SlugInfo)
                                      .Where(p => p.SlugInfo.Slug.ToLower() == Slug && p.CultureId == CultureId)
                                      .Include(p => p.PostTemplate)
                                      .AsNoTracking()
                                      .FirstOrDefault();

                        EntityMetaTags = Post?.addtionalMetaTags;
                        TemplateMetaTags = Post?.PostTemplate?.AddtionalMetaTags;
                    }
                    else if (SlugInfo?.Entity == EEntity.Product)
                    {
                        var Product = productDetailsRepository.Query()
                                       .Include(x => x.SlugInfo)
                                       .Where(p => p.SlugInfo.Slug.ToLower() == Slug && p.CultureId == CultureId)
                                       .Include(p => p.ProductTemplate)
                                       .AsNoTracking()
                                       .FirstOrDefault();

                        EntityMetaTags = Product?.AddtionalMetaTags;
                        TemplateMetaTags = Product?.ProductTemplate?.AddtionalMetaTags;
                    }
                }
                catch (Exception)
                {
                    EntityMetaTags = string.Empty;
                    TemplateMetaTags = string.Empty;
                }

                ClosestMetaTagesObject.MetaTags = await getMetaTages(EntityMetaTags,
                                    TemplateMetaTags,
                                    SiteMetaTags?.AddtionalMetaTags);

                if (!string.IsNullOrEmpty(SiteMetaTags?.PrimaryMetaTags) && SiteMetaTags.PrimaryMetaTags != "[]")
                {
                    try
                    {
                        ClosestMetaTagesObject.PrimaryTags = JArray.Parse(SiteMetaTags.PrimaryMetaTags);
                    }
                    catch (Exception)
                    {
                        // Malformed primary tags must not drop the merged tags.
                    }
                }

                _cacheService?.SetData<string>(cacheKey, JsonConvert.SerializeObject(ClosestMetaTagesObject), DateTimeOffset.Now.AddDays(double.Parse(_Configration.GetSection("CacheConfig:CachePeriod").Value)));


                return JsonConvert.SerializeObject(ClosestMetaTagesObject);

            }
            catch (Exception E)
            {
                return null;
            }
        }



        public async Task<bool> AddMetaDataToHostName(long siteId, List<MetaDataViewModel> metaDataViewModel)
        {
            try
            {
                var sitehostname = await _SitesHostNameRepository.Query().Where(x => x.SiteId == siteId).ToListAsync();
                foreach (var meta in metaDataViewModel)
                {
                    foreach (var host in sitehostname)
                    {
                        if (host.CultureId == meta.CultureId)
                        {
                            host.AddtionalMetaTags = meta.AddtionalMetaTags;
                            host.PrimaryMetaTags = meta.PrimaryMetaTags;

                        }
                    }
                }
                await _SitesHostNameRepository.SaveChangesAsync();

                return true;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<MetaDatawithCultureViewModel>> GetMetaDataWithCulture(long siteId)
        {
            var hostnamewithmeta = await _SitesHostNameRepository.Query().Include(x => x.Culture).Include(x => x.Site).Where(x => x.SiteId == siteId).Select(x => new MetaDatawithCultureViewModel
            {
                CultureId = x.Culture.Id,
                CultureName = x.Culture.CultureId,
                AddtionalMetaTags = x.AddtionalMetaTags,
                PrimaryMetaTags = x.PrimaryMetaTags
            }).Distinct().ToListAsync();

            return hostnamewithmeta;
        }
        public async Task<bool> AddSiteScripts(long siteId, string payload)
        {
            var site = await _SitesRepository.Query().Where(x => x.Id == siteId).FirstOrDefaultAsync();
            site.Script = payload;
            await _SitesRepository.SaveChangesAsync();
            return true;

        }
        public async Task<SiteScriptVM> GetSiteScripts(long siteId)
        {
            SiteScriptVM siteScriptVM = new SiteScriptVM();

            siteScriptVM.script = await _SitesRepository.Query().Where(x => x.Id == siteId).Select(x => x.Script).FirstOrDefaultAsync();

            return siteScriptVM;
        }
        public async Task<List<SiteHostNamesWithCultureVM>> GetHostNameBySiteId(long siteId, string HostName)
        {
            List<SiteHostNamesWithCultureVM> HostNames = await _SitesHostNameRepository.Query()
                .Where(x => x.SiteId == siteId && x.IsActive)
                .FilterHostNames(HostName)
                .Select(x => new SiteHostNamesWithCultureVM
                {
                    CultureCode = x.Culture.Culture.Name,
                    CultureId = x.CultureId,
                    SiteId = x.SiteId,
                    HostNames = x.HostName,
                    Prefix = x.Prefix


                }).ToListAsync();



            foreach (var Hostname in HostNames)
            {
                if (!string.IsNullOrWhiteSpace(Hostname.Prefix))
                    Hostname.HostNames = Hostname.HostNames + $"/{Hostname.Prefix}";
            }

            return HostNames;

        }


        public async Task<string> GetActiveSiteHostName(long siteId)
        {



            const string ignoreHostName = "localhost";

            var hostName = _SitesHostNameRepository.Query()
                                                    .AsNoTracking()
                                                    .Where(x => x.SiteId == siteId && x.IsActive && !x.HostName.ToLower().Contains(ignoreHostName.ToLower()))
                                                    .FirstOrDefault()?
                                                    .HostName;

            if (string.IsNullOrEmpty(hostName))
            {
                throw new Exception("Please check host name configuration");
            }

            return hostName;

        }


        public async Task<bool> UpdateUserCulture(long cultureId)
        {

            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();



            var userSite = await _UserSitesRepository.Query()
                                                     .Where(x => x.SiteId == siteId && x.UserId == userId)
                                                     .FirstOrDefaultAsync();

            if (userSite is null)
            {
                return false;
            }
            userSite.CultureId = cultureId;

            _UserSitesRepository.Update(userSite);

            await _UserSitesRepository.SaveChangesAsync();


            return true;



        }







        #region Helper Functions

        public async Task<long> GetUserRoles(long userId)
        {
            var userRoleId = _context.UserRoles.Where(x => x.UserId == userId)
                                         .Include(x => x.Role)
                                         .OrderBy(x => x.Role.Type)
                                         .ThenBy(x => x.RoleId)
                                         .Select(x => x.RoleId)
                                         .FirstOrDefault();

            return userRoleId;
        }

        private int MatchedLevelForHosts(string OriginalHost, string NewHost)
        {
            var originalHostList = OriginalHost.Split('/');
            var NewHostList = NewHost.Split('/');
            int MatchedTimes = 0;
            for (int i = 0; i < Math.Min(originalHostList.Count(), NewHostList.Count()); i++)
            {
                if (originalHostList[i] == NewHostList[i])
                {
                    MatchedTimes++;
                }
                else
                {
                    break;
                }
            }
            return 0;
        }

        private async Task<long> UpsertSiteCultures(SitesCultures _SitesCulture, long SiteId, long UserId)
        {
            try
            {
                bool IsInEditMode = _SitesCulture.Id != null && _SitesCulture.Id != 0;

                SitesCultures SitesCulturesObj = new SitesCultures();

                if (IsInEditMode)
                {
                    SitesCulturesObj = await _SitesCulturesRepository.Query().Where(s => s.CultureId == _SitesCulture.CultureId && s.SiteId == SiteId).FirstOrDefaultAsync();

                    if (SitesCulturesObj == null)
                    {
                        throw new Exception(" Can't Find Culture Item");
                    }
                }

                if (IsInEditMode)
                {
                    SitesCulturesObj.ModificationDate = DateTime.Now;
                    SitesCulturesObj.ModifiedById = UserId;
                    SitesCulturesObj.Status = _SitesCulture.Status;
                }
                else
                {
                    SitesCulturesObj.CreationDate = DateTime.Now;
                    SitesCulturesObj.CreatedById = UserId;
                    SitesCulturesObj.Status = EStatus.Published;

                }

                SitesCulturesObj.CultureId = _SitesCulture.CultureId;
                SitesCulturesObj.CulturePrefix = _SitesCulture.CulturePrefix;
                SitesCulturesObj.CultureType = _SitesCulture.CultureType;
                SitesCulturesObj.SiteId = SiteId;

                if (!IsInEditMode)
                {
                    _SitesCulturesRepository.Add(SitesCulturesObj);
                }
                await _SitesCulturesRepository.SaveChangesAsync();

                return SitesCulturesObj.Id;

            }
            catch (Exception e)
            {
                throw;
            }
        }

        private async Task<bool> UpsertHostNames(SitesHostNamesViewModelWithStatus _HostName, long CultureId, long SiteId, long UserId)
        {

            try
            {
                bool IsInEditMode = _HostName.Id != null && _HostName.Id != 0;
                SitesHostNames HostNameObj = new SitesHostNames();
                if (IsInEditMode)
                {
                    HostNameObj = await _SitesHostNameRepository.Query().Where(h => h.Id == _HostName.Id && h.SiteId == SiteId).FirstOrDefaultAsync();

                    if (HostNameObj == null)
                    {
                        throw new Exception("Can't find host name");
                    }

                    if (_HostName.Status == EStatus.Deleted)
                    {
                        var LoggedInUserQuery = await _currentLoggedInRepository.Query().Where(x => x.HostnameId == _HostName.Id).ToListAsync();
                        foreach (var LastInUser in LoggedInUserQuery)
                        {
                            LastInUser.HostnameId = null;

                        }
                        await _currentLoggedInRepository.SaveChangesAsync();
                        _SitesHostNameRepository.Remove(HostNameObj);
                    }
                }

                HostNameObj.HostName = _HostName.HostName;
                HostNameObj.AddtionalMetaTags = _HostName.AddtionalMetaTags;
                HostNameObj.IsActive = _HostName.IsActive;
                HostNameObj.Prefix = _HostName.Prefix;

                if (IsInEditMode)
                {
                    HostNameObj.ModifiedById = UserId;
                    HostNameObj.ModificationDate = DateTime.Now;

                }
                else
                {
                    HostNameObj.CreatedById = UserId;
                    HostNameObj.CreationDate = DateTime.Now;

                }

                HostNameObj.CultureId = CultureId;
                HostNameObj.SiteId = SiteId;

                if (!IsInEditMode)
                {
                    _SitesHostNameRepository.Add(HostNameObj);
                }

                return true;

            }
            catch (Exception e)
            {
                throw;
            }
        }


        private static string GetSplittedHostName(string Host)
        {
            var SplittedHost = Host.Split('/');
            return SplittedHost.ElementAtOrDefault(1) == null ? "" : SplittedHost.ElementAtOrDefault(1);
        }


        private async Task CreateUserSiteAsync(long siteId, long userId, bool isDefult)
        {
            var ExistObj = await _UserSitesRepository.Query().Where(u => u.SiteId == siteId && u.UserId == userId).FirstOrDefaultAsync();
            var UserToSite = new UsersSites { UserId = userId, SiteId = siteId, IsDefault = isDefult };

            if (ExistObj != null)
            {
                ExistObj.IsDefault = UserToSite.IsDefault;
                ExistObj.SiteId = UserToSite.SiteId;
                ExistObj.UserId = UserToSite.UserId;

            }
            else
            {

                _UserSitesRepository.Add(UserToSite);
            }

            await _UserSitesRepository.SaveChangesAsync();

        }

        private async Task<bool> RestoreSiteCultureIfExist(long SiteId, long UserId, string CultureCode)
        {
            var Culture = _SitesCulturesRepository.Query().Include(s => s.Culture)
                                     .Where(s => s.CultureId == CultureCode && s.SiteId == SiteId)
                                     .FirstOrDefault();

            if (Culture != null)
            {
                Culture.Status = EStatus.Published;
                Culture.ModificationDate = DateTime.Now;
                Culture.ModifiedById = UserId;

                await _SitesCulturesRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        private string GetPageCompoents(string PageComponents, string TemplateComponents)
        {

            List<string> PageComp = new List<string>();
            List<string> TempComp = new List<string>();

            if (!string.IsNullOrEmpty(PageComponents))
            {
                PageComp = JsonConvert.DeserializeObject<List<string>>(PageComponents);
            }

            if (!string.IsNullOrEmpty(TemplateComponents))
            {
                TempComp = JsonConvert.DeserializeObject<List<string>>(TemplateComponents);
            }

            // filltering matches
            foreach (var Component in PageComp)
            {
                if (!TempComp.Contains(Component))
                {
                    TempComp.Add(Component);
                }
            }
            return JsonConvert.SerializeObject(TempComp);
        }


        private Task<List<List<MetaTagItem>>> getMetaTages(string? EntityMetaTags,
                                        string? TemplateMetaTags,
                                       string? SiteMetaTags)
        {
            return Task.FromResult(MergeData(ParseMetaTags(EntityMetaTags),
                                             ParseMetaTags(TemplateMetaTags),
                                             ParseMetaTags(SiteMetaTags)));
        }

        private static List<List<MetaTagItem>> ParseMetaTags(string MetaTags)
        {
            try
            {
                if (!string.IsNullOrEmpty(MetaTags) && MetaTags != "[]")
                {
                    return JsonConvert.DeserializeObject<List<List<MetaTagItem>>>(MetaTags) ?? new List<List<MetaTagItem>>();
                }
            }
            catch (Exception)
            {
                // A malformed list contributes no tags.
            }

            return new List<List<MetaTagItem>>();
        }
        // Attributes that identify a meta tag, in priority order. The website
        // matches tags by the same attributes (ModuleService.META_ID_ATTRS).
        private static readonly string[] MetaTagIdentityAttributes = { "name", "property", "http-equiv", "itemprop" };

        // Closest wins: entity tags override template tags, which override site
        // tags. Tags are matched by identity (e.g. name=description), not by their
        // attribute names: every tag has "name"/"content" keys, so matching on
        // those dropped all template and site tags once the entity had any tag,
        // and kept only the first template tag when it had none.
        private static List<List<MetaTagItem>> MergeData(List<List<MetaTagItem>> EntityMeta, List<List<MetaTagItem>> TemplateMeta, List<List<MetaTagItem>> SiteMeta)
        {
            var merged = new List<List<MetaTagItem>>();
            var seen = new HashSet<string>();

            foreach (var tag in EntityMeta.Concat(TemplateMeta).Concat(SiteMeta))
            {
                if (tag == null || tag.Count == 0)
                    continue;

                var identity = MetaTagIdentity(tag);

                // A tag with no identity attribute can't be overridden; keep it.
                if (identity == null || seen.Add(identity))
                    merged.Add(tag);
            }

            return merged;
        }

        private static string MetaTagIdentity(List<MetaTagItem> tag)
        {
            foreach (var attribute in MetaTagIdentityAttributes)
            {
                var item = tag.FirstOrDefault(t => string.Equals(t?.key?.Trim(), attribute, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(item?.value))
                    return attribute + "=" + item.value.Trim().ToLowerInvariant();
            }

            return null;
        }

        private static List<List<Dictionary<string, string>>> ConvertToOriginalStructure(Dictionary<string, string> combinedDictionary)
        {
            var result = new List<List<Dictionary<string, string>>>();
            var tempList = new List<Dictionary<string, string>>();

            foreach (var kvp in combinedDictionary)
            {
                tempList.Add(new Dictionary<string, string> { { "key", kvp.Key }, { "value", kvp.Value } });
            }

            result.Add(tempList);
            return result;
        }
        private async Task<JArray> CollectUniqueMetaTags(params JArray[] jsonArrays)
        {
            try
            {
                var combinedDict = new Dictionary<string, string>();

                foreach (var jsonArray in jsonArrays.Reverse())
                {
                    foreach (var obj in jsonArray)
                    {
                        foreach (var item in obj)
                        {
                            var key = item["key"].ToString();
                            var value = item["value"].ToString();

                            if (!combinedDict.ContainsKey(key))
                            {
                                combinedDict[key] = value;
                            }
                        }
                    }
                }

                var combinedJArray = new JArray();
                foreach (var kvp in combinedDict)
                {
                    var jsonObject = new JObject
                    {
                        ["key"] = kvp.Key,
                        ["value"] = kvp.Value
                    };
                    combinedJArray.Add(jsonObject);
                }

                return new JArray(combinedJArray);



            }
            catch (Exception ex)
            {

                return new JArray();
            }
        }

        private long ParseAnd0IfNull(string value)
        {
            return long.Parse(string.IsNullOrEmpty(value) ? "0" : value);
        }

        private static LoginType ParseLoginType(object? value)
        {
            var text = value?.ToString();
            return Enum.TryParse<LoginType>(text, out var loginType)
                ? loginType
                : (LoginType)0;
        }

        private static void AddInputParameter(DbCommand command, string name, object? value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = IsOracleCommand(command)
                ? name.TrimStart('@', ':')
                : DbProviderHelper.NormalizeParameterName(command, name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.Input;
            command.Parameters.Add(parameter);
        }

        private static void ConfigureProcedureCommand(DbCommand command, string procedureName, params string[] parameterNames)
        {
            if (IsOracleCommand(command))
            {
                command.CommandType = CommandType.Text;
                command.CommandText =
                    $"BEGIN {procedureName}({string.Join(", ", parameterNames.Select(name => $":{name}"))}); END;";
                return;
            }

            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
        }

        private static bool IsOracleCommand(DbCommand command) =>
            string.Equals(
                command.GetType().FullName,
                "Oracle.ManagedDataAccess.Client.OracleCommand",
                StringComparison.Ordinal);

        private static bool ParseDbBoolean(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (long.TryParse(text, out var numericValue))
            {
                return numericValue != 0;
            }

            return bool.TryParse(text, out var parsedBool) && parsedBool;
        }

        private async Task AddDefultSiteSettingsAsync(long siteId)
        {

            try
            {
                //set defult setting for site when we  add it 
                SiteSettings SiteSettingsCurrencyCulture = new SiteSettings("Global.CurrencyCulture");

                SiteSettingsCurrencyCulture.Value = "en-US";
                SiteSettingsCurrencyCulture.SiteId = siteId;
                SiteSettingsCurrencyCulture.IsVisibleInCommonSettingPage = false;
                SiteSettingsCurrencyCulture.Module = "Core";

                SiteSettings SiteSettingsCanUseOldPasswords = new SiteSettings("CanUseOldPasswords");

                SiteSettingsCanUseOldPasswords.Value = "1";
                SiteSettingsCanUseOldPasswords.SiteId = siteId;
                SiteSettingsCanUseOldPasswords.IsVisibleInCommonSettingPage = false;
                SiteSettingsCanUseOldPasswords.Module = "Security";



                SiteSettings SiteSettingsCurrencyDecimal = new SiteSettings("Global.CurrencyDecimalPlace");

                SiteSettingsCurrencyDecimal.Value = "2";
                SiteSettingsCurrencyDecimal.SiteId = siteId;
                SiteSettingsCurrencyDecimal.IsVisibleInCommonSettingPage = false;
                SiteSettingsCurrencyDecimal.Module = "Core";


                SiteSettings SiteSettingsLoginType = new SiteSettings("LoginType");

                SiteSettingsLoginType.Value = "DirectLogin";
                SiteSettingsLoginType.SiteId = siteId;
                SiteSettingsLoginType.IsVisibleInCommonSettingPage = false;
                SiteSettingsLoginType.Module = "CORE";

                _siteSettingsRepository.Add(SiteSettingsCurrencyCulture);
                _siteSettingsRepository.Add(SiteSettingsCurrencyDecimal);
                _siteSettingsRepository.Add(SiteSettingsLoginType);
                _siteSettingsRepository.Add(SiteSettingsCanUseOldPasswords);
                await _siteSettingsRepository.SaveChangesAsync();


            }
            catch (Exception exc)
            {
                throw;
            }


        }


        #region GetSiteIdByHostNameSP functions

        private static string NormalizeHost(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            input = input.Trim();

            // لو إجتك قيمة فيها path/query مثل: alnejmah.com/ar/shop?x=1
            // نخليها URL قابلة للـ parsing
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                input = "http://" + input;
            }

            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return uri.Host.ToLowerInvariant();

            // fallback: خذ قبل / أو ?
            var cut = input.Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("https://", "", StringComparison.OrdinalIgnoreCase);
            cut = cut.Split('/', '?')[0];
            return cut.ToLowerInvariant();
        }

        private async Task<SitesDetailsViewModel?> spGetSiteIdByHostName(string hostName)
        {
            //hostName = NormalizeHost(hostName);
            //if (string.IsNullOrWhiteSpace(hostName))
            //    return null;

            // ✅ Independent connection - never conflicts with EF
            await using var connection = dbFactory.CreateConnection(
                _configuration.GetConnectionString("DefaultConnection"), _provider);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            DbProviderHelper.ConfigureCommand(command);
            command.CommandTimeout = 30;

            AddInputParameter(command, "@P_HOSTNAME", hostName, DbType.String);
            ConfigureProcedureCommand(command, "GET_SITE_ID_BY_HOST_NAME", "P_HOSTNAME");

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.CloseConnection);

            SitesDetailsViewModel? data = null;
            bool first = true;

            while (await reader.ReadAsync())
            {
                if (first)
                {
                    var hostCulture = ParseAnd0IfNull(reader["HostCultureId"]?.ToString());
                    var siteId = ParseAnd0IfNull(reader["SiteId"]?.ToString());
                    var guid = Guid.NewGuid().ToString("N");

                    data = new SitesDetailsViewModel
                    {
                        Culture = hostCulture,
                        CultureName = reader["CultureName"]?.ToString(),
                        Prefix = reader["Prefix"]?.ToString(),
                        hostname = reader["HostName"]?.ToString(),
                        Site = new SiteVM
                        {
                            Id = siteId,
                            Name = reader["SiteName"]?.ToString(),
                            ThumbnailId = ParseAnd0IfNull(
                                reader["SiteThumbnailId"]?.ToString()),
                            LoginType = ParseLoginType(reader["LoginType"]),
                            DefaultSlug = reader["DefaultSlug"]?.ToString(),
                            IsADAuthRequired = ParseDbBoolean(
                                reader["IsADAuthRequired"]),
                            DefaultParentPageId = ParseAnd0IfNull(
                                reader["SiteDefaultParentPageId"]?.ToString()),
                            CoreBranchId = ParseAnd0IfNull(
                                reader["CoreBranchId"]?.ToString()),
                            OrganizationId = ParseAnd0IfNull(
                                reader["OrganizationId"]?.ToString()),
                            PrimaryScripts = reader["SiteScripts"]?.ToString(),
                            PrimaryStyleUrl = $"Resources/styles/siteId_{siteId}/primaryStyle.css?{guid}={guid}",
                            ThemeStyleUrl = $"Resources/styles/siteId_{siteId}/themeStyle.css?{guid}={guid}",
                            CultureStyleUrl = $"Resources/styles/siteId_{siteId}_cultureId_{hostCulture}/cultureStyle.css?{guid}={guid}",
                            LogoURL = reader["LogoUrl"]?.ToString() ?? ""
                        }
                    };

                    var defaultThemeName = reader["ThemeName"]?.ToString();
                    if (!string.IsNullOrEmpty(defaultThemeName))
                    {
                        data.DefaultTheme = new ThemeVM
                        {
                            Name = defaultThemeName,
                            Colors = reader["ThemeColors"]?.ToString(),
                            Classes = reader["ThemeClasses"]?.ToString(),
                            Fonts = reader["ThemeFonts"]?.ToString(),
                            Scripts = reader["ThemeScripts"]?.ToString(),
                            StyleUrl = $"Resources/styles/themes/siteId_{siteId}_themeId_{reader["ThemeId"]}/themeStyle.css?{guid}={guid}",
                        };
                    }

                    first = false;
                }
            }

            return data;
        }
        private async Task<ThemeVM> FillImportedFiles(ThemeVM Theme, DbDataReader row)
        {
            if (Theme != null)
            {
                var Id = ParseAnd0IfNull(row["ImportedFileId"].ToString());
                if (Id != 0 && Theme.ImportFiles.FirstOrDefault(i => i.Id == Id) == null)
                {
                    Theme.ImportFiles.Add(new ImportedFilesVM()
                    {
                        Id = Id,
                        FileId = ParseAnd0IfNull(row["FileId"].ToString()),
                        FileUrl = row["FileUrl"].ToString(),
                        contentType = row["ContentType"].ToString(),
                    });
                }
            }
            return Theme;
        }

        #endregion

        #region GetSlugSettingsSP functions

        private async Task<SlugInfoVM> spGetSlugSettings(string Slug, long CultureId, long SiteId, long Status)
        {
            return await spGetSlugSettingsp1(Slug, CultureId, SiteId, Status);
        }



        private async Task<SlugInfoVM> spGetSlugSettingsp1(string Slug, long CultureId, long SiteId, long Status)
        {
            SlugInfoVM SlugResult = new SlugInfoVM();

            long UserId = await _workContext.GetCurrentUserId();
            long roleId = await GetUserRoles(UserId);
            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            using (var command = connection.CreateCommand())
            {
                DbProviderHelper.ConfigureCommand(command);

                var paramSlug = command.CreateParameter();
                paramSlug.Value = Slug;
                paramSlug.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_SLUG");
                paramSlug.DbType = DbType.String;
                paramSlug.Direction = ParameterDirection.Input;




                var paramCultureId = command.CreateParameter();
                paramCultureId.Value = CultureId;
                paramCultureId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_CULTUREID");
                paramCultureId.DbType = DbType.Int64;
                paramCultureId.Direction = ParameterDirection.Input;


                var paramRoleId = command.CreateParameter();
                paramRoleId.Value = roleId;
                paramRoleId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_ROLEID");
                paramRoleId.DbType = DbType.Int64;
                paramRoleId.Direction = ParameterDirection.Input;


                var paramPostSlugType = command.CreateParameter();
                paramPostSlugType.Value = (long)SlugsTypes.Blog;
                paramPostSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_POSTSLUGTYPE");
                paramPostSlugType.DbType = DbType.Int64;
                paramPostSlugType.Direction = ParameterDirection.Input;


                var paramPageSlugType = command.CreateParameter();
                paramPageSlugType.Value = (long)SlugsTypes.Page;
                paramPageSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_PAGESLUGTYPE");
                paramPageSlugType.DbType = DbType.Int64;
                paramPageSlugType.Direction = ParameterDirection.Input;


                var paramProductSlugType = command.CreateParameter();
                paramProductSlugType.Value = (long)SlugsTypes.Product;
                paramProductSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_PRODUCTSLUGTYPE");
                paramProductSlugType.DbType = DbType.Int64;
                paramProductSlugType.Direction = ParameterDirection.Input;


                var paramBrandSlugType = command.CreateParameter();
                paramBrandSlugType.Value = (long)SlugsTypes.Brand;
                paramBrandSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_BRANDSLUGTYPE");
                paramBrandSlugType.DbType = DbType.Int64;
                paramBrandSlugType.Direction = ParameterDirection.Input;



                var paramCategorySlugType = command.CreateParameter();
                paramCategorySlugType.Value = (long)SlugsTypes.Category;
                paramCategorySlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_CATEGORYSLUGTYPE");
                paramCategorySlugType.DbType = DbType.Int64;
                paramCategorySlugType.Direction = ParameterDirection.Input;





                var paramSiteId = command.CreateParameter();
                paramSiteId.Value = SiteId;
                paramSiteId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_SITEID");
                paramSiteId.DbType = DbType.Int64;
                paramSiteId.Direction = ParameterDirection.Input;

                var paramStatus = command.CreateParameter();
                paramStatus.Value = Status;
                paramStatus.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_STATUS");
                paramStatus.DbType = DbType.Int64;
                paramStatus.Direction = ParameterDirection.Input;


                //var paramErrorCode = command.CreateParameter();
                //paramErrorCode.Value = null;
                //paramErrorCode.ParameterName = "@Error_Code";
                //paramErrorCode.DbType = DbType.String;
                //paramErrorCode.Direction = ParameterDirection.Output;
                //paramErrorCode.Size = 1000;

                //var paramErrorMessage = command.CreateParameter();
                //paramErrorMessage.Value = null;
                //paramErrorMessage.ParameterName = "@Error_Msg ";
                //paramErrorMessage.DbType = DbType.String;
                //paramErrorMessage.Direction = ParameterDirection.Output;
                //paramErrorMessage.Size = 1000;


                command.Parameters.Add(paramSlug);
                command.Parameters.Add(paramCultureId);
                command.Parameters.Add(paramSiteId);
                command.Parameters.Add(paramStatus);
                command.Parameters.Add(paramRoleId);

                command.Parameters.Add(paramPostSlugType);
                command.Parameters.Add(paramPageSlugType);

                command.Parameters.Add(paramProductSlugType);
                command.Parameters.Add(paramBrandSlugType);

                command.Parameters.Add(paramCategorySlugType);
                ConfigureProcedureCommand(
                    command,
                    "GET_SLUG_SETTINGS",
                    "P_SLUG",
                    "P_CULTUREID",
                    "P_SITEID",
                    "P_STATUS",
                    "P_POSTSLUGTYPE",
                    "P_PAGESLUGTYPE",
                    "P_PRODUCTSLUGTYPE",
                    "P_BRANDSLUGTYPE",
                    "P_CATEGORYSLUGTYPE",
                    "P_ROLEID");
                try
                {
                    if (shouldCloseConnection)
                    {
                        await connection.OpenAsync();
                    }


                    using (var result = await command.ExecuteReaderAsync())
                    {
                        if (result.HasRows)
                        {
                            try
                            {
                                bool IsFirstRound = true;
                                while (await result.ReadAsync())
                                {
                                    var SlugType = ParseAnd0IfNull(result["SlugType"].ToString());
                                    if (SlugType == (long)SlugsTypes.Page && IsFirstRound)
                                    {
                                        SlugResult = await fillPageDataFromSP(result);
                                    }
                                    else if (SlugType == (long)SlugsTypes.Blog && IsFirstRound)
                                    {
                                        SlugResult = await fillPostDataFromSP(result);
                                    }

                                    else if (SlugType == (long)SlugsTypes.Product && IsFirstRound)
                                    {
                                        SlugResult = await fillProductDataFromSP(result);
                                    }

                                    else if (SlugType == (long)SlugsTypes.Category && IsFirstRound)
                                    {
                                        SlugResult = await fillCategoryDataFromSP(result);
                                    }

                                    else if (SlugType == (long)SlugsTypes.Brand && IsFirstRound)
                                    {
                                        SlugResult = await fillBrandDataFromSP(result);
                                    }


                                    if (SlugType == (long)SlugsTypes.Blog)
                                    {
                                        SlugResult = await fillPostOneToManyRelationData(result, SlugResult);
                                    }

                                    if (SlugType == (long)SlugsTypes.Product)
                                    {
                                        SlugResult = await fillProductOneToManyRelationData(result, SlugResult);
                                    }
                                    else if (SlugType == (long)SlugsTypes.Page)
                                    {
                                        // SlugResult = await fillPageOneToManyRelationData(result, SlugResult);
                                    }

                                    IsFirstRound = false;
                                }
                                return SlugResult;

                            }
                            catch (Exception E2)
                            {

                                throw E2;
                            }
                        }
                    }
                }
                catch (Exception E)
                {

                    throw E;
                }
                finally
                {
                    if (shouldCloseConnection)
                    {
                        await _context.Database.CloseConnectionAsync();
                    }
                }

            }
            //throw new Exception("Could Not Find Data");
            return null;
        }

        private async Task<SlugInfoVM> fillProductOneToManyRelationData(DbDataReader result, SlugInfoVM SlugResult)
        {
            try
            {
                // Category List 
                long MediaId = ParseAnd0IfNull(result["MediaId"].ToString());
                if (MediaId != 0)
                {


                    if (SlugResult.Product.MediaIds.FirstOrDefault(c => c == MediaId) == null)
                    {
                        SlugResult.Product.MediaIds.Add(MediaId);
                    }
                }




                // PostsTags List 
                var ImageUrl = result["Imageurl"].ToString();
                if (ImageUrl != null)
                {

                    if (SlugResult.Product.ImagesUrl.FirstOrDefault(c => c == ImageUrl) == null)
                    {
                        SlugResult.Product.ImagesUrl.Add(ImageUrl);
                    }
                }



                // MediaURLs List 
                var CategoryId = ParseAnd0IfNull(result["CategoryId"].ToString());
                if (CategoryId != 0)
                {
                    TempListsObj Cat = new TempListsObj()
                    {
                        id = CategoryId,
                        label = ""
                    };

                    if (SlugResult.CategoryList.FirstOrDefault(c => c.id == Cat.id) == null)
                    {
                        SlugResult.CategoryList.Add(Cat);
                    }
                }

                // MediaURLs List 
                var CategoryName = result["CategoryName"].ToString();
                if (CategoryId != 0)
                {
                    TempListsObj Cat = new TempListsObj()
                    {
                        id = 0,
                        Name = CategoryName
                    };

                    if (SlugResult.CategoryList.FirstOrDefault(c => c.Name == Cat.Name) == null)
                    {
                        SlugResult.CategoryList.Add(Cat);
                    }
                }



                // MediaURLs List 
                var ProductLinkR = ParseAnd0IfNull(result["ProductLinkRId"].ToString());
                if (CategoryId != 0)
                {
                    ProductLinkSlugViewModel Cat = new ProductLinkSlugViewModel()
                    {
                        Slug = result["ProductLinkRSLUG"].ToString(),
                        SpeacialPrice = Convert.ToDecimal(result["ProductLinkRSpecialPrice"].ToString()),
                        IsAllowToOrder = ParseDbBoolean(result["ProductLinkRIsAllowToOrder"]),
                        IsCallForPricing = ParseDbBoolean(result["ProductLinkRIsCallForPricing"]),
                        NameProductLink = result["ProductLinkCName"].ToString(),
                        oldPrice = Convert.ToDecimal(result["ProductLinkRoldPrice"]),
                        ProductLinkId = ProductLinkR,
                        ThumbnailImageUrl = result["ProductLinkRThumbnailImageUrl"].ToString()
                    };

                    if (SlugResult.productLinkRVms.FirstOrDefault(c => c.ProductLinkId == Cat.ProductLinkId) == null)
                    {
                        SlugResult.productLinkRVms.Add(Cat);
                    }
                }
                var ProductLinkC = ParseAnd0IfNull(result["ProductLinkCId"].ToString());
                if (CategoryId != 0)
                {
                    ProductLinkSlugViewModel Cat = new ProductLinkSlugViewModel()
                    {
                        Slug = result["ProductLinkCSLUG"].ToString(),
                        SpeacialPrice = Convert.ToDecimal(result["ProductLinkSpecialCPrice"].ToString()),
                        IsAllowToOrder = ParseDbBoolean(result["ProductLinkCIsAllowToOrder"]),
                        IsCallForPricing = ParseDbBoolean(result["ProductLinkCIsCallForPricing"]),
                        NameProductLink = result["ProductLinkCName"].ToString(),
                        oldPrice = Convert.ToDecimal(result["ProductLinkColdPrice"]),
                        ProductLinkId = ProductLinkC,
                        ThumbnailImageUrl = result["ProductLinkCThumbnailImageUrl"].ToString()
                    };

                    if (SlugResult.productLinkCVms.FirstOrDefault(c => c.ProductLinkId == Cat.ProductLinkId) == null)
                    {
                        SlugResult.productLinkCVms.Add(Cat);
                    }
                }
                return SlugResult;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<SlugInfoVM> fillProductDataFromSP(DbDataReader result)
        {
            try
            {
                var data = new SlugInfoVM();

                data.Id = ParseAnd0IfNull(result["ProductId"].ToString());
                data.Name = result["ProductName"].ToString();

                data.Slug = result["ProductSlug"].ToString();

                ProductSlugViewModel Product = new ProductSlugViewModel();

                Product.IsCallForPricing = ParseAnd0IfNulltoboolean(result["IsCallForPricing"].ToString());

                Product.IsAllowToOrder = ParseAnd0IfNulltoboolean(result["IsAllowToOrder"].ToString());

                data.Image = result["ThumbnailImageUrl"].ToString();

                data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

                Product.StockTrackingIsEnabled = ParseAnd0IfNulltoboolean(result["StockTrackingIsEnabled"].ToString());
                data.Description = result["ProductDescription"].ToString();
                data.ShortDescription = result["ProductShortDescription"].ToString();
                data.MetaDescription = result["ProductMetaDescription"].ToString();
                data.MetaTitle = result["MetaTitle"].ToString();
                data.Keywords = result["MetaKeywords"].ToString();
                Product.SKU = result["SKU"].ToString();
                Product.ReviewsCount = Convert.ToInt32(result["ReviewsCount"].ToString());
                Product.RatingAverage = Convert.ToDouble(result["RatingAverage"].ToString());
                data.ThumbnailImageUrl = result["ThumbnailImageUrl"].ToString();

                data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());
                data.BrandName = result["BrandName"].ToString();
                data.SlugType = SlugsTypes.Product;

                data.Product = Product;
                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private bool ParseAnd0IfNulltoboolean(string v)
        {
            return ParseDbBoolean(v);
        }

        private async Task<SlugInfoVM> fillCategoryDataFromSP(DbDataReader result)
        {
            try
            {
                var data = new SlugInfoVM();

                data.Id = ParseAnd0IfNull(result["CategoryId"].ToString());
                data.Name = result["CategoryName"].ToString();

                data.Slug = result["Slug"].ToString();
                CategorySlugViewModel categorySlugViewModel = new CategorySlugViewModel();

                categorySlugViewModel.DisplayOrder = result["DisplayOrder"].ToString();

                categorySlugViewModel.IncludedInMenu = ParseAnd0IfNulltoboolean(result["IncludedInMenu"].ToString());

                categorySlugViewModel.IsPublished = ParseAnd0IfNulltoboolean(result["ISPublished"].ToString());

                data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

                data.Description = result["Description"].ToString();
                data.MetaDescription = result["MetaDescription"].ToString();
                data.MetaTitle = result["MetaTitle"].ToString();
                data.Keywords = result["MetaKeywords"].ToString();
                data.ParentId = ParseAnd0IfNull(result["ParentId"].ToString());
                data.Status = Convert.ToInt32(result["Status"].ToString());
                data.ThumbnailImageId = Convert.ToInt64(result["ThumbnailImageId"].ToString());
                data.ThumbnailImageUrl = result["Imageurl"].ToString();
                categorySlugViewModel.FileName = result["FileName"].ToString();
                data.SlugType = SlugsTypes.Category;
                data.category = categorySlugViewModel;
                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<SlugInfoVM> fillBrandDataFromSP(DbDataReader result)
        {
            try
            {

                var data = new SlugInfoVM();
                data.Id = ParseAnd0IfNull(result["BrandId"].ToString());
                data.Name = result["BrandName"].ToString();
                data.Slug = result["Slug"].ToString();
                data.ParentId = ParseAnd0IfNull(result["ParentId"].ToString());
                data.Status = Convert.ToInt32(result["Status"].ToString());
                data.ThumbnailImageId = Convert.ToInt64(result["ThumbnailImageId"].ToString());
                data.ThumbnailImageUrl = result["PublicUrl"].ToString();
                data.CultureId = ParseAnd0IfNull(result["CultureId"].ToString());
                data.SlugType = SlugsTypes.Brand;

                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<PageTemplateVM> spGetTemplateSettingsBySlug(string Slug, long CultureId, long SiteId, long Status, long roleId)
        {
            // ✅ Step 1: Wrap connection in try/finally to ALWAYS close it
            var connection = _context.Database.GetDbConnection();

            try
            {
                // ✅ Step 2: Open connection properly
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    DbProviderHelper.ConfigureCommand(command);
                    // ✅ Step 3: Set command timeout
                    command.CommandTimeout = 30;

                    var paramSlug = command.CreateParameter();
                    paramSlug.Value = Slug;
                    paramSlug.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_SLUG");
                    paramSlug.DbType = DbType.String;
                    paramSlug.Direction = ParameterDirection.Input;

                    var paramCultureId = command.CreateParameter();
                    paramCultureId.Value = CultureId;
                    paramCultureId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_CULTUREID");
                    paramCultureId.DbType = DbType.Int64;
                    paramCultureId.Direction = ParameterDirection.Input;

                    var paramPostSlugType = command.CreateParameter();
                    paramPostSlugType.Value = (long)SlugsTypes.Blog;
                    paramPostSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_POSTSLUGTYPE");
                    paramPostSlugType.DbType = DbType.Int64;
                    paramPostSlugType.Direction = ParameterDirection.Input;

                    var paramPageSlugType = command.CreateParameter();
                    paramPageSlugType.Value = (long)SlugsTypes.Page;
                    paramPageSlugType.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_PAGESLUGTYPE");
                    paramPageSlugType.DbType = DbType.Int64;
                    paramPageSlugType.Direction = ParameterDirection.Input;

                    var paramSiteId = command.CreateParameter();
                    paramSiteId.Value = SiteId;
                    paramSiteId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_SITEID");
                    paramSiteId.DbType = DbType.Int64;
                    paramSiteId.Direction = ParameterDirection.Input;

                    var paramStatus = command.CreateParameter();
                    paramStatus.Value = Status;
                    paramStatus.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_STATUS");
                    paramStatus.DbType = DbType.Int64;
                    paramStatus.Direction = ParameterDirection.Input;

                    var paramRoleId = command.CreateParameter();
                    paramRoleId.Value = roleId;
                    paramRoleId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@P_ROLEID");
                    paramRoleId.DbType = DbType.Int64;
                    paramRoleId.Direction = ParameterDirection.Input;

                    command.Parameters.Add(paramSlug);
                    command.Parameters.Add(paramCultureId);
                    command.Parameters.Add(paramSiteId);
                    command.Parameters.Add(paramStatus);
                    command.Parameters.Add(paramPostSlugType);
                    command.Parameters.Add(paramPageSlugType);
                    command.Parameters.Add(paramRoleId);

                    ConfigureProcedureCommand(
                        command,
                        "GET_TEMPLATE_SETTINGS",
                        "P_SLUG",
                        "P_CULTUREID",
                        "P_SITEID",
                        "P_STATUS",
                        "P_POSTSLUGTYPE",
                        "P_PAGESLUGTYPE",
                        "P_ROLEID");

                    // ✅ Step 4: Use async reader
                    using (var result = await command.ExecuteReaderAsync())
                    {
                        if (result.HasRows)
                        {
                            string GUID = Guid.NewGuid().ToString("N");
                            PageTemplateVM TemplateData = null;
                            var AssetTemplate = new List<AssetTemplateViewModel>();
                            bool IsFirstRound = true;

                            while (await result.ReadAsync())
                            {
                                if (IsFirstRound)
                                {
                                    TemplateData = await FillTemplateData(result);
                                    if (TemplateData == null)
                                        return null;
                                }

                                await FillTemplateImportedFiles(result, TemplateData);
                                await FillTemplateUrls(result, AssetTemplate);

                                IsFirstRound = false;
                            }

                            if (AssetTemplate.Count > 0)
                            {
                                TemplateData.genericAssets = AssetTemplate
                                    .OrderBy(x => x.Order)
                                    .Select(x => new GenericAssetsViewModel
                                    {
                                        Url = $"Resources/styles/siteId_{x.SiteId}/Assest/{x.Extensions}/{x.Name}.{x.Extensions.ToLower()}?{GUID}={GUID}",
                                        Type = x.Type,
                                        Extensions = x.Extensions
                                    })
                                    .ToList();
                            }

                            return TemplateData;
                        }
                    }
                }
            }
            finally
            {
                // ✅ Step 5: ALWAYS close the connection
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            return null;
        }
        private async Task FillTemplateUrls(DbDataReader result, List<AssetTemplateViewModel> assets)
        {
            try
            {
                var AssetTemplateId = ParseAnd0IfNull(result["AssettemplateId"].ToString());

                if (AssetTemplateId != 0)
                {
                    var model = new AssetTemplateViewModel();

                    model.Order = Convert.ToInt32(ParseAnd0IfNull(result["AssettemplateOrder"].ToString()));
                    model.Type = (EAssetsType)Convert.ToInt32(ParseAnd0IfNull(result["Type"].ToString()));
                    model.Extensions = Enum.GetName(typeof(EAssetsType), model.Type);
                    model.SiteId = Convert.ToInt32(ParseAnd0IfNull(result["SiteId"].ToString()));
                    model.Name = result["AssetsName"].ToString();
                    assets.Add(model);
                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<PageTemplateVM> FillTemplateData(DbDataReader result)
        {
            var TemplateId = result["TemplateId"].ToString();
            var VersionId = result["VersionId"].ToString();


            return ParseAnd0IfNull(TemplateId) == 0
            ? null
            : new PageTemplateVM()
            {
                AddtionalMetaTags = result["AddtionalMetaTags"].ToString(),
                Description = result["TemplateDescription"].ToString(),
                CurrentVersion = new TemplateVersionVM()
                {
                    Css = result["TemplateCss"].ToString(),
                    Scripts = result["TemplateScripts"].ToString(),
                    SettingsBody = result["TemplateSettingsBody"].ToString(),
                    TemplateComponents = string.IsNullOrEmpty(result["TemplateComponents"].ToString())
                    ? JsonConvert.SerializeObject(new List<string>()) : result["TemplateComponents"].ToString(),
                    TemplateId= ParseAnd0IfNull(TemplateId),
                    VersionId=ParseAnd0IfNull(VersionId),
                    Id=ParseAnd0IfNull(TemplateId),

                }

            };




        }




        private async Task FillTemplateImportedFiles(DbDataReader result, PageTemplateVM TemplateData)
        {
            // Imported Files List 
            var ImportedFileId = ParseAnd0IfNull(result["ImportedID"].ToString());
            if (TemplateData != null
                && TemplateData.CurrentVersion != null
                && ImportedFileId != 0)
            {

                ImportedFilesVM importedFilesVM = new ImportedFilesVM()
                {
                    Id = ParseAnd0IfNull(result["ImportedID"].ToString()),
                    FileId = ParseAnd0IfNull(result["MediaId"].ToString()),
                    SourceId = ParseAnd0IfNull(result["SourceId"].ToString()),
                    FileName = result["FileName"].ToString(),
                    FileUrl = result["FileUrl"].ToString()
                };

                if (importedFilesVM.Id != 0 && TemplateData.CurrentVersion.ImportFilesList.FirstOrDefault(c => c.Id == importedFilesVM.Id) == null)
                {
                    TemplateData.CurrentVersion.ImportFilesList.Add(importedFilesVM);
                }
            }

        }



        private async Task<SlugInfoVM> fillPostDataFromSP(DbDataReader result)
        {


            var data = new SlugInfoVM();

            data.Id = ParseAnd0IfNull(result["PostId"].ToString());

            data.Keywords = result["PostKeywords"].ToString();

            data.Title = result["PostTitle"].ToString();

            data.Image = result["PostImage"].ToString();

            data.Slug = result["PostSlug"].ToString();

            var PublishDate = result["PublishDate"].ToString();

            if (!string.IsNullOrEmpty(PublishDate))
            {
                data.PublishDate = Convert.ToDateTime(PublishDate);
            }

            data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

            var TemplateId = result["PostTemplateId"].ToString();

            data.PostContent = result["PostContent"].ToString();

            data.Description = result["PostDescription"].ToString();

            data.ShortDescription = result["PostShortDescription"].ToString();

            data.MetaDescription = result["PostMetaDescription"].ToString();

            data.PostSettings = result["PostSettings"].ToString();

            data.RelatedCategoriesSettings = result["PostRelatedCategoriesSettings"].ToString();

            data.SlugType = SlugsTypes.Blog;

            data.TemplateId = string.IsNullOrEmpty(TemplateId) ? 0 : long.Parse(TemplateId);

            //data.Template = data.TemplateId == 0
            //? null
            //: new PageTemplateVM()
            //{
            //    Description = result["TemplateDescription"].ToString(),
            //    CurrentVersion = new TemplateVersionVM()
            //    {
            //        Css = result["TemplateCss"].ToString(),
            //        //ImportFiles = result["TemplateImportFiles"].ToString(),
            //        Scripts = result["TemplateScripts"].ToString(),
            //        SettingsBody = result["TemplateSettingsBody"].ToString(),
            //        TemplateComponents = string.IsNullOrEmpty(result["TemplateComponents"].ToString())
            //        ? JsonConvert.SerializeObject(new List<string>())
            //        : result["TemplateComponents"].ToString()
            //    }
            //};




            return data;
        }

        private async Task<SlugInfoVM> fillPostOneToManyRelationData(DbDataReader result, SlugInfoVM SlugResult)
        {
            // Category List 
            var CatId = ParseAnd0IfNull(result["PostCategoryId"].ToString());
            if (CatId != 0)
            {
                TempListsObj tempListsObj = new TempListsObj()
                {
                    id = CatId,
                    label = result["PostCategoryName"].ToString()
                };

                if (SlugResult.CategoryList.FirstOrDefault(c => c.id == tempListsObj.id) == null)
                {
                    SlugResult.CategoryList.Add(tempListsObj);
                }
            }




            //// PostsTags List 
            //var TagId = ParseAnd0IfNull(result["PostTagId"].ToString());
            //if (TagId != 0)
            //{
            //    var tempListsObj = new TempListsObj()
            //    {
            //        id = TagId,
            //        label = result["PostTagTitle"].ToString()
            //    };

            //    if (SlugResult.TagList.FirstOrDefault(c => c.id == tempListsObj.id) == null)
            //    {
            //        SlugResult.TagList.Add(tempListsObj);
            //    }
            //}



            // MediaURLs List 
            var mediaId = ParseAnd0IfNull(result["PostMediaId"].ToString());
            if (mediaId != 0)
            {
                PostMediaWithURLViewModel PostMediaWithURLObj = new PostMediaWithURLViewModel()
                {
                    Id = mediaId,
                    Url = result["PostPublicUrl"].ToString(),
                    ContentType = result["MediaContentType"].ToString()

                };

                if (SlugResult.MediaURLs.FirstOrDefault(c => c.Id == PostMediaWithURLObj.Id) == null)
                {
                    SlugResult.MediaURLs.Add(PostMediaWithURLObj);
                }
            }




            //// Imported Files List 
            //var ImportedFileId = ParseAnd0IfNull(result["ImportedID"].ToString());
            //if (SlugResult.Template != null 
            //    && SlugResult.Template.CurrentVersion != null
            //    && ImportedFileId !=0)
            //{

            //    ImportedFilesVM importedFilesVM = new ImportedFilesVM()
            //    {
            //        Id = ParseAnd0IfNull(result["ImportedID"].ToString()),
            //        FileId = ParseAnd0IfNull(result["MediaId"].ToString()),
            //        SourceId = ParseAnd0IfNull(result["SourceId"].ToString()),
            //        FileName = result["FileName"].ToString(),
            //        FileUrl = result["FileUrl"].ToString()
            //    };

            //    if (importedFilesVM.Id != 0 && SlugResult.Template.CurrentVersion.ImportFilesList.FirstOrDefault(c => c.Id == importedFilesVM.Id) == null)
            //    {
            //        SlugResult.Template.CurrentVersion.ImportFilesList.Add(importedFilesVM);
            //    }
            //}


            return SlugResult;
        }


        private async Task<SlugInfoVM> fillPageDataFromSP(DbDataReader result)
        {
            var data = new SlugInfoVM();

            data.Id = ParseAnd0IfNull(result["Id"].ToString());
            data.AdditionalContent = result["AdditionalContent"].ToString();
            data.Description = result["Description"].ToString();
            data.IsLoginRequired = ParseDbBoolean(result["IsLoginRequired"]);
            data.Keywords = result["Keywords"].ToString();
            data.Name = result["Name"].ToString();
            data.Slug = result["Slug"].ToString();
            data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());
            data.SlugType = SlugsTypes.Page;
            data.Image = result["ThumbnailUrl"].ToString();

            var TemplateId = result["PageTemplateId"].ToString();

            data.TemplateId = string.IsNullOrEmpty(TemplateId) ? null : Convert.ToInt64(TemplateId);


            //data.Template = string.IsNullOrEmpty(TemplateId)
            //? null
            //: new PageTemplateVM()
            //{
            //    Description = result["TemplateDescription"].ToString(),
            //    CurrentVersion = new TemplateVersionVM()
            //    {
            //        Css = result["TemplateCss"].ToString(),
            //        Scripts = result["TemplateScripts"].ToString(),
            //        SettingsBody = result["TemplateSettingsBody"].ToString(),
            //        TemplateComponents = string.IsNullOrEmpty(result["TemplateComponents"].ToString())
            //        ? JsonConvert.SerializeObject(new List<string>())
            //        : result["TemplateComponents"].ToString()
            //    }

            //};


            var PageCurrentVersionId = result["PageCurrentVersionId"].ToString();
            data.CurrentVersion = string.IsNullOrEmpty(PageCurrentVersionId)
                ? null
                : new PageVersionVM()
                {
                    SettingsBody = result["PageSettingsBody"].ToString(),
                    PageComponents = string.IsNullOrEmpty(result["PageComponents"].ToString())
                    ? JsonConvert.SerializeObject(new List<string>())
                    : result["PageComponents"].ToString()

                };


            return data;
        }


        //private async Task<SlugInfoVM> fillPageOneToManyRelationData(DbDataReader result, SlugInfoVM SlugResult)
        //{

        //    // Imported Files List 
        //    if (SlugResult.Template != null && SlugResult.Template.CurrentVersion != null&& !string.IsNullOrEmpty(result["ImportedID"].ToString()))
        //    {

        //        ImportedFilesVM importedFilesVM = new ImportedFilesVM()
        //        {
        //            Id = ParseAnd0IfNull(result["ImportedID"].ToString()),
        //            FileId = ParseAnd0IfNull(result["MediaId"].ToString()),
        //            SourceId = ParseAnd0IfNull(result["SourceId"].ToString()),
        //            FileName = result["FileName"].ToString(),
        //            FileUrl = result["FileUrl"].ToString(),
        //            contentType = result["ContentType"].ToString(),
        //        };

        //        if (importedFilesVM.Id != 0 && SlugResult.Template.CurrentVersion.ImportFilesList.FirstOrDefault(c => c.Id == importedFilesVM.Id) == null)
        //        {
        //            SlugResult.Template.CurrentVersion.ImportFilesList.Add(importedFilesVM);
        //        }
        //    }


        //    return SlugResult;
        //}








        #endregion


        #endregion
    }
}
