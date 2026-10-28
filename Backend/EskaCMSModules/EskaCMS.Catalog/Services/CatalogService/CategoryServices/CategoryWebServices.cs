using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.CategoryServices
{
    public class CategoryWebServices : ICategoryWebServices
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IUserSiteService userSiteService;
        private readonly IWorkContext _workContext;
        private readonly IMapper mapper;
        private readonly IMediator _mediator;
        private readonly IRepository<ProductCategory> _productCategoryRepository;

        public CategoryWebServices(IRepository<Category> CategoryRepository,

            IUserSiteService userSiteService,
            IWorkContext workContext,
                         IMapper mapper,
                         IMediator mediator,
            IRepository<ProductCategory> ProductCategoryRepository
           )
        {
            _categoryRepository = CategoryRepository;
            this.userSiteService = userSiteService;
            _workContext = workContext;
            this.mapper = mapper;
            _productCategoryRepository = ProductCategoryRepository;
            _mediator = mediator;
        }

        public async Task<CategoryWebViewModel> GetById(long Id)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();

                var category = await _categoryRepository.Query()
                                                        .Include(x => x.CategoryDetails)
                                                        .Include("CategoryDetails.SlugInfo")
                                                        .Include(x => x.ThumbnailImage)
                                                        .Where(x => x.Id == Id && x.CategoryDetails.Any(x => x.CultureId == CultureId))
                                                        .ProjectTo<CategoryWebViewModel>(mapper.ConfigurationProvider)
                                                        .FirstOrDefaultAsync();


                return category;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<CategoryWebViewModel>> GetCategoriesByIds(List<long> ids, long? SiteId)
        {
            var CultureId = _workContext.GetCurrentCultureId();
            var gridData = await _categoryRepository.Query()
                .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId && x.CategoryTypeId == (long)ECategory.Ecommerce)
                .Where(x => ids.Count() <= 0 || ids.Any(u => u == x.Id))
                .ProjectTo<CategoryWebViewModel>(mapper.ConfigurationProvider)
                .AsNoTrackingWithIdentityResolution()
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return gridData;
        }
        public async Task<List<CategoryWebResponseViewModel>> GetCategoriesNavigator(CategoryWebVM Model)
        {
            try
            {

                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                var warehouse =await _workContext.GetCurrentWarehouseId();
                var AllCategoriesQuery = _categoryRepository.Query()
                   .Include(x=>x.CategoryWarehouses)
                .Where(x =>
                    x.SiteId == SiteId &&
                    x.Status == EStatus.Published &&
                    x.CategoryTypeId == Model.CategoryTypeId &&
                    x.CategoryDetails.Any(cd => cd.CultureId == CultureId));

                if (warehouse != null && warehouse > 0)
                {

                    AllCategoriesQuery = AllCategoriesQuery.Where(x =>x.CategoryWarehouses.Count()==0 || x.CategoryWarehouses.Any(y => y.WarehouseId == warehouse));
                }
               var  AllCategories = await AllCategoriesQuery.Where(x=>x.Status==EStatus.Published).Include(x => x.Parent).ThenInclude(x => x.CategoryDetails)
                .Include(x => x.CategoryDetails).ThenInclude(cd => cd.SlugInfo)
                .Include(x => x.ThumbnailImage)
                .Include(x => x.CoverImage)
                .Include(x => x.Children).ThenInclude(x => x.CategoryDetails)
                .Include(x => x.Children).ThenInclude(x => x.CategoryWarehouses)
                .AsNoTracking() 
                .ToListAsync();

                var AllSubCategories = AllCategories.Where(x => x.ParentId != null).ToList();


                Model.ParentIds = Model.ParentIds == null || Model.ParentIds.Count() == 0 ? AllCategories.Where(x => x.ParentId == null).Select(x => x.Id).ToArray() : Model.ParentIds;

                var ProductCategory = await _productCategoryRepository.Query()
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductAvaliablity)
                    .ThenInclude(x => x.Country)
                    .Where(x => x.Product.Status == EStatus.Published)
                    .Select(x => new CategoryFeatureVM
                    {
                        CategoryId = x.CategoryId,
                        CategoryIdWithFeatured = x.Product.IsFeatured
                    })
                .Distinct()
                .AsNoTrackingWithIdentityResolution()
                .ToListAsync();


                var productCategoryIds = ProductCategory.Select(x => x.CategoryId).ToHashSet();
                var featuredCategoryIds = ProductCategory
                    .Where(x => x.CategoryIdWithFeatured == Model.ContainsFeaturedProduct)
                    .Select(x => x.CategoryId)
                    .ToHashSet();
                var searchValue = Model.SearchValue?.ToLower();

                var Categories = AllCategories
                    .Where(x => Model.ParentIds.Contains(x.Id))
                    .Select(x =>
                    {
                        var detail = x.CategoryDetails?.FirstOrDefault(d => d.CultureId == CultureId);
                        var slug = detail?.SlugInfo?.Slug ?? "";

                        return new CategoryWebResponseViewModel(Model.ActiveIds)
                        {
                            Id = x.Id,
                            DisplayOrder = x.DisplayOrder,
                            PublicUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
                            CoverImageUrl = x.CoverImage == null ? "" : x.CoverImage.PublicUrl,
                            HasProduct = productCategoryIds.Contains(x.Id),
                            TopParentId = x.ParentId.HasValue ? x.ParentId.Value : x.Id,
                            TopParentPublicURL = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,

                            Name = detail?.Name ?? "",
                            Slug = slug,
                            TopParentSlug = slug,
                            TopParentTitle = detail?.Name ?? "",
                            TopParentDescription = detail?.Description ?? "",
                            Children = GetChildrenForCategories(
                                x.Id,
                                AllSubCategories,
                                productCategoryIds,
                                slug,
                                x.Id,
                                x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
                                Model,
                                featuredCategoryIds,
                                searchValue,
                                CultureId,
                                warehouse),
                            ContainsFeaturedProducts = featuredCategoryIds.Contains(x.Id) || (x.Children?.Any(c => featuredCategoryIds.Contains(c.Id)) ?? false)
                        };
                    })
                    .Where(x => Model.ContainsFeaturedProduct == null || x.ContainsFeaturedProducts == Model.ContainsFeaturedProduct || x.Children != null && x.Children.Any())
                    .ToList();

                var result = Categories;


                var IsLocalCountry = await userSiteService.IsLocalCountry();

                result = await ReturnCategoriesByCountry(result, IsLocalCountry);

                var allRecordCount = result.Where(x => x.Children != null && x.Children.Any()).Sum(x => x.Children.Count) + result.Count;


                await _mediator.Publish(new SearchQuery { SearchText = Model.SearchValue, ResultCount = allRecordCount, SiteId = SiteId });

                return result.OrderBy(x => x.DisplayOrder).ToList();


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<List<CategoryWebResponseViewModel>> ReturnCategoriesByCountry(List<CategoryWebResponseViewModel> Categories, bool IsLocalCountry)
        {
            try
            {
                var UserCountry = await userSiteService.GetUserCountry();
                var filteredCategories = Categories
                    .Where(category => category.ContainsFeaturedProducts == null || !category.ContainsFeaturedProducts.Value)
                    .ToList();

                var categoryIdsWithFeaturedProducts = filteredCategories
                    .Where(category => category.ContainsFeaturedProducts.HasValue && category.ContainsFeaturedProducts.Value)
                    .Select(category => category.Id)
                    .ToList();

                var productCategories = await _productCategoryRepository.Query()
                    .AsNoTracking()
                    .Include(pc => pc.Product)
                    .ThenInclude(x => x.ProductAvaliablity)
                    .ThenInclude(x => x.Country)

                    .Where(pc => categoryIdsWithFeaturedProducts.Contains(pc.Id))
                    .ToListAsync();

                var ProductsDependCountry = productCategories
                         .Where(pc => pc.Product.ProductAvaliablity.Count() == 0 || pc.Product.ProductAvaliablity
                         .Any(i => i.CountryId == UserCountry.Id && i.Permission == EProductCountryPermission.Allowed))
                         .ToList();
                if (IsLocalCountry)
                {
                    ProductsDependCountry = ProductsDependCountry
                                               .Where(pc => pc.Product.ForeignPrice.HasValue)
                                               .ToList();
                }

                var CategoryIds = ProductsDependCountry.Select(pc => pc.CategoryId)
                                                      .Distinct()
                                                      .ToList();

                filteredCategories.AddRange(Categories.Where(category => CategoryIds.Contains(category.Id)));

                return filteredCategories;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<CategoryWebViewModel> GetCategoriesById(long CategoryId)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var Result = await _categoryRepository
                                        .Query()
                                        .AsNoTrackingWithIdentityResolution()
                                        .Where(x => x.Id == CategoryId)
                                        .Include(x => x.ThumbnailImage)
                                        .Include(x => x.CategoryDetails)
                                        .Include("CategoryDetails.Culture")
                                        .Include("CategoryDetails.SlugInfo")
                                        .Where(x => x.CategoryDetails.Any(x => x.CultureId == CultureId))
                                        .ProjectTo<CategoryWebViewModel>(mapper.ConfigurationProvider).FirstOrDefaultAsync();
                return Result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<List<CategoriesBreadCrumbVM>> GetCategoryBreadCrumb(long CategoryId)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var TargetCategory = await _categoryRepository.Query()
                                         .AsNoTracking()
                                         .Where(x => x.Id == CategoryId && x.SiteId == SiteId)
                                         .Include(x => x.CategoryDetails)
                                         .Include("CategoryDetails.SlugInfo")
                                         .Select(x => new CategoriesBreadCrumbVM
                                         {
                                             CategoryId = x.Id,

                                             ParentId = x.ParentId ?? 0,

                                             Name = x.CategoryDetails == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                                             Slug = x.CategoryDetails == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().SlugInfo == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().SlugInfo.Slug,

                                         }).FirstOrDefaultAsync();


                var CategoryList = await _categoryRepository.Query()
                                         .AsNoTracking()
                                         .Where(c => c.Status == EStatus.Published && c.SiteId == SiteId)


                                         .Include(x => x.CategoryDetails)
                                         .Include("CategoryDetails.SlugInfo").Select(x => new CategoriesBreadCrumbVM
                                         {
                                             CategoryId = x.Id,

                                             ParentId = x.ParentId ?? 0,
                                             Name = x.CategoryDetails == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                                             Slug = x.CategoryDetails == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().SlugInfo == null ? "" : x.CategoryDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().SlugInfo.Slug,

                                         }).ToListAsync();


                var CategoryBreadCrumb = await GetBreadCrumb(TargetCategory, CategoryList, new List<CategoriesBreadCrumbVM>());
                CategoryBreadCrumb.Reverse();
                return CategoryBreadCrumb;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<long>> GetCategoriesByBrandId(long brandId)
        {
            try
            {
                var Query = await _productCategoryRepository.Query()

                                             .Include(x => x.Product)
                                             .ThenInclude(x => x.Brand)
                                             .Where(x => x.Product.Status != EStatus.Deleted)
                                             .Where(x => x.Product.Brand.Id == brandId)
                                             .Select(x => x.CategoryId).ToListAsync();


                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        #region Helpers

        private List<CategoryWebResponseViewModel> GetChildrenForCategories(
            long parentId,
            List<Category> subcategories,
            HashSet<long> productCategoryIds,
            string topParentSlug,
            long topParentId,
            string topParentPublicURL,
            CategoryWebVM model,
            HashSet<long> featuredCategoryIds,
            string search,
            long cultureId,
            long warehouseId)
        {
            try
            {
                var children = subcategories
                    .Where(x => x.ParentId == parentId &&
                                (!x.CategoryWarehouses.Any()  || x.CategoryWarehouses.Any(w => w.WarehouseId == warehouseId)))
                    .Select(x =>
                    {
                        var detail = x.CategoryDetails?.FirstOrDefault(d => d.CultureId == cultureId);
                        var slug = detail?.SlugInfo?.Slug ?? "";
                        var name = detail?.Name ?? "";

                        var childViewModel = new CategoryWebResponseViewModel(model.ActiveIds)
                        {
                            Id = x.Id,
                            DisplayOrder = x.DisplayOrder,
                            PublicUrl = x.ThumbnailImage?.PublicUrl ?? "",
                            CoverImageUrl = x.CoverImage?.PublicUrl ?? "",
                            TopParentPublicURL = topParentPublicURL,
                            Name = name,
                            Slug = slug,
                            TopParentSlug = topParentSlug,
                            TopParentId = topParentId,
                            HasProduct = productCategoryIds.Contains(x.Id),
                        };

                        // Recursive children
                        childViewModel.Children = GetChildrenForCategories(
                            x.Id,
                            subcategories,
                            productCategoryIds,
                            topParentSlug,
                            topParentId,
                            topParentPublicURL,
                            model,
                            featuredCategoryIds,
                            search,
                            cultureId,
                            warehouseId
                        );

                        childViewModel.ContainsFeaturedProducts =
                            featuredCategoryIds.Contains(x.Id) ||
                            (childViewModel.Children?.Any(c => featuredCategoryIds.Contains(c.Id)) ?? false);

                        return childViewModel;
                    })
                    .Where(x =>
                        // if there's a search, match name
                        (string.IsNullOrEmpty(search) || x.Name.ToLower().Contains(search)) &&
                        // if featured filter exists, match on it or must have children
                        (model.ContainsFeaturedProduct == null ||
                         x.ContainsFeaturedProducts == model.ContainsFeaturedProduct ||
                         (x.Children?.Any() ?? false))
                    )
                    .ToList();

                var orderedChildren = ReturnOrderdCategoriesList(children, model.SortType, model.OrderBy);
                return orderedChildren.Any() ? orderedChildren : null;
            }
            catch
            {
                throw;
            }
        }


        private List<CategoryWebResponseViewModel> ReturnOrderdCategoriesList(
       List<CategoryWebResponseViewModel> categories,
       ESortType sortType,
       OrderByEnum orderBy)
        {
            try
            {
                if (categories == null || !categories.Any())
                    return new List<CategoryWebResponseViewModel>();

                return (sortType, orderBy) switch
                {
                    (ESortType.asc, OrderByEnum.Order) => categories.OrderBy(x => x.DisplayOrder).ToList(),
                    (ESortType.asc, OrderByEnum.Alphabetical) => categories.OrderBy(x => x.Name).ToList(),
                    (ESortType.asc, OrderByEnum.Id) => categories.OrderBy(x => x.Id).ToList(),

                    (ESortType.desc, OrderByEnum.Order) => categories.OrderByDescending(x => x.DisplayOrder).ToList(),
                    (ESortType.desc, OrderByEnum.Alphabetical) => categories.OrderByDescending(x => x.Name).ToList(),
                    (ESortType.desc, OrderByEnum.Id) => categories.OrderByDescending(x => x.Id).ToList(),

                    _ => categories.OrderBy(x => x.DisplayOrder).ToList()
                };
            }
            catch
            {
                throw;
            }
        }
        public async Task<List<CategoriesBreadCrumbVM>> GetBreadCrumb(CategoriesBreadCrumbVM target, List<CategoriesBreadCrumbVM> CategoryList, List<CategoriesBreadCrumbVM> CategoryResult)
        {
            try
            {
                if (target == null)
                {
                    return CategoryResult;
                }
                else
                {
                    var categoryitem = CategoryList.Where(x => x.CategoryId == target.ParentId).FirstOrDefault();
                    CategoryResult.Add(target);
                    return await GetBreadCrumb(categoryitem, CategoryList, CategoryResult);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<List<CatelogFlatViewModel>> GetAllCategoriesByTypeId(long categoryTypeId, bool? isFeatured = null )
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                var query = await _categoryRepository.Query()
                                                .AsNoTrackingWithIdentityResolution()
                                                .Include(x => x.CategoryDetails)
                                                .Include("CategoryDetails.SlugInfo")
                                                .Where(x => !isFeatured.HasValue || x.IsFeatured == isFeatured.Value)
                                                .Where(x =>
                                                x.Status != EStatus.Deleted
                                                && x.SiteId == SiteId)
                                                .SelectMany(x => x.CategoryDetails)
                                                .Where(x => CultureId == 0 || x.CultureId == CultureId)
                                                .Where(x => categoryTypeId == 0 || x.Category.CategoryTypeId == categoryTypeId)
                                                .Select(x => new CatelogFlatViewModel
                                                {
                                                    Id = x.CategoryId,
                                                    Name = x.Name,
                                                    Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug,
                                                }).ToListAsync();

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<CategoryWebViewModel>> GetAllCategories(CategoryFiltrationViewModel model)
        {
            try
            {

                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var gridData = await _categoryRepository.Query()
                    .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId && x.CategoryTypeId == (long)ECategory.Ecommerce)
                    .Include(x => x.CategoryDetails.Where(x => x.CultureId == CultureId))
                    .Include(x => x.ThumbnailImage)
                    .Include("CategoryDetails.SlugInfo")
                    .Include("CategoryDetails.Culture")
                     .Where(x => !model.IsPromotional.HasValue || model.IsPromotional == x.IsPromotional)
                     .Where(x => !model.IsFeatured.HasValue || model.IsFeatured == x.IsFeatured)
                    .Select(x => new CategoryWebViewModel
                    {
                        Id = x.Id,
                        IsFeatured = x.IsFeatured,
                        Status = x.Status,
                        IsPromotional = x.IsPromotional,
                        DisplayOrder = x.DisplayOrder,
                        CategoryTypeId = x.CategoryTypeId,
                        ParentId = x.ParentId,
                        ThumbnailImage = x.ThumbnailImageId.HasValue ? new MediaViewModel { PublicUrl = x.ThumbnailImage.PublicUrl, FileName = x.ThumbnailImage.Filename, Id = x.ThumbnailImageId } : new MediaViewModel(),
                        Description = x.CategoryDetails.Where(x => x.CultureId == CultureId).Select(x => x.Description).FirstOrDefault(),
                        Name = x.CategoryDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(),
                        Slug = x.CategoryDetails.Where(x => x.SlugInfoId.HasValue).Where(x => x.CultureId == CultureId).Select(x => x.SlugInfo.Slug).FirstOrDefault()
                    })
                    .AsNoTrackingWithIdentityResolution()
                    .OrderByDescending(x => x.Id)
                    .PaginateAsync(model.Pagination.PageSize, model.Pagination.PageNumber);
                return gridData;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion
    }
}
