using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea.Interfaces;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using ProductAvaliablity = EskaCMS.Entities.Entity.Catalog.ProductAvaliablity;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea
{
    public class ProductServices : IProductServices
    {
        private readonly IWorkContext workContext;
        private readonly IRepository<ProductMedia> productMediaRepository;
        private readonly IVariationServices variationServices;
        private readonly IRepository<ProductCategory> productCategoryRepository;
        private readonly ICurrencyService currencyService;
        private readonly IRepository<ProductLink> productLinkRepository;
        private readonly IRepository<ProductDetails> productDetailsrepository;
        private readonly ISlugServices slugServices;
        private readonly IRepository<StockAmount> stockAmountRepository;
        private readonly IProductPricingService productPricingService;
        private readonly IRepository<ProductPriceHistory> productPricehistoryRepository;
        private readonly EskaDCMSDbContext context;
        private readonly IRepository<Product> productRepository;
        private readonly ISearchRepository<NaviagtableSearchModel> _searchRepository;
        private readonly ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> _meiliRepo;

        private readonly ISearchRepository<ProductElasticModel> _productSearchRepository;
        private readonly IClientCacheService clientCacheService;
        private readonly ICacheService cacheService;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IUserSiteService _userSiteService;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<ProductAvaliablity> productAvaliablityRepository;
        private readonly IRepository<ProductCategoryHistory> _productCategoryHistoryRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> appSettingRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<BrandDetails> brandDetailsRepository;
        private readonly IConfiguration _config;
        public ProductServices(IWorkContext workContext, IRepository<ProductMedia> ProductMediaRepository,
            IVariationServices variationServices,
             IUserSiteService userSiteService,
             IRepository<User> userRepository,
             IRepository<Stock> stockRepository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<BrandDetails> brandDetailsRepository,
            IRepository<ProductCategoryHistory> productCategoryHistoryRepository,
             IRepository<ProductAvaliablity> ProductAvaliablityRepository,
             IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IRepository<ProductCategory> ProductCategoryRepository, ICurrencyService currencyService, IRepository<OrderItem> orderItemRepository,
            IRepository<ProductLink> productLinkRepository, IRepository<ProductDetails> ProductDetailsrepository,
            ISlugServices SlugServices, IRepository<StockAmount> StockAmountRepository, IProductPricingService productPricingService, IRepository<ProductPriceHistory> productPricehistoryRepository, EskaDCMSDbContext context, IRepository<Product> ProductRepository,
            ISearchRepository<NaviagtableSearchModel> searchRepository, IClientCacheService clientCacheService, ISearchRepository<ProductElasticModel> productSearchRepository,
            ICacheService cacheService,
            IConfiguration config,
            ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> meiliRepo
)
        {
            this.workContext = workContext;
            productMediaRepository = ProductMediaRepository;
            this.variationServices = variationServices;
            productCategoryRepository = ProductCategoryRepository;
            this.currencyService = currencyService;
            this.productLinkRepository = productLinkRepository;
            productDetailsrepository = ProductDetailsrepository;
            slugServices = SlugServices;
            stockAmountRepository = StockAmountRepository;
            this.productPricingService = productPricingService;
            this.productPricehistoryRepository = productPricehistoryRepository;
            this.context = context;
            productRepository = ProductRepository;
            _searchRepository = searchRepository;
            this.clientCacheService = clientCacheService;
            this.cacheService = cacheService;
            _orderItemRepository = orderItemRepository;
            _userSiteService = userSiteService;
            this.userRepository = userRepository;
            productAvaliablityRepository = ProductAvaliablityRepository;
            this.appSettingRepository = appSettingRepository;
            _stockRepository = stockRepository;
            _warehouseRepository = warehouseRepository;
            this.brandDetailsRepository=brandDetailsRepository;
            _productCategoryHistoryRepository = productCategoryHistoryRepository;
            _productSearchRepository = productSearchRepository;
            _meiliRepo = meiliRepo;
            _config = config;
        }


        public async Task<(bool, List<long> InValidProducts)> CheckAllowedQuantity(List<CheckProductQuantityVM> products, long customerId)
        {

            var UserRole = await userRepository.Query().Where(x => x.Id == customerId).Include(x => x.UserRoles).SelectMany(x => x.UserRoles).Select(x => x.RoleId).FirstOrDefaultAsync();
            var ExceptRoleAllowedToMaxQuantity = await appSettingRepository.Query().Where(x => x.Id == "ExceptRoleAllowedToMaxQuantity").FirstOrDefaultAsync();

            if (ExceptRoleAllowedToMaxQuantity != null)
            {
                var ExceptRole = Convert.ToInt64(ExceptRoleAllowedToMaxQuantity.Value);
                if (UserRole != ExceptRole)
                {
                    return (true, new List<long>());
                }

            }

            var ProductIds = products.Select(x => x.ProductId).ToList();

            var InValidProducts = new List<long>();

            var productsInDb = await productRepository.Query().Where(x => x.MaxAllowedQuantity > 0 && ProductIds.Contains(x.Id)).ToListAsync();

            var previousOrderItems = _orderItemRepository.Query()
                                                    .AsNoTracking()
                                                    .Include(x => x.Order)
                                                  .Where(x => x.Order.CustomerId == customerId &&
                                                                ProductIds.Contains(x.ProductId) &&
                                                                x.Order.OrderStatus != OrderStatus.Canceled &&
                                                                x.Order.OrderStatus != OrderStatus.PendingPayment)
                                                  .ToList();




            foreach (var product in productsInDb)
            {


                var InValidProduct = products
                                              .Where(x => x.ProductId == product.Id && x.Quantity > product.MaxAllowedQuantity)
                                              .FirstOrDefault();
                if (InValidProduct != null)
                {
                    InValidProducts.Add(InValidProduct.ProductId);
                    continue;
                }

                //check previous orders 
                if (previousOrderItems.Any())
                {
                    int quantityFromOrders = previousOrderItems
                                               .Where(x => x.ProductId == product.Id)
                                               .Sum(x => x.Quantity);
                    var requestedProduct = products
                                              .Where(x => x.ProductId == product.Id)
                                              .FirstOrDefault();
                    if (quantityFromOrders + requestedProduct.Quantity > product.MaxAllowedQuantity)
                        InValidProducts.Add(product.Id);
                }

            }

            if (InValidProducts.Any())
                return (false, InValidProducts);

            return (true, new List<long>());
        }
        public async Task<long> AddProduct(ProductRequestViewModel model)
        {


            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var UserId = await workContext.GetCurrentUserId();

                var product = new Product();

                product.VendorId = model.VendorId.HasValue ? model.VendorId.Value : null;
                product.ThumbnailImageId = model.ThumbnailImageId == 0 || model.ThumbnailImageId == null ? null : model.ThumbnailImageId;
                product.Status = model.Status;
                product.RequesStatus = ERequestStatus.Approved;
                product.RequestStatusDate = DateTime.Now;
                product.ReviewsCount = 0;
                product.SiteId = SiteId;
                product.SpecialPrice = model.SpecialPrice;


                product.Price =model.Price==0 ? null : model.Price;


                product.SpecialPriceEnd = model.SpecialPriceEnd;
                product.SpecialPriceStart = model.SpecialPriceStart;
                product.BuyoutPrice = model.BuyoutPrice;
                product.MinPrice = model.MinPrice;
                product.BrandId = model.BrandId == 0 || model.BrandId == null ? null : model.BrandId;
                product.SpecialForignPrice = model.SpecialForignPrice;
                product.IsProductSerial = model.IsProductSerial;
                product.CreatedById = UserId;
                product.CreationDate = DateTime.Now;
                product.DisplayOrder = model.DisplayOrder;
                product.BidEndtDate = model.BidEndtDate.HasValue ? model.BidEndtDate.Value : null;
                product.BidStartDate = model.BidStartDate.HasValue ? model.BidStartDate.Value : null;
                product.BidIncrement = model.BidIncrement.HasValue ? model.BidIncrement.Value : null;
                product.BuyoutPrice = model.BuyoutPrice.HasValue ? model.BuyoutPrice.Value : null;
                product.MinPrice = model.MinPrice.HasValue ? model.MinPrice.Value : 0;
                product.IsFeatured = model.IsFeatured;
                product.ForeignPrice = model.ForeignPrice;
                product.IsAllowForeign = model.IsAllowForeign;
                product.StockTrackingIsEnabled = model.StockTrackingIsEnabled;
                product.Type = ProductTypes.Product;
                product.Sku = model.Sku;
                product.Gtin = model.Gtin;
                product.IsAllowToOrder = model.IsAllowToOrder;
                product.TaxClassId = model.TaxClassId == null || model.TaxClassId == 0 ? null : model.TaxClassId.Value;
                product.MaxAllowedQuantity = model.MaxAllowedQuantity;
                product.IsPlan = model.IsPlan;
                product.PublishDate = model.PublishDate;
                product.ExpiryDate = model.ExpiryDate;
                product.PriceType = model.PriceType;
                product.IsPromotional = model.IsPromotional;
                product.IsCallForPricing = model.IsCallForPricing ?? false;
                product.IsVisibleIndividually = model.IsVisibleIndividually ?? false;

                await ValidateStatusAndPublishDate_ExpireDate(product.Status, product.PublishDate, product.ExpiryDate);

                productRepository.Add(product);
                await productRepository.SaveChangesAsync();

                #region  ProductPriceHistory

                await HandleProductPriceHistory(product, null, UserId);

                #endregion

                #region ProductAvaliablity


                if (model.ProductAvaliablity != null && model.ProductAvaliablity.Count() > 0)
                {
                    var ProductAvaliablityvariation = new List<ProductAvaliablity>();
                    ProductAvaliablityvariation.AddRange(model.ProductAvaliablity.ConvertAll(x => new ProductAvaliablity
                    {

                        CountryId = x.CountryId,
                        Permission = x.Permission,
                        ProductId = product.Id,
                        StateOrProvinceId = x.StateOrProvinceId == 0 ? null : x.StateOrProvinceId
                    }));


                    if (model.applyForVariations)
                    {
                        var variations = await productLinkRepository.Query().Where(x => x.ProductId == product.Id && x.LinkType == ItemLinkType.Super).Select(x => x.LinkedProductId).ToListAsync();

                        foreach (var Id in variations)
                        {
                            ProductAvaliablityvariation.AddRange(model.ProductAvaliablity.ConvertAll(x => new ProductAvaliablity
                            {

                                CountryId = x.CountryId,
                                Permission = x.Permission,
                                ProductId = Id,
                                StateOrProvinceId = x.StateOrProvinceId == 0 ? null : x.StateOrProvinceId
                            }));

                        }

                    }
                    productAvaliablityRepository.AddRange(ProductAvaliablityvariation);
                    await productAvaliablityRepository.SaveChangesAsync();
                }
                #endregion




                #region AddProductDetails
                var ProductDetails = new List<ProductDetails>();

                foreach (var item in model.ProductDetails)
                {
                    var Productdetails = new ProductDetails();

                    SlugManagementVm slugManagementVm = new()
                    {
                        CultureId = item.CultureId,

                        Slug = item.Slug,
                        Entity = EEntity.Product,
                        SiteId = SiteId

                    };
                    Productdetails.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                    Productdetails.ShortDescription = item.ShortDescription;
                    Productdetails.Description = item.Description;
                    Productdetails.Specification = item.Specification;
                    Productdetails.ProductTemplateId = item.ProductTemplateId;
                    Productdetails.Name = item.Name;
                    Productdetails.Name2 = model.BrandId.HasValue ? await brandDetailsRepository.Query().Where(x => model.BrandId.Value==x.BrandId && x.CultureId==item.CultureId).Select(x => x.Name).FirstOrDefaultAsync() : null;
                    Productdetails.AddtionalMetaTags = item.AdditonalMetaTags;
                    Productdetails.CultureId = item.CultureId;
                    Productdetails.NormalizedName = item.NormalizedName;
                    Productdetails.ProductId = product.Id;

                    ProductDetails.Add(Productdetails);


                }
                productDetailsrepository.AddRange(ProductDetails);
                await productDetailsrepository.SaveChangesAsync();
                #endregion

                if (model.CategoryIds.Count() > 0)
                {
                    IList<ProductCategory> AddProductCategory = new List<ProductCategory>();

                    foreach (var CategoryId in model.CategoryIds)
                    {

                        AddProductCategory.Add(new ProductCategory() { ProductId = product.Id, CategoryId = CategoryId });


                    }
                    productCategoryRepository.AddRange(AddProductCategory);
                    await productCategoryRepository.SaveChangesAsync();
                }
                int DisplayOrder = 0;

                // Batch the product-media inserts: build all rows, then a single
                // SaveChanges (was one DB round trip per image). Same rows/order inserted.
                var productMedias = new List<ProductMedia>();
                foreach (var Image in model.ImagesIds)
                {
                    productMedias.Add(new ProductMedia
                    {
                        ProductId = product.Id,
                        MediaId = Image,
                        DisplayOrder = DisplayOrder,
                    });
                    DisplayOrder += 1;
                }

                if (productMedias.Count > 0)
                {
                    productMediaRepository.AddRange(productMedias);
                    await productMediaRepository.SaveChangesAsync();
                }

            
                return product.Id;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<long> UpdateProduct(ProductRequestViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var UserId = await workContext.GetCurrentUserId();
                //    var userCountry = await _userSiteService.GetUserCountry();
                // var Currencey = await currencyService.GetUserCurrencyByCulture();
                var product = await productRepository.Query()

                      .Include(x => x.ThumbnailImage)
                      .Include(x => x.Medias)
                      .ThenInclude(m => m.Media)
                      .Include(x => x.TaxClass)
                      .ThenInclude(x => x.TaxRate)
                      .Include(x => x.ProductDetails)
                      .ThenInclude(x => x.SlugInfo)
                      .Where(x => x.Id == model.Id)
                      .FirstOrDefaultAsync();

                var orginalProduct = new Product { Id = product.Id, SpecialForignPrice = product.SpecialForignPrice, SpecialPriceStart = product.SpecialPriceStart, SpecialPriceEnd = product.SpecialPriceEnd, Price = product.Price, ForeignPrice = product.ForeignPrice, IsAllowForeign = product.IsAllowForeign, SpecialPrice = product.SpecialPrice };
                product.VendorId = model.VendorId.HasValue ? model.VendorId : null;
                product.ThumbnailImageId = model.ThumbnailImageId == 0 || model.ThumbnailImageId == null ? null : model.ThumbnailImageId;
                product.Status = model.Status;
                product.RequesStatus = ERequestStatus.Approved;
                product.RequestStatusDate = DateTime.Now;
                product.ReviewsCount = 0;
                product.SiteId = SiteId;
                product.BrandId = model.BrandId == 0 || model.BrandId == null ? null : model.BrandId;
                product.SpecialPrice = model.SpecialPrice;
                //if (model.Price == null || model.Price <= 0)
                //{
                //    throw new Exception("The Price lower than 0 ");
                //}

                //else
                //{

                product.Price =model.Price==0 ? null : model.Price;

                //}

                if (model.ProductAvaliablity != null && model.ProductAvaliablity.Count() > 0)
                {
                    var RemoveProductAvaliablity = await productAvaliablityRepository.Query().Where(x => x.ProductId == product.Id).ToListAsync();

                    productAvaliablityRepository.RemoveRange(RemoveProductAvaliablity);

                    await productAvaliablityRepository.SaveChangesAsync();
                    var ProductAvaliablityvariation = new List<ProductAvaliablity>();
                    ProductAvaliablityvariation.AddRange(model.ProductAvaliablity.ConvertAll(x => new ProductAvaliablity
                    {

                        CountryId = x.CountryId,
                        Permission = x.Permission,
                        ProductId = product.Id,
                        StateOrProvinceId = x.StateOrProvinceId == 0 ? null : x.StateOrProvinceId
                    }));


                    if (model.applyForVariations)
                    {
                        var variations = await productLinkRepository.Query().Where(x => x.ProductId == product.Id && x.LinkType == ItemLinkType.Super).Select(x => x.LinkedProductId).ToListAsync();

                        var RemoveProductVariationsAvaliablity = await productAvaliablityRepository.Query().Where(x => variations.Contains(x.ProductId)).ToListAsync();

                        productAvaliablityRepository.RemoveRange(RemoveProductVariationsAvaliablity);

                        await productAvaliablityRepository.SaveChangesAsync();


                        foreach (var Id in variations)
                        {
                            ProductAvaliablityvariation.AddRange(model.ProductAvaliablity.ConvertAll(x => new ProductAvaliablity
                            {

                                CountryId = x.CountryId,
                                Permission = x.Permission,
                                ProductId = Id,
                                StateOrProvinceId = x.StateOrProvinceId == 0 ? null : x.StateOrProvinceId
                            }));

                        }

                    }
                    productAvaliablityRepository.AddRange(ProductAvaliablityvariation);
                    await productAvaliablityRepository.SaveChangesAsync();

                }
                else
                {
                    var ProductAvaliablity = await productAvaliablityRepository.Query().Where(x => x.ProductId == product.Id).ToListAsync();

                    productAvaliablityRepository.RemoveRange(ProductAvaliablity);

                    await productAvaliablityRepository.SaveChangesAsync();

                }

                product.SpecialPriceEnd = model.SpecialPriceEnd;
                product.SpecialPriceStart = model.SpecialPriceStart;
                product.BuyoutPrice = model.BuyoutPrice;
                product.MinPrice = model.MinPrice;
                product.SpecialForignPrice = model.SpecialForignPrice;
                product.IsProductSerial = model.IsProductSerial;
                product.ModifiedById = UserId;
                product.Sku = model.Sku;
                product.ForeignPrice = model.ForeignPrice;
                product.IsAllowForeign = model.IsAllowForeign;
                product.Gtin = model.Gtin;
                product.ModificationDate = DateTime.Now;
                product.DisplayOrder = model.DisplayOrder;
                product.BidEndtDate = model.BidEndtDate.HasValue ? model.BidEndtDate.Value : null;
                product.BidStartDate = model.BidStartDate.HasValue ? model.BidStartDate.Value : null;
                product.BidIncrement = model.BidIncrement.HasValue ? model.BidIncrement.Value : null;
                product.BuyoutPrice = model.BuyoutPrice.HasValue ? model.BuyoutPrice.Value : null;
                product.MinPrice = model.MinPrice.HasValue ? model.MinPrice.Value : 0;
                product.IsFeatured = model.IsFeatured;
                product.StockTrackingIsEnabled = model.StockTrackingIsEnabled;
                product.Type = ProductTypes.Product;
                product.IsAllowToOrder = model.IsAllowToOrder;
                product.TaxClassId = model.TaxClassId == null || model.TaxClassId == 0 ? null : model.TaxClassId.Value;
                product.MaxAllowedQuantity = model.MaxAllowedQuantity;
                product.IsPlan = model.IsPlan;
                product.PublishDate = model.PublishDate;
                product.ExpiryDate = model.ExpiryDate;
                product.PriceType = model.PriceType;
                product.IsPromotional = model.IsPromotional;
                product.IsCallForPricing = model.IsCallForPricing ?? product.IsCallForPricing;
                product.IsVisibleIndividually = model.IsVisibleIndividually ?? product.IsVisibleIndividually;

                await ValidateStatusAndPublishDate_ExpireDate(product.Status, product.PublishDate, product.ExpiryDate);

                await productRepository.SaveChangesAsync();


                #region  ProductPriceHistory

                await HandleProductPriceHistory(orginalProduct, product, UserId);

                #endregion


                #region AddProductDetails

                foreach (var item in model.ProductDetails)
                {

                    var existingProductDetail = product.ProductDetails?.FirstOrDefault(pro => pro.CultureId == item.CultureId);
                    if (existingProductDetail != null)
                    {
                        cacheService.RemoveByPrefix("Slug" + existingProductDetail.SlugInfo.Slug + existingProductDetail.CultureId + SiteId);
                        await clientCacheService.RemoveCacheFile(existingProductDetail.SlugInfo.Slug, SiteId, existingProductDetail.CultureId);
                        SlugManagementVm slugManagementVm = new()
                        {
                            Id = existingProductDetail.SlugInfo.Id,
                            CultureId = item.CultureId,

                            Slug = item.Slug,
                            Entity = EEntity.Product,
                            SiteId = SiteId

                        };
                        existingProductDetail.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                        existingProductDetail.ShortDescription = item.ShortDescription;
                        existingProductDetail.Description = item.Description;
                        existingProductDetail.Specification = item.Specification;
                        existingProductDetail.ProductTemplateId = item.ProductTemplateId;
                        existingProductDetail.Name = item.Name;
                        existingProductDetail.Name2 = model.BrandId.HasValue ? await brandDetailsRepository.Query().Where(x => model.BrandId.Value==x.BrandId && x.CultureId==item.CultureId).Select(x => x.Name).FirstOrDefaultAsync()  : null;
                        existingProductDetail.AddtionalMetaTags = item.AdditonalMetaTags;

                        existingProductDetail.CultureId = item.CultureId;
                        existingProductDetail.NormalizedName = item.NormalizedName;
                        existingProductDetail.ProductId = product.Id;
                        await productDetailsrepository.SaveChangesAsync();
                    }
                    else
                    {

                        SlugManagementVm slugManagementVm = new()
                        {
                            CultureId = item.CultureId,
                            Slug = item.Slug,
                            Entity = EEntity.Product,
                            SiteId = SiteId

                        };
                        var ProductDetail = new ProductDetails();
                        ProductDetail.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                        ProductDetail.ShortDescription = item.ShortDescription;
                        ProductDetail.Description = item.Description;
                        ProductDetail.Specification = item.Specification;
                        ProductDetail.ProductTemplateId = item.ProductTemplateId;
                        ProductDetail.Name = item.Name;
                        //Temporary fix for JPM
                        ProductDetail.Name2 = model.BrandId.HasValue ? await brandDetailsRepository.Query().Where(x => model.BrandId.Value==x.BrandId && x.CultureId==item.CultureId).Select(x => x.Name).FirstOrDefaultAsync() : null;
                        ProductDetail.AddtionalMetaTags = item.AdditonalMetaTags;

                        ProductDetail.CultureId = item.CultureId;
                        ProductDetail.NormalizedName = item.NormalizedName;
                        ProductDetail.ProductId = product.Id;
                        productDetailsrepository.Add(ProductDetail);
                        await productDetailsrepository.SaveChangesAsync();

                    }
                }

                await productDetailsrepository.SaveChangesAsync();
                await productRepository.SaveChangesAsync();

                #endregion


                var ProductCategory = await productCategoryRepository.Query()
                                                                     .Where(x => x.ProductId == product.Id)
                                                                     .ToListAsync();

                var productCategoryToBeRemoved = ProductCategory.Where(x => !model.CategoryIds.Contains(x.CategoryId))
                                                                .ToList();

                var categoryTobeAdded = model.CategoryIds.Where(x => !ProductCategory.Any(y => y.CategoryId == x))
                                                         .ToList();
                if (productCategoryToBeRemoved.Count() > 0)
                {
                    productCategoryRepository.RemoveRange(productCategoryToBeRemoved);
                    await HandleProductCategoryHistory(productCategoryToBeRemoved, UserId, EHistoryAction.Delete);

                }


                IList<ProductCategory> AddProductCategory = new List<ProductCategory>();

                foreach (var Category in categoryTobeAdded)
                {

                    AddProductCategory.Add(new ProductCategory() { ProductId = product.Id, CategoryId = Category });


                }
                productCategoryRepository.AddRange(AddProductCategory);
                await productCategoryRepository.SaveChangesAsync();

                await HandleProductCategoryHistory(AddProductCategory.ToList(), UserId, EHistoryAction.Add);
                int DisplayOrder = 0;


                var Img = await productMediaRepository.Query().Where(x => x.ProductId == product.Id).ToListAsync();
                if (Img != null && Img.Count() > 0)
                {

                    productMediaRepository.RemoveRange(Img);
                    await productMediaRepository.SaveChangesAsync();


                }
                foreach (var Image in model.ImagesIds)
                {

                    var productMedia = new ProductMedia
                    {
                        ProductId = product.Id,
                        MediaId = Image,
                        DisplayOrder = DisplayOrder,
                    };

                    productMediaRepository.Add(productMedia);
                    await productMediaRepository.SaveChangesAsync();
                    DisplayOrder += 1;
                }



               // if (model.AddStockForAllWarehouses.HasValue && model.AddStockForAllWarehouses.Value && model.StockTrackingIsEnabled)
                //{
                //    if (model.StockForAllWarehouses != null && model.SelectedWarehouseIds != null)
                //    {
                //        var productIds = new List<long>() { product.Id };

                //        if (model.applyStockForVariations)
                //        {
                //            var variations = await productLinkRepository.Query().Where(x => x.ProductId == product.Id && x.LinkType == ItemLinkType.Super).Select(x => x.LinkedProductId).ToListAsync();

                //            productIds.AddRange(variations);

                //        }

                //        foreach (var productId in productIds)
                //        {


                //            var existedStocks = await _stockRepository.Query().Where(y => model.SelectedWarehouseIds.Any(x => x == y.WarehouseId) && y.ProductId == productId).ToListAsync();



                //            var newStocks = model.SelectedWarehouseIds.Where(y => !existedStocks.Any(x => x.WarehouseId == y))
                //                                                      .Select(x => new Stock
                //                                                      {
                //                                                          ProductId = productId,
                //                                                          Quantity = model.StockForAllWarehouses.Value,
                //                                                          WarehouseId = x,
                //                                                          Status=EStatus.Published
                //                                                      }).ToList();

                //            existedStocks.ForEach(x => x.Quantity = model.StockForAllWarehouses.Value);
                //            _stockRepository.UpdateRange(existedStocks);
                //            _stockRepository.AddRange(newStocks);
                //            await _stockRepository.SaveChangesAsync();

                //        }
                //    }

                //}

                return product.Id;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<CatelogFlatViewModel>> GetAllProductFlat(long cultureId, long categoryId)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var query = await productRepository.Query()
                                                 //.Where(x =>categoryId==0|| x.CategoryId == categoryId)
                                                 .Include(x => x.ProductDetails)
                                                 .ThenInclude(x => x.SlugInfo)
                                                 .Include(x => x.Categories)
                                                 .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId)
                                                 .Where(x => categoryId == 0 || x.Categories.Any(x => x.CategoryId == categoryId))
                                                 .AsNoTrackingWithIdentityResolution()
                                                 .SelectMany(x => x.ProductDetails)
                                                 .Where(d => d.CultureId == cultureId)
                                                 .Select(x => new CatelogFlatViewModel
                                                 {
                                                     IsPromotional=x.Product.IsPromotional,
                                                     Id = x.Product.Id,
                                                     Name = x.Name,
                                                     Slug = x.SlugInfo == null ? "" : x.SlugInfo.Slug
                                                 }).ToListAsync();

                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<CatelogFlatViewModel>> GetProductsForAdmin()
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var Query = await productRepository.Query()
                                                   .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted)

                                                    .Select(x => new CatelogFlatViewModel
                                                    {
                                                        Id = x.Id,
                                                        Name = x.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name
                                                    }).ToListAsync();


                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<AllProductsResponseViewModel>> GetProducts(SearchParamsViewModel product)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();

                var searchText = string.IsNullOrWhiteSpace(product.SearchText)
                    ? null
                    : product.SearchText.Trim();

                var pageSize = product.pagination.PageSize;
                var pageNumber = product.pagination.PageNumber;

                var baseQuery = productRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted);

                if (product.CategoryIds != null && product.CategoryIds.Any())
                {
                    baseQuery = baseQuery.Where(x =>
                        x.Categories.Any(c => product.CategoryIds.Contains(c.CategoryId)));
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    baseQuery = baseQuery.Where(x =>
                        x.ProductDetails.Any(d =>
                            EF.Functions.Like(d.Name, $"%{searchText}%")));
                }

                if (product.ProductIds != null && product.ProductIds.Any())
                {
                    baseQuery = baseQuery.Where(x =>
                        product.ProductIds.Contains(x.Id));
                }

                // Exclude variation products from main grid
                baseQuery = baseQuery.Where(x =>
                    !x.LinkedProductLinks.Any(l => l.LinkType == ItemLinkType.Super));

                var result = await baseQuery
                    .OrderByDescending(x => x.Id)
                    .Select(x => new AllProductsResponseViewModel
                    {
                        Id = x.Id,
                        Sku = x.Sku,
                        Gtin = x.Gtin,
                        Type = x.Type,
                        Price = x.Price,
                        ForeignPrice = x.ForeignPrice,
                        SpecialPrice = x.SpecialPrice,
                        SpecialForignPrice = x.SpecialForignPrice,
                        IsAllowForeign = x.IsAllowForeign,
                        VendorId = x.VendorId ?? 0,
                        TaxClassId = x.TaxClassId,
                        IsFeatured = x.IsFeatured,
                        IsCallForPricing = x.IsCallForPricing,
                        IsAllowToOrder = x.IsAllowToOrder,
                        IsPlan = x.IsPlan,
                        IsPromotional = x.IsPromotional,
                        CreatedOn = x.CreationDate,

                        CreatedBy = x.CreatedBy != null
                            ? x.CreatedBy.FullName
                            : null,

                        ModifiedBy = x.ModifiedBy != null
                            ? x.ModifiedBy.FullName
                            : null,

                        Status = x.Status,
                        PriceType = x.PriceType,
                        StockTrackingIsEnabled = x.StockTrackingIsEnabled,

                        Thumbnail = x.ThumbnailImage != null
                            ? x.ThumbnailImage.PublicUrl
                            : null,

                        DefaultName = x.ProductDetails
                            .OrderByDescending(d => d.CultureId)
                            .Select(d => d.Name)
                            .FirstOrDefault(),

                        Stock = new List<ProductStockQuantity>(),
                        Variations = new List<AllProductsResponseViewModel>()
                    })
                    .PaginateAsync(pageSize, pageNumber);

                var parentProductIds = result.Data
                    .Where(x => x.Id.HasValue)
                    .Select(x => x.Id.Value)
                    .Distinct()
                    .ToList();

                if (!parentProductIds.Any())
                    return result;

                // Load parent products with ProductLinks into memory.
                // This avoids CROSS APPLY translation issue in Pomelo/MySQL.
                var parentProductsWithLinks = await productRepository.Query()
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(p => parentProductIds.Contains(p.Id))
                    .Include(p => p.ProductDetails)
                    .Include(p => p.ProductLinks)
                        .ThenInclude(l => l.LinkedProduct)
                    .ToListAsync();

                var variationRows = parentProductsWithLinks
                    .SelectMany(parent => parent.ProductLinks
                        .Where(link =>
                            link.LinkType == ItemLinkType.Super &&
                            link.LinkedProduct != null &&
                            link.LinkedProduct.Status != EStatus.Deleted)
                        .Select(link => new
                        {
                            ParentId = parent.Id,

                            VariationId = link.LinkedProduct.Id,

                            ParentName = parent.ProductDetails
                                .OrderByDescending(d => d.CultureId)
                                .Select(d => d.Name)
                                .FirstOrDefault(),

                            Variation = new AllProductsResponseViewModel
                            {
                                Id = link.LinkedProduct.Id,
                                HasOptions = link.LinkedProduct.HasOptions,
                                IsVisibleIndividually = link.LinkedProduct.IsVisibleIndividually,
                                IsFeatured = link.LinkedProduct.IsFeatured,
                                IsAllowToOrder = link.LinkedProduct.IsAllowToOrder,
                                IsCallForPricing = link.LinkedProduct.IsCallForPricing,
                                Gtin = link.LinkedProduct.Gtin,
                                Sku = link.LinkedProduct.Sku,
                                PriceType = link.LinkedProduct.PriceType,
                                CreatedOn = link.LinkedProduct.CreationDate,
                                Status = link.LinkedProduct.Status,

                                Stock = new List<ProductStockQuantity>(),
                                Variations = new List<AllProductsResponseViewModel>()
                            }
                        }))
                    .ToList();

                var variationProductIds = variationRows
                    .Select(x => x.VariationId)
                    .Distinct()
                    .ToList();

                var allProductIdsForStock = parentProductIds
                    .Concat(variationProductIds)
                    .Distinct()
                    .ToList();

                if (allProductIdsForStock.Any())
                {
                    var stockProducts = await productRepository.Query()
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Where(p => allProductIdsForStock.Contains(p.Id))
                        .Include(p => p.StockAmounts)
                            .ThenInclude(sa => sa.WareHouse)
                        .ToListAsync();

                    var stockByProductId = stockProducts
                        .ToDictionary(
                            p => p.Id,
                            p => p.StockAmounts
                                .Select(sa => new ProductStockQuantity
                                {
                                    Quantity = sa.Quantity,
                                    WareHouseName = sa.WareHouse != null
                                        ? sa.WareHouse.Name
                                        : null
                                })
                                .ToList());

                    foreach (var parentProduct in result.Data)
                    {
                        if (parentProduct.Id.HasValue &&
                            stockByProductId.TryGetValue(parentProduct.Id.Value, out var parentStock))
                        {
                            parentProduct.Stock = parentStock;
                        }
                        else
                        {
                            parentProduct.Stock = new List<ProductStockQuantity>();
                        }
                    }

                    foreach (var row in variationRows)
                    {
                        row.Variation.Stock = stockByProductId.TryGetValue(row.VariationId, out var variationStock)
                            ? variationStock
                            : new List<ProductStockQuantity>();
                    }
                }

                if (variationProductIds.Any())
                {
                    var variationProductsWithOptions = await productRepository.Query()
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Where(p => variationProductIds.Contains(p.Id))
                        .Include(p => p.OptionCombinations)
                            .ThenInclude(o => o.Value)
                                .ThenInclude(v => v.productOptionValueDetails)
                        .ToListAsync();

                    var optionTextByProductId = variationProductsWithOptions
                        .ToDictionary(
                            p => p.Id,
                            p =>
                            {
                                if (p.OptionCombinations == null || !p.OptionCombinations.Any())
                                    return string.Empty;

                                var optionDetails = p.OptionCombinations
                                    .Where(o =>
                                        o.Value != null &&
                                        o.Value.productOptionValueDetails != null)
                                    .SelectMany(o => o.Value.productOptionValueDetails.Select(d => new
                                    {
                                        OptionCombinationId = o.Id,
                                        d.CultureId,
                                        d.Value
                                    }))
                                    .ToList();

                                if (!optionDetails.Any())
                                    return string.Empty;

                                var maxCultureId = optionDetails.Max(x => x.CultureId);

                                var values = optionDetails
                                    .Where(x => x.CultureId == maxCultureId)
                                    .OrderBy(x => x.OptionCombinationId)
                                    .Select(x => x.Value)
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList();

                                return string.Join("_", values);
                            });

                    foreach (var row in variationRows)
                    {
                        optionTextByProductId.TryGetValue(row.VariationId, out var optionText);

                        row.Variation.DefaultName = string.IsNullOrWhiteSpace(optionText)
                            ? row.ParentName
                            : $"{row.ParentName} {optionText}";
                    }
                }

                var variationsByParentId = variationRows
                    .GroupBy(x => x.ParentId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.Variation).ToList());

                foreach (var parentProduct in result.Data)
                {
                    if (parentProduct.Id.HasValue &&
                        variationsByParentId.TryGetValue(parentProduct.Id.Value, out var variations))
                    {
                        parentProduct.Variations = variations;
                    }
                    else
                    {
                        parentProduct.Variations = new List<AllProductsResponseViewModel>();
                    }
                }

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<CatelogSettingsViewModel>> GetAllProductsByCategoryIdsForAdmin(long[] ids)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();
                var Query = await productCategoryRepository.Query()
                                                    .Where(x => ids.Contains(x.CategoryId))
                                                    .Include(x => x.Product)
                                                    .Include("Product.ThumbnailImage")
                                                    .Include("Product.ProductDetails")
                                                    .Include(x => x.Category)
                                                    .ThenInclude(x => x.CategoryDetails)
                                                    .Where(x => x.Product.SiteId == siteId)
                                                    .OrderBy(x => x.DisplayOrder)
                                                    .Select(x => new CatelogSettingsViewModel
                                                    {
                                                        CategoryName = x.Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                                                        Name = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                                                        ThumbnailImageUrl = x.Product.ThumbnailImage == null ? "" : x.Product.ThumbnailImage.PublicUrl,
                                                        ProductId = x.ProductId,
                                                        DisplayOrder = x.DisplayOrder,

                                                    }).ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<long> GetOriginalProductId(long ProdId)
        {
            var Original = await GetOriginalProduct(ProdId);
            return Original == null ? ProdId : Original.Id;
        }
        public async Task<bool> UpsertProductCategoryOrders(UpsertProductCategoryOrderDto model)
        {
            var siteId = await workContext.GetCurrentSiteIdAsync();

            var userId = await workContext.GetCurrentUserId();

            var allProductIds = model.ProductOrders.Select(x => x.ProductId).ToList();

            var allProductCategory = await productCategoryRepository.Query()
                                                              .Where(x => x.Category.SiteId == siteId && x.CategoryId == model.CategoryId && allProductIds.Contains(x.ProductId))
                                                              .ToListAsync();


            foreach (var item in allProductCategory)
            {
                var updatedItem = model.ProductOrders.Where(x => x.ProductId == item.ProductId).FirstOrDefault();

                if (updatedItem is not null)
                {
                    item.DisplayOrder = updatedItem.DisplayOrder;
                }
            }

            productCategoryRepository.UpdateRange(allProductCategory);
            await productCategoryRepository.SaveChangesAsync();

            await HandleProductCategoryHistory(allProductCategory, userId, EHistoryAction.Update);





            return true;


        }

        public async Task<bool> DeleteProductCategory(DeleteProductCategoryDto model)
        {
            var siteId = await workContext.GetCurrentSiteIdAsync();
            var userId = await workContext.GetCurrentUserId();


            var allProductCategory = await productCategoryRepository.Query()
                                                              .Where(x => x.Category.SiteId == siteId && x.CategoryId == model.CategoryId && model.ProductIds.Contains(x.ProductId))
                                                              .ToListAsync();

            if (allProductCategory.Any())
            {
                productCategoryRepository.RemoveRange(allProductCategory);
                await productCategoryRepository.SaveChangesAsync();

                await HandleProductCategoryHistory(allProductCategory, userId, EHistoryAction.Delete);
            }



            return true;


        }


        public async Task<Product> GetOriginalProduct(long ProdId)
        {
            var Link = await productLinkRepository.Query().AsNoTracking()
                .Where(x => x.LinkedProductId == ProdId)
                .Include(s => s.Product)
                .Include("Product.Categories.Category")
                .FirstOrDefaultAsync();
            return Link == null ? null : Link.Product;
        }

        public async Task<ProductResponseViewModel> GetProductById(long id)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var StockQuantity = await stockAmountRepository.Query().AsNoTracking().Where(x => x.ProductId == id).FirstOrDefaultAsync();

                var IsProductVariations = productLinkRepository.Query().Where(x => x.LinkedProductId == id).AsNoTracking().Count() == 0 ? false : true;

                var query = await productRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted && x.Id == id)
                        .Include(x => x.ProductAvaliablity)
                        .ThenInclude(x => x.Country)
                        .Include(x => x.ThumbnailImage)
                        .Include(x => x.Categories)
                        .ThenInclude(x => x.Category)
                        .Include(x => x.ProductDetails)
                        .Include("ProductDetails.SlugInfo")
                        .Include("ProductDetails.Culture")
                        .Include(x => x.Stock)
                        .Include(x => x.Medias).ThenInclude(x => x.Media)
                        .ToListAsync();


                var result = query.Select(x => new ProductResponseViewModel
                {

                    BidEndtDate = x.BidEndtDate,
                    BidStartDate = x.BidStartDate,
                    BidIncrement = x.BidIncrement,
                    BrandId = x.BrandId,
                    BuyoutPrice = x.BuyoutPrice,
                    ThumbnailUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
                    IsCallForPricing = x.IsCallForPricing,
                    IsVisibleIndividually = x.IsVisibleIndividually,
                    IsFeatured = x.IsFeatured,
                    MinPrice = x.MinPrice,
                    ProductAdditionalFees = x.ProductAdditionalFees,
                    IsAllowToOrder = x.IsAllowToOrder,
                    SpecialForignPrice = x.SpecialForignPrice,
                    CreatedOn = x.CreationDate,
                    ThumbnailImageId = x.ThumbnailImageId,
                    RequestStatus = x.RequesStatus ?? 0,
                    TaxClassId = x.TaxClassId,
                    StockQuantity = StockQuantity == null ? 0 : StockQuantity.Quantity,
                    SpecialPrice = x.SpecialPrice,
                    selectedWarehouseIds = x.Stock != null ? x.Stock.Select(x => x.WarehouseId).ToList() : new List<long>(),
                    SpecialPriceStart = x.SpecialPriceStart,
                    SpecialPriceEnd = x.SpecialPriceEnd,
                    VendorId = x.VendorId ?? 0,
                    IsPromotional=x.IsPromotional,
                    Price = x.Price,
                    ForeignPrice = x.ForeignPrice,

                    IsAllowForeign = x.IsAllowForeign,
                    Type = x.Type,
                    Id = x.Id,
                    PriceType=x.PriceType,
                    IsPlan = x.IsPlan,
                    MaxAllowedQuantity = x.MaxAllowedQuantity,
                    ProductAvaliablity = x.ProductAvaliablity == null && x.ProductAvaliablity.Count() > 0
                                                  ? new List<ProductAvaliablityRequest>() :
                                                  x.ProductAvaliablity.Select(x => new ProductAvaliablityRequest
                                                  {

                                                      CountryId = x.CountryId,
                                                      Permission = x.Permission,
                                                      StateOrProvinceId = x.StateOrProvinceId
                                                  }).ToList()
                                                  ,
                    Gtin = x.Gtin,
                    Sku = x.Sku,
                    PublishDate = x.PublishDate,
                    ExpiryDate = x.ExpiryDate,
                    ImagelUrl = x.Medias == null ? new List<string>() : x.Medias.OrderBy(x => x.DisplayOrder).Where(x => x.MediaId == x.Media.Id).Select(m => m.Media.PublicUrl).ToList(),
                    ImagesIds = x.Medias == null ? new List<long>() : x.Medias.OrderBy(x => x.DisplayOrder).Select(m => m.MediaId).ToList(),
                    CategoryIds = x.Categories == null ? new List<long>() : x.Categories.Where(x=>x.Category.Status==EStatus.Published).Select(x => x.CategoryId).ToList(),
                    StockTrackingIsEnabled = x.StockTrackingIsEnabled,
                    Status = x.Status,
                    IsProductSerial = x.IsProductSerial,
                    IsProductVariations = IsProductVariations,
                    ProductDetails = x.ProductDetails.Select(x => new ProductDetailsResponseViewModel
                    {
                        Description = x.Description,
                        ShortDescription = x.ShortDescription,
                        CultureId = x.CultureId,
                        CultureCode = x.Culture == null ? "" : x.Culture.CultureId,
                        AdditonalMetaTags = x.AddtionalMetaTags,
                        Name = x.Name,
                        Name2 = x.Name2,
                        NormalizedName = x.NormalizedName,
                        ProductTemplateId = x.ProductTemplateId,
                        Slug = x.SlugInfo.Slug,
                        Specification = x.Specification,
                        Id = x.Id,
                        ProductId = x.ProductId

                    }).ToList()

                }).FirstOrDefault();



                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<ProductViewModel>> GetProductByCategoryId(ProductSearchViewModel productSearch)
        {
            try
            {
                var WarehouseId = await workContext.GetCurrentWarehouseId();
                var SiteId = workContext.GetCurrentSiteId();
                var Query = productCategoryRepository
                                           .Query()
                                           .AsNoTrackingWithIdentityResolution()
                                            
                                           .Where(x => productSearch.CategoryIds.Contains(x.CategoryId))
                                           .Include(x => x.Product)
                                           .Include("Product.AttributeValues.Attribute")
                                           .Include("Product.AttributeValues.AttributeValueDetails")
                                           .Include("Product.AttributeValues.Attribute.AttributeDetails")
                                           .Include("Product.ProductAdditionalFees")
                                           .Include("Product.Brand")
                                           .Include("Product.Brand.BrandDetails")
                                           .Include("Product.ProductDetails")
                                           .Include("Product.ProductDetails.SlugInfo")
                                           .Include("Product.ProductDetails.Culture")
                                           .Include("Product.ThumbnailImage")
                                            .Include(x => x.Category).ThenInclude(x => x.ProductCategories)
                                             .Where(x => x.Category.CategoryWarehouses.Count() == 0|| x.Category.CategoryWarehouses.Any(x => x.WarehouseId == WarehouseId))
                                           .Where(x =>
                                           x.Product.SiteId == SiteId && x.Product.Status == EStatus.Published
                                           && (productSearch.BrandId == 0 || x.Product.Brand.Id == productSearch.BrandId)
                                           && (productSearch.IsFeatuerItem == null || x.Product.IsFeatured == productSearch.IsFeatuerItem)
                                           && (string.IsNullOrEmpty(productSearch.SearchValue) || x.Product.Brand.BrandDetails.Any(c => c.Name.Contains(productSearch.SearchValue))));



                if (productSearch.ProductSort != null)
                {
                    Query = SortProducts(productSearch.ProductSort, Query);
                }
                else
                {
                    Query = Query.OrderByDescending(x => x.Id);
                }


                var Products = await Query.Select(x => new ProductViewModel
                {
                    Id = x.Product.Id,
                    Name = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                    Name2 = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name2,
                    BidEndtDate = x.Product.BidEndtDate,
                    ShortDescription = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().ShortDescription,
                    BidStartDate = x.Product.BidStartDate,
                    Slug = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo.Slug,
                    Specification = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Specification,
                    Description = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description,
                    MinPrice = x.Product.MinPrice,
                    ProductTemplateId = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().ProductTemplateId,
                    BidIncrement = x.Product.BidIncrement,
                    IsPromotional= x.Product.IsPromotional,
                    AddtionalMetaTags = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().AddtionalMetaTags,
                    ThumbnailUrl = x.Product.ThumbnailImage == null ? null : x.Product.ThumbnailImage.PublicUrl,
                    ForeignPrice = x.Product.ForeignPrice,
                    BuyoutPrice = x.Product.BuyoutPrice,
                    IsFeatured = x.Product.IsFeatured,
                    PriceType= x.Product.PriceType,
                    IsPlan = x.Product.IsPlan,

                    AttributeValues = x.Product.AttributeValues.Select(a => new ProductAttribueWithvaluesModel
                    {
                        Id = a.Id,
                        value = a.AttributeValueDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Value,
                        AttributeId = a.AttributeId
                    }).ToList(),
                    Attribute = x.Product.AttributeValues.Select(a => new ProductAttribueWithvaluesVM
                    {
                        Id = a.AttributeId,
                        Name = a.Attribute.AttributeDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name
                    }).ToList(),
                    StockTrackingIsEnabled = x.Product.StockTrackingIsEnabled
                }).PaginateAsync(productSearch.PageSize, productSearch.PageNumber);


                foreach (var product in Products.Data)
                {
                    product.Options = await GetOptionByProductId(product.Id);
                }
                return Products;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> ChangeStatus(long id, EStatus status)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var Product = await productRepository.Query()
                    .Include(x => x.ProductDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.LinkedProductLinks)
                    .Where(x => x.Id == id).FirstOrDefaultAsync();


                if (Product == null)
                {
                    return false;
                }

                await ValidateStatusAndPublishDate_ExpireDate(status, Product.PublishDate, Product.ExpiryDate);

                if (status == EStatus.Deleted)
                {
                    foreach (var item in Product.ProductDetails)
                    {
                        SlugManagementVm slugManagementVm = new()
                        {
                            CultureId = item.CultureId,
                            Slug = item.Id + "Deleted" + item.SlugInfo.Slug,
                            Entity = EEntity.Product,
                            SiteId = SiteId,
                            Id = item.SlugInfoId

                        };
                        item.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                    }

                    await variationServices.DeleteAllVariations(Product.Id);
                    foreach (var link in Product.LinkedProductLinks.ToList())
                    {
                        if (link.LinkType == ItemLinkType.Super)
                        {
                            productLinkRepository.Remove(link);
                            await productLinkRepository.SaveChangesAsync();
                        }
                    }


                }
                var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
                    if (IsMeliEnabled&&(status == EStatus.Deleted || status == EStatus.Unpublished))
                {
                    //var elasticId = $"{id}-{nameof(EEntity.Product)}";
                    //await _searchRepository.DeleteDocuemnt(elasticId);

                    //MeiliSearch
                    var MeiliId = $"{id}-{nameof(EEntity.Product)}";
                    await _meiliRepo.DeleteDocuemnt(MeiliId);

                    foreach (var item in Product.ProductDetails)
                    {
                        await _productSearchRepository.DeleteDocuemnt($"{Product.Id}-{nameof(EEntity.Product)}-{item.CultureId}");
                    }

                }


                Product.Status = status;

                await productRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #region Helpers
        private IQueryable<ProductCategory> SortProducts(ProductSearchSortViewModel search, IQueryable<ProductCategory> query)
        {
            if (search.SortField == ProductSearchSortFieldsEnum.Name)
            {
                if (search.SortType == ESortType.asc)
                {
                    query = query.OrderBy(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name);
                }
            }

            if (search.SortField == ProductSearchSortFieldsEnum.Description)
            {
                if (search.SortType == ESortType.asc)
                {
                    query = query.OrderBy(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description);
                }
            }
            if (search.SortField == ProductSearchSortFieldsEnum.ShortDesciption)
            {
                if (search.SortType == ESortType.asc)
                {
                    query = query.OrderBy(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().ShortDescription);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().ShortDescription);
                }
            }




            if (search.SortField == ProductSearchSortFieldsEnum.Slug)
            {
                if (search.SortType == ESortType.asc)
                {
                    query = query.OrderBy(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo.Slug);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Product.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo.Slug);
                }
            }
            return query;

        }
        private async Task<List<ItemDetailVariationOptionAdmin>> GetOptionByProductId(long id)
        {
            try
            {
                var CultureId = workContext.GetCurrentCultureId();
                var productLinks = await productLinkRepository.Query()
                                       .AsNoTracking()
                                       .AsSplitQuery()
                                       .Where(x => x.ProductId == id
                                           && x.LinkType == ItemLinkType.Super)
                                       .Include(x => x.LinkedProduct)
                                           .ThenInclude(x => x.OptionCombinations)
                                               .ThenInclude(x => x.Option)
                                                   .ThenInclude(x => x.productOptionValues)
                                                       .ThenInclude(x => x.productOptionValueDetails)
                                       .Include(x => x.LinkedProduct)
                                           .ThenInclude(x => x.OptionCombinations)
                                               .ThenInclude(x => x.Option)
                                                   .ThenInclude(x => x.optionDetails)
                                       .ToListAsync();

                var options = new List<ItemDetailVariationOptionAdmin>();

                foreach (var link in productLinks)
                {
                    if (link.LinkedProduct?.OptionCombinations == null)
                    {
                        continue;
                    }

                    options.AddRange(link.LinkedProduct.OptionCombinations.Select(x => new ItemDetailVariationOptionAdmin
                    {
                        OptionId = x.OptionId,
                        OptionName = x.Option?.optionDetails?.Select(d => d.Name).ToList() ?? new List<string>(),
                        Value = x.Option?.productOptionValues?
                            .SelectMany(v => v.productOptionValueDetails ?? new List<ProductOptionValueDetails>())
                            .Select(v => v.Value)
                            .ToList() ?? new List<string>()
                    }).ToList());
                }

                return options;


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

        private async Task<bool> HandleProductPriceHistory(Product orgignalProduct, Product updatedProduct, long userId)
        {

            if (updatedProduct is null)
            {
                var productPriceHistory = new ProductPriceHistory(orgignalProduct, userId, EHistoryAction.Add);

                productPricehistoryRepository.Add(productPriceHistory);
                await productPricehistoryRepository.SaveChangesAsync();
            }
            else
            {

                if (HasPriceChanged(orgignalProduct, updatedProduct))
                {
                    var productPriceHistory = new ProductPriceHistory(orgignalProduct, userId, EHistoryAction.Update);

                    productPricehistoryRepository.Add(productPriceHistory);
                    await productPricehistoryRepository.SaveChangesAsync();

                }

            }




            return true;
        }


        public bool HasPriceChanged(Product originalProduct, Product updatedProduct)
        {
            if (updatedProduct == null) return true;

            return originalProduct.Price != updatedProduct.Price ||
                   originalProduct.ForeignPrice != updatedProduct.ForeignPrice ||
                   originalProduct.IsAllowForeign != updatedProduct.IsAllowForeign ||
                   originalProduct.SpecialPrice != updatedProduct.SpecialPrice ||
                   originalProduct.SpecialForignPrice != updatedProduct.SpecialForignPrice ||
                   originalProduct.SpecialPriceStart != updatedProduct.SpecialPriceStart ||
                   originalProduct.SpecialPriceEnd != updatedProduct.SpecialPriceEnd;
        }


        private async Task<bool> HandleProductCategoryHistory(List<ProductCategory> productCategory, long userId, EHistoryAction action)
        {

            if (productCategory.Any())
            {
                var productLinkHistory = productCategory.Select(x => new ProductCategoryHistory
                {
                    ProductId = x.ProductId,
                    CategoryId = x.CategoryId,
                    DisplayOrder = x.DisplayOrder,
                    Action = action,
                    CreatedById = userId,
                    CreatedOn = DateTimeOffset.Now
                }).ToList();


                _productCategoryHistoryRepository.AddRange(productLinkHistory);
                await _productCategoryHistoryRepository.SaveChangesAsync();
            }


            return true;
        }
        #endregion
    }
}
