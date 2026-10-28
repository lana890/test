using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels;
using EskaCommerce.ProductRecentlyViewed.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ProductRecentlyViewed.Services
{
    public class RecentlyViewedProductServices : IRecentlyViewedProductServices
    {
        private readonly IWorkContext workContext;
        private readonly IRepository<ProductDetails> productdetailsRepository;
        private readonly IProductPricingService productPricingService;
        private readonly ICurrencyService currencyService;
        private readonly IRepository<RecentlyViewedProduct> recentlyViewdProduct;
        private readonly IRepository<Product> productRepository;
        private readonly IRepository<ProductLink> productLinkRepository;
        private readonly IUserSiteService _userSiteService;

        public RecentlyViewedProductServices(IWorkContext workContext,

            IRepository<ProductDetails> productdetailsRepository,
            IProductPricingService productPricingService,
            ICurrencyService currencyService,
            IUserSiteService userSiteService,
            IRepository<RecentlyViewedProduct> RecentlyViewdProduct,
            IRepository<ProductLink> productLinkRepository,

            IRepository<Product> productRepository)
        {
            this.workContext = workContext;
            this.productdetailsRepository = productdetailsRepository;
            this.productPricingService = productPricingService;
            this.currencyService = currencyService;
            recentlyViewdProduct = RecentlyViewdProduct;
            this.productRepository = productRepository;
            this.productLinkRepository= productLinkRepository;
            _userSiteService = userSiteService;
        }
        public async Task<PaginationResult<RecentlyViewUserVM>> GetRecentlyViewedProduct(RecentlyRequestViewModel model)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var siteId = await workContext.GetCurrentSiteIdAsync();
                var CultureId = workContext.GetCurrentCultureId();
                var Currency = await currencyService.GetUserCurrencyByCulture();
                var userCountry = await _userSiteService.GetUserCountry();
                var siteCountryId = await _userSiteService.GetSiteCountry(siteId);
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var WarehouseId = await workContext.GetCurrentWarehouseId();

                IQueryable<RecentlyViewedProduct> baseQuery = recentlyViewdProduct
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.Product.SiteId == siteId && x.Product.Status == GeneralEnums.EStatus.Published);

                if (model.ByUserId)
                {
                    if (UserId == 0)
                        throw new Exception("User not logged in but byUserId = true");

                    baseQuery = baseQuery.Where(x => x.UserId == UserId);
                }

                if (WarehouseId > 0)
                {
                    baseQuery = baseQuery.Where(x => x.Product.ProductBranch != null &&
                                                      x.Product.ProductBranch.Any(c => c.WarehouseId == WarehouseId));
                }

                var viewedRows = await baseQuery
                    .OrderByDescending(x => x.LatestViewedOn)
                    .Select(x => new { x.Id, x.ProductId, x.LatestViewedOn })
                    .ToListAsync();

                if (!viewedRows.Any())
                    return new PaginationResult<RecentlyViewUserVM>(new List<RecentlyViewUserVM>(), 0, 0);

                var viewedProductIds = viewedRows.Select(x => x.ProductId).Distinct().ToList();

                var variantToParent = await productLinkRepository.Query()
                    .AsNoTracking()
                    .Where(l => l.LinkType == ItemLinkType.Super && viewedProductIds.Contains(l.LinkedProductId))
                    .Select(l => new { ParentId = l.ProductId, VariantId = l.LinkedProductId })
                    .ToDictionaryAsync(x => x.VariantId, x => x.ParentId);

                var effectiveViews = viewedRows.Select(row => new
                {
                    EffectiveProductId = variantToParent.TryGetValue(row.ProductId, out var parentId) ? parentId : row.ProductId,
                    row.LatestViewedOn
                });

                var mostRecentViewByMainProduct = effectiveViews
                    .GroupBy(x => x.EffectiveProductId)
                    .Select(g => new { MainProductId = g.Key, LastViewedOn = g.Max(x => x.LatestViewedOn) })
                    .OrderByDescending(x => x.LastViewedOn)
                    .ToList();

                var mainProductIds = mostRecentViewByMainProduct.Select(x => x.MainProductId).ToList();

                var products = await productRepository.Query() 
                    .AsNoTrackingWithIdentityResolution()
                    .AsSplitQuery()
                    .Where(p => mainProductIds.Contains(p.Id)
                                && p.SiteId == siteId
                                && p.Status == GeneralEnums.EStatus.Published
                                && p.ProductDetails.Any(d => d.CultureId == CultureId))
                    .Include(p => p.ProductLinks)
                    .Include(p => p.ProductPriceWarehouses)
                    .Include(p => p.ProductBranch)
                    .Include(p => p.StockAmounts)
                    .Include(p => p.ProductAvaliablity)
                    .Include(p => p.MainProductReviewProducts)
                        .ThenInclude(r => r.Review)
                    .Include(p => p.ProductDetails)
                        .ThenInclude(d => d.SlugInfo)
                    .Include(p => p.TaxClass)
                    .Include(p => p.ThumbnailImage)
                    .Include(p => p.DiscountProducts)
                        .ThenInclude(dp => dp.Discount)
                            .ThenInclude(d => d.DiscountTiers)
                    .Include(p => p.DiscountProducts)
                        .ThenInclude(dp => dp.Discount.CustomerGroups)
                            .ThenInclude(cg => cg.CustomerGroup)
                                .ThenInclude(g => g.Users)
                    .Include(p => p.Categories)
                        .ThenInclude(c => c.Category)
                            .ThenInclude(c => c.CategoryDetails)
                                .ThenInclude(d => d.SlugInfo)
                    .ToListAsync();

                var productsById = products.ToDictionary(p => p.Id);

                var orderedVMs = new List<RecentlyViewUserVM>();
                foreach (var entry in mostRecentViewByMainProduct)
                {
                    if (!productsById.TryGetValue(entry.MainProductId, out var product))
                        continue; 

                    var approvedReviews = product.MainProductReviewProducts
                                        ?.Where(i => i.MainProductId == product.Id
                                                     && i.Review != null && i.Review.Status == ReviewStatus.Approved)
                                        .Select(r => r.Review.Rating)
                                        .ToList() ?? new List<int>();

                    orderedVMs.Add(new RecentlyViewUserVM
                    {
                        Id = product.Id,
                        Slug = product.ProductDetails.FirstOrDefault(d => d.CultureId == CultureId)?.SlugInfo?.Slug ?? "",
                        Name = product.ProductDetails.FirstOrDefault(d => d.CultureId == CultureId)?.Name ?? "",
                        IsAvailable = IsAvaliableProduct(product, userCountry, siteCountryId),
                        ThuumbnailImageUrl = product.ThumbnailImage?.PublicUrl ?? "",
                        IsCallForPricing = product.IsCallForPricing,
                        Price = productPricingService.CalculatedProductPrice(product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                        CreationDate = product.CreationDate,
                        CategorySlug = product.Categories.SelectMany(c => c.Category.CategoryDetails).FirstOrDefault(d => d.CultureId == CultureId)?.SlugInfo?.Slug,
                        InStock = product.StockAmounts.Any(s => s.Quantity > 0),
                        Rating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(), 1) : 0,
                        HasVariations = product.ProductLinks != null && product.ProductLinks.Any(l => l.LinkType == ItemLinkType.Super)
                    });
                }

                var totalCount = orderedVMs.Count;
                var pageNumber = model.PageNumber;
                var pageSize = model.PageSize;
                var startIndex = pageNumber * pageSize;

                var paginatedResults = orderedVMs.Skip(startIndex).Take(pageSize).ToList();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return new PaginationResult<RecentlyViewUserVM>(paginatedResults, totalPages, totalCount);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static bool IsAvaliableProduct(Product product, UserCountryVM userCountry, string siteCountryId)
        {
            try
            {

                var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

                return (AllowdProductBaseedOnPrice && (product.ProductAvaliablity == null || product.ProductAvaliablity.Count() == 0 || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed)));

            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<PaginationResult<MostViewedProducts>> GetMostViewedProduct(SmartTableParam model)
        {
            try
            {

                var cultureId = workContext.GetCurrentCultureId();
                var siteId = workContext.GetCurrentSiteId();

                var query = productRepository.Query()
                                                .Where(x => x.SiteId == siteId && x.Status == GeneralEnums.EStatus.Published)
                                                .Include(x => x.ProductDetails)
                                                .Include(x => x.ProductViews)

                                                .Select(x => new MostViewedProducts
                                                {
                                                    ProductId = x.Id,
                                                    Count = x.ProductViews.Count(),
                                                    Name = cultureId == 0 ? x.ProductDetails.OrderBy(x => x.CultureId).FirstOrDefault().Name : x.ProductDetails.FirstOrDefault(x => x.CultureId == cultureId).Name
                                                }).AsNoTracking();




                var result = await query.OrderByDescending(x => x.Count)
                                        .PaginateAsync(model.Pagination.Number, model.Pagination.Start);


                return result;



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }








    }
}
