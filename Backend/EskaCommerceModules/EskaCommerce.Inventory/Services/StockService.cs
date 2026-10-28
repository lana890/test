using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Inventory.Services;
using EskaCMS.Entities.Entity.Inventory;

using EskaCMS.Entities.Entity;
using System.Collections.Generic;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCMS.Core.Extensions;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCommerce.Inventory.Services
{
    public class StockService : IStockService
    {
        private readonly IRepository<Stock> _stockRepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<StockHistory> _stockHistoryRepository;

        public StockService(IRepository<Stock> stockRepository, IWorkContext workContext, IRepository<Warehouse> warehouseRepository, IRepository<Product> productRepository, IRepository<StockHistory> stockHistoryRepository)
        {
            _stockRepository = stockRepository;
            this.workContext = workContext;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
            _stockHistoryRepository = stockHistoryRepository;
        }

        public async Task AddAllProduct(Warehouse warehouse)
        {
            var products = await _productRepository.Query()
                .Where(x => !x.HasOptions && x.VendorId == warehouse.VendorId &&
                x.SiteId == warehouse.SiteId && x.Status != EskaCMS.Entities.Enum.GeneralEnums.EStatus.Deleted &&
                !_stockRepository.Query().Where(x => x.WarehouseId == warehouse.Id).Select(x => x.ProductId).Contains(x.Id))
                .ToArrayAsync();

            var stocks = products.Select(x => new Stock { ProductId = x.Id, WarehouseId = warehouse.Id, Quantity = 0 ,Status=EStatus.Published});
            _stockRepository.AddRange(stocks);
            await _stockRepository.SaveChangesAsync();
        }

        public async Task UpdateStock(StockUpdateRequest stockUpdateRequest)
        {
            var product = await _productRepository.Query().FirstOrDefaultAsync(x => x.Id == stockUpdateRequest.ProductId);
            var stock = await _stockRepository.Query().FirstOrDefaultAsync(x => x.ProductId == stockUpdateRequest.ProductId && x.WarehouseId == stockUpdateRequest.WarehouseId);

            stock.Quantity = stock.Quantity + stockUpdateRequest.AdjustedQuantity;
            // product.StockQuantity = produc  t.StockQuantity + stockUpdateRequest.AdjustedQuantity;
            var stockHistory = new StockHistory
            {
                ProductId = stockUpdateRequest.ProductId,
                WarehouseId = stockUpdateRequest.WarehouseId,
                AdjustedQuantity = stockUpdateRequest.AdjustedQuantity,
                Note = stockUpdateRequest.Note,
                CreatedById = stockUpdateRequest.UserId,
                CreatedOn = DateTimeOffset.Now,

            };

            _stockHistoryRepository.Add(stockHistory);
            await _stockHistoryRepository.SaveChangesAsync();
        }


        #region Stock Products
        public async Task<List<GetProductsStockViewModel>> GetAvailableStockByProductId(long productId)
        {
            try
            {
                var result = new List<GetProductsStockViewModel>();
                var siteId = workContext.GetCurrentSiteId();
                var warehouses = await GetWarehousesBySiteId(siteId);
                var product = await GetProductStockById(productId);

                if (product != null)
                {
                    result.Add(GetStockViewModel(product, warehouses));

                    var linkedProducts = GetLinkedProducts(product);
                    foreach (var linkedProduct in linkedProducts)
                    {
                        result.Add(GetStockViewModel(linkedProduct, warehouses));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<Warehouse>> GetWarehousesBySiteId(long siteId)
        {
            return await _warehouseRepository.Query()
                .Where(x => x.SiteId == siteId)
                .ToListAsync();
        }

        private async Task<Product> GetProductStockById(long productId)
        {
            return await _productRepository.Query()
                .Where(c => c.Id == productId && c.StockTrackingIsEnabled)
                .Include(x => x.ProductDetails)
                .Include(x => x.ProductLinks)
                .Include("ProductLinks.LinkedProduct")
                .Include("ProductLinks.LinkedProduct.ProductDetails")
                .Include("ProductLinks.LinkedProduct.StockAmounts")
                .Include("ProductLinks.LinkedProduct.Stock")
                .Include("ProductLinks.LinkedProduct.Stock.Warehouse")
                .Include(x => x.StockAmounts)
                .Include(x => x.Stock)
                .ThenInclude(x => x.Warehouse)
                .FirstOrDefaultAsync();
        }

        private GetProductsStockViewModel GetStockViewModel(Product product, List<Warehouse> warehouses)
        {
            var warehouseStocks = product.Stock.Select(x => CreateWarehouseStock(x, product)).ToList();

            return new GetProductsStockViewModel
            {
                ProductId = product.Id,
                ProductName = product.ProductDetails.Select(x => x.Name).FirstOrDefault(),
                WarehouseStocks = warehouseStocks,
                AvaliableQuantity = warehouseStocks.Sum(x => x.AvaliableQuantity)
            };
        }

        private GetWarehouseStock CreateWarehouseStock(Stock? stock, Product product)
        {
            return new GetWarehouseStock
            {
                Id = stock.Id,
                Quantity = stock.Quantity,
                WarehouseId = stock.WarehouseId,

                WarehouseName = stock.Warehouse.Name,
                AvaliableQuantity = product.StockAmounts
                    .Where(t => t.WareHouseId == stock.WarehouseId)
                    .Select(x => x.Quantity)
                    .FirstOrDefault()
            };
        }


        private List<Product> GetLinkedProducts(Product product)
        {
            return product.ProductLinks
                .Where(x => x.LinkType==ItemLinkType.Super)
                .Select(x => x.LinkedProduct)
                .Where(x => x.IsAllowToOrder && x.Status!=EStatus.Deleted)
                .ToList();
        }
        public async Task<bool> UpsertProductStock(List<AddEditProductStockViewModel> model)
        {
            if (model == null || !model.Any())
                return false;

            try
            {
                var objStocks = model.SelectMany(stock => stock.Warehouses.Select(warehouse => new ObjWarehouseStockVM
                {
                    AdjustedQuantity = warehouse.AdjustedQuantity,
                    Note = warehouse.Note,
                    ProductId = stock.ProductId,
                    WarehouseId = warehouse.WarehouseId,



                })).ToList();

                var updatedStocks = await GetExistingStocksAsync(objStocks);
                var newStocks = GetNewStocks(objStocks, updatedStocks);

                await UpdateExistingStocksAsync(updatedStocks, objStocks);
                await AddNewStocksAsync(newStocks);

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<Stock>> GetExistingStocksAsync(List<ObjWarehouseStockVM> objStocks)
        {
            // Was: load the ENTIRE Stock table then filter in memory.
            // Now: narrow the DB load to only the relevant products/warehouses, then
            // match the exact (ProductId, WarehouseId) pairs in memory. Entities stay
            // TRACKED (the caller updates Quantity and saves), so behavior is unchanged.
            var productIds = objStocks.Select(o => o.ProductId).Distinct().ToList();
            var warehouseIds = objStocks.Select(o => o.WarehouseId).Distinct().ToList();

            var candidates = await _stockRepository.Query()
                .Where(x => productIds.Contains(x.ProductId) && warehouseIds.Contains(x.WarehouseId))
                .ToListAsync();

            return candidates
                .Where(x => objStocks.Any(u => u.ProductId == x.ProductId && u.WarehouseId == x.WarehouseId))
                .ToList();
        }

        private List<ObjWarehouseStockVM> GetNewStocks(IEnumerable<ObjWarehouseStockVM> objStocks, IEnumerable<Stock> updatedStocks)
        {
            return objStocks
                .Where(x => !updatedStocks.Any(u => u.ProductId == x.ProductId && u.WarehouseId == x.WarehouseId))
                .ToList();
        }

        private async Task UpdateExistingStocksAsync(IEnumerable<Stock> updatedStocks, IEnumerable<ObjWarehouseStockVM> objStocks)
        {
            foreach (var stock in updatedStocks)
            {
                var matchingStock = objStocks.FirstOrDefault(x => x.ProductId == stock.ProductId && x.WarehouseId == stock.WarehouseId);
                if (matchingStock == null) continue;

                stock.Quantity += matchingStock.AdjustedQuantity;
                await _stockRepository.SaveChangesAsync();

                var history = await CreateStockHistory(matchingStock, stock.WarehouseId, stock.ProductId);
                _stockHistoryRepository.Add(history);
                await _stockHistoryRepository.SaveChangesAsync();
            }
        }

        private async Task AddNewStocksAsync(IEnumerable<ObjWarehouseStockVM> newStocks)
        {
            foreach (var stock in newStocks)
            {
                var newStock = new Stock
                {
                    ProductId = stock.ProductId,
                    Quantity = stock.AdjustedQuantity,
                    WarehouseId = stock.WarehouseId,
                    Status=EStatus.Published
                };

                _stockRepository.Add(newStock);
                await _stockRepository.SaveChangesAsync();

                var history = await CreateStockHistory(stock, stock.WarehouseId, stock.ProductId);
                _stockHistoryRepository.Add(history);
                await _stockHistoryRepository.SaveChangesAsync();
            }
        }

        private async Task<StockHistory> CreateStockHistory(ObjWarehouseStockVM stock, long warehouseId, long productId)
        {
            try
            {


                return new StockHistory
                {
                    AdjustedQuantity = stock.AdjustedQuantity,
                    CreatedById = await workContext.GetCurrentUserId(),
                    CreatedOn = DateTimeOffset.Now,
                    WarehouseId = warehouseId,
                    Note = stock.Note,
                    ProductId = productId

                };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> ChangeStockStatus(StockStatusViewModel model)
        {
            try
            {
                var stock = await _stockRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == model.StockId);

                if (stock == null)
                    throw new Exception($"Stock not found for ID {model.StockId}");

                stock.Status = model.Status;
                try
                {


                    var productLinks = await _productRepository.Query()
                        .Where(x => x.Id == stock.ProductId)
                        .SelectMany(x => x.ProductLinks)
                        .Select(x => x.LinkedProductId)
                        .ToListAsync();

                    var stockVariations = await _stockRepository.Query()
                        .Where(x => productLinks.Contains(x.ProductId) && x.WarehouseId == model.warehouseId)
                        .ToListAsync();

                    foreach (var variation in stockVariations)
                    {
                        variation.Status = model.Status;
                    }
                }
                catch (Exception)
                {

                }

                await _stockRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
