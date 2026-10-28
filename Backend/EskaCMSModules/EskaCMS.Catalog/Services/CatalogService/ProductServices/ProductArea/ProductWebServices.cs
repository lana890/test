using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using Elastic.Clients.Elasticsearch;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Filters.ActionFilters;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea
{
    public class ProductWebServices : IProductWebServices
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductDetails> productDetailsRepositorty;
        private readonly IProductPricingService _productPricingService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly IProductApiAttribute productAttributeServices;
        private readonly IRepository<ProductCategory> productCategoryRepository;
        private readonly IRepository<ProductLink> productLinkRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<SitesCultures> _SitesCulturesRepository;
        private readonly IRepository<BiddingTracker> biddingTrackerRepository;
        private readonly IProductApiAttribute productAttribute;
        private readonly IProductServices productServices;
        private readonly ICurrencyService currencyService;
        private readonly ITagsServices tagsServices;
        private readonly EskaDCMSDbContext _context;
        private readonly IUserSiteService _userSiteService;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<ProductOptionCombination> _productCombinationRepository;
        private readonly IRepository<RecentlyViewedProduct> _recentlyViewedProductRepository;
        private readonly ICacheService _cacheService;

        public ProductWebServices(
            IRepository<Product> productRepository,
            IRepository<ProductDetails> ProductDetailsRepositorty,
            IProductPricingService productPricingService,
          IWorkContext workContext, IMediator mediator,
          IProductApiAttribute productAttributeServices,
          IRepository<ProductCategory> productCategoryRepository,
          IRepository<ProductLink> productLinkRepository,
          IRepository<SitesCultures> SitesCulturesRepository,
           IRepository<BiddingTracker> biddingTrackerRepository,
           IProductApiAttribute productAttribute,
           IProductServices productServices,
           ICurrencyService currencyService,
           ITagsServices TagsServices,
            IUserSiteService userSiteService,
            IRepository<OrderItem> orderItemRepository,
            IRepository<ProductOptionCombination> productCombinationRepository,
            IRepository<User> userRepository,
            IRepository<RecentlyViewedProduct> recentlyViewedProductRepository,
                     ICacheService cacheService,

        EskaDCMSDbContext context)
        {
            _productRepository = productRepository;
            productDetailsRepositorty = ProductDetailsRepositorty;
            _productPricingService = productPricingService;
            _workContext = workContext;
            _mediator = mediator;
            this.productAttributeServices = productAttributeServices;
            this.productCategoryRepository = productCategoryRepository;
            this.productLinkRepository = productLinkRepository;
            _SitesCulturesRepository = SitesCulturesRepository;
            this.biddingTrackerRepository = biddingTrackerRepository;
            this.productAttribute = productAttribute;
            this.productServices = productServices;
            this.currencyService = currencyService;
            tagsServices = TagsServices;
            _context = context;
            _userSiteService = userSiteService;
            _orderItemRepository = orderItemRepository;
            _productCombinationRepository = productCombinationRepository;
            _userRepository = userRepository;
            _recentlyViewedProductRepository = recentlyViewedProductRepository;
            _cacheService = cacheService;
        }

        private static string NormalizeOptionalSlug(string slug)
        {
            var normalizedSlug = slug?.Trim();

            return string.IsNullOrWhiteSpace(normalizedSlug)
                || normalizedSlug.Equals("undefined", StringComparison.OrdinalIgnoreCase)
                || normalizedSlug.Equals("null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : normalizedSlug;
        }

        public async Task<ProductViewModelForWeb> GetMostSellingProducts(ProductParamsVM model)
        {

            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();
                var ProductIds = await _orderItemRepository.Query()
                                                     .Where(x => WarehouseId == 0 || x.Order.WarehouseId == WarehouseId)
                                                     .Where(x => (x.Order.OrderStatus == OrderStatus.CashOnDelivery ||
                                                                x.Order.OrderStatus == OrderStatus.PaymentReceived) && x.Product.Status == EStatus.Published && x.Order.SiteId == siteId)
                                                                 .GroupBy(x => x.ProductId)
                                                                    .Select(group => new
                                                                    {
                                                                        ProductId = group.Key,
                                                                        TotalSales = group.Sum(y => y.Quantity)
                                                                    })
                                                                    .OrderByDescending(x => x.TotalSales)
                                                                    .Skip(model.PageIndex * model.PageSize)
                                                                    .Take(model.PageSize)
                                                                    .Select(x => x.ProductId)
                                                                    .ToListAsync();

                if (!ProductIds.Any())
                {
                    return new ProductViewModelForWeb()
                    {
                        Catname = "",
                        products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(), 0, 0),
                    };
                }
                ProductParamsModel productParamsVM = new ProductParamsModel();

                productParamsVM.ProductIds = ProductIds;
                productParamsVM.PageNumber = model.PageIndex;
                productParamsVM.PageSize = model.PageSize;
                productParamsVM.Sort = new ProductSearchSort
                {
                    SortField = Array.Empty<int>(),
                    SortType = 0,
                };




                var result = await GetProductsForWeb(productParamsVM);




                return result;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public async Task<ViewProductDetailaVM> ViewProductDetails(long? Id, string slug = null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();

                var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);
                var WarehouseId = await _workContext.GetCurrentWarehouseId();
                var userCountry = await _userSiteService.GetUserCountry();

                var normalizedSlug = string.IsNullOrWhiteSpace(slug) || slug.Trim().ToLower() == "undefined"
                    ? null
                    : slug.Trim().ToLower();

                var product = await productDetailsRepositorty.Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Include(x => x.SlugInfo)
                    .Where(x => x.CultureId == CultureId)
                    .Where(x =>
                        Id.HasValue && Id > 0
                            ? x.ProductId == Id
                            : !string.IsNullOrWhiteSpace(normalizedSlug)
                               && x.SlugInfo != null
                               && x.SlugInfo.Slug.ToLower() == normalizedSlug
                    )
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .Include("Product.Brand")
                    .Include("Product.Brand.BrandDetails")
                    .Include("Product.ThumbnailImage")
                    .Include("Product.StockAmounts")
                    .Include("Product.Stock")
                    .Include("Product.ProductBranch")
                    .Include("Product.Medias")
                    .Include("Product.Medias.Media")
                    .Include("Product.ProductAvaliablity")
                    .Include("Product.DiscountProducts")
                    .Include("Product.DiscountProducts.Discount")
                    .Include("Product.DiscountProducts.Discount.DiscountTiers")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup.Users")
                    .Include("Product.ProductPriceWarehouses")
                    .Include("Product.ReviewProducts")
                    .Include("Product.ReviewProducts.Review")
                    .Where(x => WarehouseId == 0 ||
                                !x.Product.ProductBranch.Any() ||
                                x.Product.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                var Currency = await currencyService.GetUserCurrencyByCulture();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();

                if (product == null || product.Product == null)
                {
                    return new ViewProductDetailaVM();
                }
                var model = new ViewProductDetailaVM();

                var approvedReviews = product.Product.ReviewProducts?
                                     .Where(x => x.Review != null &&
                                                 x.Review.Status == ReviewStatus.Approved)
                                     .ToList();

               

                model.BrandName = product.Product.Brand?.BrandDetails?
                    .FirstOrDefault(x => x.CultureId == CultureId)?.Name ?? "";

                model.Id = product.ProductId;
                model.Name = product.Name;

                model.CalculatedItemPrice = _productPricingService.CalculatedProductPrice(
                    product.Product,
                    userCountry.Id,
                    Currency.Rate,
                    Currency.Code,
                    IsLocalCountry);

                model.IsCallForPricing = product.Product.IsCallForPricing;
                model.IsAllowToOrder = product.Product.IsAllowToOrder;
                model.StockTrackingIsEnabled = product.Product.StockTrackingIsEnabled;
                model.ShortDescription = product.ShortDescription;
                model.Description = product.Description;
                model.Specification = product.Specification;
                model.ThumbnailImageUrl = product.Product.ThumbnailImage?.PublicUrl ?? "";

                model.ImagesUrl = product.Product.Medias?
                    .Where(x => x.Media != null && !string.IsNullOrWhiteSpace(x.Media.PublicUrl))
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Media.PublicUrl)
                    .ToList() ?? new List<string>();

                model.InStock =
                    (product.Product.StockAmounts?.Any(y => y.Quantity > 0) ?? false) &&
                    (WarehouseId == 0 ||
                     !(product.Product.Stock?.Any(x => x.Status == EStatus.Unpublished &&
                                                       x.WarehouseId == WarehouseId) ?? false));

                model.RatingAverage = approvedReviews.Any()
                    ? Math.Round(approvedReviews.Average(y => y.Review.Rating), 1)
                    : 0;

                model.ReviewsCount = approvedReviews.Count;

                model.IsAvailable = IsAvaliableProduct(product.Product, userCountry, siteCountryId);

                await _mediator.Publish(new EntityViewed
                {
                    EntityId = product.ProductId,
                    EntityTypeId = "Product"
                });

                return model;
            }
            catch
            {
                throw;
            }
        }
        public async Task<ProductOptionDetailsVM> GetProductOptionsDetails(long? Id = null, string slug = null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var userCountry = await _userSiteService.GetUserCountry();
                var Currency = await currencyService.GetUserCurrencyByCulture();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();


                var model = new ProductDetail();

                var normalizedSlug = NormalizeOptionalSlug(slug);

                var optionsWithDistinctValues =
                    await _productCombinationRepository.Query().Include(x => x.ParentProduct)
                    .ThenInclude(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo).Include(x => x.Option)
                      .ThenInclude(x => x.optionDetails)
                  .Include(x => x.Value)
                      .ThenInclude(x => x.productOptionValueDetails)
                        .Where(x =>
                            Id.HasValue && Id > 0
                                && (x.ParentProductId == Id.Value
                                    || x.ParentProduct.ProductDetails.Any(pd => pd.Id == Id.Value))
                            || 
                                    !string.IsNullOrWhiteSpace(normalizedSlug)
                                    && x.ParentProduct.ProductDetails.Any(pd =>
                                            pd.SlugInfo != null &&
                                            pd.SlugInfo.Slug == normalizedSlug
                                        )
                                   
                        )
                        .Select(x => new
                        {
                            OptionId = x.Option.Id,

                            OptionName = x.Option.optionDetails
                                .Where(d => d.CultureId == CultureId)
                                .Select(d => d.Name)
                                .FirstOrDefault(),

                            x.ValueId,

                            ValueName = x.Value.productOptionValueDetails
                                .Where(d => d.CultureId == CultureId)
                                .Select(d => d.Value)
                                .FirstOrDefault(),

                            x.SortIndex
                        })
                        .OrderBy(x => x.SortIndex)
                        .ToListAsync();


       

                var result = optionsWithDistinctValues
                    .GroupBy(x => x.OptionId)
                    .Select(group => new ItemDetailOption
                    {
                        OptionId = group.Key,
                        OptionName = group.First().OptionName,
                        Values = group.DistinctBy(x => x.ValueId).OrderBy(x => x.SortIndex).Select(x => x.ValueName).ToList()
                    }).ToList();







                //await MapProductVariantToProductVm(product, model, CultureId, IsLocalCountry, Currency, userCountry);
                //MapProductOptionToProductVm(product, model, CultureId);

                return new ProductOptionDetailsVM()
                {
                    AvailableOptions = result,
                };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<ProductCategoryDetailsVM> GetProductCategoryDetails(long? Id = null, string slug = null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();


                var normalizedSlug = NormalizeOptionalSlug(slug);

                var product = await productDetailsRepositorty.Query()
                    .Include(x=>x.SlugInfo)
                    .Where(x =>
    Id.HasValue && Id > 0 && (x.ProductId == Id.Value || x.Id == Id.Value)
        || !string.IsNullOrWhiteSpace(normalizedSlug)
           && x.SlugInfo != null
           && x.SlugInfo.Slug == normalizedSlug
)

                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductBranch)
                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.CultureId == CultureId)
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .Include("Product.Categories")
                    .Include("Product.Categories.Category")
                    .Include("Product.Categories.Category.CategoryDetails")
                    .Include("Product.Categories.Category.CategoryDetails.SlugInfo")
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();



                var model = new ProductCategoryDetailsVM();

                if (product?.Product == null)
                {
                    return model;
                }

                model.Categories = product.Product.Categories.SelectMany(x => x.Category.CategoryDetails)
                         .Where(x => x.CultureId == CultureId)
                         .Select(x => new ItemDetailCategory { Id = x.CategoryId, Name = x.Name, Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug }).ToList();




                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<ProductAttributeDetailsVM> GetProductAttributeDetails(long? Id=null,string slug=null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();


                var normalizedSlug = NormalizeOptionalSlug(slug);

                var product = await productDetailsRepositorty.Query()
                    .Include(x=>x.SlugInfo)
                    .Where(x =>
    Id.HasValue && Id > 0 && (x.ProductId == Id.Value || x.Id == Id.Value)
        || !string.IsNullOrWhiteSpace(normalizedSlug)
           && x.SlugInfo != null
           && x.SlugInfo.Slug == normalizedSlug
)

                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.CultureId == CultureId)
                    .Where(x => x.Product.Status != EStatus.Deleted)

                    .Include("Product.AttributeValues")
                    .Include("Product.AttributeValues.Attribute")
                    .Include("Product.AttributeValues.Attribute.AttributeDetails")
                    .Include("Product.AttributeValues.AttributeValueDetails")
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();



                var model = new ProductAttributeDetailsVM();

                if (product?.Product == null)
                {
                    return model;
                }

                model.Attributes = !product.Product.AttributeValues.Any() ? new List<ItemDetailAttribute>() : product.Product.AttributeValues.Select(x => new ItemDetailAttribute
                {
                    Name = x.Attribute == null ? "" : x.Attribute.AttributeDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Name,
                    Value = x.AttributeValueDetails.Count == 0 ? "" : x.AttributeValueDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Value
                }).ToList();



                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<LinkedProductDetailsVM> GetProductLinksDetails(long? Id=null,string slug=null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var warehouseId = await _workContext.GetCurrentWarehouseId();


                var normalizedSlug = NormalizeOptionalSlug(slug);

                var product = await productDetailsRepositorty.Query()
                    .Include(x=>x.SlugInfo)
                    .Where(x =>
    Id.HasValue && Id > 0 && (x.ProductId == Id.Value || x.Id == Id.Value)
        || !string.IsNullOrWhiteSpace(normalizedSlug)
           && x.SlugInfo != null
           && x.SlugInfo.Slug == normalizedSlug
)

                    .Where(x => x.CultureId == CultureId)
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .AsNoTracking()
                    .Include("Product.ProductLinks")
                    .Include("Product.ProductLinks.LinkedProduct")
                    .Include("Product.ProductLinks.LinkedProduct.ProductBranch")
                    .Include("Product.ProductLinks.LinkedProduct.ProductPriceWarehouses")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails")
                    .Include("Product.ProductLinks.LinkedProduct.ThumbnailImage")
                    .Include("Product.ProductLinks.LinkedProduct.ProductAvaliablity")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails.SlugInfo")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails.SlugInfo")
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();



                var productDetail = new ProductDetail();

                if (product?.Product == null)
                {
                    return new LinkedProductDetailsVM();
                }

                await MapRelatedProductToProductVm(product.Product, productDetail, CultureId);




                return new LinkedProductDetailsVM()
                {
                    CrossSellProducts = productDetail.CrossSellProducts,
                    RelatedProducts = productDetail.RelatedProducts,
                    SubProducts = productDetail.SubProducts,

                };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<ProductReviewDetailsVM> GetProductRevieweDetails(long? Id=null,string slug=null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();


                var normalizedSlug = NormalizeOptionalSlug(slug);

                var product = await productDetailsRepositorty.Query()
                    .Include(x=>x.SlugInfo)
                    .Where(x =>
                            Id.HasValue && Id > 0 && (x.ProductId == Id.Value || x.Id == Id.Value)
                                || !string.IsNullOrWhiteSpace(normalizedSlug)
                                   && x.SlugInfo != null
                                   && x.SlugInfo.Slug == normalizedSlug
                        )

                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductBranch)
                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.CultureId == CultureId)
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .Where(x => WarehouseId == 0 || !x.Product.ProductBranch.Any() || x.Product.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                    .Include("Product.ReviewProducts")
                    .Include("Product.ReviewProducts.Review")
                    .FirstOrDefaultAsync();



                var model = new ProductReviewDetailsVM();

                if (product?.Product == null)
                {
                    return model;
                }

                var approvedReviews = product.Product.ReviewProducts?
                    .Where(x => x.Review != null && x.Review.Status == ReviewStatus.Approved)
                    .ToList();

                model.ReviewsCount = approvedReviews?.Count ?? 0;
                model.RatingAverage = model.ReviewsCount > 0 ? Math.Round(approvedReviews.Average(x => x.Review.Rating), 2) : 0;
                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<ProductVariationDetailsVM> GetProductVariationDetails(long? Id=null, string slug = null)
        {
            try
            {

                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();

                var userCountry = await _userSiteService.GetUserCountry();
                var Currency = await currencyService.GetUserCurrencyByCulture();
                var normalizedSlug = NormalizeOptionalSlug(slug);

                var product = await productDetailsRepositorty.Query()
                     .Include(x => x.SlugInfo)
                     .Where(x =>
                         Id.HasValue && Id > 0 && x.ProductId == Id.Value
                             || !string.IsNullOrWhiteSpace(normalizedSlug)
                                && x.SlugInfo != null
                                && x.SlugInfo.Slug == normalizedSlug
                     )

                     .Where(x => x.Product.Status != EStatus.Deleted)
                     .Where(x => x.CultureId == CultureId)
                     .AsNoTracking()
                     .Include(x => x.Product)
                     .FirstOrDefaultAsync();
                if (product is null)
                {
                    return new ProductVariationDetailsVM();
                }

                var IsLocalCountry = await _userSiteService.IsLocalCountry();


                var model = new ProductDetail();
                await MapProductVariantToProductVm(product.Product, model, CultureId, IsLocalCountry, Currency, userCountry);
                return new ProductVariationDetailsVM()
                {
                    Variations = model.Variations
                };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ProductDetail> ProductDetails(long? Id = null, string Slug = null)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var userCountry = await _userSiteService.GetUserCountry();
                var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);

                var Currency = await currencyService.GetUserCurrencyByCulture();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();

                var normalizedSlug = NormalizeOptionalSlug(Slug);

                var product = await productDetailsRepositorty.Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.CultureId == CultureId
                        && (Id.HasValue && Id > 0 && (x.ProductId == Id.Value || x.Id == Id.Value)
                            || !string.IsNullOrWhiteSpace(normalizedSlug)
                                && x.SlugInfo != null
                                && x.SlugInfo.Slug == normalizedSlug))
                    .Where(x => x.Product.SiteId == SiteId)
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .Include(x => x.SlugInfo)
                    .Include(x => x.Product)
                    .Include("Product.OptionCombinations")
                    .Include("Product.ProductBranch")
                    .Include("Product.ProductPriceWarehouses")
                    .Include("Product.ProductAvaliablity")
                    .Include("Product.OptionCombinations.Value")
                    .Include("Product.OptionCombinations.Value.productOptionValueDetails")
                    .Include("Product.OptionCombinations.Option")
                    .Include("Product.OptionCombinations.Option.optionDetails")
                    .Include("Product.Brand")
                    .Include("Product.Brand.BrandDetails")
                    .Include("Product.Categories")
                    .Include("Product.Categories.Category")
                    .Include("Product.Categories.Category.CategoryDetails")
                    .Include("Product.Categories.Category.CategoryDetails.SlugInfo")
                    .Include("Product.AttributeValues")
                    .Include("Product.AttributeValues.Attribute")
                    .Include("Product.AttributeValues.AttributeValueDetails")
                    .Include("Product.AttributeValues.Attribute.AttributeDetails")
                    .Include("Product.ProductLinks")
                    .Include("Product.ProductLinks.LinkedProduct.ProductBranch")
                    .Include("Product.ProductLinks.LinkedProduct.ProductPriceWarehouses")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails")
                    .Include("Product.ProductLinks.LinkedProduct.ThumbnailImage")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails.SlugInfo")
                    .Include("Product.ThumbnailImage")
                    .Include("Product.StockAmounts")
                    .Include("Product.Medias")
                    .Include("Product.Medias.Media")
                    .Include("Product.ReviewProducts")
                    .Include("Product.ReviewProducts.Review")
                    .Include("Product.DiscountProducts")
                    .Include("Product.DiscountProducts.Discount")
                    .Include("Product.DiscountProducts.Discount.DiscountTiers")
                     .Include("Product.DiscountProducts.Discount.CustomerGroups")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup.Users")
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();



                if (product == null)
                {

                    return new ProductDetail();
                }

                var model = new ProductDetail();

                model.Id = product.ProductId;
                model.Name = product.Name;
                model.BrandName = product.Product.Brand == null ? "" : product.Product.Brand.BrandDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name;
                model.CalculatedItemPrice = _productPricingService.CalculatedProductPrice(product.Product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry);
                model.IsCallForPricing = product.Product.IsCallForPricing;
                model.IsAllowToOrder = product.Product.IsAllowToOrder;
                model.StockTrackingIsEnabled = product.Product.StockTrackingIsEnabled;
                model.ShortDescription = product.ShortDescription;
                model.AddtionalMetaTags = product.AddtionalMetaTags;
                model.IsAvailable = IsAvaliableProduct(product.Product, userCountry, siteCountryId) && (WarehouseId == 0 || !product.Product.ProductBranch.Any() || product.Product.ProductBranch.Any(c => c.WarehouseId == WarehouseId));
                model.Description = product.Description;
                model.InStock = product.Product.StockAmounts.Any(y => y.Quantity > 0);
                model.Specification = product.Specification;
                model.ReviewsCount = product.Product.ReviewProducts.Any() ? product.Product.ReviewProducts.Where(x => x.Review.Status == ReviewStatus.Approved).Count() : 0;
                model.RatingAverage = model.ReviewsCount > 0 ? Math.Round(product.Product.ReviewProducts.Where(x => x.Review.Status == ReviewStatus.Approved).Average(x => x.Review.Rating), 1) : 0;
                model.ThumbnailImageUrl = product.Product.ThumbnailImage == null ? "" : product.Product.ThumbnailImage.PublicUrl;
                model.Attributes = !product.Product.AttributeValues.Any() ? new List<ItemDetailAttribute>() : product.Product.AttributeValues.Select(x => new ItemDetailAttribute
                {
                    Name = x.Attribute == null ? "" : x.Attribute.AttributeDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Name,
                    Value = x.AttributeValueDetails.Count == 0 ? "" : x.AttributeValueDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Value
                }).ToList();

                model.Categories = product.Product.Categories.SelectMany(x => x.Category.CategoryDetails)
                    .Where(x => x.CultureId == CultureId)
                    .Select(x => new ItemDetailCategory { Id = x.CategoryId, Name = x.Name, Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug }).ToList();

                model.ImagesUrl = !product.Product.Medias.Any() ? new List<string>() : product.Product.Medias.OrderBy(x => x.DisplayOrder).Select(x => x.Media.PublicUrl).ToList();



                await MapProductVariantToProductVm(product.Product, model, CultureId, IsLocalCountry, Currency, userCountry);
                MapProductOptionToProductVm(product.Product, model, CultureId);
                MapBiddingHistoryToProductVM(product.Product, model, CultureId);

                try
                {
                    await _mediator.Publish(new EntityViewed { EntityId = product.ProductId, EntityTypeId = "Product" });
                    _productRepository.SaveChanges();
                }
                catch
                {
                    // View tracking should not block the product details response.
                }

                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ProductDetail> ProductOverview(long id)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var userCountry = await _userSiteService.GetUserCountry();
                var siteCountryId = await _userSiteService.GetSiteCountry(_workContext.GetCurrentSiteId());

                var Currency = await currencyService.GetUserCurrencyByCulture();

                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var product = await productDetailsRepositorty.Query()
                    .Where(x => id == null || x.ProductId == id)
                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.CultureId == CultureId)
                    .Where(x => x.Product.Status != EStatus.Deleted)
                    .Include(x => x.SlugInfo)

                    .Include("Product.ProductBranch")
                    .Include("Product.ProductPriceWarehouses")
                    .Include("Product.OptionCombinations")
                    .Include("Product.OptionCombinations.Value")
                    .Include("Product.OptionCombinations.Value.productOptionValueDetails")
                    .Include("Product.OptionCombinations.Option")
                    .Include("Product.OptionCombinations.Option.optionDetails")
                    .Include("Product.Brand")
                    .Include("Product.Brand.BrandDetails")
                    .Include("Product.Categories")
                    .Include("Product.Categories.Category")
                    .Include("Product.Categories.Category.CategoryDetails")
                    .Include("Product.Categories.Category.CategoryDetails.SlugInfo")
                    .Include("Product.AttributeValues")
                    .Include("Product.AttributeValues.Attribute")
                    .Include("Product.AttributeValues.Attribute.AttributeDetails")
                    .Include("Product.AttributeValues.AttributeValueDetails")
                    .Include("Product.ProductLinks")
                    .Include("Product.ProductAvaliablity")
                    .Include("Product.ProductLinks.LinkedProduct")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails")
                    .Include("Product.ProductLinks.LinkedProduct.ThumbnailImage")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails.SlugInfo")

                    .Include("Product.ProductLinks.LinkedProduct.ProductBranch")
                    .Include("Product.ProductLinks.LinkedProduct.ProductPriceWarehouses")
                    .Include("Product.ThumbnailImage")
                    .Include("Product.Medias")
                    .Include("Product.Medias.Media")
                    .Include("Product.ReviewProducts")
                    .Include("Product.ReviewProducts.Review")
                    .Include("Product.DiscountProducts")
                    .Include("Product.DiscountProducts.Discount")
                    .Include("Product.DiscountProducts.Discount.DiscountTiers")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup")
                    .Include("Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup.Users")
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();





                if (product == null)
                {

                    return new ProductDetail();
                }

                var model = new ProductDetail
                {
                    Id = product.ProductId,
                    Name = product.Name,
                    BrandName = product.Product.Brand == null ? "" : product.Product.Brand.BrandDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                    CalculatedItemPrice = _productPricingService.CalculatedProductPrice(product.Product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                    IsCallForPricing = product.Product.IsCallForPricing,
                    IsAllowToOrder = product.Product.IsAllowToOrder,
                    StockTrackingIsEnabled = product.Product.StockTrackingIsEnabled,
                    ShortDescription = product.ShortDescription,
                    AddtionalMetaTags = product.AddtionalMetaTags,
                    Description = product.Description,
                    Specification = product.Specification,
                    IsPromotional = product.Product.IsPromotional,
                    IsAvailable = IsAvaliableProduct(product.Product, userCountry, siteCountryId),
                    ReviewsCount = product.Product.ReviewProducts.Any() ? product.Product.ReviewProducts.Where(x => x.Review.Status == ReviewStatus.Approved).Count() : 0,
                    RatingAverage = product.Product.ReviewProducts.Any() ? Math.Round(product.Product.ReviewProducts.Where(x => x.Review.Status == ReviewStatus.Approved).Average(x => x.Review.Rating), 2) : 0,
                    ThumbnailImageUrl = product.Product.ThumbnailImage == null ? "" : product.Product.ThumbnailImage.PublicUrl,
                    Attributes = !product.Product.AttributeValues.Any() ? new List<ItemDetailAttribute>() : product.Product.AttributeValues.Select(x => new ItemDetailAttribute
                    {
                        Name = x.Attribute == null ? "" : x.Attribute.AttributeDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Name,
                        Value = x.AttributeValueDetails.Count == 0 ? "" : x.AttributeValueDetails.Where(x => x.CultureId == CultureId).FirstOrDefault()?.Value
                    }).ToList(),
                    Categories = product.Product.Categories.SelectMany(x => x.Category.CategoryDetails)
                    .Where(x => x.CultureId == CultureId)
                    .Select(x => new ItemDetailCategory { Id = x.CategoryId, Name = x.Name, Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug }).ToList(),

                    ImagesUrl = product.Product.Medias == null ? new List<string>() : product.Product.Medias.Select(x => x.Media.PublicUrl).ToList()
                };
                //  var imagesList = _productMediaRepository.Query().Where(x => x.ProductId == product.Id).ToList();
                foreach (var image in product.Product.Medias)
                {
                    model.ImagesUrl.Add(image.Media.PublicUrl);
                }
                await MapProductVariantToProductVm(product.Product, model, CultureId, IsLocalCountry, Currency, userCountry);

                MapProductOptionToProductVm(product.Product, model, CultureId);
                MapProductImagesToProductVm(product.Product, model, CultureId);
                await MapRelatedProductToProductVm(product.Product, model, CultureId);

                return model;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ProductDetail> GetMappingProductByAttributeValue(MappingProductVM model)

        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (string.IsNullOrEmpty(model.AttributeValue))
                    {
                    }

                    long SiteId = await _workContext.GetCurrentSiteIdAsync();
                    long CultureId = _workContext.GetCurrentCultureId();
                    long UserId = await _workContext.GetCurrentUserId();
                    long productId = 0;
                    var product = await _productRepository.Query()
                         .Include(x => x.ProductDetails)
                         .Include(x => x.AttributeValues)
                         .Include("AttributeValues.AttributeValueDetails")
                         .Include("AttributeValues.Attribute")
                         .Include("AttributeValues.Attribute.AttributeDetails")
                         .Include(y => y.Medias)
                         .ThenInclude(x => x.Media)
                         .Include(x => x.TaxClass)
                        .ThenInclude(x => x.TaxRate)
                         .Include(x => x.ThumbnailImage)
                         .Include(x => x.ProductAvaliablity)
                         .Where(x => x.SiteId == SiteId)
                         .Where(x => x.Status != EStatus.Deleted)
                         .Where(y =>
                           y.AttributeValues.Any(x => x.AttributeValueDetails.Any(o => o.Value.ToUpper().Equals(model.AttributeValue.ToUpper()))
                           && x.AttributeId == model.AttributeId))
                         .FirstOrDefaultAsync();


                    if (product == null)
                    {
                        var siteCultures = await _SitesCulturesRepository.Query().Where(x => x.SiteId == SiteId && x.Status == EStatus.Published)
                            .Include(x => x.Culture).AsNoTracking().ToListAsync();

                        List<ProductDetailsViewModel> ProductDetails = new List<ProductDetailsViewModel>();


                        foreach (var culture in siteCultures)
                        {
                            var productWithCuture = new ProductDetailsViewModel
                            {
                                Id = 0,
                                Name = model.ProductName,
                                Slug = (model.AttributeValue + culture.CultureId).Replace("_", "-"),
                                ProductTemplateId = model.ProductTemplateId,
                                CultureId = culture.Id

                            };

                            ProductDetails.Add(productWithCuture);
                        }

                        var productVM = new ProductRequestViewModel
                        {
                            ImagesIds = new List<long>(),
                            ThumbnailImageId = 0,
                            Gtin = null,
                            Sku = null,
                            TaxClassId = null,
                            Price = Convert.ToDecimal(0.01),
                            ForeignPrice = null,
                            SpecialPrice = null,
                            SpecialPriceStart = null,
                            SpecialForignPrice = null,
                            IsCallForPricing = false,
                            IsAllowToOrder = true,
                            StockTrackingIsEnabled = false,
                            IsFeatured = false,
                            SpecialPriceEnd = null,
                            IsPlan = false,
                            ProductDetails = ProductDetails,
                            Status = EStatus.Published

                        };


                        productId = await productServices.AddProduct(productVM);


                        var productAttributeVM = new List<RequestProductValueVM>();

                        var productAttribute = new RequestProductValueVM
                        {
                            AttributeId = model.AttributeId,
                        };

                        productAttribute.AttributeValueCulture.Add(new AttributeValueCultureViewModel
                        {
                            CultureId = CultureId,
                            Value = model.AttributeValue


                        });

                        productAttributeVM.Add(productAttribute);

                        await productAttributeServices.AddProductAttributeValue(productAttributeVM, productId);




                    }
                    await _mediator.Publish(new EntityViewed { EntityId = product == null ? productId : product.Id, EntityTypeId = "Product" });

                    if (product == null)
                    {

                        product = await _productRepository.Query()
                             .Include(x => x.ProductDetails)
                             .Include(x => x.AttributeValues)
                             .Include("AttributeValues.AttributeValueDetails")
                             .Include("AttributeValues.Attribute")
                             .Include("AttributeValues.Attribute.AttributeDetails")
                             .Include(x => x.ProductAvaliablity)
                             .Include(y => y.Medias)
                             .ThenInclude(x => x.Media)
                             .Include(x => x.TaxClass)
                             .ThenInclude(x => x.TaxRate)
                             .Include(x => x.ThumbnailImage)
                             .Where(x => x.SiteId == SiteId)
                             .Where(x => x.Status == EStatus.Published)
                             .Where(y =>
                               y.AttributeValues.Any(x => x.AttributeValueDetails.Any(o => o.Value.ToUpper().Equals(model.AttributeValue.ToUpper()))
                               && x.AttributeId == model.AttributeId))
                             .FirstOrDefaultAsync();

                    }

                    var userCountry = await _userSiteService.GetUserCountry();
                    var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);

                    var Currency = await currencyService.GetUserCurrencyByCulture();
                    var IsLocalCountry = await _userSiteService.IsLocalCountry();


                    if (product == null)
                    {

                        throw new Exception("Product not Found");
                    }

                    var productDetails = new ProductDetail
                    {
                        Id = product.Id,
                        Name = product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                        CalculatedItemPrice = _productPricingService.CalculatedProductPrice(product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                        IsCallForPricing = product.IsCallForPricing,
                        IsAllowToOrder = product.IsAllowToOrder,
                        StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                        ShortDescription = product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().ShortDescription,
                        ReviewsCount = product.ReviewsCount,
                        RatingAverage = product.RatingAverage,
                        AddtionalMetaTags = product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().AddtionalMetaTags,
                        Description = product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Description,
                        Specification = product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Specification,
                        IsAvailable = IsAvaliableProduct(product, userCountry, siteCountryId),
                        ThumbnailImageUrl = product.ThumbnailImage != null ? product.ThumbnailImage.PublicUrl : null,
                        ImagesUrl = product.Medias != null ? product.Medias.Select(x => x.Media.PublicUrl).ToList() : null,
                        ImagesIds = product.Medias != null ? product.Medias.Select(x => x.Media.Id).ToList() : null
                    };
                    await DBTrans.CommitAsync();

                    return productDetails;
                }
                catch (Exception ex)
                {
                    await DBTrans.RollbackAsync();

                    throw ex;
                }
            });
        }
        public async Task<PaginationResult<ProductWebViewModel>> GetProductByCategoryId(ProductSearchViewModel productSearch)
        {
            ValidateProductSearch(productSearch);

            var CultureId = _workContext.GetCurrentCultureId();
            var SiteId = _workContext.GetCurrentSiteId();
            var Currency = await currencyService.GetUserCurrencyByCulture();
            var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);
            var userCountry = await _userSiteService.GetUserCountry();
            var IsLocalCountry = await _userSiteService.IsLocalCountry();
            var WarehouseId = await _workContext.GetCurrentWarehouseId();

            if (userCountry == null)
                throw new InvalidOperationException("Unable to resolve the current user's country.");
            if (Currency == null)
                throw new InvalidOperationException("Unable to resolve the current user's currency.");

            const int BestSellerTopN = 5;
            const int PopularTopN = 5;

            // Base scope for badges: SiteId + Published only, untouched by category/search/price filters.
            var badgeScopeQuery = _productRepository.Query().AsNoTracking()
                .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published);

            var query = _productRepository.Query().AsNoTracking()
                .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published);

            int totalCount;
            List<long> pagedIds;

            if (WarehouseId != 0)
                query = query.Where(x =>
                    !x.ProductBranch.Any() ||
                    x.ProductBranch.Any(c => c.WarehouseId == WarehouseId));

            if (productSearch?.CategoryIds?.Any() == true)
                query = query.Where(x =>
                    x.Categories.Any(c => productSearch.CategoryIds.Contains(c.CategoryId)));
            else if (productSearch?.CategoryId > 0)
                query = query.Where(x =>
                    x.Categories.Any(c => c.CategoryId == productSearch.CategoryId));
            else
                query = query.Where(x => x.Categories.Any());

            if (productSearch?.BrandId > 0)
                query = query.Where(x => x.Brand != null &&
                                          x.Brand.Id == productSearch.BrandId);

            if (productSearch?.IsFeatuerItem.HasValue == true)
                query = query.Where(x => x.IsFeatured == productSearch.IsFeatuerItem.Value);

            if (!string.IsNullOrEmpty(productSearch?.SearchValue))
            {
                var term = productSearch.SearchValue.ToLower();
                query = query.Where(x =>
                    x.ProductDetails.Any(pd =>
                        pd.Name.Contains(term) || pd.Name2.Contains(term)));
            }

            if (WarehouseId != 0)
            {
                query = query.Where(x =>
                    x.Categories.Any(c => c.Category != null && c.Category.CategoryWarehouses.Any())
                        ? x.Categories.Any(c => c.Category != null &&
                            c.Category.CategoryWarehouses.Any(cw => cw.WarehouseId == WarehouseId))
                        : !x.Categories.Any(c => c.Category != null && c.Category.Status == EStatus.Active &&
                                c.Category.Parent != null && c.Category.Parent.CategoryWarehouses.Any())
                           || x.Categories.Any(c => c.Category != null && c.Category.Status == EStatus.Active &&
                                c.Category.Parent != null &&
                                c.Category.Parent.CategoryWarehouses.Any(cw => cw.WarehouseId == WarehouseId)));
            }

            if (productSearch?.TagsId?.Any() == true)
            {
                var allIds = await query.Select(x => x.Id).ToListAsync();
                var taggedIds = await tagsServices.FetchTagsByEntityAsync(
                    allIds, EEntity.Product, productSearch.TagsId) ?? new List<long>();
                query = query.Where(x => taggedIds.Contains(x.Id));
            }

            if (productSearch?.AttributeFilter?.Any() == true)
            {
                var attributeSearchType = productSearch.AttributeFilter
                    .Select(f => f.SearchType)
                    .FirstOrDefault();

                if (attributeSearchType == EProductSearchType.SearchText)
                {
                    var filterValues = productSearch.AttributeFilter
                        .Select(f => f.Value == null ? null : f.Value.ToLower())
                        .Where(v => v != null)
                        .ToList();

                    query = query.Where(x =>
                        x.AttributeValues.Any(av =>
                            av.AttributeValueDetails.Any(avd =>
                                avd.CultureId == CultureId &&
                                avd.Value != null &&
                                filterValues.Contains(avd.Value.ToLower()))));
                }
                else
                {
                    var filterAttributeIds = productSearch.AttributeFilter
                        .Select(f => f.AttributeId)
                        .Where(id => id != null)
                        .ToList();

                    query = query.Where(x =>
                        x.AttributeValues.Any(av => filterAttributeIds.Contains(av.AttributeId)));
                }
            }

            // Site-wide ranks regardless of filters.
            var (badgeOrderRankByProductId, badgeViewRankByProductId) =await GetPopularityRanksAsync(badgeScopeQuery, SiteId, WarehouseId);

            bool sortByPrice = productSearch?.ProductSort?.SortField == ProductSearchSortFieldsEnum.FromMin
                 || productSearch?.ProductSort?.SortField == ProductSearchSortFieldsEnum.FromMax;

            bool filterByPrice = productSearch?.PriceRange != null &&
                                (productSearch.PriceRange.FromPrice > 0 || productSearch.PriceRange.ToPrice.HasValue);

            bool sortByPopularity = productSearch?.ProductSort?.SortField == ProductSearchSortFieldsEnum.Popularity;
            bool sortByMostOrdered = productSearch?.ProductSort?.SortField == ProductSearchSortFieldsEnum.MostOrdered;

            #region priceFilter
            if (sortByPrice || filterByPrice)
            {
                var candidates = await query
                    .Include(x => x.ProductPriceWarehouses)
                    .Include(x => x.DiscountProducts)
                        .ThenInclude(dp => dp.Discount)
                            .ThenInclude(d => d.CustomerGroups)
                                .ThenInclude(cg => cg.CustomerGroup)
                                    .ThenInclude(u => u.Users)
                    .AsSplitQuery()
                    .ToListAsync();

                var priced = candidates.Select(p => new
                {
                    Product = p,
                    Price = _productPricingService.CalculatedProductPrice(
                        p, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry)
                })
                .Where(x => x.Price != null)
                .ToList();

                if (filterByPrice)
                {
                    if (productSearch.PriceRange.FromPrice > 0)
                        priced = priced.Where(x => Math.Round(x.Price.Price, 2) >= productSearch.PriceRange.FromPrice.Value).ToList();
                    if (productSearch.PriceRange.ToPrice.HasValue)
                        priced = priced.Where(x => Math.Round(x.Price.Price, 2) <= productSearch.PriceRange.ToPrice.Value).ToList();
                }

                totalCount = priced.Count;
                if (totalCount == 0)
                    return new PaginationResult<ProductWebViewModel>(new List<ProductWebViewModel>(), 0, 0);

                priced = productSearch.ProductSort?.SortField == ProductSearchSortFieldsEnum.FromMax
                    ? priced.OrderByDescending(x => x.Price.Price).ToList()
                    : priced.OrderBy(x => x.Price.Price).ToList();

                pagedIds = priced
                    .Skip(productSearch.PageNumber * productSearch.PageSize)
                    .Take(productSearch.PageSize)
                    .Select(x => x.Product.Id)
                    .ToList();
            }
            else if (sortByPopularity || sortByMostOrdered)
            {
                // Rank scoped to the CURRENT filtered result set (category/brand/search/etc.),
                var (filteredOrderRank, filteredViewRank) = await GetPopularityRanksAsync(query, SiteId, WarehouseId);
                var rankDict = sortByPopularity ? filteredViewRank : filteredOrderRank;

                var candidateIds = await query
                    .OrderBy(x => x.Id) 
                    .Select(x => x.Id)
                    .ToListAsync();

                totalCount = candidateIds.Count;
                if (totalCount == 0)
                    return new PaginationResult<ProductWebViewModel>(new List<ProductWebViewModel>(), 0, 0);

                var sortType = productSearch.ProductSort.SortType;

                var orderedIds = sortType == ESortType.asc
                    ? candidateIds.OrderBy(id => rankDict.TryGetValue(id, out var r) ? r : int.MaxValue)
                    : candidateIds.OrderByDescending(id => rankDict.TryGetValue(id, out var r) ? r : int.MaxValue);

                pagedIds = orderedIds
                    .Skip(productSearch.PageNumber * productSearch.PageSize)
                    .Take(productSearch.PageSize)
                    .ToList();
            }
            else
            {
                totalCount = await query.CountAsync();
                if (totalCount == 0)
                    return new PaginationResult<ProductWebViewModel>(new List<ProductWebViewModel>(), 0, 0);

                IQueryable<Product> sortedQuery = query;

                if (productSearch?.ProductSort != null)
                {
                    var sort = productSearch.ProductSort;

                    switch (sort.SortField)
                    {
                        case ProductSearchSortFieldsEnum.Name:
                            sortedQuery = sortedQuery.ApplySort(
                                x => x.ProductDetails
                                    .Where(d => d.CultureId == CultureId)
                                    .Select(d => d.Name)
                                    .FirstOrDefault(),
                                sort.SortType);
                            break;

                        case ProductSearchSortFieldsEnum.CreationDate:
                            sortedQuery = sortedQuery.ApplySort(
                                x => x.CreationDate,
                                sort.SortType);
                            break;

                        case ProductSearchSortFieldsEnum.Description:
                            sortedQuery = sortedQuery.ApplySort(
                                x => x.ProductDetails
                                    .Where(d => d.CultureId == CultureId)
                                    .Select(d => d.Description)
                                    .FirstOrDefault(),
                                sort.SortType);
                            break;

                        case ProductSearchSortFieldsEnum.ShortDesciption:
                            sortedQuery = sortedQuery.ApplySort(
                                x => x.ProductDetails
                                    .Where(d => d.CultureId == CultureId)
                                    .Select(d => d.ShortDescription)
                                    .FirstOrDefault(),
                                sort.SortType);
                            break;

                        case ProductSearchSortFieldsEnum.Slug:
                            sortedQuery = sortedQuery.ApplySort(
                                x => x.ProductDetails
                                    .Where(d => d.CultureId == CultureId)
                                    .Select(d => d.SlugInfo != null ? d.SlugInfo.Slug : null)
                                    .FirstOrDefault(),
                                sort.SortType);
                            break;


                        default:
                            sortedQuery = sort.SortType == ESortType.asc
                                ? sortedQuery.OrderBy(x => x.Id)
                                : sortedQuery.OrderByDescending(x => x.Id);
                            break;
                    }
                }
                else if (productSearch?.AttributeSort != null && productSearch.AttributeFilter?.Any() == true)
                {
                    var attributeId = productSearch.AttributeSort.AttributeId;
                    sortedQuery = sortedQuery.ApplySort(
                        x => x.AttributeValues
                            .Where(av => av.AttributeId == attributeId)
                            .SelectMany(av => av.AttributeValueDetails.Where(avd => avd.CultureId == CultureId))
                            .Select(avd => avd.Value)
                            .FirstOrDefault(),
                        productSearch.AttributeSort.SortType);
                }
                else
                {
                    sortedQuery = sortedQuery.OrderByDescending(x => x.Id);
                }

                pagedIds = await sortedQuery
                    .Select(x => x.Id)
                    .Skip(productSearch.PageNumber * productSearch.PageSize)
                    .Take(productSearch.PageSize)
                    .ToListAsync();

                if (!pagedIds.Any())
                    return new PaginationResult<ProductWebViewModel>(new List<ProductWebViewModel>(), 0, totalCount);
            }
            #endregion

            var products = await _productRepository.Query()
                            .AsNoTracking()
                            .Where(x => pagedIds.Contains(x.Id))
                            .Include(x => x.ProductDetails).ThenInclude(x => x.SlugInfo)
                            .Include(x => x.Categories).ThenInclude(c => c.Category).ThenInclude(cd => cd.CategoryDetails).ThenInclude(x => x.SlugInfo)
                            .Include(x => x.Categories).ThenInclude(c => c.Category).ThenInclude(cd => cd.CategoryWarehouses)
                            .Include(x => x.Categories).ThenInclude(c => c.Category).ThenInclude(x => x.Parent).ThenInclude(cd => cd.CategoryWarehouses)
                            .Include(x => x.DiscountProducts).ThenInclude(dp => dp.Discount).ThenInclude(d => d.CustomerGroups).ThenInclude(cg => cg.CustomerGroup).ThenInclude(u => u.Users)
                            .Include(x => x.AttributeValues).ThenInclude(av => av.AttributeValueDetails)
                            .Include(x => x.AttributeValues).ThenInclude(av => av.Attribute).ThenInclude(a => a.AttributeDetails)
                            .Include(x => x.ProductPriceWarehouses)
                            .Include(x => x.ThumbnailImage)
                            .Include(x => x.ProductAvaliablity)
                            .Include(x => x.MainProductReviewProducts).ThenInclude(x => x.Review)
                            .Include(x => x.ProductLinks)
                            .Include(x => x.StockAmounts)
                            .Include(x => x.Stock)
                            .AsSplitQuery()
                            .ToListAsync();

            var tagByProductId = await tagsServices.GetFirstTagByEntityIds(new SearchEntitesViewModel
            {
                EntityIds = pagedIds,
                EntityType = EEntity.Product
            });

            var resultList = pagedIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Select(product =>
                {
                    var detail = product.ProductDetails?.FirstOrDefault(d => d.CultureId == CultureId)
                                 ?? product.ProductDetails?.FirstOrDefault();

                    var categoriesWithCategory = product.Categories?.Where(c => c.Category != null).ToList();

                    var detailCategory = categoriesWithCategory == null || !categoriesWithCategory.Any()
                        ? null
                        : categoriesWithCategory
                            .Where(x => x.Category.CategoryDetails != null)
                            .SelectMany(x => x.Category.CategoryDetails)
                            .FirstOrDefault(d => d.CultureId == CultureId)
                          ?? categoriesWithCategory
                            .Where(x => x.Category.CategoryDetails != null)
                            .SelectMany(x => x.Category.CategoryDetails)
                            .FirstOrDefault();

                    var approvedReviews = product.MainProductReviewProducts
                        ?.Where(i => i.MainProductId == product.Id
                                     && i.Review != null && i.Review.Status == ReviewStatus.Approved)
                        .Select(r => r.Review.Rating)
                        .ToList() ?? new List<int>();

                    var badge = ResolveBadge(
                        creationDate: product.CreationDate,
                        orderRank: badgeOrderRankByProductId.TryGetValue(product.Id, out var or) ? or : int.MaxValue,
                        viewRank: badgeViewRankByProductId.TryGetValue(product.Id, out var vr) ? vr : int.MaxValue,
                        newProductDays: 14,
                        bestSellerTopN: BestSellerTopN,
                        popularTopN: PopularTopN);

                    return new ProductWebViewModel
                    {
                        Id = product.Id,
                        CategorySlug = detailCategory?.Slug,
                        Name = detail?.Name,
                        Name2 = detail?.Name2,
                        ShortDescription = detail?.ShortDescription,
                        CreationDate = product.CreationDate,
                        Slug = detail?.SlugInfo?.Slug,
                        Specification = detail?.Specification,
                        Description = detail?.Description,
                        ProductTemplateId = detail?.ProductTemplateId,
                        AddtionalMetaTags = detail?.AddtionalMetaTags,
                        ThumbnailUrl = product.ThumbnailImage?.PublicUrl,
                        Price = _productPricingService.CalculatedProductPrice(
                            product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                        IsAvailable =
                            (userCountry.IsRegional || siteCountryId == userCountry.Id || product.ForeignPrice.HasValue) &&
                            (product.ProductAvaliablity == null || !product.ProductAvaliablity.Any() ||
                             product.ProductAvaliablity.Any(i =>
                                 i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed)),
                        IsFeatured = product.IsFeatured,
                        DisplayOrder = product.DisplayOrder,
                        InStock =
                            (product.StockAmounts?.Any(x => x.Quantity > 0) ?? false) &&
                            (WarehouseId == 0 || !(product.Stock?.Any(x =>
                                x.Status == EStatus.Unpublished && x.WarehouseId == WarehouseId) ?? false)),
                        IsPromotional = product.IsPromotional,
                        StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                        Rating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(), 1) : 0,
                        RatingCount = approvedReviews.Count(),
                        Attribute = product.AttributeValues == null
                            ? new List<ProductAttribueWithvaluesVM>()
                            : product.AttributeValues.Select(at =>
                                new ProductAttribueWithvaluesVM
                                {
                                    Id = at.AttributeId,
                                    Name = at.Attribute?.AttributeDetails
                                        ?.Where(ad => ad.CultureId == CultureId)
                                        .Select(n => n.Name).FirstOrDefault(),
                                }).ToList(),
                        Type = badge.ToString(),
                        Tag = tagByProductId.TryGetValue(product.Id, out var tagVm) ? tagVm : null,
                        AttributeValues = product.AttributeValues == null
                            ? new List<ProductAttribueWithvaluesModel>()
                            : product.AttributeValues.Select(av => new ProductAttribueWithvaluesModel
                            {
                                Id = av.Id,
                                value = av.AttributeValueDetails
                                        ?.Where(avd => avd.CultureId == CultureId).Select(v => v.Value).FirstOrDefault(),
                                AttributeId = av.AttributeId
                            }).ToList(),
                        HasVariations = product.ProductLinks?.Any(x => x.LinkType == ItemLinkType.Super) ?? false,
                        categoryProductWarehouses = categoriesWithCategory == null
                            ? new List<CategoryProductWarehouse>()
                            : categoriesWithCategory
                                .Where(x => x.Category.CategoryWarehouses != null)
                                .SelectMany(x => x.Category.CategoryWarehouses)
                                .Select(x => new CategoryProductWarehouse { CategoryId = x.CategoryId, WarehouseId = x.WarehouseId })
                                .ToList(),
                        ParentcategoryProductWarehouses = categoriesWithCategory == null
                            ? new List<CategoryProductWarehouse>()
                            : categoriesWithCategory
                                .Where(c => c.Category.Parent != null && c.Category.Parent.CategoryWarehouses != null && c.Category.Status == EStatus.Active)
                                .SelectMany(c => c.Category.Parent.CategoryWarehouses)
                                .Select(cw => new CategoryProductWarehouse { CategoryId = cw.CategoryId, WarehouseId = cw.WarehouseId })
                                .ToList(),
                    };
                }).ToList();

            var totalPages = productSearch.PageSize > 0
                ? (int)Math.Ceiling(totalCount / (double)productSearch.PageSize)
                : 0;

            return new PaginationResult<ProductWebViewModel>(resultList, totalPages, totalCount);
        }
        public async Task<PaginationResult<GetProductAttributeGroupViewModel>> GetProductAttributeByCategoryId(FilterProductAttributeViewModel model)
        {
            try
            {
                var query = productCategoryRepository.Query()
                    .AsNoTracking()
                    .Where(x => model.CategoryId == null || x.CategoryId == model.CategoryId)
                    .SelectMany(pc => pc.Product.AttributeValues
                        .Where(av => av.Attribute != null && av.Attribute.Group != null)
                        .Select(av => new
                        {
                            GroupId = av.Attribute.Group.Id,
                            GroupName = av.Attribute.Group.productAttributeGroupDetails
                                .OrderBy(d => d.Id)
                                .Select(d => d.Name)
                                .FirstOrDefault(),
                            av.AttributeId,
                            AttributeValueId = av.Id,
                            Value = av.AttributeValueDetails
                                .OrderBy(d => d.Id)
                                .Select(d => d.Value)
                                .FirstOrDefault()
                        }));

                // Grouping and projection done at DB level
                var grouped = await query
                    .GroupBy(x => new { x.GroupId, x.GroupName })
                    .Select(g => new GetProductAttributeGroupViewModel
                    {
                        GroupId = g.Key.GroupId,
                        GroupName = g.Key.GroupName,
                        AttributeValues = g
                            .GroupBy(av => av.Value)
                            .Select(av => new GetProductAttributeViewModel
                            {
                                AttributeId = av.FirstOrDefault().AttributeId,
                                AttributeValueId = av.FirstOrDefault().AttributeValueId,
                                Value = av.Key
                            })
                            .ToList()
                    })
                    .PaginateAsync(model.PageSize, model.PageNumber);

                return grouped;
            }
            catch (Exception ex)
            {
                // Ideally log before rethrow
                throw;
            }
        }

        public async Task<ProductViewModelForWeb> GetProductsbyCategoryIdForWeb(ProductParamswithCategoryIdVM model)
        {
            try
            {
                ValidateProductParams(model);

                ProductParamsModel Params = new ProductParamsModel();
                Params.CategoryId = model.CategoryId.HasValue ? new long[] { model.CategoryId.Value } : new long[] { };
                Params.Sort = model.Sort ?? new ProductSearchSort { SortField = Array.Empty<int>() };
                Params.PageNumber = model.PageNumber;
                Params.PageSize = model.PageSize;
                Params.IsFeatuerItem = model.IsFeatuerItem;
                var result = await GetProductsForWeb(Params);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<ProductViewModelForWeb> GetProductsbyIdsForWeb(ProductParamswithIdVM productParamsVM)

        {
            try
            {
                ProductParamsModel model = new ProductParamsModel();
                model.ProductIds = productParamsVM.ProductIds;
                model.CategoryId = new long[] { };
                model.PageSize = productParamsVM.PageSize;
                model.Sort = productParamsVM.Sort;
                model.PageNumber = productParamsVM.PageNumber;
                var result = await GetProductsForWeb(model);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<List<CatelogSettingsViewModel>> GetProductsByProductsIds(long[] ids)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var Query = await _productRepository.Query()
                                                     .Where(x => ids.Contains(x.Id) && x.SiteId == SiteId)
                                                     .Include(x => x.ThumbnailImage)
                                                     .Include(x => x.ProductDetails)
                                                     .Select(x => new CatelogSettingsViewModel

                                                     {
                                                         Description = x.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description,
                                                         Name = x.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                                                         ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl
                                                     }).ToListAsync();
                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<GetTagsViewModel>> GetProductTagsByCategoryId(ProductTagsViewModel product)
        {
            try
            {
                var WarehouseId = await _workContext.GetCurrentWarehouseId();
                var ProductsIds = await productCategoryRepository.Query()
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductBranch)
                                                                 .Where(x => product.CategoryIds == null
                                                                 || product.CategoryIds.Any(y => y == x.CategoryId))
                                                                 .Where(x => x.Product.Status != EStatus.Deleted)
                                                                 .Where(x => WarehouseId == 0 ||
                                                                 !x.Product.ProductBranch.Any() ||
                                                                 x.Product.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                                                                 .Select(x => x.ProductId)
                                                                 .ToListAsync();
                if (ProductsIds == null)
                {
                    return new List<GetTagsViewModel>();
                }
                return await tagsServices.GetTagsByEntityType(new SearchEntitesViewModel
                {
                    EntityIds = ProductsIds,
                    EntityType = product.EntityType

                });


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<ProductForWeb>> GetAllProducts(ProductFiltrationViewModel model)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var Currency = await currencyService.GetUserCurrencyByCulture();
                var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);
                var userCountry = await _userSiteService.GetUserCountry();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();
                var gridData = await _productRepository.Query()
                    .Include(x => x.ProductDetails)
                    .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId)
                     .Where(x => !model.IsPromotional.HasValue || model.IsPromotional == x.IsPromotional)
                     .Where(x => !model.IsFeatured.HasValue || model.IsFeatured == x.IsFeatured)
                     .Where(x => WarehouseId == 0 || x.ProductBranch.Any() && x.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                     .Where(x => !x.LinkedProductLinks.Any())
                    .Include(x => x.ThumbnailImage)
                    .Include("ProductDetails.SlugInfo")
                    .Include("ProductDetails.Culture")
                    .Include(x => x.ProductPriceWarehouses)
                    .Include(x => x.ProductAvaliablity)
                    .Include(x => x.DiscountProducts)
                    .Include("DiscountProducts.Discount")
                    .Include("DiscountProducts.Discount.DiscountTiers")
                     .Include("DiscountProducts.Discount.CustomerGroups")
                    .Include("DiscountProducts.Discount.CustomerGroups.CustomerGroup")
                    .Include("DiscountProducts.Discount.CustomerGroups.CustomerGroup.Users")
                    .Select(x => new ProductForWeb
                    {
                        Id = x.Id,
                        IsFeatured = x.IsFeatured,
                        IsPromotional = x.IsPromotional,
                        ThuumbnailImageUrl = x.ThumbnailImageId.HasValue ? x.ThumbnailImage.PublicUrl : null,
                        Description = x.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Description).FirstOrDefault(),
                        Name = x.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(),
                        Slug = x.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.SlugInfo.Slug).FirstOrDefault(),
                        Price = _productPricingService.CalculatedProductPrice(x, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                        IsAvailable = IsAvaliableProduct(x, userCountry, siteCountryId),
                    })
                    .AsNoTrackingWithIdentityResolution()
                    .OrderByDescending(x => x.Id)
                    .PaginateAsync(model.Pagination.PageSize, model.Pagination.PageNumber);
                return gridData;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region Helpers
        private async Task<ProductViewModelForWeb> GetProductsForWeb(ProductParamsModel model)
        {
            try
            {
                ValidateProductParams(model);
                model.Sort ??= new ProductSearchSort { SortField = Array.Empty<int>() };
                model.Sort.SortField ??= Array.Empty<int>();

                var Products = new ProductViewModelForWeb();
                var Currency = await currencyService.GetUserCurrencyByCulture();
                var userCountry = await _userSiteService.GetUserCountry();
                var siteCountryId = await _userSiteService.GetSiteCountry(_workContext.GetCurrentSiteId());
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();


                var query = _productRepository
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published)
                    .Where(x => WarehouseId == 0 || !x.ProductBranch.Any() || x.ProductBranch.Any(c => c.WarehouseId == WarehouseId));
                var padgeQuery = query;
                // Apply filters
                if (model.IsFeatuerItem.HasValue)
                    query = query.Where(x => x.IsFeatured == model.IsFeatuerItem);
                if (model.CategoryId != null && model.CategoryId.Any())
                    query = query.Where(x =>
                           x.Categories.Any(c => model.CategoryId.Contains(c.CategoryId)));
                if (model.ProductIds != null && model.ProductIds.Any())
                    query = query.Where(x => model.ProductIds.Contains(x.Id));

                // Apply sorting
                if (model.Sort.SortField.Any())
                {
                    if (model.Sort.SortField.Contains((int)ProductSearchSortFieldsEnum.FromMin))
                        query = query.OrderBy(x => x.Price);
                    else if (model.Sort.SortField.Contains((int)ProductSearchSortFieldsEnum.FromMax))
                        query = query.OrderByDescending(x => x.Price);
                    else if (model.Sort.SortField.Contains((int)ProductSearchSortFieldsEnum.Description))
                        query = model.Sort.SortType == ESortType.asc
                            ? query.OrderBy(x => x.ProductDetails.Where(pd => pd.CultureId == CultureId).Select(pd => pd.Description).FirstOrDefault())
                            : query.OrderByDescending(x => x.ProductDetails.Where(pd => pd.CultureId == CultureId).Select(pd => pd.Description).FirstOrDefault());
                    else if (model.Sort.SortField.Contains((int)ProductSearchSortFieldsEnum.Name))
                        query = model.Sort.SortType == ESortType.asc
                            ? query.OrderBy(x => x.ProductDetails.Where(pd => pd.CultureId == CultureId).Select(pd => pd.Name).FirstOrDefault())
                            : query.OrderByDescending(x => x.ProductDetails.Where(pd => pd.CultureId == CultureId).Select(pd => pd.Name).FirstOrDefault());
                    else if (model.Sort.SortField.Contains((int)ProductSearchSortFieldsEnum.CreationDate))
                        query = model.Sort.SortType == ESortType.asc
                            ? query.OrderBy(x => x.CreationDate)
                            : query.OrderByDescending(x => x.CreationDate);

                }

                var itemCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(itemCount / (double)model.PageSize);
                if (itemCount == 0)
                {
                    return new ProductViewModelForWeb
                    {
                        Catname = string.Empty,
                        products = new PaginationResult<ProductForWeb>(
                            new List<ProductForWeb>(), 0, 0
                        )
                    };
                }

                var pageIds = await query
                    .Select(x => x.Id)
                    .Skip(model.PageNumber * model.PageSize)
                    .Take(model.PageSize)
                    .ToListAsync();

                if (!pageIds.Any())
                {
                    return new ProductViewModelForWeb
                    {
                        Catname = string.Empty,
                        products = new PaginationResult<ProductForWeb>(
                            new List<ProductForWeb>(), totalPages, itemCount
                        )
                    };
                }

                var pageOrder = pageIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(x => x.id, x => x.index);
                var dataList = await _productRepository
                    .Query()
                    .AsNoTracking()
                    .Where(x => pageIds.Contains(x.Id))
                    .Include(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.Categories)
                    .ThenInclude(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .Include(x => x.ThumbnailImage)
                    .Include(x => x.StockAmounts)
                    .Include(x => x.ProductAvaliablity)
                    .Include(x => x.ProductLinks)
                        .ThenInclude(pl => pl.LinkedProduct)
                            .ThenInclude(lp => lp.DiscountProducts)
                                .ThenInclude(dp => dp.Discount)
                                    .ThenInclude(d => d.CustomerGroups)
                                        .ThenInclude(cg => cg.CustomerGroup)
                                            .ThenInclude(u => u.Users)
                    .Include(x => x.ProductPriceWarehouses)
                    .Include(x => x.DiscountProducts)
                    .ThenInclude(x => x.Discount)
                    .ThenInclude(x => x.CustomerGroups)
                    .ThenInclude(x => x.CustomerGroup)
                    .ThenInclude(x => x.Users)
                    .Include(x => x.DiscountProducts)
                    .ThenInclude(x => x.Discount)
                    .ThenInclude(x => x.DiscountTiers)
                    .AsSplitQuery()
                    .ToListAsync();
         
                dataList = dataList
                    .OrderBy(x => pageOrder[x.Id])
                    .ToList();
                var tagByProductId = await tagsServices.GetFirstTagByEntityIds(new SearchEntitesViewModel
                {
                    EntityIds = pageIds,
                    EntityType = EEntity.Product
                });
                var (orderRankByProductId, viewRankByProductId) = await GetPopularityRanksAsync(padgeQuery, SiteId, WarehouseId);
                var data = dataList.Select(x =>
                {
                    var details = x.ProductDetails.FirstOrDefault(pd => pd.CultureId == CultureId);
                    var badge = ResolveBadge(
                        creationDate: x.CreationDate,
                        orderRank: orderRankByProductId.TryGetValue(x.Id, out var or) ? or : int.MaxValue,
                        viewRank: viewRankByProductId.TryGetValue(x.Id, out var vr) ? vr : int.MaxValue);
                    var approvedReviews = x.MainProductReviewProducts
                        ?.Where(i => (i.MainProductId == x.Id || i.ProductId == x.Id)
                                     && i.Review != null && i.Review.Status == ReviewStatus.Approved
                                     && i.Product != null && i.Product.Status == EStatus.Published)
                        .Select(r => r.Review.Rating)
                        .ToList() ?? new List<int>();

                    var offerEndsAt = x.DiscountProducts
                              .Where(dp => dp.Discount.Status == EStatus.Published &&
                                           (!dp.Discount.StartDate.HasValue || dp.Discount.StartDate <= DateTimeOffset.UtcNow) &&
                                           (!dp.Discount.ExpiryDate.HasValue || dp.Discount.ExpiryDate >= DateTimeOffset.UtcNow))
                              .Select(dp => dp.Discount.ExpiryDate)
                              .Concat(
                                  (x.ProductLinks ?? new List<ProductLink>())
                                      .Where(pl => pl.LinkType == ItemLinkType.Super)
                                      .SelectMany(pl => pl.LinkedProduct?.DiscountProducts ?? new List<DiscountProduct>())
                                      .Where(dp => dp.Discount.Status == EStatus.Published &&
                                                   (!dp.Discount.StartDate.HasValue || dp.Discount.StartDate <= DateTimeOffset.UtcNow) &&
                                                   (!dp.Discount.ExpiryDate.HasValue || dp.Discount.ExpiryDate >= DateTimeOffset.UtcNow))
                                      .Select(dp => dp.Discount.ExpiryDate))
                              .Where(d => d.HasValue)
                              .OrderBy(d => d)
                              .FirstOrDefault();
                    return new ProductForWeb
                    {
                        Id = x.Id,
                        ShortDescription = details?.ShortDescription,
                        Description = details?.Description,
                        Name = details?.Name,
                        Type = badge.ToString(),
                        Tag = tagByProductId.TryGetValue(x.Id, out var tagVm) ? tagVm : null,
                        Slug = details?.SlugInfo?.Slug,
                        ThuumbnailImageUrl = x.ThumbnailImage?.PublicUrl ?? "",
                        Price = _productPricingService.CalculatedProductPrice(
                            x,
                            userCountry.Id,
                            Currency.Rate,
                            Currency.Code,
                            IsLocalCountry),
                        IsCallForPricing = x.IsCallForPricing,
                        IsFeatured = x.IsFeatured,
                        IsAvailable = (userCountry.IsRegional || siteCountryId == userCountry.Id || x.ForeignPrice.HasValue) &&
                                  (x.ProductAvaliablity == null || !x.ProductAvaliablity.Any() ||
                                   x.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed)),
                        InStock = x.StockAmounts.Any(s => s.Quantity > 0),
                        IsPromotional = x.IsPromotional,
                        Rating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(), 1) : 0,
                        RatingCount = approvedReviews.Count(),
                        HasVariations = x.ProductLinks?.Any(l => l.LinkType == ItemLinkType.Super) ?? false,
                        OfferEndsAt=offerEndsAt
                    };
                }).ToList();

                var cateName = dataList
                    .SelectMany(x => x.Categories)
                    .Select(c => c.Category.CategoryDetails.Where(cd => cd.CultureId == CultureId).Select(d => d.Name).FirstOrDefault())
                    .FirstOrDefault();

                var products = new ProductViewModelForWeb
                {
                    Catname = cateName?.ToString() ?? string.Empty,
                    products = new PaginationResult<ProductForWeb>(data, totalPages, itemCount)
                };

                return products;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private static void ValidateProductSearch(ProductSearchViewModel productSearch)
        {
            if (productSearch == null)
            {
                throw new ArgumentException("Product search request is required.");
            }

            if (productSearch.PageSize <= 0)
            {
                throw new ArgumentException("PageSize must be greater than zero.");
            }

            if (productSearch.PageNumber < 0)
            {
                throw new ArgumentException("PageNumber cannot be negative.");
            }
        }

        private static void ValidateProductParams(ProductParamswithCategoryIdVM model)
        {
            if (model == null)
            {
                throw new ArgumentException("Product request is required.");
            }

            if (model.PageSize <= 0)
            {
                throw new ArgumentException("PageSize must be greater than zero.");
            }

            if (model.PageNumber < 0)
            {
                throw new ArgumentException("PageNumber cannot be negative.");
            }
        }

        private static void ValidateProductParams(ProductParamsModel model)
        {
            if (model == null)
            {
                throw new ArgumentException("Product request is required.");
            }

            if (model.PageSize <= 0)
            {
                throw new ArgumentException("PageSize must be greater than zero.");
            }

            if (model.PageNumber < 0)
            {
                throw new ArgumentException("PageNumber cannot be negative.");
            }
        }
        public async Task<MinMaxProductsViewModel> GetMinAndMaxPriceProduct()
        {
            try
            {
                var Result = new MinMaxProductsViewModel();
                var CultureId = _workContext.GetCurrentCultureId();
                var SiteId = _workContext.GetCurrentSiteId();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();


                var userCountry = await _userSiteService.GetUserCountry();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var Query = _productRepository
                                           .Query()
                                           .AsNoTracking()
                                           .Include(x => x.ProductDetails)
                                           .Include(x => x.ProductAvaliablity)
                                           .Include(x => x.ProductBranch)
                                           .Include(x => x.ProductPriceWarehouses)
                                           .Where(x => WarehouseId == 0 || !x.ProductBranch.Any() || x.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                                           .Where(x => x.ProductDetails.Any(x => x.CultureId == CultureId))
                                           .Where(x =>
                                           x.SiteId == SiteId && x.Status == EStatus.Published)
                                           .Where(pc => pc.ProductAvaliablity.Count() == 0 || pc.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed))
                                           .Where(x => !x.LinkedProductLinks.Any(p => p.LinkType == ItemLinkType.Super))
;


                if (!IsLocalCountry)
                {

                    Query = Query.Where(x => x.ForeignPrice.HasValue);
                    Result.MaxpriceProduct = Query.OrderByDescending(x => x.ForeignPrice).Select(x => new PriceProductViewModel
                    {
                        ProductId = x.Id,
                        ProductName = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                        ProductPrice = x.ForeignPrice.Value

                    }).FirstOrDefault();

                    Result.MinpriceProduct = Query.OrderBy(x => x.ForeignPrice).Select(x => new PriceProductViewModel
                    {
                        ProductId = x.Id,
                        ProductName = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                        ProductPrice = x.ForeignPrice.Value

                    }).FirstOrDefault();
                }
                else
                {

                    // Min price product
                    Result.MinpriceProduct = Query
                        .Select(x => new PriceProductViewModel
                        {
                            ProductId = x.Id,
                            ProductName = x.ProductDetails
                                .Where(d => d.CultureId == CultureId)
                                .FirstOrDefault().Name,
                            ProductPrice = x.PriceType == EPriceType.ProductBase
                                ? x.Price.HasValue ? x.Price.Value : 0
                                : x.ProductPriceWarehouses
                                    .Where(pw => pw.WarehouseId == WarehouseId)
                                    .Select(pw => pw.Price)
                                    .FirstOrDefault()
                        })
                        .OrderBy(x => x.ProductPrice)
                        .FirstOrDefault();

                    // Max price product
                    Result.MaxpriceProduct = Query
                        .Select(x => new PriceProductViewModel
                        {
                            ProductId = x.Id,
                            ProductName = x.ProductDetails
                                .Where(d => d.CultureId == CultureId)
                                .FirstOrDefault().Name,
                            ProductPrice = x.PriceType == EPriceType.ProductBase
                                ? x.Price.HasValue ? x.Price.Value : 0
                                : x.ProductPriceWarehouses
                                    .Where(pw => pw.WarehouseId == WarehouseId)
                                    .Select(pw => pw.Price)
                                    .FirstOrDefault()
                        })
                        .OrderByDescending(x => x.ProductPrice)
                        .FirstOrDefault();

                }


                var CurrencyRate = await currencyService.GetUserCurrencyRateOrDefault();
                string DefaultDecimalPlaces = currencyService.GetDefaultDecimalPlaces();
                Result.MinpriceProduct.ProductPrice = currencyService.ConvertToCurrency(Result.MinpriceProduct != null ? Result.MinpriceProduct.ProductPrice : 0, CurrencyRate, DefaultDecimalPlaces);
                Result.MaxpriceProduct.ProductPrice = currencyService.ConvertToCurrency(Result.MaxpriceProduct != null ? Result.MaxpriceProduct.ProductPrice : 0, CurrencyRate, DefaultDecimalPlaces);
                Result.Currency = currencyService.GetCurrencyCode(CurrencyRate.CultureCode);
                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion
        #region Mapping
        private void MapProductOptionToProductVm(Product product, ProductDetail model, long CultureId)
        {

            var SiteId = _workContext.GetCurrentSiteId();


            foreach (var item in product.OptionCombinations.OrderBy(x => x.SortIndex))
            {
                model.AvailableOptions.Add(
                   new ItemDetailOption
                   {
                       OptionName = item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault() == null ? "" : item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                       OptionId = item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault() == null ? 0 : item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Id,
                       Values = item.Option.productOptionValues.SelectMany(x => x.productOptionValueDetails).Where(x => x.CultureId == CultureId).Select(x => x.Value).ToList()
                   });

            }

        }
        private void MapProductImagesToProductVm(Product product, ProductDetail model, long CultureId)
        {
            model.ImagesUrl = product.Medias.Select(x => x.Media.PublicUrl).ToList();
        }

        private async Task MapProductVariantToProductVm(Product product, ProductDetail model, long CultureId, bool IsLocalCountry, CurrencyByCultureVM Currency, UserCountryVM userCountry)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var siteCountryId = await _userSiteService.GetSiteCountry(SiteId);
            var WarehouseId = await _workContext.GetCurrentWarehouseId();

            // Pre-calculate values used in loop
            var userCountryId = userCountry.Id;
            var currencyRate = Currency.Rate;
            var currencyCode = Currency.Code;

            var variations = await productLinkRepository
                .Query()
                .AsNoTracking()
                .Where(link => link.ProductId == product.Id && link.LinkType == ItemLinkType.Super && link.LinkedProduct.SiteId == SiteId && link.LinkedProduct.Status == EStatus.Published)
                .Include(x => x.LinkedProduct.ProductDetails.Where(pd => pd.CultureId == CultureId))
                .Include(x => x.LinkedProduct.ProductPriceWarehouses)
                .Include(x => x.LinkedProduct.ProductBranch)
                .Include(x => x.LinkedProduct.ProductLinks)
                .Include(x => x.LinkedProduct.StockAmounts)
                .Include(x => x.LinkedProduct.ProductAvaliablity)
                .Include(x => x.LinkedProduct.OptionCombinations.OrderBy(oc => oc.SortIndex))
                    .ThenInclude(oc => oc.Value.productOptionValueDetails.Where(povd => povd.CultureId == CultureId))
                .Include(x => x.LinkedProduct.OptionCombinations)
                    .ThenInclude(oc => oc.Option.optionDetails.Where(od => od.CultureId == CultureId))
                .Include(x => x.LinkedProduct.ProductAdditionalFees.Where(paf => paf.Status != EStatus.Deleted))
                .Include(x => x.LinkedProduct.AttributeValues)
                    .ThenInclude(av => av.AttributeValueDetails.Where(avd => avd.CultureId == CultureId))
                .Include(x => x.LinkedProduct.AttributeValues)
                    .ThenInclude(av => av.Attribute.AttributeDetails.Where(ad => ad.CultureId == CultureId))
                .Include(x => x.LinkedProduct.Medias.OrderBy(m => m.DisplayOrder))
                    .ThenInclude(m => m.Media)
                .Include(x => x.LinkedProduct.TaxClass.TaxRate)
                .Include(x => x.LinkedProduct.DiscountProducts)
                    .ThenInclude(dp => dp.Discount.CustomerGroups)
                    .ThenInclude(cg => cg.CustomerGroup.Users)
                .Include(x => x.LinkedProduct.DiscountProducts)
                    .ThenInclude(dp => dp.Discount.DiscountTiers)
                .Include(x => x.LinkedProduct.ReviewProducts.Where(rp => rp.Review.Status == ReviewStatus.Approved))
                    .ThenInclude(rp => rp.Review)
                .Include(x => x.LinkedProduct.Stock.Where(s => s.Status == EStatus.Unpublished && (WarehouseId == 0 || s.WarehouseId == WarehouseId)))
                .Select(x => x.LinkedProduct)
                .Where(x => WarehouseId == 0 || !x.ProductBranch.Any() || x.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                .AsSplitQuery()
                .ToListAsync();

            foreach (var variation in variations)
            {
                // Use FirstOrDefault once - data already filtered by CultureId in Include
                var productDetail = variation.ProductDetails.FirstOrDefault();
                var hasStockQuantity = variation.StockAmounts?.Any(x => x.Quantity > 0) ?? false;
                var hasUnpublishedStock = variation.Stock?.Any() ?? false;
                var approvedReviews = variation.ReviewProducts;

                var variationVm = new ItemDetailVariation
                {
                    Id = variation.Id,
                    Name = productDetail?.Name,
                    NormalizedName = productDetail?.NormalizedName,
                    IsAllowToOrder = variation.IsAllowToOrder,
                    InStock = hasStockQuantity && (WarehouseId == 0 || !hasUnpublishedStock),
                    IsCallForPricing = variation.IsCallForPricing,
                    StockTrackingIsEnabled = variation.StockTrackingIsEnabled,
                    IsAvailable = IsAvaliableProduct(variation, userCountry, siteCountryId),
                    ShortDescription = productDetail?.ShortDescription,
                    Description = productDetail?.Description,
                    SpecialPrice = variation.SpecialPrice,
                    HasRelated = variation.ProductLinks?.Any(x => x.LinkType == ItemLinkType.Related) ?? false,
                    HasSub = variation.ProductLinks?.Any(x => x.LinkType == ItemLinkType.Sub) ?? false,
                    SpecialPriceStart = variation.SpecialPriceStart,
                    ImagesUrl = variation.Medias?.Select(x => x.Media.PublicUrl).ToList() ?? new List<string>(),
                    SpecialPriceEnd = variation.SpecialPriceEnd,
                    CalculatedItemPrice = _productPricingService.CalculatedProductPrice(variation, userCountryId, currencyRate, currencyCode, IsLocalCountry),
                    AdditionalFees = variation.ProductAdditionalFees?.Select(x => new ItemVariationFeesVM
                    {
                        Name = x.Name,
                        FeesValue = x.FeeValue,
                        FeesType = x.FeeType,
                        FeesValueString = currencyService.FormatByUserCurrency(x.FeeValue)
                    }).ToList() ?? new List<ItemVariationFeesVM>(),
                    Attributes = variation.AttributeValues?.Select(x => new ItemDetailAttribute
                    {
                        Name = x.Attribute.AttributeDetails.FirstOrDefault()?.Name ?? "",
                        Value = x.AttributeValueDetails.FirstOrDefault()?.Value ?? ""
                    }).ToList() ?? new List<ItemDetailAttribute>(),
                    RatingAverage = approvedReviews?.Any() == true
                        ? Math.Round(approvedReviews.Select(y => y.Review.Rating).Average(), 1, MidpointRounding.AwayFromZero)
                        : 0,
                    ReviewsCount = approvedReviews?.Count ?? 0
                };

                // Options already ordered in Include and filtered by CultureId
                if (variation.OptionCombinations != null)
                {
                    foreach (var combination in variation.OptionCombinations)
                    {
                        variationVm.Options.Add(new ItemDetailVariationOption
                        {
                            OptionId = combination.OptionId,
                            OptionName = combination.Option?.optionDetails.FirstOrDefault()?.Name,
                            ValueId = combination.ValueId,
                            Value = combination.Value?.productOptionValueDetails.FirstOrDefault()?.Value ?? "",
                            SortIndex = combination.SortIndex
                        });
                    }
                }

                model.Variations.Add(variationVm);
            }
        }

        private static bool IsAvaliableProduct(Product product, UserCountryVM userCountry, string siteCountryId)
        {
            var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

            return AllowdProductBaseedOnPrice && (product.ProductAvaliablity.Count() == 0 || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed));
        }
        private async Task MapRelatedProductToProductVm(Product product, ProductDetail model, long CultureId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userCountry = await _userSiteService.GetUserCountry();
            var siteCountryId = await _userSiteService.GetSiteCountry(siteId);
            var Currency = await currencyService.GetUserCurrencyByCulture();
            var IsLocalCountry = await _userSiteService.IsLocalCountry();
            var WarehouseId = await _workContext.GetCurrentWarehouseId();

            var publishedProductLinks = product.ProductLinks.Where(x => x.LinkedProduct.Status != EStatus.Deleted
                                                             && (x.LinkType == ItemLinkType.Related || x.LinkType == ItemLinkType.CrossSell))
                                                            .Where(x => WarehouseId == 0 || !x.LinkedProduct.ProductBranch.Any() || x.LinkedProduct.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                                                            .Where(pc => pc.LinkedProduct.ProductAvaliablity.Count() == 0 || pc.LinkedProduct.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed))
;


            if (product == null)
            {
                return;
            }
            foreach (var productLink in publishedProductLinks)
            {
                var linkedProduct = productLink.LinkedProduct;
                linkedProduct.ProductDetails = linkedProduct.ProductDetails.Where(x => x.CultureId == CultureId).ToList();
                var productThumbnail = ProductThumbnail.FromProduct(linkedProduct, userCountry, siteCountryId, CultureId);

                productThumbnail.CalculatedProductPrice = _productPricingService.CalculatedProductPrice(linkedProduct, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry);

                if (productLink.LinkType == ItemLinkType.Related)
                {
                    model.RelatedProducts.Add(productThumbnail);
                }

                else if (productLink.LinkType == ItemLinkType.CrossSell)
                {
                    model.CrossSellProducts.Add(productThumbnail);
                }
                else if (productLink.LinkType == ItemLinkType.Sub)
                {
                    model.SubProducts.Add(productThumbnail);
                }
            }
        }

        private void MapBiddingHistoryToProductVM(Product product, ProductDetail model, long CultureId)
        {
            var BiddingHisoty = biddingTrackerRepository
                .Query()
                .AsNoTrackingWithIdentityResolution()
                .Where(f => f.AuctionId == product.Id)
                .OrderByDescending(o => o.CreationDate)
                .ToList();

            foreach (var history in BiddingHisoty)
            {
                var BiddingVM = new BiddingTrackerVM
                {
                    Id = history.Id,
                    BiddingAmount = history.BiddingAmount,
                    BuyerId = history.BuyerId,
                    BuyerName = _userRepository.Query().AsNoTracking().Where(f => f.Id == history.BuyerId).Select(f => f.FullName).FirstOrDefault(),
                    SellerId = history.SellerId,
                    SellerName = _userRepository.Query().AsNoTracking().Where(f => f.Id == history.SellerId).Select(f => f.FullName).FirstOrDefault(),
                    CreatedById = history.CreatedById,
                    AuctionId = history.AuctionId,
                    CreationDate = history.CreationDate
                };
                model.BiddingHistory.Add(BiddingVM);
            }
        }

        public async Task<ProductViewModelForWeb> GetProductsbyCategoryIds(ProductParamsViewModel model)
        {
            try
            {
                ProductParamsModel Params = new ProductParamsModel();
                Params.CategoryId = model.CategoryId;
                Params.Sort = model.Sort;
                Params.PageNumber = model.PageNumber;
                Params.PageSize = model.PageSize;
                Params.IsFeatuerItem = model.IsFeatuerItem;
                var result = await GetProductsForWeb(Params);
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
 
        #endregion
        #region Helpers
        private async Task<(Dictionary<long, int> OrderRankByProductId, Dictionary<long, int> ViewRankByProductId)>
            GetPopularityRanksAsync(IQueryable<Product> catalogScopeQuery, long siteId, long warehouseId)
        {
            var cachKey = $"popularity-ranks:{siteId}:{warehouseId}";
            var cached = _cacheService.GetData < (Dictionary<long, int>, Dictionary<long, int>)?>(cachKey);
            if(cached.HasValue)
                return cached.Value;

            var catalogIds = await catalogScopeQuery.Select(x => x.Id).ToListAsync();

            var orderRankByProductId = await GetOrderRankByProductIdAsync(catalogIds, siteId, warehouseId)
                                        ?? new Dictionary<long, int>();
            var viewRankByProductId = await GetViewRankByProductIdAsync(catalogIds)
                                        ?? new Dictionary<long, int>();

            var result=(orderRankByProductId, viewRankByProductId);
            _cacheService.SetData(cachKey, result, DateTimeOffset.UtcNow.AddMinutes(10));
            return result;
        }

        private async Task<Dictionary<long, int>> GetOrderRankByProductIdAsync(
          IEnumerable<long> productIds, long siteId, long warehouseId)
        {
            var mainIds = productIds.ToList();
            if (!mainIds.Any()) return new Dictionary<long, int>();


            var variationLinks = await productLinkRepository.Query()
                .Where(pl => mainIds.Contains(pl.ProductId) && pl.LinkType == ItemLinkType.Super)
                .Select(pl => new { MainId = pl.ProductId, VariationId = pl.LinkedProductId })
                .ToListAsync();

            var productIdToMainId = variationLinks
                .GroupBy(x => x.VariationId)
                .ToDictionary(g => g.Key, g => g.First().MainId);

            foreach (var id in mainIds)
                if (!productIdToMainId.ContainsKey(id))
                    productIdToMainId[id] = id;

            var allOrderableIds = productIdToMainId.Keys.ToList();

            var orderItems = await _orderItemRepository.Query()
                .Where(oi =>
                    allOrderableIds.Contains(oi.ProductId) &&
                    (warehouseId == 0 || oi.Order.WarehouseId == warehouseId) &&
                    (oi.Order.OrderStatus == OrderStatus.CashOnDelivery ||
                     oi.Order.OrderStatus == OrderStatus.PaymentReceived ||
                     oi.Order.OrderStatus == OrderStatus.Complete) &&
                     oi.Product.Status == EStatus.Published &&
                    oi.Order.SiteId == siteId)
                .Select(oi => new { oi.ProductId, oi.Quantity })
                .ToListAsync();

            var orderScores = orderItems
                .GroupBy(oi => productIdToMainId.TryGetValue(oi.ProductId, out var mainId) ? mainId : oi.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSales = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalSales)
                .Select(x => x.ProductId)
                .ToList();

            return orderScores
                .Select((id, index) => new { id, rank = index + 1 })
                .ToDictionary(x => x.id, x => x.rank);
        }

        private async Task<Dictionary<long, int>> GetViewRankByProductIdAsync(IEnumerable<long> productIds)
        {
            var mainIds = productIds.ToList();
            if (!mainIds.Any()) return new Dictionary<long, int>();

            var variationLinks = await productLinkRepository.Query()
                .Where(pl => mainIds.Contains(pl.ProductId) && pl.LinkType == ItemLinkType.Super)
                .Select(pl => new { MainId = pl.ProductId, VariationId = pl.LinkedProductId })
                .ToListAsync();

            var productIdToMainId = variationLinks
                .GroupBy(x => x.VariationId)
                .ToDictionary(g => g.Key, g => g.First().MainId);

            foreach (var id in mainIds)
                if (!productIdToMainId.ContainsKey(id))
                    productIdToMainId[id] = id;

            var allViewableIds = productIdToMainId.Keys.ToList();

            var views = await _recentlyViewedProductRepository.Query()
                .Where(v => allViewableIds.Contains(v.ProductId))
                .Select(v => new { v.ProductId, v.UserId })
                .ToListAsync();

            var viewScores = views
                .GroupBy(v => productIdToMainId.TryGetValue(v.ProductId, out var mainId) ? mainId : v.ProductId)
                .Select(g => new { ProductId = g.Key, ViewCount = g.Select(v => v.UserId).Distinct().Count() })
                .OrderByDescending(x => x.ViewCount)
                .Select(x => x.ProductId)
                .ToList();

            return viewScores
                .Select((id, index) => new { id, rank = index + 1 })
                .ToDictionary(x => x.id, x => x.rank);
        }

        private static ProductBadgeType ResolveBadge(
            DateTimeOffset creationDate,
            int orderRank,
            int viewRank,
            int newProductDays = 14,
            int bestSellerTopN = 5,
            int popularTopN = 5)
        {
            if (creationDate >= DateTimeOffset.UtcNow.AddDays(-newProductDays))
                return ProductBadgeType.New;

            if (orderRank <= bestSellerTopN)
                return ProductBadgeType.BestSeller;

            if (viewRank <= popularTopN)
                return ProductBadgeType.Popular;

            return ProductBadgeType.Classic;
        }

    
             public async Task<ProductViewModelForWeb> GetTodaysOffers(ProductOffersVM ProductOffersVM)
        {
            var now = DateTimeOffset.Now;
            var SiteId = _workContext.GetCurrentSiteId();
            var WarehouseId = await _workContext.GetCurrentWarehouseId();

            var candidates = await _productRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published)
                .Where(x => WarehouseId == 0 || !x.ProductBranch.Any() || x.ProductBranch.Any(c => c.WarehouseId == WarehouseId))
                .Select(x => new
                {
                    x.Id,
                    OwnDiscountDates = x.DiscountProducts
                        .Where(dp => dp.Discount.Status == EStatus.Published &&
                        (!dp.Discount.StartDate.HasValue || dp.Discount.StartDate <= DateTimeOffset.UtcNow) &&
                                           (!dp.Discount.ExpiryDate.HasValue || dp.Discount.ExpiryDate >= DateTimeOffset.UtcNow))
                        .Select(dp => dp.Discount.CreationDate)
                    
                })
                .ToListAsync();

            var eligible = candidates
                .Select(x => new
                {
                    x.Id,
                    LatestDiscountCreated = x.OwnDiscountDates
                        .DefaultIfEmpty(DateTimeOffset.MinValue)
                        .Max()
                })
                .Where(x => x.LatestDiscountCreated != DateTimeOffset.MinValue)
                .OrderByDescending(x => x.LatestDiscountCreated)
                .Select(x => x.Id)
                .ToList();

            if (!eligible.Any())
                return new ProductViewModelForWeb
                {
                    Catname = string.Empty,
                    products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(), 0, 0)
                };

            return await GetProductsbyIdsForWeb(new ProductParamswithIdVM
            {
                ProductIds = eligible,
                PageNumber = ProductOffersVM.PageNumber,
                PageSize = ProductOffersVM.PageSize,
                Sort = null
            });
        }
    

        #endregion

    }
}
