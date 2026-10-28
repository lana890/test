using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.General.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.DomesticShipment.TopDelivery.Dtos;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EskaCommerce.DomesticShipment.SimsCreation.Dtos;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.ShippingTableRate;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.Addresses;

namespace EskaCommerce.DomesticShipment.TopDelivery.Services
{
    public class TopDeliveryServices : ITopDeliveryServices
    {
        private readonly ISiteSettingsService siteSettingsService;
        private readonly HttpClient httpClient;
        private readonly ILookupServices lookupServices;
        private readonly IWorkContext workContext;
        private readonly IRepository<ShipmentHistory> shipmentHistoryRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Orders.Order> orderRepository;
        private readonly IRepository<Shipment> shipmentRepository;
        private readonly IRepository<ShipmentItem> shipmentItemRepository;
        private readonly IRepositoryWithTypedId<ShippingProvider, string> shipemntProviderRepository;

        public TopDeliveryServices(ISiteSettingsService  siteSettingsService, HttpClient httpClient,
                                    ILookupServices lookupServices,IWorkContext workContext, IRepository<ShipmentHistory> ShipmentHistoryRepository,
                                    IRepository<EskaCMS.Entities.Entity.Orders.Order> orderRepository,
                                    IRepository<Shipment> shipmentRepository, IRepository<ShipmentItem> ShipmentItemRepository,

                                    IRepositoryWithTypedId<ShippingProvider,string> ShipemntProviderRepository)
                                            {
                                                this.siteSettingsService = siteSettingsService;
                                                this.httpClient = httpClient;
                                                this.lookupServices = lookupServices;
                                                this.workContext = workContext;
                                                shipmentHistoryRepository = ShipmentHistoryRepository;
                                                this.orderRepository = orderRepository;
                                                this.shipmentRepository = shipmentRepository;
                                                shipmentItemRepository = ShipmentItemRepository;
                                                shipemntProviderRepository = ShipemntProviderRepository;
                                            }

