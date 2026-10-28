using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Catalog.ProductsAttributes;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.options;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea.Interfaces;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea
{
    public class VariationServices : IVariationServices
    {
        private readonly IRepository<Product> productRepository;
        private readonly IWorkContext workContext;
        private readonly ISlugServices slugServices;
        private readonly IRepository<ProductLink> productLinkRepository;
        private readonly EskaDCMSDbContext context;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly IRepository<ProductSerial> productSerialRepository;
        private readonly IRepository<ProductOptionCombination> productCombinationRepository;
        private readonly IRepository<ProductPriceHistory> productPriceRepository;
        private readonly IRepository<ProductLinkHistory> _productLinkHistoryRepository;

        private readonly IRepository<ProductCategoryHistory> _productCategoryHistoryRepository;
        private readonly IRepository<ProductOptionValue> productOptionValue;
        private readonly IRepository<ProductOptionValueDetails> productOptionValueDetails;
        private readonly IRepository<SitesCultures> _SitesCulturesRepo;
        private readonly IProductSerialService productSerialService;
        private readonly IProductPricingService productPricingService;
        private readonly IUserSiteService _userSiteService;
        private readonly ICurrencyService _currencyService;




        public VariationServices(IRepository<Product> productRepository,
            IWorkContext workContext, ISlugServices slugServices
            , IRepository<ProductLink> productLinkRepository,
            EskaDCMSDbContext context,
            IRepository<ProductDetails> ProductDetailsRepository,
            IRepository<ProductSerial> ProductSerialRepository,
            IRepository<ProductOptionCombination> productCombinationRepository,
            IRepository<ProductPriceHistory> productPriceRepository,
            IRepository<ProductOptionValue> ProductOptionValue,
            IRepository<ProductOptionValueDetails> ProductOptionValueDetails,
            IProductSerialService productSerialService,
             IUserSiteService userSiteService,
             ICurrencyService currencyService,
              IRepository<ProductCategoryHistory> productCategoryHistoryRepository,
             IRepository<ProductLinkHistory> productLinkHistoryRepository,
        IProductPricingService productPricingService, IRepository<SitesCultures> sitesCulturesRepo)
        {
            this.productRepository = productRepository;
            this.workContext = workContext;
            this.slugServices = slugServices;
            this.productLinkRepository = productLinkRepository;
            this.context = context;
            productDetailsRepository = ProductDetailsRepository;
            productSerialRepository = ProductSerialRepository;
            this.productCombinationRepository = productCombinationRepository;
            this.productPriceRepository = productPriceRepository;
            productOptionValue = ProductOptionValue;
            productOptionValueDetails = ProductOptionValueDetails;
            this.productSerialService = productSerialService;
            this.productPricingService = productPricingService;
            _SitesCulturesRepo = sitesCulturesRepo;
            _userSiteService = userSiteService;
            _currencyService = currencyService;
            _productLinkHistoryRepository = productLinkHistoryRepository;
            _productCategoryHistoryRepository = productCategoryHistoryRepository;
        }

        public async Task<bool> AddProductVariations(List<VariationsViewModel> Variations, long ProductId)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await context.Database.BeginTransactionAsync();

                try
                {
                    var user = await workContext.GetCurrentUser();
                    var SiteId = workContext.GetCurrentSiteId();
                    var userCountry = await _userSiteService.GetUserCountry();

                    var OriginalProduct = await productRepository.Query()
                        .Include(x => x.ProductDetails)
                        .Where(x => x.Id == ProductId)
                        .FirstOrDefaultAsync();

                    foreach (var variations in Variations)
                    {
                        var product = new Product();
                        product.SiteId = SiteId;
                        product.Price = variations.Price.HasValue ? variations.Price.Value : 0;
                        product.Status = OriginalProduct.Status;
                        product.SpecialForignPrice = variations.SpecialForignPrice.HasValue ? variations.SpecialForignPrice.Value : 0;
                        product.HasOptions = false;
                        product.ForeignPrice = variations.ForeignPrice == 0 || variations.ForeignPrice == null ? null : variations.ForeignPrice.Value;
                        product.IsAllowForeign = variations.ForeignPrice == 0 || variations.ForeignPrice == null ? false : true;
                        product.IsProductSerial = false;
                        product.PriceType = OriginalProduct.PriceType;
                        product.IsVisibleIndividually = false;
                        product.ThumbnailImageId = variations.ThumbnailImageId == 0 ? OriginalProduct.ThumbnailImageId : variations.ThumbnailImageId;
                        product.StockTrackingIsEnabled = OriginalProduct.StockTrackingIsEnabled;
                        product.IsFeatured = OriginalProduct.IsFeatured;
                        product.IsVisibleIndividually = OriginalProduct.IsVisibleIndividually;
                        product.IsCallForPricing = OriginalProduct.IsCallForPricing;
                        product.IsAllowToOrder = OriginalProduct.IsAllowToOrder;
                        product.IsPromotional = OriginalProduct.IsPromotional;
                        product.CreatedById = await workContext.GetCurrentUserId();
                        product.CreationDate = DateTime.Now;
                        product.Sku = variations.Sku;
                        product.Gtin = variations.Gtin;
                        product.MaxAllowedQuantity = variations.MaxAllowedQuantity;

                        productRepository.Add(product);
                        await productRepository.SaveChangesAsync();

                        var List = new List<ProductDetails>();

                        foreach (var item in variations.variationsDetails)
                        {
                            foreach (var original in OriginalProduct.ProductDetails)
                            {
                                if (original.CultureId == item.CultureId)
                                {
                                    var Ids = variations.OptionCombinations
                                        .Where(x => x.ValueId.HasValue)
                                        .Select(x => x.ValueId.Value)
                                        .ToList();

                                    var Values = await productOptionValueDetails.Query()
                                        .Where(x => Ids.Contains(x.OptionValueId))
                                        .Select(x => x.Value)
                                        .ToListAsync();

                                    var productDetail = new ProductDetails();

                                    SlugManagementVm slugManagementVm = new()
                                    {
                                        CultureId = item.CultureId,
                                        Slug = item.Name + string.Join('_', Values),
                                        Entity = EEntity.Product,
                                        SiteId = SiteId
                                    };

                                    productDetail.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                                    productDetail.Name = item.Name;
                                    productDetail.NormalizedName = item.NormalizedName;
                                    productDetail.CultureId = item.CultureId == 0 ? workContext.GetCurrentCultureId() : item.CultureId;
                                    productDetail.ProductTemplateId = original.ProductTemplateId;
                                    productDetail.ProductId = product.Id;

                                    List.Add(productDetail);
                                }
                            }
                        }

                        productDetailsRepository.AddRange(List);
                        await productDetailsRepository.SaveChangesAsync();

                        await HandleProductPriceHistory(product, null, user.Id);

                        var productLink = new ProductLink
                        {
                            LinkType = ItemLinkType.Super,
                            ProductId = OriginalProduct.Id,
                            LinkedProductId = product.Id
                        };

                        productLinkRepository.Add(productLink);
                        await productLinkRepository.SaveChangesAsync();

                        await HandleProductLinkHistory(
                            new List<ProductLink> { productLink },
                            user.Id,
                            EHistoryAction.Add);

                        foreach (var combinationVm in variations.OptionCombinations)
                        {
                            if (!combinationVm.ValueId.HasValue)
                                throw new Exception("Option value is required.");

                            var productCombinaton = new ProductOptionCombination
                            {
                                OptionId = combinationVm.OptionId,
                                ValueId = combinationVm.ValueId.Value,
                                SortIndex = combinationVm.SortIndex,
                                Product = product,
                                ParentProductId = ProductId
                            };

                            productCombinationRepository.Add(productCombinaton);
                            await productCombinationRepository.SaveChangesAsync();
                        }
                    }

                    await DBTrans.CommitAsync();
                    return true;
                }
                catch
                {
                    await DBTrans.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task<bool> EditProductVariations(List<VariationsViewModel> variations, long productId)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTrans = await context.Database.BeginTransactionAsync();

                try
                {
                    var user = await workContext.GetCurrentUser();
                    var userId = await workContext.GetCurrentUserId();
                    var siteId = workContext.GetCurrentSiteId();
                    var currentCultureId = workContext.GetCurrentCultureId();

                    var originalProduct = await productRepository.Query()
                        .Include(x => x.ProductDetails)
                            .ThenInclude(x => x.SlugInfo)
                        .FirstOrDefaultAsync(x => x.Id == productId);

                    if (originalProduct == null)
                        throw new Exception($"Original product with Id {productId} was not found.");

                    foreach (var variation in variations)
                    {
                        var product = await productRepository.Query()
                            .Include(x => x.ProductDetails)
                                .ThenInclude(x => x.SlugInfo)
                            .Include(x => x.TaxClass)
                                .ThenInclude(x => x.TaxRate)
                            .FirstOrDefaultAsync(x => x.Id == variation.Id);

                        if (product == null)
                            throw new Exception($"Variation product with Id {variation.Id} was not found.");

                        var originalVariation = new Product
                        {
                            Id = product.Id,
                            SpecialForignPrice = product.SpecialForignPrice,
                            SpecialPriceStart = product.SpecialPriceStart,
                            SpecialPriceEnd = product.SpecialPriceEnd,
                            Price = product.Price,
                            ForeignPrice = product.ForeignPrice,
                            IsAllowForeign = product.IsAllowForeign,
                            SpecialPrice = product.SpecialPrice
                        };

                        product.SiteId = siteId;
                        product.Price = variation.Price ?? 0;

                        product.ForeignPrice =
                            variation.ForeignPrice == null || variation.ForeignPrice == 0
                                ? null
                                : variation.ForeignPrice.Value;

                        product.Status = originalProduct.Status;
                        product.HasOptions = false;
                        product.IsProductSerial = originalProduct.IsProductSerial;
                        product.PriceType = originalProduct.PriceType;
                        product.ThumbnailImageId = originalProduct.ThumbnailImageId;
                        product.IsPromotional = originalProduct.IsPromotional;
                        product.StockTrackingIsEnabled = originalProduct.StockTrackingIsEnabled;
                        product.IsFeatured = originalProduct.IsFeatured;
                        product.IsVisibleIndividually = originalProduct.IsVisibleIndividually;
                        product.IsCallForPricing = originalProduct.IsCallForPricing;
                        product.IsAllowToOrder = originalProduct.IsAllowToOrder;

                        product.ModificationDate = DateTime.Now;
                        product.ModifiedById = userId;
                        product.Sku = variation.Sku;
                        product.Gtin = variation.Gtin;
                        product.MaxAllowedQuantity = variation.MaxAllowedQuantity;
                        product.IsAllowForeign = variation.ForeignPrice != null && variation.ForeignPrice != 0;

                        var optionValueIds = variation.OptionCombinations?
                            .Where(x => x.ValueId.HasValue)
                            .Select(x => x.ValueId.Value)
                            .Distinct()
                            .ToList() ?? new List<long>();

                        var optionValues = optionValueIds.Any()
                            ? await productOptionValueDetails.Query()
                                .Where(x => optionValueIds.Contains(x.OptionValueId))
                                .Select(x => x.Value)
                                .ToListAsync()
                            : new List<string>();

                        var optionSlugPart = optionValues.Any()
                            ? string.Join("_", optionValues.Where(x => !string.IsNullOrWhiteSpace(x)))
                            : string.Empty;

                        if (variation.variationsDetails != null && variation.variationsDetails.Any())
                        {
                            foreach (var item in variation.variationsDetails)
                            {
                                var cultureId = item.CultureId == 0
                                    ? currentCultureId
                                    : item.CultureId;

                                var productDetail = product.ProductDetails
                                    .FirstOrDefault(x => x.CultureId == cultureId);

                                if (productDetail == null)
                                    continue;

                                var slug = string.IsNullOrWhiteSpace(optionSlugPart)
                                    ? item.Name
                                    : $"{item.Name}_{optionSlugPart}";

                                var slugManagementVm = new SlugManagementVm
                                {
                                    Id = productDetail.SlugInfoId,
                                    CultureId = cultureId,
                                    Slug = slug,
                                    Entity = EEntity.Product,
                                    SiteId = siteId
                                };

                                productDetail.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);
                                productDetail.Name = item.Name;
                                productDetail.NormalizedName = item.NormalizedName;
                                productDetail.CultureId = cultureId;
                                productDetail.ProductTemplateId = item.ProductTemplateId;
                                productDetail.ProductId = product.Id;
                            }
                        }

                        await HandleProductPriceHistory(originalVariation, product, user.Id);

                        await productRepository.SaveChangesAsync();
                    }

                    await dbTrans.CommitAsync();

                    return true;
                }
                catch
                {
                    await dbTrans.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task<List<VariationsViewModel>> ModifyCombinations(List<VariationsViewModel> Variations, long ProductId)
        {
            try
            {

                var OriginalVariations = await GetProductVaration(ProductId);

                await DeleteAllVariations(ProductId);

                await AddProductVariations(Variations, ProductId);

                var ModifiedVariations = await GetProductVaration(ProductId);

                foreach (var OriginalVariation in OriginalVariations)
                {
                    var PossibleTargetVariations = ModifiedVariations.ToList();

                    foreach (var Combination in OriginalVariation.OptionCombinations)
                    {
                        PossibleTargetVariations = PossibleTargetVariations.Where(p => p.OptionCombinations.Any(x => x.ValueId == Combination.ValueId && x.OptionId == Combination.OptionId)).ToList();
                    }
                    await CloneVariation(OriginalVariation.Id, PossibleTargetVariations.Select(x => x.Id).ToList());
                }

                return await GetProductVaration(ProductId);

            }
            catch (Exception e)
            {

                throw;
            }
        }



        public async Task<bool> DeleteAllVariations(long ProductId)
        {
            var userId = await workContext.GetCurrentUserId();
            var TargetVariationsLinks = await productLinkRepository.Query()
                        .Where(p => p.ProductId == ProductId)
                        .Include(prop => prop.LinkedProduct)
                        .ThenInclude(x => x.ProductDetails)
                        .ThenInclude(x => x.SlugInfo)
                        .ToListAsync();

       

            foreach (var variationLink in TargetVariationsLinks)
            {
                foreach (var item in variationLink.LinkedProduct.ProductDetails)
                {



                    SlugManagementVm slugManagementVm = new()
                    {
                        Id = item.SlugInfoId,
                        CultureId = item.CultureId,
                        Entity = EEntity.Product,
                        SiteId = variationLink.LinkedProduct.SiteId,
                        Slug = item.SlugInfo.Id + "Deleted" + item.SlugInfo.Slug,

                    };
                    item.SlugInfoId = await slugServices.UpsertAndGetId(slugManagementVm);


                    variationLink.LinkedProduct.Status = EStatus.Deleted;

               //     productLinkRepository.Remove(variationLink);
                }
                await RemoveProductSerialIfExist(variationLink.LinkedProductId);
            }

            var variationIds = new List<long>();

            foreach (var link in TargetVariationsLinks)
            {
                if (link.LinkType == ItemLinkType.Super)
                {
                    variationIds.Add(link.LinkedProductId);
                    productLinkRepository.Remove(link);
                    await productLinkRepository.SaveChangesAsync();
                }




            }


            #region ProductLink History
            var variationToBeRemoved = TargetVariationsLinks.Where(x => x.LinkType == ItemLinkType.Super)
                                                           .ToList();
            await HandleProductLinkHistory(variationToBeRemoved, userId, EHistoryAction.Delete);
            #endregion



            //  Remove OptionCompination
            var OptionCompination = await productCombinationRepository.Query().Where(x => variationIds.Contains(x.ProductId)).ToListAsync();
            if (OptionCompination != null)
            {
                productCombinationRepository.RemoveRange(OptionCompination);
                await productCombinationRepository.SaveChangesAsync();
            }


            await productLinkRepository.SaveChangesAsync();
             await productRepository.SaveChangesAsync();
            return true;
        }

        private async  Task<bool> RemoveProductSerialIfExist(long linkedProductId)
        {
            try
            {
                var ProductSerial =await productSerialRepository.Query().Where(x => x.ProductId == linkedProductId&&x.Status==EProductSerialStatus.Available).ToListAsync();
                if (ProductSerial.Count > 0)
                {
                    foreach (var productserial in ProductSerial)
                    {
                        await productSerialService.ChangeProductSerialStatus(productserial.Id, EProductSerialStatus.Deleted);

                    }

                    return true;
                }
                else
                {

                    return true;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<VariationsViewModel>> GetProductVaration(long ProductId)
        {
            try
            {
                var CultureId = workContext.GetCurrentCultureId();


                var SiteId = await workContext.GetCurrentSiteIdAsync();

                if (CultureId == 0)
                {
                    CultureId = _SitesCulturesRepo.Query().Where(x => x.SiteId == SiteId).Select(x => x.Id).OrderBy(x => x).FirstOrDefault();
                }



                var productLinks = await productLinkRepository.Query()
                                        .Include(x => x.LinkedProduct)
                                         .Include("LinkedProduct.OptionCombinations")
                                         .Include("LinkedProduct.OptionCombinations.Value")
                                         .Include("LinkedProduct.OptionCombinations.Option")
                                         .Include("LinkedProduct.OptionCombinations.Value.productOptionValueDetails")
                                         .Include("LinkedProduct.OptionCombinations.Option.optionDetails")
                                         .Include("LinkedProduct.ProductDetails")
                                         .Include("LinkedProduct.ProductDetails.Culture")
                                        .Include(x => x.Product)
                                        .ThenInclude(x => x.OptionCombinations)
                                        .ThenInclude(x => x.Option)
                                        .ThenInclude(x=>x.productOptionValues)
                                        .ThenInclude(x=>x.productOptionValueDetails)
                                        .Include(x => x.Product)
                                        .ThenInclude(x => x.ProductDetails)
                                        .ThenInclude(x => x.Culture)
                                        .Include(x => x.Product)
                                        .ThenInclude(x => x.ThumbnailImage)
                                        .Where(x => x.ProductId == ProductId
                                        && x.LinkType == ItemLinkType.Super&&x.LinkedProduct.Status!=EStatus.Deleted)
                                        .AsNoTrackingWithIdentityResolution()
                                        .ToListAsync();


              

                var variationList = productLinks
                    .Select(x => new VariationsViewModel
                    {
                        Id = x.LinkedProduct.Id,
                        SpecialForignPrice = x.LinkedProduct.SpecialForignPrice,
                        Price = x.LinkedProduct.Price,
                        ForeignPrice = x.LinkedProduct.ForeignPrice,
                        Sku = x.LinkedProduct.Sku,
                        Gtin = x.LinkedProduct.Gtin,
                        
                        SpecialPrice = x.LinkedProduct.SpecialPrice,
                        ThumbnailImageId = x.LinkedProduct.ThumbnailImageId,
                        ThumbnailImageUrl = x.LinkedProduct.ThumbnailImage==null?null: x.LinkedProduct.ThumbnailImage.PublicUrl,
                        OptionCombinations = x.LinkedProduct.OptionCombinations.Select(c => new ItemOptionCombinationVm
                        {
                            OptionId = c.OptionId,
                            OptionName = c.Option.optionDetails == null ? "" : c.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                            ValueId=c.ValueId,
                            Values = c.Value.productOptionValueDetails.Where(x => x.CultureId == CultureId).Select(x => new ProductValuesOptionsViewModel
                            {

                                ValueId = x.OptionValueId,
                                ValueName = x.Value,

                            }).ToList(),
                            SortIndex = c.SortIndex
                        }).ToList(),
                        variationsDetails = x.LinkedProduct.ProductDetails.Select(y => new VariationsDetailsViewModel
                        {
                            Id = y.Id,
                            Name = y.Name,
                            ProductTemplateId = y.ProductTemplateId,
                            CultureId = y.Culture != null ? y.Culture.Id : 0,
                            CultureCode=y.Culture!=null?y.Culture.CultureId:""
                        }
                        ).ToList()
                    }).ToList();
                return variationList;


            }
            catch (Exception ex)
            {
                throw ex;

            }
        }
        public async Task<bool> DeleteProductVariation(long ProductLinkId)
        {
            var Link = await productLinkRepository.Query().Where(x => x.LinkedProductId == ProductLinkId).FirstOrDefaultAsync();
            var product = await productRepository.Query().Include(x => x.ProductDetails).Where(x => x.Id == ProductLinkId).FirstOrDefaultAsync();
            product.Status = EStatus.Deleted;
            foreach (var item in product.ProductDetails)
            {

                await slugServices.DeleteSlugInfo(item.SlugInfoId);

            }
            productRepository.Update(product);
            await productRepository.SaveChangesAsync();
            bool result = false;
            if (Link != null)
            {
                productLinkRepository.Remove(Link);
                await productLinkRepository.SaveChangesAsync();
                result = true;
            }


            //  Remove OptionCompination
            var OptionCompination = await productCombinationRepository.Query().Where(x => x.ProductId == ProductLinkId).ToListAsync();
            if (OptionCompination != null)
            {
                productCombinationRepository.RemoveRange(OptionCompination);
                await productCombinationRepository.SaveChangesAsync();
            }
            return result;
        }

        public async Task<ProductWithVariationsVM> GetProductAndVariation(long VariationId)
        {
            try
            {
                var ParentProduct = await productLinkRepository.Query()
                                      .Where(x => x.LinkType == ItemLinkType.Super && x.LinkedProductId == VariationId)
                                      .Include(x => x.Product)
                                      .ThenInclude(x => x.ProductDetails)
                                      .Select(x => x.Product)

                                      .FirstOrDefaultAsync();

                if (ParentProduct == null)
                {
                    throw new Exception("VariationId Not Found");
                }


                ProductWithVariationsVM Result = new ProductWithVariationsVM();
                Result.Id = ParentProduct.Id;
                Result.Name = ParentProduct.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name;
                Result.Variations = await GetProductVaration(ParentProduct.Id);

                return Result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<VariationsViewModel>> GetProductVarationForAdmin(long ProductId)
        {
            try
            {

                var productLinks = await productLinkRepository.Query()
                                         .Include(x => x.LinkedProduct)
                                         .Include("LinkedProduct.ProductAvaliablity")
                                         .Include("LinkedProduct.OptionCombinations")
                                         .Include("LinkedProduct.OptionCombinations.Value")
                                         .Include("LinkedProduct.OptionCombinations.Option")
                                         .Include("LinkedProduct.OptionCombinations.Value.productOptionValueDetails")
                                         .Include("LinkedProduct.OptionCombinations.Option.optionDetails")
                                         .Include("LinkedProduct.ProductDetails")
                                         .Include("LinkedProduct.ProductDetails.Culture")
                                         .Include(x => x.Product)
                                         .ThenInclude(x => x.OptionCombinations)
                                         .ThenInclude(x => x.Option)
                                           .ThenInclude(x => x.productOptionValues)
                                        .ThenInclude(x => x.productOptionValueDetails)
                                         .Include(x => x.Product)
                                         .ThenInclude(x => x.ProductDetails)
                                         .ThenInclude(x => x.Culture)
                                         .Include(x => x.Product)
                                         .ThenInclude(x => x.ThumbnailImage)
                                         .Where(x => x.ProductId == ProductId
                                         && x.LinkType == ItemLinkType.Super && x.LinkedProduct.Status != EStatus.Deleted)
                                         .ToListAsync();




                var variationList = productLinks
                    .Select(x => new VariationsViewModel
                    {
                        Id = x.LinkedProduct.Id,
                        SpecialForignPrice = x.LinkedProduct.SpecialForignPrice,
                        Price = x.LinkedProduct.Price,
                        ForeignPrice = x.LinkedProduct.ForeignPrice,
                        Sku = x.LinkedProduct.Sku,
                        Gtin = x.LinkedProduct.Gtin,
                        SpecialPrice = x.LinkedProduct.SpecialPrice,
                        ThumbnailImageId = x.LinkedProduct.ThumbnailImageId,
                        ThumbnailImageUrl = x.LinkedProduct.ThumbnailImage == null ? null : x.LinkedProduct.ThumbnailImage.PublicUrl,
                        ProductAvaliablity = x.LinkedProduct.ProductAvaliablity == null && x.LinkedProduct.ProductAvaliablity.Count() > 0
                                                  ? new List<ProductAvaliablityRequest>() :
                                                  x.LinkedProduct.ProductAvaliablity.Select(x => new ProductAvaliablityRequest
                                                  {

                                                      CountryId = x.CountryId,
                                                      Permission = x.Permission,
                                                      StateOrProvinceId = x.StateOrProvinceId
                                                  }).ToList()
                                                  ,
                        OptionCombinations = x.LinkedProduct.OptionCombinations.Select(c => new ItemOptionCombinationVm
                        {
                            OptionId = c.OptionId,
                            OptionName = c.Option.optionDetails == null ? "" : c.Option.optionDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                            ValueId=c.ValueId,
                            Values = c.Value.productOptionValueDetails.Select(x => new ProductValuesOptionsViewModel
                            {

                                ValueId = x.OptionValueId,
                                ValueName = x.Value,
                                CultureId=x.CultureId
                            }).ToList(),
                            SortIndex = c.SortIndex
                        }).ToList(),
                        variationsDetails = x.LinkedProduct.ProductDetails.Select(y => new VariationsDetailsViewModel
                        {
                            Id = y.Id,
                            Name = y.Name,
                            ProductTemplateId = y.ProductTemplateId,
                            CultureId = y.Culture != null ? y.Culture.Id : 0,
                            CultureCode = y.Culture != null ? y.Culture.CultureId : ""
                        }
                        ).ToList()
                    }).ToList();
                return variationList;


            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        public async Task<bool> ChangeOrderingForOptions(long ProductId, List<ChangeAvailableOptionOrderVM> model)
        {
            try
            {
                foreach(var item in model)
                {
                    var allShouldNotHaveSameOrder = item.OrderingOptionValue.Select(x => x.DisplayOrder).Distinct().Count() == item.OrderingOptionValue.Count;

                    if (!allShouldNotHaveSameOrder)
                    {
                       
                        throw new Exception("Display orders must be unique.");
                    }


                }

                var optionIds = model.Select(x => x.OptionId).ToList();
               
                var optioncombinationIndDb = await productCombinationRepository.Query()
                                                                         .Where(x => optionIds.Contains(x.OptionId) && x.ParentProductId == ProductId)
                                                                         .ToListAsync();
                var optionValuesWithNewOrder  = model.SelectMany(x => x.OrderingOptionValue);

                foreach (var optioncombinationItem in optioncombinationIndDb)
                {

                    var newOrdering = optionValuesWithNewOrder.Where(x => x.ValueId == optioncombinationItem.ValueId).FirstOrDefault();

                    if(newOrdering is not null)
                    {
                        optioncombinationItem.SortIndex = newOrdering.DisplayOrder;
                    }
                }

                productCombinationRepository.UpdateRange(optioncombinationIndDb);
                await productCombinationRepository.SaveChangesAsync();







                return true;


            }
            catch (Exception ex)
            {
                throw ex;

            }
        }



        private async Task<bool> CloneVariation(long OriginalVariationId, List<long> NewVariationsIds)
        {
            var Products = await productRepository.Query().Where(x => x.Id == OriginalVariationId || NewVariationsIds.Contains(x.Id))
                .Include(product => product.ProductDetails)
                .Include(product => product.Categories)
                .Include(product => product.Medias)
                .Include(product => product.AttributeValues)
                .ToListAsync();

            var OriginalProduct= Products.FirstOrDefault(x=>x.Id== OriginalVariationId);

            Products.Remove(OriginalProduct);

            foreach (var NewVariation in Products)
            {
                NewVariation.Sku = OriginalProduct.Sku;
                NewVariation.IsFeatured = OriginalProduct.IsFeatured;
                NewVariation.RequesStatus = OriginalProduct.RequesStatus;
                NewVariation.BrandId = OriginalProduct.BrandId;
                NewVariation.BuyoutPrice = OriginalProduct.BuyoutPrice;
                NewVariation.Type = OriginalProduct.Type;
                NewVariation.ThumbnailImageId = OriginalProduct.ThumbnailImageId;
                NewVariation.VendorId = OriginalProduct.VendorId;
                NewVariation.TaxClassId = OriginalProduct.TaxClassId;
                NewVariation.StockTrackingIsEnabled = OriginalProduct.StockTrackingIsEnabled;
                NewVariation.SpecialPriceStart = OriginalProduct.SpecialPriceStart;
                NewVariation.SpecialPriceEnd = OriginalProduct.SpecialPriceEnd;
                NewVariation.SpecialPrice = OriginalProduct.SpecialPrice;
                NewVariation.SpecialForignPrice = OriginalProduct.SpecialForignPrice;
                NewVariation.ProductAdditionalFees = OriginalProduct.ProductAdditionalFees.Where(x => x.Status != EStatus.Deleted).ToList();
                NewVariation.Price = OriginalProduct.Price;
                NewVariation.MinPrice = OriginalProduct.MinPrice;
                NewVariation.MaxAllowedQuantity = OriginalProduct.MaxAllowedQuantity;
                NewVariation.IsVisibleIndividually = OriginalProduct.IsVisibleIndividually;
                NewVariation.IsProductSerial = OriginalProduct.IsProductSerial;
                NewVariation.IsPlan = OriginalProduct.IsPlan;
                NewVariation.IsCallForPricing = OriginalProduct.IsCallForPricing;
                NewVariation.IsAllowToOrder = OriginalProduct.IsAllowToOrder;
                NewVariation.IsAllowForeign = OriginalProduct.IsAllowForeign;
                NewVariation.IncludedInReport = OriginalProduct.IncludedInReport;
                NewVariation.Gtin = OriginalProduct.Gtin;
                NewVariation.DisplayOrder = OriginalProduct.DisplayOrder;

                foreach (var CultureDetails in NewVariation.ProductDetails)
                {

                    var OriginalCultureDetails = OriginalProduct.ProductDetails.FirstOrDefault(x => x.CultureId == CultureDetails.CultureId);
                    CultureDetails.AddtionalMetaTags=OriginalCultureDetails.AddtionalMetaTags;
                }

                foreach (var MediaItem in OriginalProduct.Medias)
                {
                    NewVariation.Medias.Add(new ProductMedia()
                    {
                        DisplayOrder = MediaItem.DisplayOrder,
                        MediaId= MediaItem.MediaId,
                        ProductId= NewVariation.Id

                    });
                }


                foreach (var AttItem in OriginalProduct.AttributeValues)
                {
                    NewVariation.AttributeValues.Add(new ProductAttributeValue()
                    {
                        AttributeId = AttItem.AttributeId,
                        AttributeValueDetails = AttItem.AttributeValueDetails
                        .Select(x => new ProductAttributeValueDetails(){
                            AttributeValue=x.AttributeValue,
                            CultureId=x.CultureId,
                            Value=x.Value
                        })
                        .ToList(),
                        ProductId = NewVariation.Id

                    });
                }

                foreach (var CatItem in OriginalProduct.Categories)
                {
                    NewVariation.Categories.Add(new ProductCategory()
                    {
                        CategoryId=CatItem.CategoryId,
                        DisplayOrder=CatItem.DisplayOrder,
                        ProductId = NewVariation.Id

                    });
                }

            }

            await productRepository.SaveChangesAsync();

            //await HandleProductPriceHistory();

            //await HandleProductCategoryHistory();


            return true;
        }



        private async Task<bool> HandleProductPriceHistory(Product orgignalProduct, Product updatedProduct, long userId)
        {

            if (updatedProduct is null)
            {
                var productPriceHistory = new ProductPriceHistory(orgignalProduct, userId, EHistoryAction.Add);

                productPriceRepository.Add(productPriceHistory);
                await productPriceRepository.SaveChangesAsync();
            }
            else
            {

                if (HasPriceChanged(orgignalProduct, updatedProduct))
                {
                    var productPriceHistory = new ProductPriceHistory(orgignalProduct, userId, EHistoryAction.Update);

                    productPriceRepository.Add(productPriceHistory);
                    await productPriceRepository.SaveChangesAsync();

                }

            }




            return true;
        }


        private async Task<bool> HandleProductLinkHistory(List<ProductLink> productLinks, long userId , EHistoryAction action )
        {

            if (productLinks.Any())
            {
                var productLinkHistory = productLinks.Select(x => new ProductLinkHistory
                {
                    ProductId = x.ProductId,
                    LinkedProductId = x.LinkedProductId,
                    Action = action,
                    CreatedById = userId,
                    LinkType = x.LinkType,
                    CreatedOn=DateTimeOffset.Now
                }).ToList();


                _productLinkHistoryRepository.AddRange(productLinkHistory);
                await _productLinkHistoryRepository.SaveChangesAsync();
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
    }
}
