using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Aramex.Services;
using EskaCommerce.Shipments.Areas.Shipments.ViewModels;
using EskaCommerce.Shipments.Models;
using EskaCommerce.Shipments.Services;
using EskaCommerce.Shipments.Services.Shippment.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EskaCommerce.Shipments.Areas.Shipments.Controllers
{
    [Area("Shipments")]
    [Authorize]
    [Route("api/shipments")]
    public class ShipmentApiController : Controller
    {
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IShipmentService _shipmentService;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        // private readonly IConfiguration _config;
        public ShipmentApiController(IRepository<Shipment> shipmentRepository,
            
            IShipmentService shipmentService,
            IUserSiteService userSiteService,
            IRepository<Order> orderRepository,
            IWorkContext workContext/* ,
            IConfiguration config */)
        {
            _shipmentRepository = shipmentRepository;
            _shipmentService = shipmentService;
            this.userSiteService = userSiteService;
            _orderRepository = orderRepository;
            _workContext = workContext;
            // _config = config;
        }

        [HttpGet("/api/orders/{orderId}/items-to-ship")]
        public async Task<IActionResult> GetItemsToShip(long orderId, long warehouseId)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            //var currentUser = await _workContext.GetCurrentUser(SiteId);
            var order = _orderRepository.Query().FirstOrDefault(x => x.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            //if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            //{
            //    return BadRequest(new { error = "You don't have permission to manage this order" });
            //}

            var model = new ShipmentForm
            {
                OrderId = orderId,
                WarehouseId = warehouseId
            };

            var itemsToShip = await _shipmentService.GetItemToShip(orderId, warehouseId);
            foreach (var item in itemsToShip)
            {
                item.QuantityToShip = item.OrderedQuantity - item.ShippedQuantity;
            }

            model.Items = itemsToShip;
            return Ok(model);
        }

        [HttpGet("/api/orders/{orderId}/shipments")]
        public async Task<IActionResult> GetByOrder(long orderId)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var order = _orderRepository.Query().FirstOrDefault(x => x.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            var shipments = await _shipmentRepository.Query()
                .Where(x => x.OrderId == orderId)
                .Select(x => new
                {
                    x.Id,
                    x.TrackingNumber,
                    x.OrderId,
                    WarehouseName = x.Warehouse.Name,
                    shippmentStatus=x.ShippmentStatus,
                    shippingDelivery=x.ShippingDelivery,
                    x.CreatedOn
                }).ToListAsync();

            return Ok(shipments);
        }

        [HttpPost("GetPickups")]
        public async Task<IActionResult> GetPickups([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await _shipmentService.GetPickups(param));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }
        [HttpPut("ChangeShipmentStatus/{Id}/{Status}")]
        public async Task<IActionResult> ChangeShipmentStatus([FromRoute] long Id, [FromRoute] ShippmentStatus Status)
        {
            try
            {
                return Ok(await _shipmentService.ChangeShipmentStatus(Id, Status));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpGet("TrackShipment/{Id}")]
        public async Task<IActionResult> TrackShipment([FromRoute] long Id)
        {
            try
            {
               
                return Ok(await _shipmentService.GetShipmentById(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpPut("GetShipmentHistory/{Id}")]
        public async Task<IActionResult> GetShipmentHistory([FromRoute] long Id)
        {
            try
            {
                return Ok(await _shipmentService.GetShipmentHistory(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var shipment = await _shipmentRepository.Query()
                .Select(x => new
                {
                    x.Id,
                    x.OrderId,
                    x.VendorId,
                    WarehouseName = x.Warehouse==null? null: x.Warehouse.Name,
                    ShippmentStatus=x.ShippmentStatus.ToString(),
                    ShippingDelivery= x.ShippingDelivery,
                    x.TrackingNumber,
                    x.CreatedOn,
                    ShippingAddress = new
                    {
                        x.Order.ShippingAddress.AddressLine1,
                        x.Order.ShippingAddress.AddressLine2,
                        x.Order.ShippingAddress.ContactName,
                        DistrictName = x.Order.ShippingAddress.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                        StateOrProvinceName = x.Order.ShippingAddress.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                        x.Order.ShippingAddress.Phone,

                    },
                    Items = x.Items.Select(i => new
                    {
                        i.Id,
                        ProductId = i.Product.Id,
                        ProductName = i.Product.ProductDetails.FirstOrDefault().Name,
                        ProductSku = i.Product.Sku,
                        i.Quantity
                    })
                })
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && shipment.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            return Ok(shipment);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ShipmentForm model)
        {
            if (ModelState.IsValid)
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUser = await _workContext.GetCurrentUser();
                var shipment = new Shipment
                {
                    OrderId = model.OrderId,
                    WarehouseId = model.WarehouseId,
                    CreatedById = currentUser.Id,
                    TrackingNumber = model.TrackingNumber,
                    CreatedOn = DateTimeOffset.Now,
                    LatestUpdatedOn = DateTimeOffset.Now
                };

                if (!User.IsInRole("admin"))
                {
                    shipment.VendorId = currentUser.VendorId;
                }

                foreach (var item in model.Items)
                {
                    if (item.QuantityToShip <= 0)
                    {
                        continue;
                    }

                    var shipmentItem = new ShipmentItem
                    {
                        Shipment = shipment,
                        Quantity = item.QuantityToShip,
                        ProductId = item.ProductId,
                        OrderItemId = item.OrderItemId
                    };

                    shipment.Items.Add(shipmentItem);
                }

                var result = await _shipmentService.CreateShipment(shipment);
                if (result.Success)
                {
                    return CreatedAtAction(nameof(Get), new { id = shipment.Id }, null);
                }

                return BadRequest(result.Error);
            }

            return BadRequest(ModelState);
        }
        [HttpPost("grid")]
        public async Task<IActionResult> List([FromBody] SmartTableParam param)
        {
            var query = _shipmentRepository.Query();
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }
            var userRoles = await userSiteService.GetUserRoles(SiteId, currentUser.Id);


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.TrackingNumber != null)
                {
                    string trackingNumber = search.TrackingNumber;
                    query = query.Where(x => x.TrackingNumber.Contains(trackingNumber));
                }

                if (search.WarehouseName != null)
                {
                    string WarehouseName = search.WarehouseName;
                    query = query.Where(x => x.Warehouse.Name.Contains(WarehouseName));
                }

                if (search.OrderId != null)
                {
                    string orderIdString = search.OrderId;
                    if (long.TryParse(orderIdString, out long orderId))
                    {
                        query = query.Where(x => x.OrderId == orderId);
                    }
                }


                if (search.ShipmentStatus != null)
                {

                    List<ShippmentStatus> statusList = new List<ShippmentStatus>();

                    foreach (dynamic status in search.ShipmentStatus)
                    {
                        ShippmentStatus parsedStatus = (ShippmentStatus)(status);
                        statusList.Add(parsedStatus);
                    }


                    query = query.Where(x => statusList.Contains(x.ShippmentStatus));
                }


                if (search.DeliveryStatus != null)
                {

                    List<DeliveryOptionEnum> statusList = new List<DeliveryOptionEnum>();

                    foreach (dynamic status in search.DeliveryStatus)
                    {
                        DeliveryOptionEnum parsedStatus = (DeliveryOptionEnum)(status);
                        statusList.Add(parsedStatus);
                    }


                    query = query.Where(x => statusList.Contains(x.ShippingDelivery));
                }
                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var shipments = query.Where(x => x.Warehouse.WarehouseRoles.Any() ? x.Warehouse.WarehouseRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                .ToSmartTableResult(
                param,
                x => new
                {
                    x.Id,
                    x.TrackingNumber,
                    x.OrderId,
                    WarehouseName = x.Warehouse == null ? null : x.Warehouse.Name,
                    x.CreatedOn,
                    x.ShippmentStatus,
                    x.ShippingDelivery
                });

            return Json(shipments);
        }
        [HttpPost("GetShipmentsByUser")]
        public async Task<IActionResult> GetShipmentsByUser([FromBody] SmartTableParam param)
        {
            try
            {

                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUser = await _workContext.GetCurrentUser();
                var query = _shipmentRepository.Query().Where(x=>x.CreatedById== currentUser.Id);
                int? ShipmentStatus = 1;
               
                if (param?.Search?.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;

                    if (search.TrackingNumber != null)
                    {
                        string trackingNumber = search.TrackingNumber;
                        query = query.Where(x => x.TrackingNumber.Contains(trackingNumber));
                    }


                    if (search.OrderId != null)
                    {
                        string orderIdString = search.OrderId;
                        if (long.TryParse(orderIdString, out long orderId))
                        {
                            query = query.Where(x => x.OrderId == orderId);
                        }
                    }
                    if (search.ShipmentStatus != null)
                    {

                        List<ShippmentStatus> statusList = new List<ShippmentStatus>();
                       
                            foreach (dynamic status in search.ShipmentStatus)
                            {
                                ShippmentStatus parsedStatus = (ShippmentStatus)(status);
                                statusList.Add(parsedStatus);
                            }


                        query = query.Where(x => statusList.Contains(x.ShippmentStatus));
                    }


                    if (search.DeliveryStatus != null)
                    {

                        List<DeliveryOptionEnum> statusList = new List<DeliveryOptionEnum>();

                        foreach (dynamic status in search.DeliveryStatus)
                        {
                            DeliveryOptionEnum parsedStatus = (DeliveryOptionEnum)(status);
                            statusList.Add(parsedStatus);
                        }


                        query = query.Where(x => statusList.Contains(x.ShippingDelivery));
                    }
                    if (search.CreatedOn != null)
                    {
                        if (search.CreatedOn.before != null)
                        {
                            DateTimeOffset before = search.CreatedOn.before;
                            query = query.Where(x => x.CreatedOn <= before);
                        }

                        if (search.CreatedOn.after != null)
                        {
                            DateTimeOffset after = search.CreatedOn.after;
                            query = query.Where(x => x.CreatedOn >= after);
                        }
                    }
                }
                if (ShipmentStatus!= null)
                {
                    List<ShippmentStatus> statusList = new List<ShippmentStatus>();
                        ShippmentStatus parsedStatus = (ShippmentStatus)(ShipmentStatus);
                        statusList.Add(parsedStatus);
                    
                    query = query.Where(x => statusList.Contains(x.ShippmentStatus));

                }
                var shipments = query.ToSmartTableResult(
                    param,
                    x => new
                    {
                        x.Id,
                        x.TrackingNumber,
						EncryptedOrderId = EncryptDecryptHelper.Encrypt(x.OrderId.ToString()),
						OrderId=x.OrderId.ToString(),
						WarehouseName = x.Warehouse == null ? null : x.Warehouse.Name,
                        x.CreatedOn,
                        x.ShippmentStatus,
                        x.ShippingDelivery
                    });

                return Ok(shipments);


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



    }
}