        public async Task<bool> CreateOrder(EskaCMS.Entities.Entity.Orders.Order order, long? ShipmentId = null)
        {
            try
            {
                var shipmentProviderJson = await shipemntProviderRepository.Query()
                    .Where(x => x.Id == "TopDelivery")
                    .Select(x => x.AdditionalSettings)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(shipmentProviderJson))
                    throw new Exception("TopDelivery settings not found.");

                var topDeliverySettings = JsonConvert.DeserializeObject<SettingsTopDeliveryViewModel>(shipmentProviderJson);

                var branchId = await FetchWarehouseLookup(order.WarehouseId.ToString());

                var customerPhone = order.ShippingAddress?.Phone;
                var customerName = order.ShippingAddress?.ContactName;
                var latitude = order.ShippingAddress?.Latitude;
                var longitude = order.ShippingAddress?.Longitude;

                if (string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(customerName))
                    throw new Exception("Missing required customer information.");

                string requestUrl;

                if (!string.IsNullOrWhiteSpace(latitude) && !string.IsNullOrWhiteSpace(longitude))
                {
                    string coordinates = $"{latitude},{longitude}";
                    requestUrl = $"{topDeliverySettings.BaseUrl}" +
                                 $"username={topDeliverySettings.Username}" +
                                 $"&password={topDeliverySettings.Password}" +
                                 $"&branchId={branchId}" +
                                 $"&customerPhone={customerPhone}" +
                                 $"&customerCoordinates={coordinates}" +
                                 $"&customerName={Uri.EscapeDataString(customerName)}";
                }
                else
                {
                    var deliveryAddress = order.ShippingAddress?.District?.DistrictDetails?.FirstOrDefault()?.Name
                                          ?? order.ShippingAddress?.StateOrProvince?.StateOrProvinceDetails?.FirstOrDefault()?.Name;

                    if (string.IsNullOrWhiteSpace(deliveryAddress))
                        throw new Exception("Delivery address is missing.");

                    requestUrl = $"{topDeliverySettings.BaseUrl}?" +
                                 $"username={topDeliverySettings.Username}" +
                                 $"&password={topDeliverySettings.Password}" +
                                 $"&branchId={branchId}" +
                                 $"&customerAddress={Uri.EscapeDataString(deliveryAddress)}" +
                                 $"&customerPhone={customerPhone}";
                }

                var responseJson = await SendHttpRequestAsync(requestUrl, HttpMethod.Get);

                var orderResponse = JsonConvert.DeserializeObject<TopDeliveryOrderResponseViewModel>(responseJson);

                await UpsertShipment(orderResponse, order.Id, ShippmentStatus.Preparing, ShipmentId);

                return true;
            }
            catch (Exception ex)
            {
                await UpsertShipment(new TopDeliveryOrderResponseViewModel(), order.Id, ShippmentStatus.Failed, ShipmentId);
                throw new Exception("Failed to create TopDelivery order", ex);
            }
        }
        public async Task CancelOrder(string trackingNumber,long OrderId,long shipmentId)
        {
            try
            {
                var shipmentProviderJson = await shipemntProviderRepository.Query()
               .Where(x => x.Id == "TopDelivery")
               .Select(x => x.AdditionalSettings)
               .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(shipmentProviderJson))
                    throw new Exception("TopDelivery settings not found.");

                var topDeliverySettings = JsonConvert.DeserializeObject<SettingsTopDeliveryViewModel>(shipmentProviderJson);
                if (topDeliverySettings == null)
                    throw new Exception("TopDelivery settings not configured.");

                // Build request URL
                var requestUrl = $"{topDeliverySettings.BaseUrl}/apiCancelOrder" +
                                 $"?username={topDeliverySettings.Username}" +
                                 $"&password={topDeliverySettings.Password}" +
                                 $"&orderId={trackingNumber}";

                // Send GET request
                var response = await SendHttpRequestAsync(requestUrl, HttpMethod.Get);
                await ChangeShipmentStatus(new List<long> { OrderId }, ShippmentStatus.Cancelled);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error while canceling order: {ex.Message}");
                // optionally log exception
            }
        }

        public async Task<(string deliveryPrice, string deliveryTime)> EstimateDeliveryPriceAsync(Cart cart,Address address)
        {
            try
            {
                var warehouseId = workContext.GetCurrentWarehouseId().Result;
                var shipmentProvider = await shipemntProviderRepository.Query()
                    .Where(x => x.Id == "TopDelivery")
                    .Select(x => x.AdditionalSettings)
                    .FirstOrDefaultAsync();

                var topDeliverySettings = JsonConvert.DeserializeObject<SettingsTopDeliveryViewModel>(shipmentProvider);

                var warehouseLookupValue = await FetchWarehouseLookup(warehouseId.ToString());

                var customerCoordinates = $"{address.Latitude},{address.Longitude}";

                var requestUrl = $"{topDeliverySettings.BaseUrlEstimateDeliveryPrice}" +
                                 $"username={topDeliverySettings.Username}" +
                                 $"&password={topDeliverySettings.Password}" +
                                 $"&branchId={warehouseLookupValue}" +
                                 $"&customerCoordinates={customerCoordinates}";

                var response = await SendHttpRequestAsync(requestUrl, HttpMethod.Get);

                var result = JsonConvert.DeserializeObject<EstimateDeliveryPriceResponse>(response);

                return (result.DeliveryPrice, result.DeliveryTime);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to estimate delivery price "+"_" + ex.Message);
            }
        }

        public async Task<DeliveryStatusViewModel> GetOrderHistory(long Id)
        {
            try
            {
                var ShipmentProvider = await shipemntProviderRepository.Query().Where(x => x.Id == "TopDelivery").Select(x => x.AdditionalSettings).FirstOrDefaultAsync();
                var TopDeliverySettings = JsonConvert.DeserializeObject<SettingsTopDeliveryViewModel>(ShipmentProvider);
                var shipObj = await shipmentRepository.Query().AsNoTracking().Where(x => x.Id == Id).FirstOrDefaultAsync();
                if (shipObj == null)
                {
                    throw new Exception("Shipment not Exist");
                }

                var BaseUrl = TopDeliverySettings.BaseUrlStatus + $"username={TopDeliverySettings.Username}&password={TopDeliverySettings.Password}&orderId={shipObj.TrackingNumber}";


                var OrderResponse = JsonConvert.DeserializeObject<DeliveryStatusViewModel>
                                                          (await SendHttpRequestAsync(BaseUrl, HttpMethod.Get));

                var OrderObj =await orderRepository.Query().AsNoTracking().Where(x => x.Id == shipObj.OrderId).Include(x => x.Warehouse)
                            .ThenInclude(x => x.priceAndDestinations).Include(x => x.BillingAddress)
                            .Include(x => x.ShippingAddress).FirstOrDefaultAsync();

                OrderResponse.ShipmentId = shipObj.Id;
                OrderResponse.TrackingNumber = shipObj.TrackingNumber;
                OrderResponse.OrderId = shipObj.OrderId;
                OrderResponse.EstimatedTime = GetEstimatedDeliveryDate(OrderObj.Warehouse, OrderObj.ShippingAddress);



                return OrderResponse;
            }
            catch (Exception ex)
            {

                throw;
            }
        
        
        }

        private  async Task<string> FetchWarehouseLookup(string WarehouseId)
        {
            try
            {
                var siteId= workContext.GetCurrentSiteId();
                var lookupId = await siteSettingsService.GetSiteSettingById("LookupTopDelivery", siteId);
                var lookupValues =await lookupServices.GetLookupValuesbylookupId(Convert.ToInt64(lookupId.Value));

                return lookupValues.LookupValuesList.Where(x=>x.Name== WarehouseId).Select(x=>x.Value).FirstOrDefault();

            }
            catch
            {

                throw;
            }
        }

        private async Task<string> SendHttpRequestAsync(string baseUrl, HttpMethod method, object? model = null)
        {
            try
            {


                HttpRequestMessage request;

                if (method == HttpMethod.Post)
                {
                    var jsonRequest = JsonConvert.SerializeObject(model);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(baseUrl),
                        Content = content
                    };
                }
                else if (method == HttpMethod.Get)
                {
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(baseUrl)
                    };
                }
                else
                {
                    throw new ArgumentException("Unsupported HTTP method");
                }
                var response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                    var xx= await response.Content.ReadAsStringAsync();
                    return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        throw new Exception($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                    }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task UpsertShipment(TopDeliveryOrderResponseViewModel response, long orderId,ShippmentStatus shipmentStatus ,long? ShipmentId=null)
        {
            try
            {


                var Shipment = ShipmentId == null ? new EskaCMS.Entities.Entity.Shipments.Shipment()
                                                           : await shipmentRepository.Query().Where(x => x.Id == ShipmentId).FirstOrDefaultAsync();
                var Order = await orderRepository.Query().Where(x => x.Id == orderId).FirstOrDefaultAsync();

                Shipment.LabelUrl =response.OrderNumId.ToString();

                Shipment.TrackingNumber = response.orderId;

                Shipment.OrderId = orderId;

                Shipment.ShippmentStatus = shipmentStatus;

                Shipment.ShippingDelivery = DeliveryOptionEnum.TopDelivery;

                Shipment.CreatedById = Order.CustomerId;
                Shipment.CreatedOn = DateTime.UtcNow;

                Shipment.WarehouseId = Order.WarehouseId;

                Shipment.LatestUpdatedOn = DateTime.UtcNow;

                if (ShipmentId == null)
                {
                    shipmentRepository.Add(Shipment);
                    await shipmentRepository.SaveChangesAsync();
                    await SaveIntoShipmentsItems(orderId, Shipment.Id);
                }

                try
                {
                    await ChangeShipmentStatus(new List<long> { orderId }, shipmentStatus);

                }
                catch (Exception ex)
                {

                    throw ex.InnerException;
                }
                await shipmentRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task SaveIntoShipmentsItems(long orderId, long ShipmentId)
        {
            try
            {
                var Order = await orderRepository.Query().Include(x => x.OrderItems).Where(x => x.Id == orderId).FirstOrDefaultAsync();

                var ShipmentsItems = Order.OrderItems.ToList().ConvertAll(x => new ShipmentItem
                {
                    OrderItemId = x.Id,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    ShipmentId = ShipmentId,

                });

                shipmentItemRepository.AddRange(ShipmentsItems);

                await shipmentItemRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> ChangeShipmentStatus(List<long> orderIds, ShippmentStatus shippmentStatus)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var Shipments = await shipmentRepository
                                            .Query()
                                            .Include(x => x.Order)
                                            .Where(x => orderIds.Any(u => u == x.OrderId))
                                            .ToListAsync();


                foreach (var ship in Shipments)
                {
                    var History = new ShipmentHistory
                    {

                        OldStatus = ship.ShippmentStatus,
                        NewStatus = shippmentStatus,
                        ShipmentId = ship.Id,
                        CreatedById = ship.Order.CustomerId,
                        CreatedOn = DateTime.Now,

                    };

                    shipmentHistoryRepository.Add(History);
                    await shipmentHistoryRepository.SaveChangesAsync();

                    ship.ShippmentStatus = shippmentStatus;
                }
                await shipmentRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            };
        }
        public  int? GetEstimatedDeliveryDate(Warehouse? warehouse, OrderAddress? address)
        {
            try
            {
                if (warehouse != null)
                {
                    if (warehouse.priceAndDestinations != null && warehouse.priceAndDestinations.Count() > 0)
                    {
                        if (address == null)
                        {
                            return null;

                        }
                        return GetEstimatedTime(warehouse.priceAndDestinations.ToList(), address);
                    }
                }

                return null;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private int? GetEstimatedTime(List<PriceAndDestination> priceAndDestinations, OrderAddress shippingAddress)
        {
            try
            {
                var results = priceAndDestinations
                    .Where(rate => rate.CountryId == shippingAddress.CountryId && rate.Status != EStatus.Deleted)
                    .ToList();

                if (results.Count == 0)
                {
                    return null;
                }

                var state = results.Where(rate => rate.StateOrProvinceId == shippingAddress.StateOrProvinceId).ToList();
                if (state != null && state.Count() > 0)
                {
                    var district = state.Where(rate => rate.DistrictId == shippingAddress.DistrictId).Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
                    return district ?? state.Where(x => !x.DistrictId.HasValue).Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
                }

                return results.Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> RepushShipmentAsync(long shipmentId)
        {
            try
            {
                var Shipment = await shipmentRepository
                                                  .Query()
                                                  .Include(x => x.Order)
                                                  .AsNoTrackingWithIdentityResolution()
                                                  .Where(x => x.Id == shipmentId && x.ShippingDelivery == DeliveryOptionEnum.TopDelivery)
                                                  .FirstOrDefaultAsync();
                if (Shipment == null)
                {
                    throw new Exception("Shipment Not Exist");

                }

                if ((Shipment.Order.OrderStatus == OrderStatus.PaidByCoupon || Shipment.Order.OrderStatus == OrderStatus.PaymentReceived || Shipment.Order.OrderStatus == OrderStatus.CashOnDelivery) && Shipment.ShippmentStatus == ShippmentStatus.Failed)
                {
                    var order = orderRepository
                              .Query()
                              .Include(x => x.Customer)
                              .Include(x => x.BillingAddress)
                              .Include(d => d.ProductSerials)
                              .Include(x => x.ShippingAddress)
                              .Include(x => x.OrderItems)
                              .ThenInclude(x => x.Product)
                              .Include(x => x.Cart)
                              .Where(x => x.Id == Shipment.OrderId)
                              .FirstOrDefault();
                    await CreateOrder(order);
                }
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        
    }
}
