using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Localization.Models;
using EskaCMS.Localization.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Localization;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Localization.Services
{
    public class LocalizationServices : ILocalizationServices
    {
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<SlugInfo> slugInfoRepository;
        private readonly IWorkContext _workContext;
        private readonly ISiteCultureService _siteCultureService;
        private readonly IRepository<ResourcesKey> resourcesKeysRepository;
        private readonly IRepository<ResourcesValue> resourcesValueRespository;
        private readonly IRepositoryWithTypedId<ResourcesGroup, string> ResourcesGroupRepo;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;

        public LocalizationServices(IRepository<ResourcesKey> ResourcesKeysRepository, EskaDCMSDbContext context, IRepository<SlugInfo> slugInfoRepository

            , IRepository<ResourcesValue> ResourcesValueRespository, IWorkContext workContext, ISiteCultureService siteCultureService,
                ICacheService cacheService, IConfiguration configuration, IRepositoryWithTypedId<ResourcesGroup, string> resourcesGroupRepo)
        {
            resourcesKeysRepository = ResourcesKeysRepository;
            _context = context;
            this.slugInfoRepository = slugInfoRepository;
            resourcesValueRespository = ResourcesValueRespository;
            _workContext = workContext;
            _siteCultureService = siteCultureService;
            _cacheService = cacheService;
            _configuration = configuration;
            ResourcesGroupRepo = resourcesGroupRepo;
        }


        public async Task<bool> UpsertLocalization(List<LocalizationViewModel> model, long siteId, long cultureId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in model)
                    {
                        var isEditMode = item.Id != 0;
                        var resourcesKeys = await GetOrCreateResourcesKeysAsync(item.Id, isEditMode, siteId, item.SlugInfoId, item.Key, item.GroupId, item.Component);

                        if (!isEditMode)
                        {
                            await AddResourcesKeysAsync(resourcesKeys);
                        }
                        if (isEditMode)
                        {
                            await DeleteResourcesValuesAsync(resourcesKeys.Id);
                        }
                        await AddResourcesValuesAsync(resourcesKeys.Id, item.LocalizationValues);
                    }

                    await DBTrans.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await DBTrans.RollbackAsync();
                    throw ex;
                }
            });
        }

        //public async Task<bool> UpdateLocalizationValues(List<LocalizationViewModel> model, long siteId, long cultureId)
        //{
        //    using (var DBTrans = _context.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            foreach (var item in model)
        //            {


        //                await AddResourcesValuesAsync(resourcesKeys.Id, item.LocalizationValues);
        //            }

        //            DBTrans.Commit();
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            DBTrans.Rollback();
        //            throw ex;
        //        }
        //    }
        //}

        private async Task<ResourcesKey> GetOrCreateResourcesKeysAsync(long id, bool isEditMode, long siteId, long? slugInfoId, string key, string groupId, string component)
        {
            if (isEditMode)
            {
                return await resourcesKeysRepository.Query().Where(m => m.Id == id).FirstOrDefaultAsync() ?? new ResourcesKey();
            }
            else
            {
                return new ResourcesKey
                {
                    SiteId = siteId,
                    //SlugInfoId = slugInfoId,
                    Key = key,
                    GroupId = groupId,
                    Component = component
                };
            }
        }



        public async Task<bool> DeleteGroup(string GroupId)
        {
            try
            {
                var Group = await ResourcesGroupRepo
                              .Query()
                              .Where(g => g.Id.ToLower() == GroupId.ToLower())
                              .FirstOrDefaultAsync();

                ResourcesGroupRepo.Remove(Group);
                await ResourcesGroupRepo.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public async Task<Dictionary<string, Dictionary<string, string>>> GetLocalization(string Slug, long SiteId, long CultureId)
        {
            try
            {

                var cacheData = _cacheService?.GetData<Dictionary<string, Dictionary<string, string>>>("Localization" + Slug + SiteId.ToString() + CultureId.ToString());

                if (cacheData != null)
                {
                    return cacheData;
                }


                Dictionary<string, Dictionary<string, string>> Result = new();

                Slug = Slug.ToLower();

                //var Locallizations = await resourcesKeysRepository.Query()
                //                               .Include(x => x.SlugInfo)
                //                               .Where(x => x.SiteId == SiteId &&
                //                                           (x.SlugInfoId == null || x.SlugInfo.Slug.ToLower() == Slug && x.SlugInfo.CultureId == CultureId))
                //                               .Include(o => o.ResourcesValues)
                //                               .Select(r => new LocalizationVM()
                //                               {
                //                                   Value = r.ResourcesValues.Count() > 0 ? r.ResourcesValues.Where(value => value.CultureId == CultureId).FirstOrDefault().Value : string.Empty,
                //                                   Key = r.Key,
                //                                   SlugInfoId = r.SlugInfoId,
                //                                   Scope = r.Scope,
                //                                   GroupId = r.GroupId
                //                               })
                //                               .ToListAsync();


                var Locallizations = await resourcesValueRespository.Query()

                                                .Where(x => x.Key.SiteId == SiteId && x.CultureId == CultureId && (x.SlugInfoId == null || x.SlugInfo.Slug.ToLower() == Slug && x.SlugInfo.CultureId == CultureId))
                                                .Include(o => o.Key)
                                                .Include(x => x.SlugInfo)
                                                .Select(r => new LocalizationVM()
                                                {
                                                    Value = r.Value,
                                                    Key = r.Key.Key,
                                                    SlugInfoId = r.SlugInfoId,
                                                    Scope = r.Key.Scope,
                                                    GroupId = r.Key.GroupId,
                                                    Component = r.Key.Component,
                                                    IsShared = r.Key.IsShared

                                                })
                                                .ToListAsync();

                Result.Add("SharedResources", new());

                Result.Add("SlugResources", new());





                //var SlugGroups = Locallizations
                //    .Where(x => x.SlugInfoId != null && x.GroupId != null)
                //    .GroupBy(x => x.GroupId)
                //    .Select(x => x.Key)
                //    .ToList();

                //var Groups = new List<string>();


                if (!string.IsNullOrEmpty(Slug))
                {
                    var ComponentKeys = Locallizations.Where(x => !string.IsNullOrEmpty(x.Component) && !x.IsShared)
                        .GroupBy(x => x.Component)
                        .Select(x => x.Key)
                        .ToList();

                    foreach (var component in ComponentKeys)
                    {
                        Result.Add(component, new());
                    }

                }





                foreach (var Resource in Locallizations)

                {
                    
                    if (!string.IsNullOrEmpty(Resource.Component) && !Resource.IsShared && !string.IsNullOrEmpty(Slug))
                    {

                        Result[Resource.Component].Add(Resource.Key, Resource.Value);
                    }

                    if (Resource.Scope == EResouseScope.AmongSlug && string.IsNullOrEmpty(Resource.Component) && !string.IsNullOrEmpty(Slug))
                    {
                        try
                        {
                          Result["SlugResources"].Add(Resource.Key, Resource.Value);

                        }
                        catch (Exception)
                        {

                            //throw;
                        }

                    }
                    else if (Resource.IsShared)
                    {
                        try
                        {

                        Result["SharedResources"].Add(Resource.Key, Resource.Value);

                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }


                _cacheService.SetData<Dictionary<string, Dictionary<string, string>>>("Localization" + Slug + SiteId.ToString() + CultureId.ToString(), Result, DateTimeOffset.Now.AddDays(double.Parse(_configuration.GetSection("CacheConfig:CachePeriod").Value)));


                return Result;

            }

            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<string>> GetComponentKeys(string ComponentName)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();

            return await resourcesKeysRepository
                                    .Query()
                                    .Where(x => x.GroupId == null &&
                                               x.Component.ToLower() == ComponentName &&

                                               x.SiteId == siteId)
                                    .Select(r => r.Key)
                                    .Distinct()
                                    .ToListAsync();

        }
        public async Task<PaginationResult<LocalizationViewModel>> GetAllKeys(SmartSearchTableParam<LocalizationViewModel> param)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();

            //var query = resourcesKeysRepository
            //                        .Query()
            //.Include(k => k.SlugInfo)
            //.Include(x=>x.ResourcesValues)
            //.ThenInclude(x=>x.Culture)
            //.Where(x => x.SiteId == siteId);

            var query = resourcesValueRespository
                                    .Query()
                                    .Where(x => x.Key.SiteId == siteId && x.CultureId == param.Search.CultureId)
                                    .Include(x => x.Key)
                                    .Include(k => k.SlugInfo)
                                    .ThenInclude(x => x.Culture)
                                    .AsNoTracking();

            //if(param.Search.Id != 0)
            //{
            //    query = query.Where(x => x.Id == param.Search.Id);
            //}

            if (!string.IsNullOrEmpty(param.Search.Slug))
            {
                query = query.Where(x => x.SlugInfo.Slug.Contains(param.Search.Slug));

            }

            //if (!string.IsNullOrEmpty(param.Search.Component))
            //{
            //    query = query.Where(x => x.Key.Component.Contains(param.Search.Component));
            //}

            if (!string.IsNullOrEmpty(param.Search.Key))
            {
                var upperKey = param.Search.Key.ToUpper();
                query = query.Where(x => x.Key.Key.ToUpper().Contains(upperKey) || x.Key.Component.ToUpper().Contains(upperKey) || x.Value.ToUpper().Contains(upperKey));
            }

            var Result = await query
                            .Select(r => new LocalizationViewModel()
                            {
                                Id = r.Key.Id,
                                Component = r.Key.Component,
                                GroupId = r.Key.GroupId,
                                Key = r.Key.Key,
                                Slug = r.SlugInfo != null ? r.SlugInfo.Slug : "Empty Slug",
                                LocalizationValues = new List<LocalizationValueVM>() { new LocalizationValueVM()
                                        {
                                            Id=r.Id,
                                            CultureId = r.CultureId,
                                            Value = r.Value,
                                            CultureCode = r.Culture.CultureId
                                        }},

                            })
                            .OrderByDescending(x => x.Id)
                            .PaginateAsync(param.Pagination.Number, param.Pagination.Start);


            return Result;


        }

        public async Task<bool> ScanLocalization(string Component, string key, string Group = null, string Slug = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Unable to scan key. Please provide a key for scanning.");

            }

            if (!string.IsNullOrEmpty(Component) && string.IsNullOrEmpty(Slug))
            {
                throw new Exception("Unable to scan key. Please provide both Slug and Component or just Slug for scanning.");

            }
            var KeyItem = await AddLocalization(key, Slug, Component: Component, Group: Group);

            //await _cacheService.RemoveAll();
            // await AddDefaultLocalizationValues(KeyItem); 
            return true;
        }

        public async Task<bool> AddLocalization(string Key, string Slug = null, string Component = null, string Group = null)
        {
            try
            {



                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var resourcesKey = resourcesKeysRepository.Query()
                                                                .Where(r => r.Key.ToLower() == Key.ToLower() && r.SiteId == SiteId && (!string.IsNullOrEmpty(Component) ? r.Component.ToLower() == Component.ToLower() : true))
                                                                .Include(x => x.ResourcesValues)
                                                                .Include("ResourcesValues.SlugInfo")
                                                                .ToList();

                if (!resourcesKey.Any())
                {
                    var resourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


                    await AddDefaultLocalizationValues(resourceItem, Slug);
                }
                else
                {

                    if (!string.IsNullOrEmpty(Slug) && !string.IsNullOrEmpty(Component))
                    {

                        var existingKey = resourcesKey.Where(x => x.Component.ToLower() == Component.ToLower() && !x.IsShared).FirstOrDefault();
                        if (existingKey != null)
                        {

                            await AddLocalizationValue(existingKey, Slug, SiteId);
                        }
                        else
                        {
                            var resourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


                            await AddDefaultLocalizationValues(resourceItem, Slug);
                        }
                    }
                    else if (!string.IsNullOrEmpty(Slug) && string.IsNullOrEmpty(Component))
                    {
                        var cultureId = _workContext.GetCurrentCultureId();

                        var targetSlug = await GetSlug(cultureId, SiteId, Slug);
                        var resourceValue = resourcesKey.SelectMany(x => x.ResourcesValues.Where(x => string.IsNullOrEmpty(x.Key.Component) && x.CultureId == cultureId && x.SlugInfoId == targetSlug.Id)).FirstOrDefault();

                        if (resourceValue == null)
                        {

                            var resourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


                            await AddDefaultLocalizationValues(resourceItem, Slug);
                        }
                    }
                    else if (string.IsNullOrEmpty(Slug) && string.IsNullOrEmpty(Component))
                    {

                        if (resourcesKey.Any(x => x.IsShared))
                            return true;
                        else
                        {
                            var resourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


                            await AddDefaultLocalizationValues(resourceItem, Slug);
                        }
                    }


                }

                return true;

            }
            catch (Exception ex)
            {

                throw;
            }
        }



        private async Task<ResourcesKey> CreateLocalizationKey(long siteId, string Key, string Slug = null, string Component = null, string Group = null)
        {
            ResourcesKey ResourceItem = new()
            {
                Component = string.IsNullOrEmpty(Component) ? null : Component,
                GroupId = string.IsNullOrEmpty(Group) ? null : Group,
                Key = Key,
                SiteId = siteId,
                IsShared = string.IsNullOrEmpty(Slug) ? true : false,

            };

            resourcesKeysRepository.Add(ResourceItem);

            await resourcesKeysRepository.SaveChangesAsync();

            return ResourceItem;
        }

        public async Task<bool> AddDefaultLocalizationValues(ResourcesKey ResourcesKey, string slug)
        {

            long SiteId = await _workContext.GetCurrentSiteIdAsync();


            var Cultures = await _siteCultureService.GetSiteCultures(SiteId);

            if (ResourcesKey.IsShared)
            {
                foreach (var Culture in Cultures)
                {

                    ResourcesValue ResourceValue = new()
                    {
                        Value = ResourcesKey.Key,
                        KeyId = ResourcesKey.Id,
                        CultureId = Culture.Id.Value,
                        SlugInfoId = null

                    };

                    resourcesValueRespository.Add(ResourceValue);


                }

                await resourcesValueRespository.SaveChangesAsync();
            }

            else if (!ResourcesKey.IsShared && !string.IsNullOrEmpty(slug))
            {

                await AddLocalizationValue(ResourcesKey, slug, SiteId);
            }



            return true;
        }



        public async Task<bool> AddLocalizationValue(ResourcesKey ResourcesKey, string slug, long SiteId)
        {



            var cultureId = _workContext.GetCurrentCultureId();

            var Slug = await GetSlug(cultureId, SiteId, slug);
            var key = ResourcesKey.Key.ToLower();

            if (!string.IsNullOrEmpty(ResourcesKey.Component))
            {
                var component = ResourcesKey.Component.ToLower();
                var resourceValue = resourcesKeysRepository.Query()
                                                           .Where(x => x.Key.ToLower() == key && x.Component.ToLower() == component && x.SiteId == SiteId)
                                                           .Include(x => x.ResourcesValues)
                                                           .SelectMany(x => x.ResourcesValues)
                                                           .FirstOrDefault(x => x.CultureId == cultureId && x.SlugInfoId == Slug.Id);
                if (resourceValue != null)
                {
                    return true;

                }
            }



            //var checkDuplicateValue = resourcesKeysRepository.Query()
            //                                              .Where(x => x.Key. == key && x.SiteId == SiteId)
            //                                              .Include(x => x.ResourcesValues)
            //                                              .SelectMany(x => x.ResourcesValues)
            //                                              .FirstOrDefault(x => x.CultureId == cultureId && x.SlugInfoId == Slug.Id);

            //if (checkDuplicateValue != null)
            //{
            //    return true;

            //}

            ResourcesValue ResourceValue = new()
            {
                Value = ResourcesKey.Key,
                KeyId = ResourcesKey.Id,
                CultureId = cultureId,
                SlugInfoId = Slug?.Id,

            };


            resourcesValueRespository.Add(ResourceValue);

            await resourcesValueRespository.SaveChangesAsync();




            return true;
        }

        public async Task<string> GetLocalizationValueByKey(string ResourcesKey)
        {

            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            long cultureId = _workContext.GetCurrentCultureId();

            var localizationResource = resourcesKeysRepository.Query().AsNoTracking()
                                                           .Where(x => x.SiteId == SiteId && x.Key.ToLower() == ResourcesKey.ToLower())
                                                           .Include(x => x.ResourcesValues)
                                                           .FirstOrDefault();

            var localizationValue = localizationResource.ResourcesValues.Where(x => x.CultureId == cultureId).FirstOrDefault()?.Value;


            return localizationValue;
        }

        public async Task<bool> EditLocaliztion(LocalizationValueEditVM model)
        {
            try
            {
                var query = await resourcesValueRespository
                                                        .Query()
                                                        .Where(x => x.Id == model.ValueId &&
                                                        x.CultureId == x.CultureId)
                                                        .FirstOrDefaultAsync();

                if (query == null)
                {

                    throw new Exception("Not Found");
                }

                query.Value = model.Value;

                await resourcesValueRespository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateBulkLocaliztionValues(List<LocalizationValueEditVM> model)
        {
            try
            {

                if (model.Any())
                {
                    var valueIds = model.Select(x => x.ValueId).ToList();

                    var siteId = _workContext.GetCurrentSiteId();
                    var targetValues = resourcesValueRespository.Query()
                                                    .Where(x => x.Key.SiteId == siteId && valueIds.Contains(x.Id))
                                                    .ToList();


                    foreach (var newValue in model)
                    {

                        var value = targetValues.Where(x => x.Id == newValue.ValueId).FirstOrDefault();

                        if (value != null)
                        {
                            value.Value = newValue.Value;
                        }

                    }

                    await resourcesValueRespository.SaveChangesAsync();

                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<SlugInfo> GetSlug(long cultureId, long SiteId, string Slug)
        {
            try
            {


                var slug = Slug.ToLower();
                SlugInfo SlugItem = await slugInfoRepository.Query()
                                                         .Where(x => x.Slug.ToLower() == slug
                                                                 && x.SiteId == SiteId
                                                                 && x.CultureId == cultureId)
                                                         .FirstOrDefaultAsync();


                if (SlugItem == null)
                {
                    throw new Exception("Slug doesn't exist");

                }

                return SlugItem;

            }
            catch (Exception ex)
            {

                return null;
            }
        }


        private async Task AddResourcesKeysAsync(ResourcesKey resourcesKeys)
        {
            resourcesKeysRepository.Add(resourcesKeys);
            await resourcesKeysRepository.SaveChangesAsync();
        }

        private async Task DeleteResourcesValuesAsync(long keyId)
        {
            var resourceValues = await resourcesValueRespository.Query().Where(x => x.KeyId == keyId).ToListAsync();
            resourcesValueRespository.RemoveRange(resourceValues);
            await resourcesValueRespository.SaveChangesAsync();
        }

        private async Task AddResourcesValuesAsync(long keyId, List<LocalizationValueVM> localizationValues)
        {
            var resourceValuesToBeAdded = localizationValues.Select(item => new ResourcesValue
            {
                CultureId = item.CultureId,
                KeyId = keyId,
                Value = item.Value
            }).ToList();
            resourcesValueRespository.AddRange(resourceValuesToBeAdded);
            await resourcesValueRespository.SaveChangesAsync();
        }

        private async Task<bool> BeforeUpdate(string Key, string Slug = null, string Component = null, string Group = null)
        {

            long SiteId = await _workContext.GetCurrentSiteIdAsync();

            var resourcesKey = await resourcesKeysRepository.Query().FirstOrDefaultAsync(r => r.Key.ToLower() == Key.ToLower() && r.SiteId == SiteId && (!string.IsNullOrEmpty(Component) ? r.Component.ToLower() == Component.ToLower() : true) && (!string.IsNullOrEmpty(Group) ? r.GroupId == Group : true));

            if (resourcesKey != null && !resourcesKey.IsShared && !string.IsNullOrEmpty(Component) && !string.IsNullOrEmpty(Slug))
            {

                if (resourcesKey.Component.ToLower() == Component.ToLower())
                {
                    await AddLocalizationValue(resourcesKey, Slug, SiteId);

                }
                else if (resourcesKey != null && string.IsNullOrEmpty(Component) && !string.IsNullOrEmpty(Slug))
                {
                    var resourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


                    await AddDefaultLocalizationValues(resourceItem, Slug);


                }
                return true;

            }

            if (resourcesKey != null && resourcesKey.IsShared)
            {

                return true;
            }

            //ResourcesKey ResourceItem = new()
            //{
            //    Component = Component,
            //    GroupId = Group,
            //    Key = Key,
            //    SiteId = SiteId,
            //    IsShared = string.IsNullOrEmpty(Slug) ? true : false,

            //};

            //resourcesKeysRepository.Add(ResourceItem);

            //await resourcesKeysRepository.SaveChangesAsync();

            var ResourceItem = await CreateLocalizationKey(SiteId, Key, Slug, Component, Group);


            await AddDefaultLocalizationValues(ResourceItem, Slug);



            return true;
        }



    }
}
