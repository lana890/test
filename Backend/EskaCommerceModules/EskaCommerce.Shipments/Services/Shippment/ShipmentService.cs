using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Shipments.Areas.Shipments.ViewModels;
using EskaCMS.Core.Extensions;
using System;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Mvc;
using EskaCommerce.Shipments.Services.Shippment.Interfaces;

namespace EskaCommerce.Shipments.Services.Shippment
{
    public class ShipmentService : IShipmentService
    {
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentHistory> shipmentHistoryRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<ShipmentItem> _shipmentItemRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Pickup> pickupRepository;

        public ShipmentService(
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentHistory> shipmentHistoryRepository,
            IRepository<OrderItem> orderItemRepository,
            IWorkContext workContext,
            IRepository<ShipmentItem> shipmentItemRepository,
            IRepository<Stock> stockRepository,
            IRepository<Pickup> PickupRepository)
        {
            _shipmentRepository = shipmentRepository;
            this.shipmentHistoryRepository = shipmentHistoryRepository;
            _orderItemRepository = orderItemRepository;
            this.workContext = workContext;
            _shipmentItemRepository = shipmentItemRepository;
            _stockRepository = stockRepository;
            pickupRepository = PickupRepository;
        }

        public async Task<IList<ShipmentItemVm>> GetItemToShip(long orderId, long warehouseId)
        {
            try
            {


                var itemsToShip = await GetShipmentItem(orderId);
                var productIdsToShip = itemsToShip.Select(x => x.ProductId);
                var stocks = await _stockRepository.Query().AsNoTracking().Where(x => x.WarehouseId == warehouseId && productIdsToShip.Contains(x.ProductId)).ToListAsync();
                foreach (var item in itemsToShip)
                {
                    var stock = stocks.FirstOrDefault(x => x.ProductId == item.ProductId);
                    if (stock != null)
                    {
                        item.AvailableQuantity = stock.Quantity;
                    }
                }

                return itemsToShip;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public async Task<bool> ChangeShipmentStatus(long shipmentId, ShippmentStatus shippmentStatus)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var Shipments = await _shipmentRepository
                                            .Query()
                                            .Where(x => x.Id == shipmentId)
                                            .FirstOrDefaultAsync();


                var History = new ShipmentHistory
                {

                    OldStatus = Shipments.ShippmentStatus,
                    NewStatus = shippmentStatus,
                    ShipmentId = Shipments.Id,
                    CreatedById =  UserId,
                    CreatedOn = DateTime.Now,

                };

                shipmentHistoryRepository.Add(History);
                await shipmentHistoryRepository.SaveChangesAsync();

                Shipments.ShippmentStatus = shippmentStatus;



                await _shipmentRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
        public async Task<IList<ShipmentItemVm>> GetShipmentItem(long orderId)
        {
            var orderedItems = await _orderItemRepository.Query()
                .Where(x => x.Order.Id == orderId)
                .Select(x => new ShipmentItemVm
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product.ProductDetails.FirstOrDefault().Name,
                    ProductSku = x.Product.Sku,
                    OrderItemId = x.Id,
                    OrderedQuantity = x.Quantity
                })
                .ToListAsync();

            var shippedItems = await _shipmentItemRepository.Query().AsNoTracking()
                .Where(x => x.Shipment.OrderId == orderId)
                .ToListAsync();

            if (!shippedItems.Any())
            {
                return orderedItems;
            }

            foreach (var item in orderedItems)
            {
                var shippedItemsByProduct = shippedItems.Where(x => x.ProductId == item.ProductId);
                foreach (var shippedItemByProduct in shippedItemsByProduct)
                {
                    item.ShippedQuantity = item.ShippedQuantity + shippedItemByProduct.Quantity;
                }
            }

            return orderedItems;
        }

        public async Task<Result> CreateShipment(Shipment shipment)
        {
            if (!shipment.Items.Any())
            {
                return Result.Fail($"No item to ship");
            }

            var orderedItems = await GetShipmentItem(shipment.OrderId);
            foreach (var item in shipment.Items)
            {
                var orderedItem = orderedItems.FirstOrDefault(x => x.OrderItemId == item.OrderItemId);
                if (orderedItem == null)
                {
                    return Result.Fail($"Order item {item.OrderItemId} is not found");
                }

                if (item.Quantity > orderedItem.OrderedQuantity - orderedItem.ShippedQuantity)
                {
                    return Result.Fail($"Quantity to ship cannot be larger than ordered quantity + shipped quantity");
                }

                var stock = await _stockRepository.Query().Where(x => x.ProductId == item.ProductId && x.WarehouseId == shipment.WarehouseId).FirstOrDefaultAsync();
                if (stock == null || stock.Quantity < item.Quantity)
                {
                    return Result.Fail($"The product {item.ProductId} is out of stock in warehouse {shipment.WarehouseId}");
                }

                stock.Quantity = stock.Quantity - item.Quantity;
            }

            _shipmentRepository.Add(shipment);
            await _shipmentRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<SmartTableResult<PickupsAdminVewModel>> GetPickups(SmartTableParam param)
        {
            try
            {
                var Result =  pickupRepository.Query()
                                                  .Include(x => x.CreatedBy)
                                                  .Include(x => x.Shipments)
                                                  .Include("Shipments.Items")
                                                  .Include("Shipments.Items.Product")
                                                  .Include("Shipments.Items.Product.ProductDetails")
                                                  .Include("Shipments.Warehouse")
                                                  .Include("Shipments.Warehouse.Address")
                                                  .AsNoTrackingWithIdentityResolution()
                                                  .Where(x => x.Shipments.Any(x => x.ShippingDelivery == DeliveryOptionEnum.Aramex));


                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;

                    if (search.TrackingNumber != null)
                    {
                        string trackingNumber = search.TrackingNumber;
                        Result = Result.Where(x => x.Shipments.Any(i => i.TrackingNumber.Contains(trackingNumber)));
                    }

                    if (search.Guid != null)
                    {
                        string Guid = search.Guid;
                        Result = Result.Where(x => x.GUID== Guid);
                    }

                    if (search.PickupId != null)
                    {
                        string PickupId = search.PickupId;
                        Result = Result.Where(x => x.PickupId == PickupId);
                    }

                    if (search.OrderId != null)

                    {
                        string orderIdString = search.OrderId;
                        if (long.TryParse(orderIdString, out long orderId))
                        {
                            Result = Result.Where(x => x.Shipments.Any(i => i.OrderId == orderId));
                        }
                    }


                    if (search.CreatedOn != null)
                    {
                        if (search.CreatedOn.before != null)
                        {
                            DateTimeOffset before = search.CreatedOn.before;
                            Result = Result.Where(x => x.CreatedOn <= before);
                        }

                        if (search.CreatedOn.after != null)
                        {
                            DateTimeOffset after = search.CreatedOn.after;
                            Result = Result.Where(x => x.CreatedOn >= after);
                        }
                    }
                }

                var shipments = Result.ToSmartTableResult(
                                 param,
                                 x => 
                                     new PickupsAdminVewModel
                                     {
                                         GUID = x.GUID,
                                         UserName = x.CreatedBy.UserName,
                                         Id = x.Id,
                                         Reference1 = x.Reference1,
                                         Reference2 = x.Reference2,
                                         PickupId = x.PickupId,
                                         CreatedOn = x.CreatedOn,
                                         Shipments = x.Shipments.Select(x => new ShipmentAdminViewModel
                                         {   
                                             City = x.Warehouse == null ? null : x.Warehouse.Address.City,
                                             TrackingNumber = x.TrackingNumber,
                                             OrderId = x.OrderId,
                                             Id = x.Id,
                                             ShipmentStatus = x.ShippmentStatus,
                                             Items = x.Items.Select(y => new ShipmentItemViewModel
                                             {
                                                 ProductName = y.Product.ProductDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                                 Quentity = y.Quantity

                                             }).ToList()


                                         }).ToList()
                                     });
                                              
                return shipments;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<ShipmentHistoryViewModel>> GetShipmentHistory(long id)
        {
            try
            {
                var Query = await shipmentHistoryRepository.Query()
                                                           .AsNoTrackingWithIdentityResolution()
                                                           .Include(x=>x.CreatedBy)
                                                           .Where(x => x.ShipmentId == id)
                                                           .Select(x => new ShipmentHistoryViewModel
                                                            {

                                                                CreatedOn = x.CreatedOn,
                                                                OldStatus = x.OldStatus == null ? null : x.OldStatus.ToString(),
                                                                NewStatus = x.NewStatus.ToString(),
                                                                ShipmentId = x.ShipmentId,
                                                                UserName = x.CreatedBy.UserName



                                                            }).ToListAsync();

                return Query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<DeliveryStatusViewModel> GetShipmentById(long id)
        {
            try
            {
                var Result =await _shipmentRepository.Query()
                                                  .Include(x => x.CreatedBy)
                                                   .Include(x => x.Items)
                                                  .ThenInclude(x => x.Product)
                                                  .ThenInclude(x => x.ProductDetails)
                                                  .Include(x => x.Warehouse)
                                                  .ThenInclude(x => x.Address)
                                                  .AsNoTrackingWithIdentityResolution()
                                                  .Where(x => x.Id == id).Select(x => new DeliveryStatusViewModel
                                                  {
                                                      TrackingNumber = x.TrackingNumber,
                                                      OrderId = x.OrderId,
                                                      ShipmentId = x.Id,
                                                      Warehouse = x.Warehouse == null ? null : x.Warehouse.Name,
                                                      Cancelled = x.ShippmentStatus == ShippmentStatus.Cancelled ? 1 : 0,
                                                      Pending = x.ShippmentStatus == ShippmentStatus.Preparing ? 1 : 0,
                                                      Ready = x.ShippmentStatus == ShippmentStatus.Ready ? 1 : 0,
                                                      Delivered = x.ShippmentStatus==ShippmentStatus.delivered ?1:0,
                                                  }).FirstOrDefaultAsync();
                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
