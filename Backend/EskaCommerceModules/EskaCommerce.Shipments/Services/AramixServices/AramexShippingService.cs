using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using static BusinessModels.BusinessModel;

using EskaCommerce.Aramex.Services.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using System.Globalization;
using EskaCMS.Entities.Entity.Addresses;
using MimeKit.Cryptography;
using System.Xml.Linq;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;

using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Enum;
using EskaCMS.Core.Services.Interfaces;
using EskaCommerce.Shipments.Models.AramixModels.Responses.Tracking;
using EskaCommerce.Shipments.Models.AramixModels.Responses.LabelPrinting;
using EskaCommerce.Shipments.Models.AramixModels.Responses.RateCalculate;
using System.Linq;
using EskaCommerce.Shipments.Models.AramixModels.Responses.Shipment;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Locations;
using System.Collections.Generic;
using EskaCommerce.Shipments.Models.AramixModels.Responses.Locations;
using EskaCommerce.Shipments.Models.AramixModels.Responses.Pickup;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Settings;

namespace EskaCommerce.Aramex.Services
{
    public class AramexShippingService : IAramexShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly IIntagrateDCMSFeatures _intagrateDCMSFeatures;
        private readonly IWorkContext workContext;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Pickup> pickupRepository;
        private readonly IRepository<ShipmentItem> shipmentItemRepository;
        private readonly IRepository<StateOrProvince> stateOrProvinceRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> shipmentRepository;
        private readonly IRepository<ShipmentHistory> shipmentHistoryRepository;
        private readonly IdcmsLogService dcmsLogServices;
        private readonly IRepositoryWithTypedId<ShippingProvider, string> shippmentproviderRepository;
        private readonly IRepository<Warehouse> wareHouseRepository;
        private readonly IRepositoryWithTypedId<Country, string> countryRepository;
        private readonly IRepository<Order> _orderRepository;

        public AramexShippingService(HttpClient httpClient,
            IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
            IIntagrateDCMSFeatures intagrateDCMSFeatures,
            IWorkContext workContext,
            IUserSiteService userSiteService,
            IRepository<EskaCMS.Entities.Entity.Shipments.Pickup> PickupRepository,
            IRepository<ShipmentItem> ShipmentItemRepository,
             IRepository<StateOrProvince> stateOrProvinceRepository,
             IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> ShipmentRepository,
             IRepository<ShipmentHistory> ShipmentHistoryRepository,
             IdcmsLogService DcmsLogServices,
             IRepositoryWithTypedId<ShippingProvider, string> shippmentproviderRepository,
             IRepository<Warehouse> WareHouseRepository,
            IRepositoryWithTypedId<Country, string> CountryRepository,
            IRepository<Order> orderRepository)
        {
            _httpClient = httpClient;
            _siteSettingsRepository = siteSettingsRepository;
            _intagrateDCMSFeatures = intagrateDCMSFeatures;
            this.workContext = workContext;
            this.userSiteService = userSiteService;
            pickupRepository = PickupRepository;
            shipmentItemRepository = ShipmentItemRepository;
            this.stateOrProvinceRepository = stateOrProvinceRepository;
            shipmentRepository = ShipmentRepository;
            shipmentHistoryRepository = ShipmentHistoryRepository;
            dcmsLogServices = DcmsLogServices;
            this.shippmentproviderRepository = shippmentproviderRepository;
            wareHouseRepository = WareHouseRepository;
            countryRepository = CountryRepository;
            _orderRepository = orderRepository;
        }

        #region Shipment Services
        public async Task<bool> CreateShipmentAsync(long orderId, long? ShipmentId = null)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var result = await _intagrateDCMSFeatures.CreateShipments(orderId, userInfo);

                result.ClientInfo = userInfo.ClientInfo;

                var response = await SendHttpRequestJsonAsync(userInfo.BaseUrlShipping, "CreateShipments", result);

                var DeserializeResponse = JsonConvert.DeserializeObject<AramexResponseResponse>(response);

                if (DeserializeResponse.HasErrors.Value)
                {
                    await dcmsLogServices.AddError("From Create Shipment", string.Join(',', DeserializeResponse.Notifications.Select(x => x.Message).ToList()), string.Join(',', DeserializeResponse.Notifications.Select(x => x.Message).ToList()), null, null);
                    throw new Exception(string.Join(',', DeserializeResponse.Notifications.Select(x => x.Message).ToList()));

                }

                await UpsertShipment(DeserializeResponse, orderId, ShippmentStatus.Preparing, ShipmentId);

