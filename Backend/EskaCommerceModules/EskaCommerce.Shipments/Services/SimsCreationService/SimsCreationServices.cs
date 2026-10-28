using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.DomesticShipment.SimsCreation.Dtos;
using EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.SimsCreation.Services
{
    public class SimsCreationServices : ISimsCreationServices
    {
        private readonly IWorkContext workContext;
        private readonly HttpClient httpClient;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingrepository;
        private readonly IRepository<Order> orderRepository;
        private readonly IRepository<Shipment> shipmentRepository;
        private readonly IRepository<ShipmentHistory> shipmenthistoryrepository;

        public SimsCreationServices(IWorkContext workContext, 
            HttpClient httpClient,
            IRepositoryWithTypedId<SiteSettings,string> SiteSettingrepository,
            IRepository<Order> orderRepository,
            IRepository<Shipment> ShipmentRepository,
            IRepository<ShipmentHistory>  Shipmenthistoryrepository
            
            )
        {
            this.workContext = workContext;
            this.httpClient = httpClient;
            siteSettingrepository = SiteSettingrepository;
            this.orderRepository = orderRepository;
            shipmentRepository = ShipmentRepository;
            shipmenthistoryrepository = Shipmenthistoryrepository;
        }

        public async Task<OrderResponseViewModel> CreateOrder(long orderId)
        {
            try
            {
                var Configuration = await GetSimsCredentials(workContext.GetCurrentSiteId());

                var request = new Simsloginviewmodel
                {
                    email = Configuration.email,
                    password = Configuration.password

                };

                var responselogin =JsonConvert.DeserializeObject<SimsLoginResponseViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl,"service-providers/login",HttpMethod.Post, request));

                var Order = await orderRepository.Query().Where(x => x.Id == orderId)
                    .Include(x => x.ShippingAddress)
                    .Include("ShippingAddress.StateOrProvince")
                    .Include("ShippingAddress.Country")
                    .FirstOrDefaultAsync();

                var SimsOrderCreation = new SimsCreationDto
                {
                    dates = DateTime.Now.ToString(),
                    days = null,
                    delivery_fees = Order.ShippingFeeAmount,
                    amount = Order.OrderTotal,
                    service_providersId = 1,
                    status = "Urgent",
                 //   city = Order.ShippingAddress.StateOrProvince.Name,
                    client_name = Order.ShippingAddress.ContactName,
                    client_number = Order.ShippingAddress.Phone,
                    payment = Order.PaymentMethod.ToString(),
                    street = Order.ShippingAddress.AddressLine1,
                    building_no = Order.ShippingAddress.AddressLine2,
                    client_address = Order.ShippingAddress.AddressLine2,
                    area = Order.ShippingAddress.AddressLine2,
                    location_id= responselogin.user.location_id,
                    est_time=null
                };
                
                var OrderResponse= JsonConvert.DeserializeObject<OrderResponseViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, "order-out",HttpMethod.Post, SimsOrderCreation, responselogin.access_token));

                if (OrderResponse != null)
                {

                    await CreateShipment(OrderResponse.Data.Id.ToString(), Order.Id);
                }        
                return OrderResponse;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<OrderUserViewModel> GetOrderById(string U_Id)                                                  
        {
            try
            {
                var Configuration = await GetSimsCredentials(workContext.GetCurrentSiteId());

                var request = new Simsloginviewmodel
                {
                    email = Configuration.email,
                    password = Configuration.password

                };

                var responselogin = JsonConvert.DeserializeObject<SimsLoginResponseViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, "service-providers/login", HttpMethod.Post,request));

                var OrderResponse = JsonConvert.DeserializeObject<OrderUserViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, $"order-out/{U_Id}", HttpMethod.Post,null, responselogin.access_token));
                return OrderResponse;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<ServiceProviderViewModel>> GetAllServiceProvider()
        {
            try
            {
                var Configuration = await GetSimsCredentials(workContext.GetCurrentSiteId());

                var request = new Simsloginviewmodel
                {
                    email = Configuration.email,
                    password = Configuration.password

                };
                var responselogin = JsonConvert.DeserializeObject<SimsLoginResponseViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, "service-providers/login", HttpMethod.Post, request));
                var servicesProviders = JsonConvert.DeserializeObject<List<ServiceProviderViewModel>>(await SendHttpRequestAsync(Configuration.BaseUrl, "service-providers", HttpMethod.Get, null, responselogin.access_token));

                return servicesProviders;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<DeleteOrderViewModel> DeleteOrder(string U_Id)
        {
            try
            {
                var Configuration = await GetSimsCredentials(workContext.GetCurrentSiteId());

                var request = new Simsloginviewmodel
                {
                    email = Configuration.email,
                    password = Configuration.password

                };

                var responselogin = JsonConvert.DeserializeObject<SimsLoginResponseViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, "service-providers/login", HttpMethod.Post, request));

                var OrderResponse = JsonConvert.DeserializeObject<DeleteOrderViewModel>(await SendHttpRequestAsync(Configuration.BaseUrl, $"order-out/{U_Id}", HttpMethod.Delete, null, responselogin.access_token));

                if (OrderResponse != null)
                {
                    var shipment =await shipmentRepository.Query()
                                                          .Where(x => x.TrackingNumber == U_Id )
                                                          
                                                          //&&
                                                          //x.ShippingDelivery == DeliveryOptionEnum.SimsCreation)
                                                          .FirstOrDefaultAsync();

                    if (shipment != null)
                    {
                        shipment.ShippmentStatus = ShippmentStatus.Cancelled;
                        var ShipmentHistory =await shipmenthistoryrepository.Query()
                                                                       .Where(x => x.ShipmentId == shipment.Id)
                                                                       .OrderByDescending(x => x.Id)
                                                                       .FirstOrDefaultAsync();
                        var History = new ShipmentHistory
                        {

                            OldStatus = ShipmentHistory.NewStatus,
                            NewStatus = ShippmentStatus.Cancelled,
                            ShipmentId = shipment.Id,
                            CreatedById = shipment.Order.CustomerId,
                            CreatedOn = DateTime.UtcNow,

                        };

                        shipmenthistoryrepository.Add(History);
                        await shipmenthistoryrepository.SaveChangesAsync();
                        var Order = await orderRepository.Query().Where(x => x.Id == shipment.OrderId)
                                          .Include(x => x.ShippingAddress)
                                          .Include("ShippingAddress.StateOrProvince")
                                          .Include("ShippingAddress.Country")
                                          .FirstOrDefaultAsync();

                        Order.OrderStatus = OrderStatus.Refunded;
                        await orderRepository.SaveChangesAsync();   
                        await shipmentRepository.SaveChangesAsync();
                    }
                
                }

                return OrderResponse;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region 

        private async Task<SimsConfigurationViewModel> GetSimsCredentials(long SiteId)
        {
            try
            {
                var Results = await siteSettingrepository.Query().AsNoTracking().Where(x => x.SiteId == SiteId && x.Id == "SimsCreation").FirstOrDefaultAsync();

                return JsonConvert.DeserializeObject<SimsConfigurationViewModel>(Results.Value);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<string> SendHttpRequestAsync(string baseUrl, string endpoint, HttpMethod method, object? model = null, string token = "")
        {
            try
            {
                // Prepare request
                HttpRequestMessage request;
                if (method == HttpMethod.Post)
                {
                    var jsonRequest = JsonConvert.SerializeObject(model);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(baseUrl + endpoint),
                        Content = content
                    };
                }
                else if (method == HttpMethod.Get)
                {
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(baseUrl + endpoint)
                    };
                }
                else if (method == HttpMethod.Delete)
                {
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(baseUrl + endpoint)
                    };
                }
                else
                {
                    throw new ArgumentException("Unsupported HTTP method");
                }

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception($"Error in {endpoint}: {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CreateShipment(string U_Id,long OrderId)
        {
            var Order = await orderRepository.Query().Where(x => x.Id == OrderId).FirstOrDefaultAsync();

            var Shipment = new EskaCMS.Entities.Entity.Shipments.Shipment();

            Shipment.OrderId = OrderId;

            Shipment.TrackingNumber = U_Id.ToString();

            Shipment.ShippmentStatus = ShippmentStatus.Preparing;

         //   Shipment.ShippingDelivery = EskaCMS.Entities.Enum.DeliveryOptionEnum.SimsCreation;

            Shipment.CreatedById = Order.CustomerId;

            Shipment.CreatedOn = DateTime.UtcNow;

            Shipment.WarehouseId = Order.WarehouseId;

            Shipment.LatestUpdatedOn = DateTime.UtcNow;

            shipmentRepository.Add(Shipment);

            await shipmentRepository.SaveChangesAsync();


            var History = new ShipmentHistory
            {

                OldStatus = null,
                NewStatus = ShippmentStatus.Preparing,
                ShipmentId = Shipment.Id,
                CreatedById = Shipment.Order.CustomerId,
                CreatedOn = DateTime.UtcNow,

            };

            shipmenthistoryrepository.Add(History);
            await shipmenthistoryrepository.SaveChangesAsync();

            return true;

        }
        #endregion

    }
}
