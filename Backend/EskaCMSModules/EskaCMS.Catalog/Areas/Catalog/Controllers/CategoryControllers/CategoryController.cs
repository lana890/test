using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;

using System.Threading.Tasks;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using EskaCMS.Core.Areas.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces;


namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.CategoryControllers
{
    [Area("Catalog")]
    [Route("api/Categories")]

    public class CategoryController : Controller
    {
        // private readonly IRepository<Category> _categoryRepository;
        // private readonly IMediaService _mediaService;
        // private readonly IRepository<Product> _productRepository;
        // private readonly ICurrencyService _currencyService;
        // private readonly IRepository<Brand> _brandRepository;
        // private readonly IProductPricingService _productPricingService;
        private readonly ICategoryWebServices _categoryService;
        // private readonly ISlugServices _slugServices;

        public CategoryController(/* IRepository<Product> productRepository, */ ICategoryWebServices categoryService
            /* IMediaService mediaService, */
            /* IRepository<Category> categoryRepository, */
            /* IRepository<Brand> brandRepository, */
            /* IProductPricingService productPricingService, */
            /* , IConfiguration config *//* ,
            ISlugServices slugServices *//* ,
            ICurrencyService currencyService */)
        {
            // _productRepository = productRepository;
            // _mediaService = mediaService;
            // _categoryRepository = categoryRepository;
            // _brandRepository = brandRepository;
            // _slugServices = slugServices;
            // _productPricingService = productPricingService;
            _categoryService = categoryService;
            // _currencyService = currencyService;

        }




        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                return Ok(await _categoryService.GetById(id));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [Route("GetCategoriesNavigator")]
        public async Task<IActionResult> GetCategoriesNavigator([FromBody] CategoryWebVM category)
        {
            try
            {

                return Ok(await _categoryService.GetCategoriesNavigator(category));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetCategoriesByIds")]
        public async Task<IActionResult> GetCategoriesByIds(List<long> ids)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _categoryService.GetCategoriesByIds(ids, SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetCategoriesById/{Id}")]
        public async Task<IActionResult> GetCategoriesById(long Id)
        {
            try
            {
                return Ok(await _categoryService.GetCategoriesById(Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetCategoryBreadCrumb/{CategoryId}")]
        public async Task<IActionResult> GetCategoryBreadCrumb(long CategoryId)
        {
            try
            {
                return Ok(await _categoryService.GetCategoryBreadCrumb(CategoryId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetCategoriesByBrandId/{BrandId}")]

        public async Task<IActionResult> GetCategoriesByBrandId(long BrandId)
        {
            try
            {
                return Ok(await _categoryService.GetCategoriesByBrandId(BrandId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        [Route("GetAllCategoriesByTypeId/{CategoryTypeId}")]
        [Route("GetAllCategoriesByTypeId/{CategoryTypeId}/{isFeatured?}")]
        [HttpGet]
        public async Task<IActionResult> GetAllCategoriesByTypeId([FromRoute] long CategoryTypeId, [FromRoute] bool? isFeatured = null)
        {
            try
            {
                return Ok(await _categoryService.GetAllCategoriesByTypeId(CategoryTypeId, isFeatured));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("GetAllCategories")]
        [HttpPost]
        public async Task<IActionResult> GetAllCategories([FromBody] CategoryFiltrationViewModel model)
        {
            try
            {
                return Ok(await _categoryService.GetAllCategories(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #region Old Apies

        //[HttpGet("GetCategoryProductsById/{id}")]
        //public IActionResult GetCategoryProductsById(long id)
        //{
        //    try
        //    {
        //        var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
        //        var category = _categoryRepository.Query().Include(x => x.SlugInfo).Include(x => x.ThumbnailImage).Where(x => x.Id == id && x.SiteId == SiteId
        //        && x.CategoryTypeId == (long)ECategoryTypes.Product).FirstOrDefault();

        //        if (category == null)
        //            return null;

        //        var model = new ProductsByCategory
        //        {
        //            CategoryId = category.Id,
        //            ParentCategorId = category.ParentId,
        //            Categoryimage = category.ThumbnailImage.PublicUrl,
        //            CategoryName = category.Name,
        //            CategoryDescription = category.Description,
        //            CategorySlug = category.SlugInfo.Slug,
        //            CategoryMetaTitle = category.MetaTitle,
        //            CategoryMetaKeywords = category.MetaKeywords,
        //            CategoryMetaDescription = category.MetaDescription,
        //            Currency = _currencyService.GetDefaultCurrencyCode()
        //        };

        //        var query = _productRepository.Query()
        //            .Where(x => x.Categories.Any(c => c.CategoryId == category.Id) && x.IsPublished && !x.IsDeleted && x.IsVisibleIndividually)
        //             .Include(x => x.SlugInfo)
        //            .Include(x => x.ProductLinks).ThenInclude(p => p.LinkedProduct).ThenInclude(x => x.TaxClass).ThenInclude(x => x.TaxRate)
        //            .Include(x => x.ProductAdditionalFees)
        //            .Include(x => x.ThumbnailImage)
        //            .Include(x => x.TaxClass)
        //            .ThenInclude(x => x.TaxRate);

        //        var products = query.Select(x => x).ToList();

        //        var productsWithVariations = new List<ProductVm>();


        //        foreach (var product in products)
        //        {

        //            var productVm = new ProductVm
        //            {
        //                Id = product.Id,
        //                Name = product.Name,
        //                //Name2 = ProductName2,
        //                Slug = product.SlugInfo.Slug,
        //                MetaTitle = product.MetaTitle,
        //                MetaKeywords = product.MetaKeywords,
        //                MetaDescription = product.MetaDescription,
        //                Sku = product.Sku,
        //                Gtin = product.Gtin,
        //                ShortDescription = product.ShortDescription,
        //                Description = product.Description,
        //                Specification = product.Specification,
        //                OldPrice = product.OldPrice,
        //                Price = product.Price,
        //                SpecialPrice = product.SpecialPrice,
        //                SpecialPriceStart = product.SpecialPriceStart,
        //                SpecialPriceEnd = product.SpecialPriceEnd,
        //                IsFeatured = product.IsFeatured,
        //                IsPublished = product.IsPublished,
        //                IsCallForPricing = product.IsCallForPricing,
        //                IsAllowToOrder = product.IsAllowToOrder,
        //                CategoryIds = product.Categories.Select(x => x.CategoryId).ToList(),
        //                BrandId = product.BrandId,
        //                TaxClassId = product.TaxClassId,
        //                StockTrackingIsEnabled = product.StockTrackingIsEnabled,
        //                Type = product.Type,
        //                BidIncrement = product.BidIncrement,
        //                BidStartDate = product.BidStartDate,
        //                BidEndtDate = product.BidEndtDate,
        //                MinPrice = product.MinPrice,
        //                BuyoutPrice = product.BuyoutPrice,
        //                CalculatedItemPrice = _productPricingService.CalculateProductPrice(product),
        //                ThumbnailImageId = product.ThumbnailImageId,
        //                ThumbnailImageUrl = product.ThumbnailImage.PublicUrl,
        //                ProductAdditionalFees = product.ProductAdditionalFees.Select(x => new ProductFeesVM { Name = x.Name, FeesValue = x.FeeValue, FeesType = x.FeeType }).ToList()
        //            };

        //            foreach (var variation in product.ProductLinks.Where(x => x.LinkType == ItemLinkType.Super)
        //    .Select(x => x.LinkedProduct).Where(x => !x.IsDeleted).OrderBy(x => x.Id))
        //            {
        //                productVm.Variations.Add(new ItemVariationVm
        //                {
        //                    Id = variation.Id,
        //                    Name = variation.Name,
        //                    Sku = variation.Sku,
        //                    Gtin = variation.Gtin,
        //                    Price = variation.Price,
        //                    OldPrice = variation.OldPrice,
        //                    NormalizedName = variation.NormalizedName,
        //                    CalculatedItemPrice = _productPricingService.CalculateProductPrice(variation),
        //                    OptionCombinations = variation.OptionCombinations.Select(x => new ItemOptionCombinationVm
        //                    {
        //                        OptionId = x.OptionId,
        //                        OptionName = x.Option.Name,
        //                        Value = x.Value,
        //                        SortIndex = x.SortIndex
        //                    }).OrderBy(x => x.SortIndex).ToList()
        //                });
        //            }
        //            productsWithVariations.Add(productVm);
        //        }

        //        model.ProductsWithVariations = productsWithVariations;

        //        return Json(model);

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}


        //[HttpGet("GetCategoryProductsBySlug/{slug}")]
        //public async Task<IActionResult> GetCategoryProductsBySlug(string slug)
        //{
        //    try
        //    {
        //        var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);


        //        var category = await _categoryRepository.Query()
        //                                                .Include(x => x.SlugInfo)
        //                                                .Include(x => x.ThumbnailImage)
        //                                                .Where(x => x.SlugInfo.Slug == slug &&
        //                                                            x.SiteId == SiteId &&
        //                                                            x.CategoryTypeId == (long)ECategoryTypes.Product)
        //                                                .FirstOrDefaultAsync();
        //        if (category == null)
        //        {
        //            return null;
        //        }

        //        var model = new ProductsByCategory
        //        {
        //            CategoryId = category.Id,
        //            ParentCategorId = category.ParentId,
        //            Categoryimage = category.ThumbnailImage.PublicUrl,
        //            CategoryName = category.Name,
        //            CategoryDescription = category.Description,

        //            CategorySlug = category.SlugInfo.Slug,
        //            CategoryMetaTitle = category.MetaTitle,
        //            CategoryMetaKeywords = category.MetaKeywords,
        //            CategoryMetaDescription = category.MetaDescription,
        //        };

        //        model.Products = await _productRepository
        //            .Query()
        //            .Where(x => x.Categories.Any(c => c.CategoryId == category.Id) &&
        //                        x.IsPublished &&
        //                        !x.IsDeleted &&
        //                        x.IsVisibleIndividually)
        //            .ToListAsync();



        //        return Json(model);
        //    }
        //    catch (Exception Exc)
        //    {

        //        return BadRequest(Exc.Message);
        //    }

        //}


        //public IActionResult CategoryDetail(long id, SearchOption searchOption)
        //{
        //    var category = _categoryRepository.Query().Include(x => x.SlugInfo).FirstOrDefault(x => x.Id == id);
        //    if (category == null)
        //    {
        //        return Redirect("~/Error/FindNotFound");
        //    }


        //    var model = new ProductsByCategory
        //    {
        //        CategoryId = category.Id,
        //        ParentCategorId = category.ParentId,
        //        CategoryName = category.Name,
        //        CategorySlug = category.SlugInfo.Slug,
        //        CategoryMetaTitle = category.MetaTitle,
        //        CategoryMetaKeywords = category.MetaKeywords,
        //        CategoryMetaDescription = category.MetaDescription,
        //        CurrentSearchOption = searchOption,
        //        FilterOption = new FilterOption()
        //    };

        //    var query = _productRepository
        //        .Query()
        //        .Where(x => x.Categories.Any(c => c.CategoryId == category.Id) && x.IsPublished && x.IsVisibleIndividually);

        //    if (query.Count() == 0)
        //    {
        //        model.TotalProduct = 0;
        //        return View(model);
        //    }

        //    AppendFilterOptionsToModel(model, query);

        //    if (searchOption.MinPrice.HasValue)
        //    {
        //        query = query.Where(x => x.Price >= searchOption.MinPrice.Value);
        //    }

        //    if (searchOption.MaxPrice.HasValue)
        //    {
        //        query = query.Where(x => x.Price <= searchOption.MaxPrice.Value);
        //    }

        //    var brands = searchOption.GetBrands();
        //    if (brands.Any())
        //    {
        //        var brandIds = _brandRepository.Query().Where(x => brands.Contains(x.Slug)).Select(x => x.Id).ToList();
        //        query = query.Where(x => x.BrandId.HasValue && brandIds.Contains(x.BrandId.Value));
        //    }

        //    model.TotalProduct = query.Count();
        //    var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
        //    var offset = _pageSize * currentPageNum - _pageSize;
        //    while (currentPageNum > 1 && offset >= model.TotalProduct)
        //    {
        //        currentPageNum--;
        //        offset = _pageSize * currentPageNum - _pageSize;
        //    }

        //    query = query
        //        .Include(x => x.Brand)
        //        .Include(x => x.ThumbnailImage);

        //    query = ApplySort(searchOption, query);

        //    var products = query
        //        .Select(x => x)
        //        .Skip(offset)
        //        .Take(_pageSize)
        //        .ToList();

        //    //foreach (var product in products)
        //    //{
        //    //    product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
        //    //    product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
        //    //}

        //    model.Products = products;
        //    model.CurrentSearchOption.PageSize = _pageSize;
        //    model.CurrentSearchOption.Page = currentPageNum;

        //    return View(model);
        //}

        //private static IQueryable<Product> ApplySort(SearchOption searchOption, IQueryable<Product> query)
        //{
        //    var sortBy = searchOption.Sort ?? string.Empty;
        //    switch (sortBy.ToLower())
        //    {
        //        case "price-desc":
        //            query = query.OrderByDescending(x => x.Price);
        //            break;
        //        case "price-asc":
        //            query = query.OrderBy(x => x.Categories.Select(a => a.Category.DisplayOrder).OrderByDescending(a => a).FirstOrDefault()).ThenBy(x => x.Type).ThenBy(x => x.Price);
        //            break;
        //        case "alpha-asc":
        //            query = query.OrderBy(x => x.Categories.Select(a => a.Category.DisplayOrder).OrderByDescending(a => a).FirstOrDefault()).ThenBy(x => x.Name);
        //            break;
        //        case "alpha-desc":
        //            query = query.OrderBy(x => x.Categories.Select(a => a.Category.DisplayOrder).OrderByDescending(a => a).FirstOrDefault()).ThenByDescending(x => x.Name);
        //            break;
        //        default:
        //            query = query.OrderBy(x => x.Price);
        //            break;
        //    }

        //    return query;
        //}

        //private static void AppendFilterOptionsToModel(ProductsByCategory model, IQueryable<Product> query)
        //{
        //    model.FilterOption.Price.MaxPrice = query.Max(x => x.Price);
        //    model.FilterOption.Price.MinPrice = query.Min(x => x.Price);

        //    model.FilterOption.Brands = query
        //        .Where(x => x.BrandId != null)
        //        .GroupBy(x => x.Brand)
        //        .Select(g => new FilterBrand
        //        {
        //            Id = (int)g.Key.Id,
        //            Name = g.Key.Name,
        //            Slug = g.Key.Slug,
        //            Count = g.Count()
        //        }).ToList();
        //}


        //[HttpPost("GetCategories")]
        //public async Task<IActionResult> GetCategories([FromBody] List<long> ids)
        //{
        //    var gridData = await _categoryRepository.Query().Include(x => x.SlugInfo).Include(x => x.ThumbnailImage).Where(x => x.Status == EStatus.Published && x.CategoryTypeId == (long)ECategory.Ecommerce).Select(x =>
        //         new CategoryForm
        //         {
        //             Id = x.Id,
        //             Name = x.Name,
        //             Slug = x.SlugInfo.Slug,
        //             MetaTitle = x.MetaTitle,
        //             MetaKeywords = x.MetaKeywords,
        //             MetaDescription = x.MetaDescription,
        //             DisplayOrder = x.DisplayOrder,
        //             Description = x.Description,
        //             ParentId = x.ParentId,
        //             IncludeInMenu = x.IncludeInMenu,
        //             IsPublished = x.IsPublished,
        //             ThumbnailImageUrl = x.ThumbnailImage.PublicUrl// _mediaService.GetThumbnailUrl(x.ThumbnailImage),
        //         }
        //        ).ToListAsync();

        //    if (ids.Count > 0)
        //    {
        //        var FilteredCategories = new List<CategoryForm>();
        //        foreach (var item in gridData)
        //        {
        //            foreach (var id in ids)
        //            {
        //                if (item.Id == id)
        //                {
        //                    FilteredCategories.Add(item);
        //                }
        //            }

        //        }
        //        return Json(FilteredCategories);
        //    }
        //    else
        //        return Json(gridData);

        //}


        ////HomeCuts custom API's 
        ////Get Categories With Products

        //[HttpPost("GetCategoriesProducts")]
        //public async Task<IActionResult> GetCategoriesProducts([FromBody] List<long> ids)
        //{
        //    try
        //    {
        //        var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
        //        var Data = await _categoryService.GetCategoriesWithProductsByIds(SiteId, ids);
        //        return Json(Data);
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}
        //[HttpGet("GetCategoriesWithProducts")]
        //public async Task<IActionResult> GetCategoriesWithProducts()
        //{
        //    try
        //    {
        //        var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
        //        var Data = await _categoryService.GetCategoryWithProducts(SiteId);
        //        return Ok(Data);
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }

        //}
        //[HttpGet]
        //[Route("GetCategoryBreadCrumb/{CategoryId}")]
        //public async Task<IActionResult> GetCategoryBreadCrumb(long CategoryId)
        //{
        //    try
        //    {
        //        return Ok(await _categoryService.GetCategoryBreadCrumb(CategoryId));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
        #endregion

    }
}