                return true;
            }
            catch (Exception ex)
            {
                await UpsertShipment(new AramexResponseResponse(), orderId, ShippmentStatus.Failed, ShipmentId);

                await dcmsLogServices.AddError("From Create Shipment", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }

        public async Task<bool> CancelPickup(long pickupId)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var Pickup = await pickupRepository.Query().AsNoTracking().Where(x => x.Id == pickupId).FirstOrDefaultAsync();

                PickupCancelationRequestDto CancelPickup = new PickupCancelationRequestDto();

                CancelPickup.PickupGUID = Pickup.GUID;
                CancelPickup.ClientInfo = userInfo.ClientInfo;

                var response = await CancellationPickupParser(await SendHttpRequestXmlAsync("CancelPickup", CancelPickup));

                if (response.HasErrors)
                {
                    await dcmsLogServices.AddError("From Create Shipment", string.Join(',', response.Notifications.Select(x => x.Message).ToList()), string.Join(',', response.Notifications.Select(x => x.Message).ToList()), null, null);

                    throw new Exception(string.Join(',', response.Notifications.Select(x => x.Message).ToList()));
                }
                else
                {
                    await UnMappingShipmentPickup(pickupId);
                }
                return true;
            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From Cancel Pickup", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }
        public async Task<bool> RepushShipmentAsync(long orderId)
        {
            try
            {
                var order = await _orderRepository.Query().AsNoTrackingWithIdentityResolution().Where(x => x.Id ==orderId).Include(x=>x.Shipment).FirstOrDefaultAsync();
              

                if ((order.OrderStatus == OrderStatus.PaidByCoupon || order.OrderStatus == OrderStatus.PaymentReceived || order.OrderStatus == OrderStatus.CashOnDelivery) )
                {
                    await CreateShipmentAsync(order.Id);
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> CreatePickup(RequestPickupAdmin model)
        {
            try
            {
                var userInfo = await GetAramexSettings();
                var OrderIds = await shipmentRepository
                                                    .Query()
                                                    .Where(x => model.ShipmentIds.Any(g => g == x.Id) || x.ShippmentStatus == ShippmentStatus.Ready && x.ShippingDelivery == EskaCMS.Entities.Enum.DeliveryOptionEnum.Aramex)
                                                    .Select(x => x.OrderId)
                                                    .ToListAsync();

                var result = await _intagrateDCMSFeatures.CreatePickup(model, OrderIds, userInfo);

                result.Transaction.Reference1 = string.Join(',', OrderIds);

                result.ClientInfo = userInfo.ClientInfo;

                var response = await PickupParser(await SendHttpRequestXmlAsync("CreatePickup", result));

                if (response.HasErrors)
                {
                    await dcmsLogServices.AddError("From Create Shipment", string.Join(',', response.Notifications.Select(x => x.Message).ToList()), string.Join(',', response.Notifications.Select(x => x.Message).ToList()), null, null);

                    throw new Exception(string.Join(',', response.Notifications.Select(x => x.Message).ToList()));
                }
                else
                {

                    await ChangeShipmentStatus(OrderIds, ShippmentStatus.Pickuped);


                    await MappingReferancesIntoPickup(OrderIds, response);

                }
                return true;
            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From Create Pickup", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }
        public async Task<string> PrintLabel(long ShipmentId)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                LabelPrintingRequestDto label = new LabelPrintingRequestDto();

                var Shipment = await shipmentRepository.Query().Include(x => x.Order).ThenInclude(x => x.ShippingAddress).ThenInclude(x => x.Country).Where(x => x.Id == ShipmentId).FirstOrDefaultAsync();

                var CountrySiteId = await userSiteService.GetSiteCountry(workContext.GetCurrentSiteId());


                label.ClientInfo = userInfo.ClientInfo;
                label.ShipmentNumber = Shipment.TrackingNumber;
                label.ProductGroup = CountrySiteId.ToLower() != Shipment.Order.ShippingAddress.CountryId.ToLower() ? userInfo.ExpressSettings.ProductGroup : userInfo.DomisticSettings.ProductGroup;
                label.OriginEntity = "AMM";
                label.LabelInfo = new LabelInfo
                {

                    ReportID = string.IsNullOrEmpty(userInfo.ReportID) ? 9729 : Convert.ToInt32(userInfo.ReportID),
                    ReportType = "URL"

                };

                var model = LabelPrintingParser(await SendHttpRequestXmlAsync("PrintLabel", label));

                if (model.HasErrors)
                {
                    await dcmsLogServices.AddError("From Create Shipment", string.Join(',', model.Notifications.Select(x => x.Message).ToList()), string.Join(',', model.Notifications.Select(x => x.Message).ToList()), null, null);
                    throw new Exception(string.Join(',', model.Notifications.Select(x => x.Message).ToList()));
                }
                Shipment.LabelUrl = model.ShipmentLabel.LabelURL;

                await shipmentRepository.SaveChangesAsync();

                return model.ShipmentLabel.LabelURL;


            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From Print Label", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }
        #endregion


        #region Tracking Services
        public async Task<RootViewModel> TrackShipments(long ShipmentId)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var result = await _intagrateDCMSFeatures.CreateShipmentTracking(ShipmentId);

                result.ClientInfo = userInfo.ClientInfo;

                var httpResult = await SendHttpRequestJsonAsync(userInfo.BaseUrlTracking, "TrackShipments", result);

                return JsonConvert.DeserializeObject<RootViewModel>(httpResult);
            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From Track Shipment", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }

        public async Task<TrackingPickupResponse> TrackPickup(long PickupId)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var result = await _intagrateDCMSFeatures.CreateTrackingPickup(PickupId);

                result.ClientInfo = userInfo.ClientInfo;

                var httpResult = await SendHttpRequestJsonAsync(userInfo.BaseUrlTracking, "TrackPickup", result);

                return JsonConvert.DeserializeObject<TrackingPickupResponse>(httpResult);
            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From Track Pickup", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }
        #endregion
        #region CalculateRate
        public async Task<RateCalculatorResponse> CalculateRate(long cartId)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var result = await _intagrateDCMSFeatures.CreateCalculateRate(cartId);

                result.ClientInfo = userInfo.ClientInfo;

                var response = await SendHttpRequestJsonAsync(userInfo.BaseUrlCalculateRate, "CalculateRate", result);

                var RateCalculatorResponse = JsonConvert.DeserializeObject<RateCalculatorResponse>(response);

                if (RateCalculatorResponse.HasErrors)
                {
                    throw new Exception(string.Join(',', RateCalculatorResponse.Notifications.Select(x => x.Message).ToList()));

                }
                return RateCalculatorResponse;
            }
            catch (Exception ex)
            {
                await dcmsLogServices.AddError("From RateCalculator", ex.Message, ex.Message, ex.StackTrace, null);

                throw ex;
            }
        }
        #endregion
        #region Locations
        public async Task<bool> AddCitiesByAramex()
        {
            try
            {
                try
                {
                    var userInfo = await GetAramexSettings();

                    var Countries = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures)
                      .Where(C => !C.IsNeutralCulture && C.LCID != 0x7F)
                      .Select(ci => ci.LCID).Distinct()
                      .Select(id => new RegionInfo(id))
                      .GroupBy(r => r.Name)
                      .Select(g => g.First())
                      .Select(C => new CountryForm()
                      {
                          Id = C.Name,
                          Name = C.EnglishName,
                          Code3 = C.ThreeLetterISORegionName,
                          IsCityEnabled = true
                      })
                      .ToList();



                    foreach (var country in Countries)
                    {
                        var result = new CitiesViewModelRequest();

                        result.ClientInfo = userInfo.ClientInfo;
                        result.CountryCode = country.Id;

                        var response = await SendHttpRequestJsonAsync(userInfo.BaseUrlLocation, "FetchCities", result);

                        var Value = JsonConvert.DeserializeObject<CitiesViewModel>(response);

                        var Stats = new List<StateOrProvince>();

                        foreach (var item in Value.Cities)
                        {
                            Stats.Add(new StateOrProvince
                            {
                                //   Name = item,
                                Code = null,
                                CountryId = country.Id,
                                Type = null
                            });

                        }


                        stateOrProvinceRepository.AddRange(Stats);
                        await stateOrProvinceRepository.SaveChangesAsync();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
        #region Helpers
        private async Task MappingReferancesIntoPickup(List<long> orderIds, PickupCreationResponse response)
        {
            try
            {

                var Shipments = await shipmentRepository
                                             .Query()
                                             .Where(x => orderIds.Any(u => u == x.OrderId))
                                             .ToListAsync();

                var MainWareHouse = await wareHouseRepository.Query().Where(x => x.IsMain).FirstOrDefaultAsync();

                if (MainWareHouse == null)
                {
                    throw new Exception("Main Warehouse not exist");

                }

                EskaCMS.Entities.Entity.Shipments.Pickup Pickup = new EskaCMS.Entities.Entity.Shipments.Pickup()
                {
                    GUID = response.ProcessedPickup.GUID,
                    PickupId = response.ProcessedPickup.ID,
                    Reference1 = response.ProcessedPickup.Reference1,
                    Reference2 = response.ProcessedPickup.Reference2,
                    CreatedById = await workContext.GetCurrentUserId(),
                    CreatedOn = DateTime.Now
                };

                pickupRepository.Add(Pickup);
                await pickupRepository.SaveChangesAsync();

                foreach (var ship in Shipments)
                {
                    ship.PickupId = Pickup.Id;
                    ship.WarehouseId = MainWareHouse.Id;
                }
                await shipmentRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task UpsertShipment(AramexResponseResponse response, long orderId, ShippmentStatus shippmentStatus, long? ShipmentId = null)
        {
            try
            {
                var Shipment = ShipmentId == null ? new EskaCMS.Entities.Entity.Shipments.Shipment()
                                               : await shipmentRepository.Query().Where(x => x.Id == ShipmentId).FirstOrDefaultAsync();

                var Order = await _orderRepository.Query().Where(x => x.Id == orderId).FirstOrDefaultAsync();

                Shipment.LabelUrl = response.Shipments.Select(x => x.ShipmentLabel.LabelURL).FirstOrDefault();

                Shipment.TrackingNumber = response.Shipments.Select(x => x.ID).FirstOrDefault();

                Shipment.OrderId = orderId;

                Shipment.ShippmentStatus = shippmentStatus;

                Shipment.ShippingDelivery = DeliveryOptionEnum.Aramex;

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
                    await ChangeShipmentStatus(new List<long> { orderId }, shippmentStatus);

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
                var Order = await _orderRepository.Query().Include(x => x.OrderItems).Where(x => x.Id == orderId).FirstOrDefaultAsync();

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

        private async Task<SettingsAramxViewModel> GetAramexSettings()
        {
            try
            {

                var Result = await shippmentproviderRepository.Query().AsNoTracking().Where(x => x.Id == "Aramex").FirstOrDefaultAsync();

                return JsonConvert.DeserializeObject<SettingsAramxViewModel>(Result.AdditionalSettings); ;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> UnMappingShipmentPickup(long pickupId)
        {
            try
            {
                var Shipments = await shipmentRepository.Query().Where(x => x.PickupId == pickupId).ToListAsync();

                if (Shipments.Count > 0)
                {

                    foreach (var item in Shipments)
                    {
                        item.ShippmentStatus = ShippmentStatus.Ready;
                        item.PickupId = null;
                        await ChangeShipmentStatus(new List<long> { item.OrderId }, ShippmentStatus.Ready);
                    }

                    await shipmentRepository.SaveChangesAsync();


                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        #endregion


        #region HttpClient Functions


        private async Task<string> SendHttpRequestXmlAsync(string endpoint, object requestObject)
        {
            try
            {
                var userInfo = await GetAramexSettings();

                var xmlSerializer = new XmlSerializer(requestObject.GetType());
                using (var stringWriter = new StringWriter())
                {
                    var xmlSettings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true,
                        Encoding = Encoding.UTF8,
                    };

                    using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                    {
                        xmlSerializer.Serialize(xmlWriter, requestObject);
                    }

                    var xmlRequest = stringWriter.ToString();

                    xmlRequest = $"<?xml version=\"1.0\"?>{Environment.NewLine}{xmlRequest}";

                    var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");

                    var response = await _httpClient.PostAsync(userInfo.BaseUrlShipping + endpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var vv = await response.Content.ReadAsStringAsync();
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        throw new Exception($"Error in {endpoint}: {response.StatusCode}:{await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("PostCode"))
                {
                    throw new Exception("The Post Code Is not valid");

                }
                throw ex;
            }
        }

        private async Task<string> SendHttpRequestJsonAsync(string baseUrl, string endpoint, object requestObject)
        {
            try
            {


                var jsonRequest = JsonConvert.SerializeObject(requestObject, new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
                });

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(baseUrl + endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error in {endpoint}: {response.StatusCode}: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion


        #region Parser Xml

        private LabelPrintingResponseViewModel LabelPrintingParser(string xml)
        {
            XDocument xdoc = XDocument.Parse(xml);

            var pickupCancellationResponseElement = xdoc.Root;
            var transactionElement = pickupCancellationResponseElement.Element(XName.Get("Transaction", "http://ws.aramex.net/ShippingAPI/v1/"));
            var notificationsElement = pickupCancellationResponseElement.Element(XName.Get("Notifications", "http://ws.aramex.net/ShippingAPI/v1/"));
            var LabelUrl = pickupCancellationResponseElement.Element(XName.Get("ShipmentLabel", "http://ws.aramex.net/ShippingAPI/v1/"));
            var hasErrorsElement = pickupCancellationResponseElement.Element(XName.Get("HasErrors", "http://ws.aramex.net/ShippingAPI/v1/"));

            var reference1 = (string)transactionElement?.Element(XName.Get("Reference1", "http://ws.aramex.net/ShippingAPI/v1/"));
            var reference2 = (string)transactionElement?.Element(XName.Get("Reference2", "http://ws.aramex.net/ShippingAPI/v1/"));
            var reference3 = (string)transactionElement?.Element(XName.Get("Reference3", "http://ws.aramex.net/ShippingAPI/v1/"));
            var reference4 = (string)transactionElement?.Element(XName.Get("Reference4", "http://ws.aramex.net/ShippingAPI/v1/"));
            var reference5 = (string)transactionElement?.Element(XName.Get("Reference5", "http://ws.aramex.net/ShippingAPI/v1/"));

            bool hasErrors = hasErrorsElement != null && bool.Parse(hasErrorsElement.Value);

            List<NotificationsErrors> notificationList = new List<NotificationsErrors>();

            if (hasErrors)
            {
                var notifications = notificationsElement
                    ?.Elements(XName.Get("Notification", "http://ws.aramex.net/ShippingAPI/v1/"))
                    .Select(notificationElement => new NotificationsErrors
                    {
                        Code = (string)notificationElement.Element(XName.Get("Code", "http://ws.aramex.net/ShippingAPI/v1/")),
                        Message = (string)notificationElement.Element(XName.Get("Message", "http://ws.aramex.net/ShippingAPI/v1/"))
                    })
                    .ToList();

                if (notifications != null)
                {
                    notificationList = notifications;
                }
            }
            var labelURL = "";
            var labelFileContents = "";
            if (notificationList.Count == 0)
            {
                labelURL = (string)LabelUrl.Element(XName.Get("LabelURL", "http://ws.aramex.net/ShippingAPI/v1/"));
                labelFileContents = (string)LabelUrl.Element(XName.Get("LabelFileContents", "http://ws.aramex.net/ShippingAPI/v1/"));
            }

            return new LabelPrintingResponseViewModel
            {
                HasErrors = hasErrors,
                Notifications = notificationList,
                Transaction = new Shipments.Models.AramixModels.Responses.RateCalculate.Transaction
                {
                    Reference1 = reference1,
                    Reference2 = reference2,
                    Reference3 = reference3,
                    Reference4 = reference4,
                    Reference5 = reference5
                },
                ShipmentLabel = new ShipmentLabel
                {
                    LabelFileContents = labelFileContents,
                    LabelURL = labelURL


                },

            };

        }

        //private async Task<AramexResponseResponse> ShipmentParser(string xmlResponse)
        //{
        //    try
        //    {
        //        var xmlDoc = new XmlDocument();
        //        xmlDoc.LoadXml(xmlResponse);

        //        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        //        nsManager.AddNamespace("ns", "http://ws.aramex.net/ShippingAPI/v1/");
        //        XmlNode idNode = xmlDoc.SelectSingleNode("/ns:ShipmentCreationResponse/ns:Shipments/ns:ProcessedShipment/ns:ID", nsManager);

        //        XmlNode labelURLNode = xmlDoc.SelectSingleNode("/ns:ShipmentCreationResponse/ns:Shipments/ns:ProcessedShipment/ns:ShipmentLabel/ns:LabelURL", nsManager);

        //        IList<NotificationsErrors> notifications = new List<NotificationsErrors>();
        //        XmlNode hasErrorsNode = xmlDoc.SelectSingleNode("/ns:ShipmentCreationResponse/ns:HasErrors", nsManager);
        //        bool hasErrors = hasErrorsNode != null && !string.IsNullOrEmpty(hasErrorsNode.InnerText) ? bool.Parse(hasErrorsNode.InnerText) : false;
        //        if (hasErrors)
        //        {
        //            XmlNode processedShipmentNode = xmlDoc.SelectSingleNode("//ns:ProcessedShipment", nsManager);

        //            if (processedShipmentNode != null)
        //            {
        //                XmlNodeList notificationsNode = processedShipmentNode.SelectNodes(".//ns:Notification", nsManager);

        //                if (notificationsNode != null)
        //                {


        //                    foreach (XmlNode notificationNode in notificationsNode)
        //                    {
        //                        NotificationsErrors notification = new NotificationsErrors();
        //                        notification.Code = notificationNode.SelectSingleNode("ns:Code", nsManager)?.InnerText;
        //                        notification.Message = notificationNode.SelectSingleNode("ns:Message", nsManager)?.InnerText;

        //                        notifications.Add(notification);
        //                    }

        //                }
        //            }
        //        }
        //        string shipmentID = idNode != null ? idNode.InnerText : "ID not found";
        //        string labelURL = labelURLNode != null ? labelURLNode.InnerText : "LabelURL not found";

        //        return new ShipmentReponse
        //        {
        //            ShipmentID = shipmentID,
        //            LabelUrl = labelURL,
        //            Notifications = notifications,
        //            HasError = hasErrors,
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        private async Task<PickupCreationResponse> PickupParser(string xmlResponse)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(xmlResponse);

                XElement pickupResponseElement = xdoc.Root;
                XElement transactionElement = pickupResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Transaction");
                XElement notificationsElement = pickupResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Notifications");
                XElement hasErrorsElement = pickupResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}HasErrors");
                XElement processedPickupElement = pickupResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}ProcessedPickup");

                string reference1 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference1");
                string reference2 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference2");
                string reference3 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference3");
                string reference4 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference4");
                string reference5 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference5");
                bool hasErrors = bool.Parse(hasErrorsElement.Value);
                List<NotificationsErrors> notificationList = new List<NotificationsErrors>();
                if (hasErrors)
                {
                    var notifications = notificationsElement
                    .Elements("{http://ws.aramex.net/ShippingAPI/v1/}Notification")
                    .Select(notificationElement => new NotificationsErrors
                    {
                        Code = (string)notificationElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Code"),
                        Message = (string)notificationElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Message")
                    })
                    .ToList();

                    notificationList = notifications;
                }

                string id = (string)processedPickupElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}ID");
                string guid = (string)processedPickupElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}GUID");
                string Referance1 = (string)processedPickupElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference1");

                var PickupCreationResponse = new PickupCreationResponse
                {
                    HasErrors = hasErrors,
                    Notifications = notificationList,
                    ProcessedPickup = new ProcessedPickup
                    {
                        GUID = guid,
                        ID = id,
                        Reference1 = Referance1
                    },
                    Transaction = new Shipments.Models.AramixModels.Responses.RateCalculate.Transaction
                    {

                        Reference1 = reference1,
                        Reference2 = reference2,
                        Reference3 = reference3,
                        Reference4 = reference4,
                        Reference5 = reference5

                    }


                };

                return PickupCreationResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task<PickupeCancellationResponse> CancellationPickupParser(string xml)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(xml);

                XElement pickupCancellationResponseElement = xdoc.Root;
                XElement transactionElement = pickupCancellationResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Transaction");
                XElement notificationsElement = pickupCancellationResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Notifications");
                XElement hasErrorsElement = pickupCancellationResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}HasErrors");
                XElement messageElement = pickupCancellationResponseElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Message");

                string reference1 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference1");
                string reference2 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference2");
                string reference3 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference3");
                string reference4 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference4");
                string reference5 = (string)transactionElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Reference5");

                bool hasErrors = bool.Parse(hasErrorsElement.Value);
                string message = (string)messageElement;

                List<NotificationsErrors> notificationList = new List<NotificationsErrors>();

                if (hasErrors)
                {

                    var notifications = notificationsElement
                    .Elements("{http://ws.aramex.net/ShippingAPI/v1/}Notification")
                    .Select(notificationElement => new NotificationsErrors
                    {
                        Code = (string)notificationElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Code"),
                        Message = (string)notificationElement.Element("{http://ws.aramex.net/ShippingAPI/v1/}Message")
                    })
                    .ToList();

                    notificationList = notifications;

                }

                return new PickupeCancellationResponse
                {
                    HasErrors = hasErrors,
                    Message = message,
                    Notifications = notificationList
                ,
                    Transaction = new Shipments.Models.AramixModels.Responses.RateCalculate.Transaction
                    {

                        Reference1 = reference1,
                        Reference2 = reference2,
                        Reference3 = reference3,
                        Reference4 = reference4,
                        Reference5 = reference5

                    }

                };

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        #endregion
    }
}
