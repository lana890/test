using EskaCMS.Core.Data;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCommerce.Catalog.ViewModels.Category.Responses;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Core.Cache;
using Microsoft.Extensions.Configuration;
using EskaCommerce.Catalog.Services.CatalogService.BrandServices.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.BrandServices
{
    public class BrandService : IBrandService
    {
        private const string BrandEntityTypeId = "Brand";

        private readonly IRepository<Brand> _brandRepository;
        private readonly IEntityService _entityService;
        private readonly IRepository<ProductCategory> _productCategoryRepo;
        private readonly ISlugServices _SlugServices;
        private readonly IRepository<BrandDetails> brandDetailsRepository;
        private readonly IWorkContext workContext;
        private readonly ISearchRepository<NaviagtableSearchModel> _searchRepository;
        private readonly ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> _meiliRepo;


        private readonly ICacheService cacheService;
        private readonly IClientCacheService clientCacheService;
        private readonly IConfiguration _config;
        public BrandService(IRepository<Brand> brandRepository, IEntityService entityService,
            IRepository<ProductCategory> ProductCategoryRepo, ISlugServices SlugServices,
            IRepository<BrandDetails> BrandDetailsRepository,
            IWorkContext workContext,
        ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> meiliRepo,
        ISearchRepository<NaviagtableSearchModel> searchRepository,
            ICacheService cacheService, IClientCacheService clientCacheService,
                   IConfiguration config
)
        {
            _brandRepository = brandRepository;
            _entityService = entityService;
            _productCategoryRepo = ProductCategoryRepo;
            _SlugServices = SlugServices;
            brandDetailsRepository = BrandDetailsRepository;
            this.workContext = workContext;
            _searchRepository = searchRepository;
            this.cacheService = cacheService;
            this.clientCacheService = clientCacheService;
            _meiliRepo = meiliRepo;
            _config = config;
        }
        public async Task<PaginationResult<BrandViewModel>> GetAllBrand(BrandParamsViewModel model, long SiteId)
        {
            try
            {
                // Includes are ignored by EF Core because the query terminates in a .Select
                // projection to BrandViewModel (the projection generates the needed joins),
                // so they are removed as dead query operators; SQL and results are unchanged.
                var brandList = _brandRepository
                                                .Query()
                                                .Where(x => x.Status != EStatus.Deleted
                                                 && x.Status != EStatus.Inactive
                                                 && x.SiteId == SiteId
                                                 && (model.BrandIds == null || model.BrandIds.Count() == 0 || model.BrandIds.Contains(x.Id)))
                                                .Where(x => string.IsNullOrEmpty(model.SearchText) || x.BrandDetails.Any(d=>d.Name.ToLower().Contains(model.SearchText.ToLower())));



                var Result = await brandList.Select(x => new BrandViewModel
                {
                    Id = x.Id,
                    PublicUrl = x.Thumbnail.PublicUrl,
                    Status = x.Status,
                    ThumbnailId = x.ThumbnailId,
                    PublishDate=x.PublishDate,
                    ExpiryDate=x.ExpiryDate,
                    brandDetails = x.BrandDetails.Select(x => new BrandDetailsViewModel
                    {

                        Description = x.Description,
                        BrandTemplateId = x.BrandTemplateId,
                        CultureCode = x.Culture.CultureId,
                        Name = x.Name,
                        CultureId = x.CultureId,
                        Slug = x.SlugInfo.Slug,
                        BrandId = x.Brand.Id,
                        Id = x.Id
                    }).ToList()


                }).PaginateAsync(model.pagination.PageSize, model.pagination.PageNumber);

                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> AddBrand(BrandViewModel model, long UserId = 0, long SiteId = 0)
        {


            return await _brandRepository.ExecuteInTransactionAsync(async () =>
            {
                var ObjBrand = new Brand();
                ObjBrand.CreatedById = UserId;
                ObjBrand.CreationDate = DateTime.Now;
                ObjBrand.Status = model.Status;
                ObjBrand.SiteId = SiteId;
                ObjBrand.ThumbnailId = model.ThumbnailId;
                ObjBrand.PublishDate = model.PublishDate;
                ObjBrand.ExpiryDate = model.ExpiryDate;

                await ValidateStatusAndPublishDate_ExpireDate(ObjBrand.Status, ObjBrand.PublishDate, ObjBrand.ExpiryDate);

                _brandRepository.Add(ObjBrand);
                await _brandRepository.SaveChangesAsync();
                var List = new List<BrandDetails>();
                foreach (var item in model.brandDetails)
                {
                    var BrandDetails = new BrandDetails();
                    BrandDetails.BrandTemplateId = item.BrandTemplateId;
                    BrandDetails.CultureId = item.CultureId;
                    BrandDetails.Description = item.Description;
                    BrandDetails.Name = item.Name;
                    BrandDetails.BrandId = ObjBrand.Id;
                    SlugManagementVm slugManagementVm = new()
                    {
                        CultureId = item.CultureId,
                        Slug = item.Slug,
                        Entity = EEntity.Brand,
                        SiteId = SiteId
                    };
                    BrandDetails.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                    List.Add(BrandDetails);
                }
                brandDetailsRepository.AddRange(List);
                await brandDetailsRepository.SaveChangesAsync();
                return true;
            });




        }

        public async Task<bool> UpdateBrand(BrandViewModel model, long UserId = 0, long SiteId = 0)
        {


            return await _brandRepository.ExecuteInTransactionAsync(async () =>
            {
                var ObjBrand = await _brandRepository.Query()
                    .Include(x => x.BrandDetails)
                    .Include("BrandDetails.SlugInfo")
                    .Include("BrandDetails.Culture")
                    .Where(x => x.Id == model.Id).FirstOrDefaultAsync();

                ObjBrand.ModifiedById = UserId;
                ObjBrand.ModificationDate = DateTime.Now;
                ObjBrand.Status = model.Status;
                ObjBrand.SiteId = SiteId;
                ObjBrand.ThumbnailId = model.ThumbnailId;
                ObjBrand.PublishDate = model.PublishDate;
                ObjBrand.ExpiryDate = model.ExpiryDate;

                await ValidateStatusAndPublishDate_ExpireDate(ObjBrand.Status, ObjBrand.PublishDate, ObjBrand.ExpiryDate);

                foreach (var brand in ObjBrand.BrandDetails)
                {
                    foreach (var item in model.brandDetails)
                    {
                        if (item.CultureId == brand.CultureId)
                        {
                            cacheService.RemoveByPrefix("Slug" + brand.SlugInfo.Slug + brand.CultureId + SiteId);
                            await clientCacheService.RemoveCacheFile(brand.SlugInfo.Slug, SiteId, brand.CultureId);
                            brand.BrandTemplateId = item.BrandTemplateId;
                            brand.Description = item.Description;
                            brand.Name = item.Name;
                            SlugManagementVm slugManagementVm = new()
                            {
                                Id = brand.SlugInfoId,
                                CultureId = item.CultureId,
                                Slug = item.Slug,
                                Entity = EEntity.Brand,
                                SiteId = SiteId
                            };
                            brand.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                        }
                    }
                }
                await _brandRepository.SaveChangesAsync();
                return true;
            });




        }

        public async Task<bool> ChangeBrandStatus(long id, EStatus status, long UserId = 0)
        {
            try
            {
                var brand = await _brandRepository.Query()
                          .Include(x => x.BrandDetails)
                          .Include("BrandDetails.Culture")
                          .Include("BrandDetails.SlugInfo")
                          .FirstOrDefaultAsync(x => x.Id == id);

                await ValidateStatusAndPublishDate_ExpireDate(status, brand.PublishDate, brand.ExpiryDate);

                foreach (var item in brand.BrandDetails)
                {


                    SlugManagementVm slugManagementVm = new()
                    {
                        CultureId = item.CultureId,
                        Slug = item.Slug,
                        Entity = EEntity.Product,
                        SiteId = brand.SiteId

                    };
                    if (brand == null)
                    {
                        return false;
                    }
                    if (status == EStatus.Deleted && string.IsNullOrEmpty(item.Slug))
                    {
                        item.Slug = brand.Id + "Deleted" + item.Slug;
                        item.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);

                    }
                    else if (brand.Status == EStatus.Deleted)
                    {
                        if (status == EStatus.Unpublished || status == EStatus.Published ||status==EStatus.Active)
                        {
                            item.Slug = item.Slug.Split("Deleted").LastOrDefault();
                            item.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                        }

                    }
                }
                brand.Status = status;
                brand.ModifiedById = UserId;
                brand.ModificationDate = DateTimeOffset.Now;
                await _brandRepository.SaveChangesAsync();
                var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
              
                    if (IsMeliEnabled&&( status == EStatus.Deleted || status == EStatus.Unpublished))
                {
                    //var elasticId = $"{id}-{nameof(EEntity.Brand)}";
                    //await _searchRepository.DeleteDocuemnt(elasticId);

                    //meiliSearch
                    var MeiliId = $"{id}-{nameof(EEntity.Brand)}";
                    await _meiliRepo.DeleteDocuemnt(MeiliId, nameof(EEntity.Brand));
                }

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<BrandViewModel> GetBrandById(long id)
        {

            try
            {
                var brand = await _brandRepository.Query()
                    .Where(x => x.Id == id)
                   .Select(x => new BrandViewModel
                   {
                       Id = x.Id,
                       PublicUrl = x.Thumbnail.PublicUrl,
                       Status = x.Status,
                       ThumbnailId = x.ThumbnailId,
                       ExpiryDate=x.ExpiryDate,
                       PublishDate=x.PublishDate,
                       brandDetails = x.BrandDetails.Select(x => new BrandDetailsViewModel
                       {

                           Description = x.Description,
                           BrandTemplateId = x.BrandTemplateId,
                           CultureCode = x.Culture.CultureId,
                           Name = x.Name,
                           CultureId = x.CultureId,
                           Slug = x.SlugInfo.Slug,
                           BrandId = x.Brand.Id,
                           Id = x.Id
                       }).ToList()


                   }).FirstOrDefaultAsync();

                return brand;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        public async Task<List<BrandForProductViewModel>> GetBrands(long siteId)
        {
            try
            {
                var Query = await _brandRepository
                                     .Query()
                                     .Include(x=>x.BrandDetails)
                                     .Where(x =>
                                      x.SiteId == siteId
                                
                                      && x.Status != EStatus.Deleted && x.Status != EStatus.Unpublished)
                                     .Select(x => new BrandForProductViewModel
                                     {
                                         Id= x.Id,
                                         Name = x.BrandDetails.OrderByDescending(x=>x.CultureId).FirstOrDefault().Name
                                        
                                     }).ToListAsync();


                return Query;
            }
            catch (Exception ex)
            {

                throw ex;

            }
        }




        public async Task<List<CatelogFlatViewModel>> GetAllBrandFlat(long CultureId, long Siteid)
        {
            try
            {
                var query = await _brandRepository.Query()
                                                  .Include(x => x.BrandDetails)
                                                  .ThenInclude(x=>x.SlugInfo)
                                                  .Include(x=>x.Thumbnail)
                                                  .Where(x => x.Status == EStatus.Published && x.SiteId == Siteid)
                                                  .SelectMany(x => x.BrandDetails)
                                                  .Where(x=>CultureId == 0 || x.CultureId == CultureId)
                                                  .Select(x => new CatelogFlatViewModel
                                                  {
                                                      Id = x.Id,
                                                      Name = x.Name,
                                                      Slug = x.SlugInfo.Slug==null?"": x.SlugInfo.Slug,
                                                      PublicUrl=x.Brand.Thumbnail==null?"": x.Brand.Thumbnail.PublicUrl,
                                                      Description=x.Description
                                                  }).ToListAsync();

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async Task<bool> ValidateStatusAndPublishDate_ExpireDate(EStatus status, DateTimeOffset? publishDate, DateTimeOffset? unPublishDate)
        {

            if (status == EStatus.Published && unPublishDate.HasValue && unPublishDate.Value < DateTimeOffset.Now)
            {
                throw new Exception("Please Add a valid date for unpublish date");
            }

            if (status == EStatus.Unpublished && publishDate.HasValue && publishDate.Value < DateTimeOffset.Now)
            {
                throw new Exception("Please Add a valid date for publish date");
            }

            if (publishDate.HasValue && unPublishDate.HasValue && (unPublishDate.Value < publishDate.Value || unPublishDate.Value == publishDate.Value))
            {
                throw new Exception("Please Add a valid range for publish and unpublish date");
            }

            return true;
        }
    }

}


