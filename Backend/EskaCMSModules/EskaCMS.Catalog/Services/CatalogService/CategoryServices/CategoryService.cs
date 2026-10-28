using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity.Catalog;
using System;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.ViewModels.Category;
using EskaCMS.Core.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using EskaCMS.Infrastructure;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.Response;

using EskaCMS.Media.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Core.Cache;
using EskaCMS.Entities.Entity.Inventory;
using Microsoft.Extensions.Configuration;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.CategoryServices
{
    public class CategoryService : ICategoryService
    {
        private const string CategoryEntityTypeId = "Category";

        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IEntityService _entityService;
        private readonly IWorkContext _workContext;
        private readonly IMapper mapper;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<CategoryDetails> _categoryDetails;
        private readonly EskaCMS.Media.Services.Interfaces.IMediaService _mediaService;
        private readonly ISearchRepository<NaviagtableSearchModel> _searchRepository;
        private readonly IClientCacheService clientCacheService;
        private readonly ICacheService cacheService;
        private readonly ISlugServices _slugServices;
        private readonly IRepository<CategoryWarehouse> categoryWarehouseRepository;
        private readonly IRepository<Warehouse> _warehouses;
        private readonly ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> _meiliRepo;
        private readonly IConfiguration _config;

        public CategoryService(
            IRepository<Category> categoryRepository,
            IEntityService entityService,
            IRepository<Product> productRepository,
            ISlugServices slugServices,
            IRepository<CategoryWarehouse> categoryWarehouseRepository,
            IWorkContext workContext,
             IMapper mapper,
            EskaDCMSDbContext context,
            IRepository<CategoryDetails> CategoryDetails,
            IRepository<ProductCategory> productCategoryRepository,
            EskaCMS.Media.Services.Interfaces.IMediaService mediaService,
            ISearchRepository<NaviagtableSearchModel> searchRepository,
            IRepository<Warehouse> warehouses,
        IClientCacheService clientCacheService,
         ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> meiliRepo,
        ICacheService cacheService,
        IConfiguration config)
        {
            _categoryRepository = categoryRepository;
            _entityService = entityService;
            _productRepository = productRepository;
            _slugServices = slugServices;
            this.categoryWarehouseRepository=categoryWarehouseRepository;
            _workContext = workContext;
            this.mapper = mapper;
            _context = context;
            _categoryDetails = CategoryDetails;
            _productCategoryRepository = productCategoryRepository;
            _mediaService = mediaService;
            _searchRepository = searchRepository;
            this.clientCacheService = clientCacheService;
            this.cacheService = cacheService;
            _warehouses = warehouses;
            _meiliRepo = meiliRepo;
            _config = config;
        }

        public async Task<long> AddCategory(CategoryRequestViewModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    long SiteId = _workContext.GetCurrentSiteId();

                    var Category = new Category();

                    Category.SiteId = SiteId;
                    Category.CategoryTypeId = model.CategoryTypeId;
                    Category.CreatedById = UserId;
                    Category.IncludeInMenu = model.IncludeInMenu;
                    Category.StatusDate = DateTime.Now;
                    Category.CreationDate = DateTime.Now;
                    Category.ParentId = model.ParentId == 0 ? null : model.ParentId;
                    Category.ThumbnailImageId = model.ThumbnailImageId == 0 || model.ThumbnailImageId == null ? null : model.ThumbnailImageId;
                    Category.CoverImageId = model.CoverImageId.HasValue ? model.CoverImageId.Value : null;
                    Category.DisplayOrder = model.DisplayOrder;
                    Category.Status = model.Status;

                    Category.PublishDate = model.PublishDate;
                    Category.ExpiryDate = model.ExpiryDate;
                    Category.IsFeatured = model.IsFeatured;
                    Category.IsPromotional = model.IsPromotional;
                    await ValidateStatusAndPublishDate_ExpireDate(Category.Status, Category.PublishDate, Category.ExpiryDate);

                    _categoryRepository.Add(Category);
                    await _categoryRepository.SaveChangesAsync();
           //         var AddDocumant = await _meiliRepo.AddDocument(EskaCMS.Infrastructure.Enums.EEntity.Category,   Category );

                    await insertCategoryDetails(model.CategoryDetails, Category.Id);
                    await insertCategoryWarehouse(model.WarehouseIds, Category.Id);

                    await transaction.CommitAsync();
                    return Category.Id;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    throw new Exception(ex.Message.Replace("Category: ", ""));
                }
            });
        }

        private async Task insertCategoryWarehouse(IList<long> warehouseIds, long id)
        {
            try
            {
                var categoryWarehousesToBeDeleted = await categoryWarehouseRepository.Query().Where(x => x.CategoryId==id).ToListAsync();
                categoryWarehouseRepository.RemoveRange(categoryWarehousesToBeDeleted);
                await categoryWarehouseRepository.SaveChangesAsync();   

                if (warehouseIds!=null &&warehouseIds.Any())
                {
                    var objCategory = warehouseIds.Select(x => new CategoryWarehouse
                    {
                        CategoryId=id,
                        WarehouseId=x

                    }).ToList();
                    categoryWarehouseRepository.AddRange(objCategory);
                    await categoryWarehouseRepository.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {

                throw;
            }
        }


        public async Task<bool> UpdateCategory(CategoryRequestViewModel model)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                long SiteId = _workContext.GetCurrentSiteId();

                Category Category = await _categoryRepository.Query().Include(x=>x.CategoryWarehouses).Include(x => x.CategoryDetails).ThenInclude(x => x.SlugInfo).Where(x => x.Id == model.Id).FirstOrDefaultAsync();

                Category.SiteId = SiteId;
                Category.CategoryTypeId = model.CategoryTypeId;
                Category.CreatedById = UserId;
                Category.IncludeInMenu = model.IncludeInMenu;
                Category.StatusDate = DateTime.Now;
                Category.CreationDate = DateTime.Now;
                Category.ParentId = model.ParentId == 0 ? null : model.ParentId;
                Category.ThumbnailImageId = model.ThumbnailImageId == 0 || model.ThumbnailImageId == null ? null : model.ThumbnailImageId;
                //  var warehouse = await _warehouses.Query().Where(x =>model.WarehouseIds!=null && model.WarehouseIds.Contains(x.Id)).ToListAsync();

                categoryWarehouseRepository.RemoveRange(Category.CategoryWarehouses);
                if (model.WarehouseIds!=null)
                {
                    Category.CategoryWarehouses = model.WarehouseIds.Select(x => new CategoryWarehouse
                    {
                        CategoryId = Category.Id,
                        WarehouseId = x
                    }).ToList();
                }


                Category.CoverImageId = model.CoverImageId;

                Category.DisplayOrder = model.DisplayOrder;
                Category.Status = model.Status;
                Category.PublishDate = model.PublishDate;
                Category.ExpiryDate = model.ExpiryDate;
                Category.IsFeatured = model.IsFeatured;
                Category.IsPromotional = model.IsPromotional;

                await ValidateStatusAndPublishDate_ExpireDate(Category.Status, Category.PublishDate, Category.ExpiryDate);
                await _categoryRepository.SaveChangesAsync();

                await UpdateCategoryDetails(model.CategoryDetails, Category, Category.Id);

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> ChangeStatus(long CatId, EStatus Status)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var UserId = await _workContext.GetCurrentUserId();
                var Category = await _categoryRepository
                                .Query()
                                .Where(x => x.Id == CatId)
                                .Include(x => x.CategoryDetails)
                                .ThenInclude(x => x.SlugInfo)
                                .FirstOrDefaultAsync();


                Category.ModifiedById = UserId;
                Category.ModificationDate = DateTimeOffset.Now;
                Category.Status = Status;

                await ValidateStatusAndPublishDate_ExpireDate(Category.Status, Category.PublishDate, Category.ExpiryDate);
                foreach (var category in Category.CategoryDetails)
                {
                    if (category.SlugInfo != null)

                    {
                        var SlugManagement = new SlugManagementVm
                        {



                            Entity = EEntity.Category,
                            Id = category.SlugInfoId.Value,
                            CultureId = category.CultureId,
                            SiteId = SiteId
                        };
                        if (Status == EStatus.Deleted && category.SlugInfoId != null)
                        {

                            SlugManagement.Slug = Category.Id + "Deleted" + category.SlugInfo.Slug;


                            await _slugServices.Upsert(SlugManagement);


                        }
                        else if (Status != EStatus.Deleted && category.SlugInfoId != null)
                        {



                            SlugManagement.Slug = category.SlugInfo.Slug.Split("Deleted").LastOrDefault();



                            category.SlugInfoId = await _slugServices.Upsert(SlugManagement);

                        }
                    }
                    await _categoryRepository.SaveChangesAsync();

                }
                var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
             
                    if (IsMeliEnabled&&(Status == EStatus.Deleted || Status == EStatus.Unpublished))
                {
                    //var elasticId = $"{CatId}-{nameof(EEntity.Category)}";
                    //await _searchRepository.DeleteDocuemnt(elasticId);
                    //MeiliSearch
                    var MeiliId = $"{CatId}-{nameof(EEntity.Category)}";
                    await _meiliRepo.DeleteDocuemnt(MeiliId, nameof(EEntity.Category));
                }

                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<PaginationResult<CategoryAdminViewModel>> GetAllCategories(CategoryParamsViewModel model)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var Query = await _categoryRepository.Query()
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.Status != EStatus.Deleted && x.SiteId == SiteId && (long)model.eCategoryTypes == x.CategoryTypeId)
                .Include(x => x.CategoryDetails)
                .Where(x => string.IsNullOrEmpty(model.SearchText) || x.CategoryDetails.Any(y => y.Name.Contains(model.SearchText.ToLower())))
                .Include(x => x.ThumbnailImage)
                .Include("CategoryDetails.Culture")
                .Include("CategoryDetails.SlugInfo")
                .Include(x => x.Parent)
                .ThenInclude(x => x.CategoryDetails)
                .ProjectTo<CategoryAdminViewModel>(mapper.ConfigurationProvider).OrderByDescending(x => x.Id).PaginateAsync(model.Pagination.PageSize, model.Pagination.PageNumber);



            return Query;
        }
        public List<object> GetCategoriesType()
        {
            try
            {


                var enumVals = new List<object>();

                foreach (var item in Enum.GetValues(typeof(ECategoryTypes)))
                {

                    enumVals.Add(new
                    {
                        id = (int)item,
                        name = item.ToString()
                    });
                }

                return enumVals;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<AllCategoryViewModel>> GetCategoriesbySiteId(CategorySearchSiteViewModel model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var query = _categoryRepository.Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Include(x => x.CategoryDetails)
                    .Include("CategoryDetails.Culture")
                    .Include("CategoryDetails.SlugInfo")
                    .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted);
                if (!string.IsNullOrEmpty(model.Name))
                {
                    query = query.Where(x => x.CategoryDetails.Any(x => x.Name.ToLower().Contains(model.Name.ToLower())));
                }

                if (model.CategoryTypedId.Count() > 0)
                {
                    query = query.Where(x => model.CategoryTypedId.Any(y => y == x.CategoryTypeId));
                }



                var Result = await query.Select(x => new AllCategoryViewModel
                {

                    CategoryTypeId = x.CategoryTypeId,
                    Status = x.Status,
                    Id = x.Id,
                    IsFeatured = x.IsFeatured,
                    IsPromotional = x.IsPromotional,
                    CategoryDetails = x.CategoryDetails.Where(x => model.CultureId == 0 || model.CultureId == null || x.CultureId == model.CultureId).Select(x => new Areas.Catalog.ViewModels.Category.Responses.CategoryDetailsViewModel
                    {
                        Id = x.Id,
                        Culture = x.Culture,
                        Name = x.Name,
                        SlugInfo = x.SlugInfo
                    }).ToList()
                }).OrderByDescending(x => x.Id).PaginateAsync(model.Pagination.PageSize, model.Pagination.PageNumber);


                return Result;
            }

            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<CategoryAdminViewModel> GetCategoriesById(long CategoryId)
        {
            try
            {
                var Result = await _categoryRepository
                                        .Query()
                                        .Where(x => x.Id == CategoryId)
                                        .Include(x => x.ThumbnailImage)
                                        .Include(x => x.CategoryWarehouses)
                                        .Include(x => x.CategoryDetails)
                                        .Include("CategoryDetails.Culture")
                                        .Include("CategoryDetails.SlugInfo")
                                        .AsNoTrackingWithIdentityResolution()
                                        .ProjectTo<CategoryAdminViewModel>(mapper.ConfigurationProvider)
                                        .OrderByDescending(x => x.Id)
                                        .FirstOrDefaultAsync();


                return Result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<List<CategoryAdminViewModel>> GetCategoriesByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false)
        {
            try
            {
                var Query = await _categoryRepository.Query()
                                 .AsNoTrackingWithIdentityResolution()
                                .Include(c => c.CategoryDetails)
                                .Include(x => x.ThumbnailImage)
                                .Include("CategoryDetails.Culture")
                                .Include("CategoryDetails.SlugInfo")
                                .Where(x => x.CategoryTypeId == (long)CategoryTypeId
                                && (ParentId.HasValue ? x.ParentId == ParentId : true)
                                && (x.SiteId == SiteId || SiteId == 0 || WithShared)
                                && x.Status != EStatus.Deleted)
                                .Where(x => x.CategoryDetails.Any(x => CultureId == null || CultureId == 0 || x.CultureId == CultureId))
                                .ProjectTo<CategoryAdminViewModel>(mapper.ConfigurationProvider).ToListAsync();
                return Query;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<CategoryAdminViewModel>> GetCategoriesTreeByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false)
        {
            try
            {

                var Category = await _categoryRepository
                                        .Query()
                                        .AsNoTrackingWithIdentityResolution()
                                        .Where(x => x.CategoryTypeId == (long)CategoryTypeId
                                         && (ParentId == 0 || x.ParentId == ParentId)
                                        && (x.SiteId == SiteId || SiteId == 0 || WithShared)
                                        && x.Status != EStatus.Deleted && x.Status != EStatus.Inactive)
                                        .Include(x => x.CategoryDetails)
                                        .Include(x => x.ThumbnailImage)
                                        .Include("CategoryDetails.SlugInfo")
                                        .Include("CategoryDetails.Culture")
                                        .ProjectTo<CategoryAdminViewModel>(mapper.ConfigurationProvider).OrderByDescending(x => x.Id).ToListAsync();


                return Category;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<CategoryAdminViewModel>> GetCategoryByParentId(long parentId)
        {
            try
            {
                var Categories = await _categoryRepository.Query()
                                                                .AsNoTrackingWithIdentityResolution()

                                                                .Include(x => x.CategoryDetails)
                                                                .Include(x => x.ThumbnailImage)
                                                                .Include("CategoryDetails.SlugInfo")
                                                                .Include("CategoryDetails.Culture")
                                                                .Where(x => x.Status != EStatus.Deleted && parentId == x.ParentId)
                                                                .ProjectTo<CategoryAdminViewModel>(mapper.ConfigurationProvider).OrderByDescending(x => x.Id).ToListAsync();



                return Categories;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> ChangeCategoryStatusByParentId(long ParentId, EStatus status)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var Transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    var Category = await _categoryRepository
                            .Query()
                            .Where(x => x.ParentId == ParentId)
                            .Include(x => x.CategoryDetails)
                            .ThenInclude(x => x.SlugInfo)
                            .ToListAsync();
                    foreach (var category in Category)
                    {
                        foreach (var categorydetails in category.CategoryDetails)
                        {


                            if (status == EStatus.Deleted && categorydetails.SlugInfoId != null)
                            {
                                SlugManagementVm slugManagementVm = new()
                                {
                                    Id = categorydetails.SlugInfoId.Value,
                                    CultureId = categorydetails.CultureId,
                                    Slug = categorydetails.Id + "Deleted" + categorydetails.SlugInfo.Slug,
                                    Entity = EEntity.Category,

                                };


                                await _slugServices.Upsert(slugManagementVm);


                            }
                            else if (category.Status != EStatus.Deleted && categorydetails.SlugInfoId != null)
                            {

                                SlugManagementVm slugManagementVm = new()
                                {
                                    Id = categorydetails.SlugInfoId.Value,
                                    CultureId = categorydetails.CultureId,
                                    Slug = categorydetails.SlugInfo.Slug.Split("Deleted").LastOrDefault(),
                                    Entity = EEntity.Category,

                                };

                                categorydetails.SlugInfoId = await _slugServices.Upsert(slugManagementVm);

                            }

                            category.Status = status;
                            category.ModifiedById = UserId;
                            category.ModificationDate = DateTimeOffset.Now;

                        }
                    }

                    await _categoryRepository.SaveChangesAsync();
                    await Transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await Transaction.RollbackAsync();

                    throw ex;
                }
            });
        }
        public async Task<List<CategoryListVm>> GetCategories()
        {
            var SiteId = _workContext.GetCurrentSiteId();
            try
            {
                var Query = await _categoryRepository
                                              .Query()
                                              .AsNoTrackingWithIdentityResolution()
                                              .Where(x => x.SiteId == SiteId
                                               && x.Status != EStatus.Deleted
                                               && x.Status != EStatus.Unpublished
                                               && x.CategoryTypeId == (long)ECategoryTypes.Product)
                                              .Include(x => x.CategoryDetails)
                                              .Include("CategoryDetails.Culture")
                                              .ProjectTo<CategoryListVm>(mapper.ConfigurationProvider).OrderByDescending(x => x.Id).ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<CatelogFlatViewModel>> GetAllCategoriesFlat(long CultureId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var query = await _categoryRepository.Query()
                                                .AsNoTrackingWithIdentityResolution()
                                                .Include(x => x.CategoryDetails)
                                                .Include("CategoryDetails.SlugInfo")
                                                .Where(x =>
                                                x.Status != EStatus.Deleted
                                                && x.SiteId == SiteId)
                                                .SelectMany(x => x.CategoryDetails)
                                                .Where(x => CultureId == 0 || x.CultureId == CultureId)
                                                .Select(x => new CatelogFlatViewModel
                                                {
                                                    Id = x.CategoryId,
                                                    Name = x.Name,
                                                    Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug,
                                                    ParentId = x.Category.ParentId.HasValue ? x.Category.ParentId : null,
                                                }).ToListAsync();

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<SmartTableResult<ProductSmartTableVm>> GetProducts(long id, SmartTableParam param)
        {
            try
            {

                var query = _productCategoryRepository.Query().Include(x => x.Product).ThenInclude(x => x.ProductDetails)
                    .Where(x => x.CategoryId == id && x.Product.Status != EStatus.Deleted && x.Product.IsVisibleIndividually);

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Name != null)
                    {
                        string name = search.Name;
                        query = query.Where(x => x.Product.ProductDetails.Any(x => x.Name.Contains(name)));
                    }


                }

                var gridData = query.ToSmartTableResult(
                    param,
                    x => new ProductSmartTableVm
                    {
                        Id = x.Id,
                        ProductName = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                        IsFeaturedProduct = x.Product.IsFeatured,
                        DisplayOrder = x.DisplayOrder,
                    });
                return gridData;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateProductCategory(long id, ProductCategoryForm model)
        {
            try
            {
                var productCategory = await _productCategoryRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
                productCategory.Product.IsFeatured = model.IsFeaturedProduct;
                productCategory.DisplayOrder = model.DisplayOrder;

                await _productCategoryRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region Helpers Function
        private async Task<bool> insertCategoryDetails(IList<ViewModels.Category.CategoryDetailsViewModel> categoryDetails, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();


                foreach (var category in categoryDetails)
                {
                    var Category = category.Id != 0 ? await _categoryDetails.Query().Where(x => x.Id == category.Id).FirstOrDefaultAsync()
                                : new CategoryDetails();

                    var slugInfo = new SlugManagementVm
                    {

                        CultureId = category.CultureId,
                        Entity = EEntity.Category,
                        SiteId = SiteId,
                        Slug = category.Slug

                    };

                    Category.CategoryId = id;
                    Category.Name = category.Name;
                    Category.Description = category.Description;
                    Category.MetaTitle = category.MetaTitle;
                    Category.MetaDescription = category.MetaDescription;
                    Category.Description = category.Description;
                    Category.CategoryTemplateId = category.CategoryTemplateId == 0 || category.CategoryTemplateId == null ? null : category.CategoryTemplateId;
                    Category.CultureId = category.CultureId == 0 ? null : category.CultureId;
                    Category.SlugInfoId = await _slugServices.UpsertAndGetId(slugInfo);


                    _categoryDetails.Add(Category);

                    await _categoryDetails.SaveChangesAsync();



                }




                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> UpdateCategoryDetails(IList<ViewModels.Category.CategoryDetailsViewModel> CategoryDetails, Category category, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();


                foreach (var categorydetails in CategoryDetails)
                {
                    var existingcategoryDetails = category.CategoryDetails?.FirstOrDefault(pro => pro.CultureId == categorydetails.CultureId);

                    var slugInfo = new SlugManagementVm
                    {
                        CultureId = categorydetails.CultureId,
                        Entity = EEntity.Category,
                        SiteId = SiteId,
                        Slug = categorydetails.Slug
                    };
                    if (existingcategoryDetails != null)
                    {

                        cacheService.RemoveByPrefix("Slug" + existingcategoryDetails.SlugInfo.Slug + existingcategoryDetails.CultureId + SiteId);
                        await clientCacheService.RemoveCacheFile(existingcategoryDetails.SlugInfo.Slug, SiteId, existingcategoryDetails.CultureId ?? 0);
                        slugInfo.Id = existingcategoryDetails.SlugInfoId.Value;
                        existingcategoryDetails.Name = categorydetails.Name;
                        existingcategoryDetails.Description = categorydetails.Description;
                        existingcategoryDetails.MetaTitle = categorydetails.MetaTitle;
                        existingcategoryDetails.MetaDescription = categorydetails.MetaDescription;
                        existingcategoryDetails.Description = categorydetails.Description;
                        existingcategoryDetails.CategoryTemplateId = categorydetails.CategoryTemplateId == 0 || categorydetails.CategoryTemplateId == null ? null : categorydetails.CategoryTemplateId;
                        existingcategoryDetails.CultureId = categorydetails.CultureId == 0 ? null : categorydetails.CultureId;
                        existingcategoryDetails.SlugInfoId = await _slugServices.UpsertAndGetId(slugInfo);
                        await _categoryDetails.SaveChangesAsync();
                        await _categoryRepository.SaveChangesAsync();

                    }
                    else
                    {
                        var addnewCategoryDetails = new CategoryDetails();
                        addnewCategoryDetails.CategoryId = id;
                        addnewCategoryDetails.Name = categorydetails.Name;
                        addnewCategoryDetails.Description = categorydetails.Description;
                        addnewCategoryDetails.MetaTitle = categorydetails.MetaTitle;
                        addnewCategoryDetails.MetaDescription = categorydetails.MetaDescription;
                        addnewCategoryDetails.Description = categorydetails.Description;
                        addnewCategoryDetails.CategoryTemplateId = categorydetails.CategoryTemplateId == 0 || categorydetails.CategoryTemplateId == null ? null : categorydetails.CategoryTemplateId;
                        addnewCategoryDetails.CultureId = categorydetails.CultureId == 0 ? null : categorydetails.CultureId;
                        addnewCategoryDetails.SlugInfoId = await _slugServices.UpsertAndGetId(slugInfo);

                        _categoryDetails.Add(addnewCategoryDetails);
                        await _categoryDetails.SaveChangesAsync();

                    }

                }
                await _categoryRepository.SaveChangesAsync();


                return true;

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

        #endregion

    }
}
