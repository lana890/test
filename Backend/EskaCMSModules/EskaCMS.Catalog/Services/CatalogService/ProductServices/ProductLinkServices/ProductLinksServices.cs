using EskaCMS.Core.Data;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Areas.Core.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;

using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices
{
    public class ProductLinksServices : IProductLinkServices
    {
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<Product> _productRepository;
        private readonly IProductServices _productService;
        private readonly ICurrencyService currencyService;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IProductPricingService productPricingService;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<ProductLink> _productLinkRepository;


        private readonly IRepository<ProductCategoryHistory> _productCategoryHistoryRepository;
        public ProductLinksServices(IWorkContext workContext, EskaDCMSDbContext context,
        IRepository<Product> productRepository, IProductServices productService,
        ICurrencyService currencyService,
        IRepository<ProductCategory> productCategoryRepository,
        IProductPricingService productPricingService,
        IUserSiteService userSiteService,
        IRepository<ProductCategoryHistory> productCategoryHistoryRepository,
        IRepository<ProductLink> productLinkRepository)
        {
            _workContext = workContext;
            _context = context;
            _productRepository = productRepository;
            _productLinkRepository = productLinkRepository;
            _productCategoryRepository = productCategoryRepository;
            this.productPricingService = productPricingService;
            this.userSiteService = userSiteService;
            _productService = productService;
            this.currencyService = currencyService;
            _productCategoryHistoryRepository = productCategoryHistoryRepository;
        }


        public async Task<bool> DeleteProductLinks(long ProductId)
        {
            bool result = false;
            var ProductLink = await _productLinkRepository.Query().Where(x => x.Id == ProductId).FirstOrDefaultAsync();
            if (ProductLink != null)
            {
                _productLinkRepository.Remove(ProductLink);
                await _productLinkRepository.SaveChangesAsync();
                result = true;
            }
            return result;
        }





        public async Task<bool> DeleteLink(long LinkId)
        {
            bool result = false;
            var ProductLink = await _productLinkRepository.Query().Where(x => x.Id == LinkId).FirstOrDefaultAsync();
            if (ProductLink != null)
            {
                _productLinkRepository.Remove(ProductLink);
                await _productLinkRepository.SaveChangesAsync();
                result = true;
            }
            return result;
        }




        public async Task<List<ProductLinkViewModel>> GetLinkedProducts(long ProductId, ItemLinkType linkType)
        {
            try
            {
                var product = await _productLinkRepository.Query()
                                        .AsNoTracking()
                                        .Include(x => x.LinkedProduct)
                                        .ThenInclude(x => x.ProductDetails)
                                          .ThenInclude(x => x.Culture)
                                        .Include(x => x.Product)
                                        .ThenInclude(x => x.ProductDetails)
                                        .ThenInclude(z => z.Culture)
                                        .Where(x => x.ProductId == ProductId
                                         && x.LinkType == linkType)
                                        .ToListAsync();

                if (product.Count <= 0)
                {

                    return new List<ProductLinkViewModel>();

                }
                var RelatedProductList = product.Select(x => new ProductLinkViewModel
                {

                    Id = x.Id,

                    LinkedProductId = x.LinkedProductId,
                    Status = x.LinkedProduct.Status,
                    CultureCode = x.LinkedProduct.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Culture?.CultureId,
                    CultureId = x.LinkedProduct.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CultureId,
                    Name = x.LinkedProduct.ProductDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,



                }).ToList();

                return RelatedProductList;
            }


            catch (Exception ex)
            {
                throw ex;
            }
        }




        public async Task<bool> UpsertProductLinks(List<ItemLinkVm> ProductLinks, long ProductId, ItemLinkType itemLinkType)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var Transaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    await DeleteUnexistedLinks(ProductId, itemLinkType, ProductLinks.Select(x => x.Id).ToArray());
                    await AddExtraLinks(ProductId, itemLinkType, ProductLinks);

                    await Transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await Transaction.RollbackAsync();

                    throw ex;
                }
            });

            return true;
        }
        public async Task<PaginationResult<ProductThumbnail>> GetLinkedProductsForWeb(SmartTableParam param, long productId)
        {
            var cultureId = _workContext.GetCurrentCultureId();
            var UserCountry = await userSiteService.GetUserCountry();
            var siteCountryId = await userSiteService.GetSiteCountry(_workContext.GetCurrentSiteId());
            var warehouseId = await _workContext.GetCurrentWarehouseId();

            var Currency = await currencyService.GetUserCurrencyByCulture();

            var IsLocalCountry = await userSiteService.IsLocalCountry();
            var allRelatedproduct = _productLinkRepository.Query()
                                       .AsNoTracking()
                                       .Where(x => x.ProductId == productId)
                                       .Where(x => x.LinkedProduct.Status == EStatus.Published)
                                       .Where(x=>warehouseId==0 || !x.LinkedProduct.ProductBranch.Any()|| x.LinkedProduct.ProductBranch.Any(u=>u.WarehouseId==warehouseId));

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.ItemLinkType != null)
                {
                    ItemLinkType LinkType = (ItemLinkType)search.ItemLinkType;
                    if (LinkType!=ItemLinkType.Sub)
                    {
                        allRelatedproduct = allRelatedproduct.Where(x => x.LinkType == LinkType);
                    }
                }
            }

            var totalCount = await allRelatedproduct.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)param.Pagination.Number);
            var pageLinkIds = await allRelatedproduct
                .Select(x => x.Id)
                .Skip(param.Pagination.Start * param.Pagination.Number)
                .Take(param.Pagination.Number)
                .ToListAsync();

            if (!pageLinkIds.Any())
            {
                return new PaginationResult<ProductThumbnail>(new List<ProductThumbnail>(), totalPages, totalCount);
            }

            var pageOrder = pageLinkIds
                .Select((id, index) => new { id, index })
                .ToDictionary(x => x.id, x => x.index);

            var pageProducts = await _productLinkRepository.Query()
                .AsNoTracking()
                .Where(x => pageLinkIds.Contains(x.Id))
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ProductBranch)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ProductPriceWarehouses)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ProductAvaliablity)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ProductDetails)
                .ThenInclude(x => x.Culture)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ThumbnailImage)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.ProductDetails)
                .ThenInclude(x => x.SlugInfo)
                .Include(x => x.LinkedProduct)
                .ThenInclude(x => x.StockAmounts)
                .AsSplitQuery()
                .ToListAsync();

            var result = new PaginationResult<ProductThumbnail>(
                pageProducts
                    .OrderBy(x => pageOrder[x.Id])
                    .Select(x => ProductThumbnail.FromProduct(x.LinkedProduct, UserCountry, siteCountryId, cultureId))
                    .ToList(),
                totalPages,
                totalCount);


            foreach (var item in result.Data)
            {
                item.CalculatedProductPrice = productPricingService.CalculatedProductPrice(item, UserCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry);
                item.OriginalProductId = productId;

            }
            return result;


        }



        public async Task<List<long>> GetProductsCategory(long ParentId)

        {
            try
            {
                var categories = await _productCategoryRepository.Query().Where(x => x.ProductId == ParentId).Select(i => i.CategoryId).ToListAsync();
                return categories;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> UpsertProductsCategory(ProductCategoryViewModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var DbTran = await _context.Database.BeginTransactionAsync();
                try
                {
                    var userId = await _workContext.GetCurrentUserId();

                    var Query = await _productCategoryRepository.Query().Where(x => x.ProductId == model.ProductId).AsNoTracking().ToListAsync();

                    var productCategoryToBeRemoved = Query.Where(x => !model.CategoriesId.Contains(x.CategoryId))
                                                                .ToList();

                    var categoryTobeAdded = model.CategoriesId.Where(x => !Query.Any(y => y.CategoryId == x))
                                                             .ToList();
                    if (productCategoryToBeRemoved.Count() > 0)
                    {
                        _productCategoryRepository.RemoveRange(productCategoryToBeRemoved);
                        await HandleProductCategoryHistory(productCategoryToBeRemoved, userId, EHistoryAction.Delete);

                    }


                    IList<ProductCategory> AddProductCategory = new List<ProductCategory>();

                    foreach (var Category in categoryTobeAdded)
                    {

                        AddProductCategory.Add(new ProductCategory() { ProductId = model.ProductId, CategoryId = Category });


                    }
                    _productCategoryRepository.AddRange(AddProductCategory);
                    await _productCategoryRepository.SaveChangesAsync();

                    await HandleProductCategoryHistory(AddProductCategory.ToList(), userId, EHistoryAction.Add);

                    await DbTran.CommitAsync();
                }
                catch (Exception ex)
                {
                    await DbTran.RollbackAsync();
                    throw ex;
                }
            });

            return true;
        }

        #region Helper Functions

        private async Task DeleteUnexistedLinks(long ProductId, ItemLinkType LinkType, long[] TargetLinksIds)
        {
            var AllLinks = await _productLinkRepository.Query().Where(x => x.ProductId == ProductId && x.LinkType == LinkType).ToListAsync();
            var DeletedLinks = AllLinks.Where(x => !TargetLinksIds.Contains(x.Id)).ToList();

            if (DeletedLinks.Count > 0)
            {
                _productLinkRepository.RemoveRange(DeletedLinks);
                await _productLinkRepository.SaveChangesAsync();
            }
        }




        private async Task AddExtraLinks(long ProductId, ItemLinkType LinkType, List<ItemLinkVm> TargetLinks)
        {
            List<ProductLink> NewLinks = TargetLinks
                                                .Where(x => x.Id == 0)
                                                .Select(x => new ProductLink()
                                                {
                                                    Id = 0,
                                                    LinkedProductId = x.LinkedProductId,
                                                    LinkType = LinkType,
                                                    ProductId = ProductId
                                                }).ToList();

            if (NewLinks.Count > 0)
            {
                _productLinkRepository.AddRange(NewLinks);
                await _productLinkRepository.SaveChangesAsync();
            }
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


