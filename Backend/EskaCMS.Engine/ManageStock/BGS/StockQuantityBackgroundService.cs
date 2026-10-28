using EskaCMS.Core.Entities;
using EskaCMS.Engine;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.HOC.ManageStockQuentity.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.HOC.ManageStockQuantity.BGS
{
    public class StockQuantityBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockQuantityBackgroundService> _logger;
        private readonly TimeSpan _executionInterval = TimeSpan.FromMinutes(1);
        private IConfiguration _Configuration;

        public StockQuantityBackgroundService(IServiceProvider serviceProvider,
            ILogger<StockQuantityBackgroundService> logger, IConfiguration Configuration) : base(nameof(StockQuantityBackgroundService), logger, serviceProvider, Configuration)
        {

            _Configuration = Configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async Task<bool> Execute()
        {

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    await UpsertStockQuantity(scope);
                }

                await Task.Delay(_executionInterval);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating stock quantities.");
                await Task.Delay(_executionInterval);
                return false;

            }
        }
        private async Task UpsertStockQuantity(IServiceScope scope)
        {
            try
            {
                var orderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Order>>();
                var stockAmountRepository = scope.ServiceProvider.GetRequiredService<IRepository<StockAmount>>();
                var productRepository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
                var stockRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stock>>();

                // Retrieve all products with their associated stock entries
                var productsWithStock = await productRepository.Query()
                    .Include(p => p.Stock)
                    .AsNoTracking()
                    .Where(p => p.Status == EStatus.Published && p.StockTrackingIsEnabled)
                    .ToListAsync();

                var totalOrdered = await orderRepository.Query()
                .Include(c => c.OrderItems)
                .ThenInclude(x => x.Product)
                .Where(x => x.OrderItems.Any(i => i.Product.Status == EStatus.Published))
                .Where(x => x.OrderStatus == OrderStatus.PaymentReceived || x.OrderStatus == OrderStatus.CashOnDelivery
                    || x.OrderStatus == OrderStatus.PendingPayment)
                .SelectMany(x => x.OrderItems)
                .GroupBy(x => new { x.ProductId, x.Order.WarehouseId })
                .Select(x => new ProductStockViewModel
                {
                    ProductId = x.Key.ProductId,
                    Quantity = x.Sum(item => item.Quantity),
                    StockIsEnabled = x.FirstOrDefault().Product.StockTrackingIsEnabled,
                    WarehouseId = x.Key.WarehouseId
                })
                .AsNoTracking()
                .ToListAsync();


                // Retrieve all existing stock amounts untracked — only the rows we actually
                // touch get attached below, instead of the whole table riding in the change
                // tracker for every SaveChanges call (was the dominant CPU cost: EF's
                // DetectChanges rescanning the entire tracked table on every single save).
                var existingStockAmounts = await stockAmountRepository.Query().AsNoTracking().ToListAsync();

                foreach (var product in productsWithStock)
                {
                    foreach (var stock in product.Stock)
                    {

                        var ordersPerProduct = totalOrdered.Where(x => x.ProductId == product.Id && x.WarehouseId == stock.WarehouseId).Select(x => x.Quantity).FirstOrDefault();
                        // Calculate the available stock quantity
                        var availableQuantity = stock.Quantity - ordersPerProduct;

                        if (availableQuantity < 0)
                        {
                            availableQuantity = 0;
                        }

                        // Check if a StockAmount entry already exists for this product and warehouse
                        var existingStockAmount = existingStockAmounts
                            .FirstOrDefault(sa => sa.ProductId == product.Id && sa.WareHouseId == stock.WarehouseId);

                        if (existingStockAmount != null)
                        {
                            existingStockAmount.Quantity = availableQuantity;
                            existingStockAmount.LastUpdated = DateTimeOffset.Now;
                            stockAmountRepository.Update(existingStockAmount);
                        }
                        else
                        {
                            var newStockAmount = new StockAmount
                            {
                                ProductId = product.Id,
                                WareHouseId = stock.WarehouseId,
                                Quantity = availableQuantity,
                                LastUpdated = DateTimeOffset.Now
                            };
                            stockAmountRepository.Add(newStockAmount);
                        }
                    }
                }

                // One SaveChanges for the whole batch instead of one per stock row — DetectChanges
                // now runs once over only the rows actually added/updated, not once per row over
                // the entire table.
                await stockAmountRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
