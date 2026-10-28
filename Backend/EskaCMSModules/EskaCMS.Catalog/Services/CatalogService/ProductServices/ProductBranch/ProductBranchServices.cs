using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.ProductBranch;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch
{
    public class ProductBranchServices : IProductBranchServices
    {
        private readonly IWorkContext workContext;
        private readonly IRepository<EskaCMS.Entities.Entity.Catalog.ProductBranch> productbranchReporitory;
        private readonly IRepository<ProductPriceWarehouse> productPriceWarehouserepository;
        private readonly IRepository<Product> productRepository;
        private readonly IRepository<StockAmount> stockAmountRepository;
        private readonly IRepository<Stock> stockRepository;

        public ProductBranchServices(IWorkContext workContext,
                                     IRepository<EskaCMS.Entities.Entity.Catalog.ProductBranch> productbranchReporitory,
                                     IRepository<ProductPriceWarehouse> productPriceWarehouserepository,
                                     IRepository<Product> productRepository,
                                     IRepository<StockAmount> stockAmountRepository,
                                     IRepository<Stock> stockRepository)
        {
            this.workContext = workContext;
            this.productbranchReporitory = productbranchReporitory;
            this.productPriceWarehouserepository = productPriceWarehouserepository;
            this.productRepository = productRepository;
            this.stockAmountRepository=stockAmountRepository;
            this.stockRepository = stockRepository;
        }

        public async Task<bool> UpsertProductBranch(RequestProductBranchViewModel model)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var relatedProductIds = await GetRelatedProductIds(model.MainProductId);
                if (model.productBranches != null)
                {
                    if (model.productBranches.Any(x => x.Price.HasValue))
                        await UpsertProductPricesBranches(model.productBranches, relatedProductIds, userId);

                    if (model.productBranches.Any(x => x.WarehouseId.HasValue))
                        await UpsertAvailabilityBranches(model.productBranches, relatedProductIds);

                    if (model.productBranches.Any(x => x.Quantity.HasValue))
                        await UpsertProductStock(model.productBranches, relatedProductIds);
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<ResultProductBranchViewModel>> GetProductBranchById(long ProductId)
        {
            try
            {
                var productData = await productRepository.Query()
                                          .AsNoTracking()
                                          .Include(x => x.ProductLinks)
                                          .Include(x => x.ProductBranch)
                                          .Include(x => x.LinkedProductLinks)
                                          .Include(x => x.ProductPriceWarehouses)
                                          .Include(x => x.OptionCombinations)
                                          .ThenInclude(x => x.Value)
                                          .ThenInclude(x => x.productOptionValueDetails)
                                          .Include(x => x.ProductDetails)
                                          .Include(x => x.Stock)
                                          .Where(x => x.Id == ProductId || x.LinkedProductLinks.Any(k => k.ProductId == ProductId))
                                          .ToListAsync();

                var unifiedData = productData
                                     .SelectMany(p =>
                                     {
                                         var branches = p.ProductBranch?
                                             .Select(b => new { b.ProductId, b.WarehouseId })
                                             .Distinct();

                                         return branches.Select(branch =>
                                         {
                                             var price = p.ProductPriceWarehouses
                                                 .FirstOrDefault(pp => pp.ProductId == branch.ProductId && pp.WarehouseId == branch.WarehouseId);

                                             var stock = p.Stock
                                                 .FirstOrDefault(s => s.ProductId == branch.ProductId && s.WarehouseId == branch.WarehouseId);
                                             var productDetails = p.ProductDetails
                                               .FirstOrDefault(s => s.ProductId == branch.ProductId);
                                             var maxCultureId = p.OptionCombinations
                                               .SelectMany(x => x.Value.productOptionValueDetails)
                                               .OrderByDescending(x => x.CultureId).Select(x=>x.CultureId).FirstOrDefault();
                                             var productOption = p.OptionCombinations.Where(s => s.ProductId == branch.ProductId)
                                             .SelectMany(x => x.Value.productOptionValueDetails)
                                                 .Where(x => x.CultureId == maxCultureId)
                                                .Select(s => s.Value)
                                                .ToList();
                                             return new ProductBranchViewModel
                                             {
                                                 ProductId = branch.ProductId,
                                                 WarehouseId = branch.WarehouseId,
                                                 Price = price?.Price,
                                                 Name = productDetails.Name + (productOption.Count() > 0 ? "_" + string.Join("_", productOption) : ""),
                                                 ForeignPrice = price?.ForeignPrice,
                                                 Quantity = stock?.Quantity,
                                                 StockTrackingIsEnabled=p.StockTrackingIsEnabled
                                             };
                                         });
                                     });

                var result = unifiedData
                    .GroupBy(x => x.WarehouseId)
                    .Select(group => new ResultProductBranchViewModel
                    {
                        WarehouseId = group.Key.Value,
                        PriceType = productData.Select(x => x.PriceType).FirstOrDefault(),
                        ProductsData = group.Select(x => new ProductBranchDataViewModel
                        {
                            ProductId = x.ProductId,
                            Price = x.Price,
                            ForeignPrice = x.ForeignPrice,
                            Quantity = x.Quantity,
                            Name = x.Name,
                            StockTrackingIsEnabled = x.StockTrackingIsEnabled
                        }).ToList()
                    })
                    .ToList();

                return result;



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #region helpers
        private async Task<List<long>> GetRelatedProductIds(long mainProductId)
        {
            try
            {

                return await productRepository.Query()
                    .AsNoTracking()
                    .Include(x => x.ProductLinks)
                    .Include(x => x.LinkedProductLinks)
                    .Where(x => x.Id == mainProductId || x.LinkedProductLinks.Any(k => k.ProductId == mainProductId))
                    .Select(x => x.Id)
                    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task UpsertProductPricesBranches(List<ProductBranchViewModel> branches, List<long> relatedProductIds, long userId)
        {
            try
            {
                var updateProducts = await productRepository.Query()
                                         .Where(x => relatedProductIds.Contains(x.Id))
                                         .ToListAsync();

                if (updateProducts != null && updateProducts.Any())
                {
                    foreach (var product in updateProducts)
                    {
                        product.Price = null; 
                        product.PriceType = EskaCMS.Entities.Enum.EPriceType.WarehouseBase;
                    }

                    await productRepository.SaveChangesAsync();
                }

                var oldPrices = await productPriceWarehouserepository.Query()
                    .Where(x => relatedProductIds.Contains(x.ProductId))
                    .ToListAsync();

                productPriceWarehouserepository.RemoveRange(oldPrices);
                await productPriceWarehouserepository.SaveChangesAsync();

                var now = DateTimeOffset.Now;
                var newPrices = branches.Where(x => x.Price.HasValue).Select(x => new ProductPriceWarehouse
                {
                    CreationDate = now,
                    ModificationDate = now,
                    CreatedById = userId,
                    ModifiedById = userId,
                    WarehouseId = x.WarehouseId.Value,
                    Price = x.Price.Value,
                    ProductId = x.ProductId,
                    ForeignPrice = null
                }).ToList();

                productPriceWarehouserepository.AddRange(newPrices);
                await productPriceWarehouserepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        private async Task UpsertAvailabilityBranches(List<ProductBranchViewModel> branches, List<long> relatedProductIds)
        {
            try
            {
                var oldBranches = await productbranchReporitory.Query()
                    .Where(x => relatedProductIds.Contains(x.ProductId))
                    .ToListAsync();

                productbranchReporitory.RemoveRange(oldBranches);
                await productbranchReporitory.SaveChangesAsync();

                var newBranches = branches.Where(x => x.WarehouseId.HasValue).Select(x => new EskaCMS.Entities.Entity.Catalog.ProductBranch
                {
                    WarehouseId = x.WarehouseId.Value,
                    ProductId = x.ProductId
                }).ToList();

                productbranchReporitory.AddRange(newBranches);
                await productbranchReporitory.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        private async Task UpsertProductStock(List<ProductBranchViewModel> stocks, List<long> relatedProductIds)
        {
            try
            {
                var oldStock = await stockRepository.Query()
                    .Where(x => relatedProductIds.Contains(x.ProductId))
                    .ToListAsync();

                stockRepository.RemoveRange(oldStock);
                await stockRepository.SaveChangesAsync();

                var newStock = stocks.Where(x => x.Quantity.HasValue).Select(x => new Stock
                {
                    WarehouseId = x.WarehouseId.Value,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity.Value,
                    Status=EskaCMS.Entities.Enum.GeneralEnums.EStatus.Published
                }).ToList();

                stockRepository.AddRange(newStock);
                await stockRepository.SaveChangesAsync();
                // add stock Avaliblaity

                var OldavalibiltyStock = await stockAmountRepository.Query()
                .Where(x => relatedProductIds.Contains(x.ProductId))
                .ToListAsync();

                stockAmountRepository.RemoveRange(OldavalibiltyStock);
                await stockAmountRepository.SaveChangesAsync();
                var newavalibiltyStock = stocks.Where(x => x.Quantity.HasValue).Select(x => new StockAmount
                {
                    WareHouseId = x.WarehouseId.Value,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity.Value,
                    LastUpdated = DateTime.UtcNow,

                }).ToList();

                stockAmountRepository.AddRange(newavalibiltyStock);
                await stockAmountRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        #endregion
    }
}
