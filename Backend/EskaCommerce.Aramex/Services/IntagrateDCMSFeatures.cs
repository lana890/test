// Optimized version of IntegrateDCMSFeatures.cs
// - Same functionality preserved
// - Duplicate logic minimized
// - Code cleaned and restructured

using EskaCMS.Core.Extensions;
using EskaCMS.Core.Scripts;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Aramex.Models.Requests.Cart;
using EskaCommerce.Aramex.Models.Requests.RateCaluculator;
using EskaCommerce.Aramex.Models.Requests.Settings;
using EskaCommerce.Aramex.Models.Requests.Shipping;
using EskaCommerce.Aramex.Models.Requests.Tracking;
using EskaCommerce.Aramex.Models.Responses.Shipment;
using EskaCommerce.Aramex.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Aramex.Services
{
    public class IntegrateDCMSFeatures : IIntagrateDCMSFeatures
    {
        private readonly IRepository<EskaCMS.Entities.Entity.Orders.Order> _orderRepository;
        private readonly IRepository<Cart> cartRepository;
        private readonly IRepository<Warehouse> wareHouseRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> shipmentRepository;
        private readonly IRepository<UserAddress> userAddressRepository;
        private readonly IRepositoryWithTypedId<ShippingProvider, string> shippmentproviderRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Pickup> pickupRepository;
        private readonly ICurrencyService currencyService;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Dimensions> dimensionsRepository;
        private readonly IRepository<ProductDimensions> productDimensionsRepository;
        private readonly IWorkContext workContext;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private const string UNIT_KG = "Kg";
        private const string UNIT_CM = "CM";
        private const string ATTRIBUTE_WEIGHT = "weight";
        private const string ATTRIBUTE_WIDTH = "width";
        private const string ATTRIBUTE_HEIGHT = "height";
        private const string ATTRIBUTE_LENGTH = "length";
        private const string ATTRIBUTE_UNIT = "unit";

        public IntegrateDCMSFeatures(IRepository<EskaCMS.Entities.Entity.Orders.Order> orderRepository
                                               , IRepository<Cart> CartRepository
                                               , IRepository<Warehouse> WareHouseRepository
                                               , IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> ShipmentRepository,
                                                 IRepository<UserAddress> userAddressRepository,
                                                 IRepositoryWithTypedId<ShippingProvider, string> shippmentproviderRepository,
                                                 IRepository<EskaCMS.Entities.Entity.Shipments.Pickup> PickupRepository
                                               , ICurrencyService currencyService,
                                                IRepository<EskaCMS.Entities.Entity.Shipments.Dimensions> DimensionsRepository,
                                                IRepository<ProductDimensions> ProductDimensionsRepository,
                                                 IWorkContext workContext
                                               , IRepositoryWithTypedId<SiteSettings, string> SiteSettingsRepository

)
        {
            _orderRepository = orderRepository;
            cartRepository = CartRepository;
            wareHouseRepository = WareHouseRepository;
            shipmentRepository = ShipmentRepository;
            this.userAddressRepository = userAddressRepository;
            this.shippmentproviderRepository = shippmentproviderRepository;
            pickupRepository = PickupRepository;
            this.currencyService = currencyService;
            dimensionsRepository = DimensionsRepository;
            productDimensionsRepository = ProductDimensionsRepository;
            this.workContext = workContext;
            siteSettingsRepository = SiteSettingsRepository;

        }
        #region Shipments
        public async Task<ShipmentCreationRequest> CreateShipments(long orderId, SettingsAramxViewModel model)
        {
            try
            {
                var Shipment = await GetShipments(orderId, model.ClientInfo.AccountNumber);
                var result = new ShipmentCreationRequest();
                result.Transaction.Reference1 = orderId.ToString();
                result.Shipments = new List<EskaCommerce.Aramex.Models.Requests.Shipping.Shipment> { Shipment };
                result.LabelInfo = new LabelInfo
                {
                    ReportType = "URL",
                    ReportID = string.IsNullOrEmpty(model.ReportID) ? 9729 : Convert.ToInt32(model.ReportID)
                };

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PickupCreationRequest> CreatePickup(RequestPickupAdmin request, List<long> OrderIds, SettingsAramxViewModel model)
        {
            try
            {
                var result = new PickupCreationRequest();

                ValidateRequest(request);

                if (OrderIds.Count() == 0)
                {
                    throw new Exception("Shipment not have ready Order");
                }

                result.Pickup = await HandlerShipmentPickup(OrderIds, model.ExpressSettings);
                result.Pickup.PickupDate = request.PickupDate;
                result.Pickup.ClosingTime = request.ClosingTime;
                result.Pickup.LastPickupTime = request.LastPickupTime;
                result.Pickup.ReadyTime = request.ReadyTime;
                result.Pickup.Status = "Ready";

                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
        #region  Caculator Rate
        public async Task<RateCalculatorRequestDtos> CreateCalculateRate(long CartId)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var OriginAddress = await GetOriginAddress(SiteId);

                var result = new RateCalculatorRequestDtos();

                result.Transaction.Reference1 = CartId.ToString();

                result.DestinationAddress = await GetPartyCart(CartId);

                result.ShipmentDetails = await GetShipmentsCarts(CartId, result.DestinationAddress.CountryCode.ToLower(), OriginAddress.PartyAddress.CountryCode.ToLower());

                result.OriginAddress = new Models.Requests.RateCaluculator.Address
                {
                    City = OriginAddress.PartyAddress.City,
                    CountryCode = OriginAddress.PartyAddress.CountryCode,
                    Line1 = OriginAddress.PartyAddress.Line1,
                };


                if (result.DestinationAddress.CountryCode.ToLower() != result.OriginAddress.CountryCode.ToLower())
                {
                    var ProductsSettings = await GetExpressOrDomistic("ExpressSettings");

                    result.ShipmentDetails.ProductType = ProductsSettings.ProductType;
                    result.ShipmentDetails.ProductGroup = ProductsSettings.ProductGroup;


                }
                var Currency = currencyService.GetDefaultCurrencyCodeForAdmin();

                result.PreferredCurrencyCode = Currency;

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion
        #region Tracking

        public async Task<TrackingShippmentDto> CreateShipmentTracking(long ShipmentId)
        {
            try
            {
                var Result = new TrackingShippmentDto();

                var Order = await shipmentRepository.Query().Where(x => x.Id == ShipmentId).FirstOrDefaultAsync();

                Result.Shipments = new List<string>()
                {
                   Order.TrackingNumber
                };

                Result.GetLastTrackingUpdateOnly = false;

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion
        public async Task<PickupTrackingDto> CreateTrackingPickup(long PickupId)
        {
            try
            {
                var Result = new PickupTrackingDto();

                var Pickup = await pickupRepository.Query().Where(x => x.Id == PickupId).FirstOrDefaultAsync();

                Result.Reference = Pickup.PickupId;

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #region Shipments and Calucate Rate Helpers

       

        private async Task<Models.Requests.RateCaluculator.Address> GetPartyCart(long CartId)
        {
            try
            {
                var Cart = await cartRepository.Query().AsNoTracking()
                                              .Where(x => x.Id == CartId)
                                              .FirstOrDefaultAsync();

                var shippingData = JsonConvert.DeserializeObject<DeliveryInformationDtos>(Cart.ShippingData);

                EskaCMS.Entities.Entity.Addresses.Address query = await userAddressRepository
                                                                  .Query().Include(x => x.Address)
                                                                  .Include("Address.Country")
                                                                  .Include("Address.Country.CountryDetails")
                                                                  .Include("Address.StateOrProvince.StateOrProvinceDetails")
                                                                  .Where(x => x.Id == shippingData.ShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();

                var DefaultCulture = await GetDefaultCulture();


                return new Models.Requests.RateCaluculator.Address
                {

                    CountryCode = query.Country.Id,
                    City = query.StateOrProvince == null ? "" : query.StateOrProvince.StateOrProvinceDetails.Where(x => x.CultureId == DefaultCulture).Select(x => x.Name).FirstOrDefault(),
                    Line1 = query.AddressLine1,
                    Line2 = query.AddressLine2,
                    PostCode = query.ZipCode == null ? "" : query.ZipCode
                };

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task<Shipper> GetOriginAddress(long SiteId)
        {
            try
            {
                try
                {
                    var Query = await wareHouseRepository
                                                      .Query()
                                                      .Include(x => x.Address)
                                                      .Include("Address.Country.CountryDetails")
                                                      .Include("Address.StateOrProvince.StateOrProvinceDetails")
                                                      .Where(x => x.IsMain && x.SiteId == SiteId).AsNoTracking().FirstOrDefaultAsync();
                    var DefaultCulture = await GetDefaultCulture();


                    var ShipperInfo = new Shipper()
                    {

                        AccountNumber = "",
                        Reference1 = "",
                        Reference2 = "",
                        Contact = new Contact()
                        {
                            CompanyName = Query.Address.ContactName,
                            PhoneNumber1 = Query.Address.Phone,
                            PhoneNumber1Ext = "",
                            PhoneNumber2 = "",
                            PhoneNumber2Ext = "",
                            PersonName = Query.Address.ContactName,
                            EmailAddress = Query.Address.Email,
                            CellPhone = Query.Address.Phone

                        },
                        PartyAddress = new Party()
                        {

                            City = Query.Address.StateOrProvince.StateOrProvinceDetails.Where(x => x.CultureId == DefaultCulture).Select(x => x.Name).FirstOrDefault(),
                            CountryCode = Query.Address.CountryId,
                            PostCode = Query.Address.ZipCode == null ? "" : Query.Address.ZipCode,
                            Line1 = Query.Address.AddressLine1,
                            Line2 = Query.Address.AddressLine2
                        }

                    };
                    return ShipperInfo;
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


        private async Task<SettingShipment> GetExpressOrDomistic(string Module)
        {
            try
            {
                try
                {
                    SettingShipment settings = new SettingShipment();


                    var Query = await shippmentproviderRepository.Query().AsNoTracking().Where(x => x.Id == "Aramex").FirstOrDefaultAsync();

                    var Result = JsonConvert.DeserializeObject<SettingsAramxViewModel>(Query.AdditionalSettings); ;

                    if (Module == "DomisticSettings")
                    {
                        settings = Result.DomisticSettings;
                    }
                    else
                    {
                        settings = Result.ExpressSettings;
                    }

                    return settings;
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


        private double HandlerWeight(OrderItem item)
        {
            double totalWeightKg = 0;

            foreach (var attrVal in item.Product.AttributeValues)
            {
                var details = attrVal.AttributeValueDetails;
                var weightValue = details.FirstOrDefault(x =>
                    x.AttributeValue.Attribute.AttributeDetails.Any(a => a.Name.ToLower() == ATTRIBUTE_WEIGHT))?.Value;

                if (weightValue != null)
                {
                    var unit = details.FirstOrDefault(x =>
                        x.AttributeValue.Attribute.AttributeDetails.Any(a => a.Name.ToLower() == ATTRIBUTE_UNIT))?.Value?.ToLower();

                    var multiplier = unit == "g" ? 0.001 : 1;
                    totalWeightKg += Convert.ToDouble(weightValue) * multiplier * item.Quantity;
                }
            }

            if (totalWeightKg == 0)
                throw new Exception($"Weight or Unit not found for ProductId: {item.ProductId}");

            return Math.Round(totalWeightKg, 2);
        }
        private async Task<Models.Requests.Shipping.Pickup> HandlerShipmentPickup(List<long> OrderIds, SettingShipment ExpressSetting)
        {
            try
            {
                var Pickup = new Models.Requests.Shipping.Pickup();

                var order = await _orderRepository
                    .Query()
                    .Where(x => OrderIds.Any(u => u == x.Id))
                    .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                    .Include("OrderItems.Product.ProductDetails")
                    .Include(x => x.ShippingAddress)
                    .ToListAsync();



                var Warehouse = await wareHouseRepository.Query()
                                                         .Include(x => x.Address)
                                                         .Include("Address.StateOrProvince")
                                                         .Include("Address.StateOrProvince.StateOrProvinceDetails")
                                                         .Include("Address.Country")
                                                         .Where(x => x.IsMain)
                                                         .FirstOrDefaultAsync();
                var DefaultCulture = await GetDefaultCulture();

                if (Warehouse == null)
                {

                    throw new Exception("Main Warehouse not exist");
                }


                Pickup.PickupAddress = new PickupAddress
                {

                    City = Warehouse.Address.StateOrProvince.StateOrProvinceDetails.Where(x => x.CultureId == DefaultCulture).Select(x => x.Name).FirstOrDefault(),
                    CountryCode = Warehouse.Address.CountryId,
                    Latitude = 0,
                    Longitude = 0,
                    Line1 = Warehouse.Address.AddressLine1

                };

                // Any Data For Referance1 Beacause is required

                Pickup.Reference1 = order.FirstOrDefault().Id.ToString();

                Pickup.PickupContact = new PickupContact
                {
                    PhoneNumber1 = Warehouse.Address.Phone,
                    PersonName = Warehouse.Address.ContactName,
                    CompanyName = Warehouse.Address.ContactName,
                    CellPhone = Warehouse.Address.Phone,
                    EmailAddress = Warehouse.Address.Email
                };

                Pickup.PickupLocation = Warehouse.Address.City;

                Pickup.PickupItems = new PickupItems
                {
                    PickupItemDetail = new PickupItemDetail
                    {

                        ProductGroup = ExpressSetting.ProductGroup,
                        ProductType = ExpressSetting.ProductType,
                        NumberOfPieces = 1,
                        NumberOfShipments = order.Count(),
                        ShipmentVolume = new ShipmentVolume
                        {
                            Unit = "",
                            Value = 0

                        },

                        CashAmount = new Money
                        {

                            CurrencyCode = currencyService.GetCurrencyCode(Warehouse.Address.CountryId),
                            Value = 0,

                        },
                        Comments = "",

                        ShipmentWeight = new ShipmentWeight
                        {
                            Unit = "",
                            Value = 0
                        },
                        Payment = "P",

                        ExtraCharges = new Money
                        {
                            CurrencyCode = currencyService.GetCurrencyCode(Warehouse.Address.CountryId),
                            Value = 0

                        }
                    }

                };

                return Pickup;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<EskaCommerce.Aramex.Models.Requests.Shipping.Shipment> GetShipments(long orderId, string accountNumber)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var OriginAddress = await GetOriginAddress(SiteId);

                var result = new EskaCommerce.Aramex.Models.Requests.Shipping.Shipment();

                result.Shipper = OriginAddress;

                result.Shipper.AccountNumber = accountNumber;

                result.Consignee = await GetParty(orderId, accountNumber);

                result.ShippingDateTime = DateTime.Now;

                result.Details = await GetDetailsShipment(orderId, result.Shipper.PartyAddress.CountryCode, result.Consignee.PartyAddress.CountryCode);

                if (result.Consignee.PartyAddress.CountryCode.ToLower() != result.Shipper.PartyAddress.CountryCode.ToLower())
                {
                    if (result.Details != null)
                    {
                        result.Details.ProductType = "PPX";
                        result.Details.ProductGroup = "EXP";
                    }

                }


                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<Details> GetDetailsShipment(long orderId, string originCountryCode, string consigneeCountryCode)
        {
            try
            {
                var result = new Details();

                var order = await _orderRepository
                    .Query()
                    .Where(x => x.Id == orderId)
                    .Include(x => x.ShippingAddress)
                        .ThenInclude(x => x.StateOrProvince)
                    .Include(x => x.ShippingAddress.Country)
                    .Include(x => x.OrderItems)
                        .ThenInclude(x => x.Product)
                            .ThenInclude(x => x.AttributeValues)
                                .ThenInclude(x => x.AttributeValueDetails)
                    .Include("OrderItems.Product.AttributeValues.Attribute")
                    .Include("OrderItems.Product.AttributeValues.Attribute.AttributeDetails")
                    .Include("OrderItems.Product.ProductDetails")
                    .Include("OrderItems.Product.ProductDimensions")
                    .Include("OrderItems.Product.ProductDimensions.Dimensions")
                    .FirstOrDefaultAsync();

                if (order == null)
                    throw new Exception("Order not found");

                var dimensionSetting = await siteSettingsRepository
                    .Query()
                    .Where(x => x.Id == "DimensionType")
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync();

                var cartonDimensions = await GetCartonDimensions(EDimensionType.DependOnCarton);

                double totalWeight = 0;
                var selectedCartons = new List<SelectedCarton>();
                var preperSelectedCartons = new List<SelectedCartonViewModel>();
                var productNotIncludedInCartons = order.OrderItems
                    .Where(x => x.Product.ProductDimensions.Count() == 0)
                    .Select(x => x.ProductId)
                    .ToList();

                // 🧮 Step 1: Calculate product weights & pre-select carton mapping
                foreach (var item in order.OrderItems)
                {
                    double itemWeight = HandlerWeight(item);
                    totalWeight += itemWeight;

                    if (cartonDimensions.Any())
                    {
                        var selectedCarton = cartonDimensions.FirstOrDefault(i => i.ProductIds.Contains(item.ProductId));
                        if (selectedCarton != null)
                        {
                            preperSelectedCartons.Add(new SelectedCartonViewModel
                            {
                                DimensionId = selectedCarton.Id,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity
                            });
                        }
                    }
                }

                // 🧩 Step 2: Group cartons & find optimal combination
                if (preperSelectedCartons.Any())
                {
                    var cartons = preperSelectedCartons
                        .GroupBy(x => x.DimensionId)
                        .Select(g => new ResultCartonViewModel
                        {
                            DimensionId = g.Key,
                            Quantity = g.Sum(x => x.Quantity)
                        }).ToList();

                    selectedCartons = await GetOptimalCartonCombination(cartons, cartonDimensions);

                    if (selectedCartons?.Any() == true)
                        totalWeight += selectedCartons.Sum(x => Convert.ToDouble(x.Carton.Weight) * x.Count);
                }

                // ⚖️ Step 3: Actual Weight
                result.ActualWeight = new ActualWeight
                {
                    Unit = UNIT_KG,
                    Value = Math.Round(totalWeight, 2)
                };

                // 🌍 Step 4: Dimensions (International only)
                bool isInternational = !string.Equals(originCountryCode, consigneeCountryCode, StringComparison.OrdinalIgnoreCase);
                if (isInternational)
                {
                    result.Dimensions = await HandlerOrderDimensions(
                        order.OrderItems.Where(x => productNotIncludedInCartons.Contains(x.ProductId)).ToList()
                    );

                    if (selectedCartons.Any())
                    {
                        result.Dimensions.Width = Math.Max(result.Dimensions.Width, selectedCartons.Max(x => Convert.ToDouble(x.Carton.Width)));
                        result.Dimensions.Length = Math.Max(result.Dimensions.Length, selectedCartons.Max(x => Convert.ToDouble(x.Carton.Length)));
                        result.Dimensions.Height += selectedCartons.Sum(x => Convert.ToDouble(x.Carton.Height) * x.Count);
                    }
                }

                // ⚖️ Step 5: Chargeable Weight
                result.ChargeableWeight = new ChargeableWeight
                {
                    Unit = UNIT_KG,
                    Value = Math.Round(totalWeight, 2)
                };

                // 🧾 Step 6: Description
                result.DescriptionOfGoods = string.Join(" and ",
                    order.OrderItems
                        .SelectMany(x => x.Product.ProductDetails)
                        .Select(x => x.Name)
                        .Distinct()
                );

                result.GoodsOriginCountry = originCountryCode;

                // 🚚 Step 7: Product Group & Type
                var shippingSettings = await GetExpressOrDomistic("DomisticSettings");
                result.ProductGroup = shippingSettings.ProductGroup;
                result.ProductType = shippingSettings.ProductType;

                // 💰 Step 8: Payment
                result.PaymentType = "P";
                result.PaymentOptions = "ACCT";
                result.NumberOfPieces = 1;

                if (order.OrderStatus == OrderStatus.CashOnDelivery)
                {
                    result.CashOnDeliveryAmount = new CashOnDeliveryAmount
                    {
                        CurrencyCode = order.CurrencyCode,
                        Value = (double)order.OrderTotal
                    };
                    result.Services = "CODS";
                }
                result.CustomsValueAmount = new CustomsValueAmount
                {
                    CurrencyCode = order.CurrencyCode,
                    Value = (double)order.OrderTotal
                };

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void ValidateRequest(RequestPickupAdmin request)
        {
            try
            {
                DateTime now = DateTime.Now;


                if (now.DayOfWeek == DayOfWeek.Friday)
                {
                    throw new Exception("Pickup requests cannot be created on Fridays.");
                }


                if (request.PickupDate.Date < DateTime.Today ||
                    request.PickupDate.TimeOfDay < TimeSpan.FromHours(9) ||
                    request.PickupDate.TimeOfDay > TimeSpan.FromHours(14))
                {
                    throw new Exception("Invalid PickupDate. It should be today or later and between 9:00 AM - 2:00 PM.");
                }

                if (request.ReadyTime <= now.AddHours(1))
                {
                    throw new Exception("Invalid ReadyTime. It should be after the current time + 1 hour.");
                }

                if (request.LastPickupTime < request.ReadyTime.AddHours(1) ||
                    request.LastPickupTime.TimeOfDay > TimeSpan.FromHours(14))
                {
                    throw new Exception("Invalid LastPickupTime. It should be after ReadyTime + 1 hour and before or equal to 2:00 PM.");
                }

                if (request.ClosingTime < request.LastPickupTime ||
                    request.ClosingTime.TimeOfDay > TimeSpan.FromHours(14))
                {
                    throw new Exception("Invalid ClosingTime. It should be the same as or after LastPickupTime and before or equal to 2:00 PM.");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<Consignee> GetParty(long orderId, string accountNumber)
        {
            try
            {
                var query = await _orderRepository.Query().AsNoTracking().Where(x => x.Id == orderId)
                    .Include(x => x.ShippingAddress)
                    .Include("ShippingAddress.StateOrProvince")
                    .Include("ShippingAddress.StateOrProvince.StateOrProvinceDetails")
                    .Include("ShippingAddress.Country")
                    .Include("ShippingAddress.Country.CountryDetails")
                    .FirstOrDefaultAsync();

                var DefaultCulture = await GetDefaultCulture();
                return new Consignee
                {
                    AccountNumber = accountNumber,
                    PartyAddress = new PartyAddress
                    {
                        CountryCode = query.ShippingAddress.Country.Id,
                        Longitude = 0,
                        Latitude = 0,
                        City = query.ShippingAddress.StateOrProvince.StateOrProvinceDetails.Where(x => x.CultureId == DefaultCulture).Select(x => x.Name).FirstOrDefault(),
                        Line1 = query.ShippingAddress.AddressLine1,
                        Line2 = query.ShippingAddress.AddressLine2,
                        Description = "",
                        BuildingName = "",
                        PostCode = query.ShippingAddress.ZipCode,
                        Apartment = "",
                        BuildingNumber = "",
                        Floor = "",
                        Line3 = "",
                        StateOrProvinceCode = ""

                    },
                    Contact = new Contact
                    {
                        PersonName = query.ShippingAddress.ContactName,
                        CellPhone = query.ShippingAddress.Phone,
                        PhoneNumber1 = query.ShippingAddress.Phone,
                        EmailAddress = query.ShippingAddress.Email,
                        CompanyName = query.ShippingAddress.ContactName,
                        Department = "",
                        FaxNumber = "",
                        PhoneNumber1Ext = "",
                        PhoneNumber2 = "",
                        PhoneNumber2Ext = "",
                        Title = "",
                        Type = ""
                    }
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<long> GetDefaultCulture()
        {
            try
            {
                var CultureId = await siteSettingsRepository.Query().Where(x => x.Id == "DefaultCulture").Select(x => x.Value).FirstOrDefaultAsync();

                return Convert.ToInt64(CultureId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<CartonDimensionsHandler>> GetCartonDimensions(EDimensionType? eDimensionsType)
        {
            var siteId = workContext.GetCurrentSiteId();

            if (eDimensionsType != null)
            {
                switch (eDimensionsType)
                {

                    case EDimensionType.DependOnCarton:
                        return await dimensionsRepository.Query()
                             .AsNoTracking()
                             .Include(x=>x.ProductDimensions)
                            .Where(x => x.Status != EStatus.Deleted &&x.DimensionType==eDimensionsType)
                            .Select(x => new CartonDimensionsHandler
                            {
                                Id=x.Id,
                                DimensionType = eDimensionsType.Value,
                                Height = x.Height,
                                Length = x.Length,
                                ProductIds = x.ProductDimensions.Select(x=>x.ProductId).ToList(),
                                NumberOfPieces = x.NumberOfPieces,
                                Weight = x.Weight,
                                Width = x.Width
                            }).ToListAsync();

                    default:
                        return new List<CartonDimensionsHandler>();
                }
            }

            return new List<CartonDimensionsHandler>();
        }



        #endregion

        #region MyRegion
        public async Task<Details> GetShipmentsCarts(long cartId, string originCountryCode, string consigneeCountryCode)
        {
            try
            {
                var result = new Details();

                var cart = await cartRepository.Query()
                    .AsSplitQuery()
                    .Include(x => x.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.AttributeValues)
                                .ThenInclude(av => av.AttributeValueDetails)
                    .Include("Items.Product.AttributeValues.Attribute.AttributeDetails")
                    .Include("Items.Product.ProductDetails")
                    .Include("Items.Product.ProductDimensions")
                    .Include("Items.Product.ProductDimensions.Dimensions")
                    .FirstOrDefaultAsync(x => x.Id == cartId);

                if (cart == null)
                    throw new Exception("Cart not found");

                var cartonDimensions = await GetCartonDimensions(EDimensionType.DependOnCarton);

                double totalWeight = 0;
                var selectedCartons = new List<SelectedCarton>();
                var preperSelectedCartons = new List<SelectedCartonViewModel>();
                var productNotIncludedInCartons = cart.Items
                    .Where(x => x.Product.ProductDimensions.Count() == 0)
                    .Select(x => x.ProductId)
                    .ToList();

                // 🧮 Step 1: Calculate product weights & pre-select carton mapping
                foreach (var item in cart.Items)
                {
                    double itemWeight = HandlerWeight(item);
                    totalWeight += itemWeight;

                    if (cartonDimensions.Any())
                    {
                        var selectedCarton = cartonDimensions.FirstOrDefault(i => i.ProductIds.Contains(item.ProductId));
                        if (selectedCarton != null)
                        {
                            preperSelectedCartons.Add(new SelectedCartonViewModel
                            {
                                DimensionId = selectedCarton.Id,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity
                            });
                        }
                    }
                }

                // 🧩 Step 2: Group cartons & find optimal combination
                if (preperSelectedCartons.Any())
                {
                    var cartons = preperSelectedCartons
                        .GroupBy(x => x.DimensionId)
                        .Select(g => new ResultCartonViewModel
                        {
                            DimensionId = g.Key,
                            Quantity = g.Sum(x => x.Quantity)
                        }).ToList();

                    selectedCartons = await GetOptimalCartonCombination(cartons, cartonDimensions);

                    if (selectedCartons?.Any() == true)
                        totalWeight += selectedCartons.Sum(x => Convert.ToDouble(x.Carton.Weight) * x.Count);
                }

                // ⚖️ Step 3: Actual Weight
                result.ActualWeight = new ActualWeight
                {
                    Unit = UNIT_KG,
                    Value = Math.Round(totalWeight, 2)
                };

                // 🌍 Step 4: Dimensions (International only)
                bool isInternational = !string.Equals(originCountryCode, consigneeCountryCode, StringComparison.OrdinalIgnoreCase);
                if (isInternational)
                {
                    result.Dimensions = await HandlerCartDimensions(
                          cart.Items.Where(x => productNotIncludedInCartons.Contains(x.ProductId)).ToList()
                    );

                    if (selectedCartons.Any())
                    {
                        result.Dimensions.Width = Math.Max(result.Dimensions.Width, selectedCartons.Max(x => Convert.ToDouble(x.Carton.Width)));
                        result.Dimensions.Length = Math.Max(result.Dimensions.Length, selectedCartons.Max(x => Convert.ToDouble(x.Carton.Length)));
                        result.Dimensions.Height += selectedCartons.Sum(x => Convert.ToDouble(x.Carton.Height) * x.Count);
                    }
                }

                // ⚖️ Step 5: Chargeable Weight
                result.ChargeableWeight = new ChargeableWeight
                {
                    Unit = UNIT_KG,
                    Value = Math.Round(totalWeight, 2)
                };

                // 🧾 Step 6: Description
                result.DescriptionOfGoods = string.Join(" and ",
                    cart.Items
                        .SelectMany(x => x.Product.ProductDetails)
                        .Select(x => x.Name)
                        .Distinct()
                );

                result.GoodsOriginCountry = originCountryCode;

                // 🚚 Step 7: Product Group & Type
                var shippingSettings = await GetExpressOrDomistic("DomisticSettings");
                result.ProductGroup = shippingSettings.ProductGroup;
                result.ProductType = shippingSettings.ProductType;

                // 💰 Step 8: Payment
                result.PaymentType = "P";
                result.PaymentOptions = "ACCT";
                result.NumberOfPieces =1;

                var currency = (await currencyService.GetUserCurrencyRateOrDefault()).Name;
                result.CustomsValueAmount = new CustomsValueAmount
                {
                    CurrencyCode = currency,
                    Value = 0
                };

                result.CashOnDeliveryAmount = new CashOnDeliveryAmount
                {
                    CurrencyCode = currency,
                    Value = 0
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while building shipment details: {ex.Message}", ex);
            }
        }

        // ----------------------------------------------
        // Helper: Calculate Dimensions
        // ----------------------------------------------
        private async Task<Models.Requests.Shipping.Dimensions> HandlerCartDimensions(
            IList<CartItem> cartItems)
        {
            var dims = new List<DimensionsHandler>();

            foreach (var item in cartItems)
            {
                var dimension = new DimensionsHandler
                {
                    ProductId = item.ProductId,
                    Quentity = item.Quantity
                };

                var attrs = item.Product.AttributeValues.SelectMany(a => a.AttributeValueDetails);

                dimension.Width = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_WIDTH))?.Value ?? "0");

                dimension.Height = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_HEIGHT))?.Value ?? "0");

                dimension.Length = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_LENGTH))?.Value ?? "0");

                dims.Add(dimension);
            }

            // 🧩 FIX: Handle empty case safely
            if (!dims.Any())
            {
                return new Models.Requests.Shipping.Dimensions
                {
                    Unit = UNIT_CM,
                    Width = 0,
                    Length = 0,
                    Height = 0
                };
            }

            // 🧮 Calculate totals
            var result = new Models.Requests.Shipping.Dimensions
            {
                Unit = UNIT_CM,
                Width = dims.Max(d => d.Width),
                Length = dims.Max(d => d.Length),
                Height = dims.Sum(d => d.Height * d.Quentity)
            };

            return result;
        }

        private async Task<Models.Requests.Shipping.Dimensions> HandlerOrderDimensions(IList<OrderItem> cartItems)
        {
            var dims = new List<DimensionsHandler>();

            foreach (var item in cartItems)
            {
                var dimension = new DimensionsHandler
                {
                    ProductId = item.ProductId,
                    Quentity = item.Quantity
                };

                var attrs = item.Product.AttributeValues.SelectMany(a => a.AttributeValueDetails);

                dimension.Width = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_WIDTH))?.Value ?? "0");

                dimension.Height = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_HEIGHT))?.Value ?? "0");

                dimension.Length = Convert.ToDouble(attrs.FirstOrDefault(a =>
                    a.AttributeValue.Attribute.AttributeDetails.Any(x => x.Name.ToLower() == ATTRIBUTE_LENGTH))?.Value ?? "0");

                dims.Add(dimension);
            }

            // 🧩 FIX: Handle empty case safely
            if (!dims.Any())
            {
                return new Models.Requests.Shipping.Dimensions
                {
                    Unit = UNIT_CM,
                    Width = 0,
                    Length = 0,
                    Height = 0
                };
            }

            // 🧮 Calculate totals
            var result = new Models.Requests.Shipping.Dimensions
            {
                Unit = UNIT_CM,
                Width = dims.Max(d => d.Width),
                Length = dims.Max(d => d.Length),
                Height = dims.Sum(d => d.Height * d.Quentity)
            };

            return result;
        }
        // ----------------------------------------------
        // Helper: Calculate Weight from Attributes
        // ----------------------------------------------
        private double HandlerWeight(CartItem item)
        {
            double totalWeightKg = 0;

            foreach (var attrVal in item.Product.AttributeValues)
            {
                var details = attrVal.AttributeValueDetails;
                var weightValue = details.FirstOrDefault(x =>
                    x.AttributeValue.Attribute.AttributeDetails.Any(a => a.Name.ToLower() == ATTRIBUTE_WEIGHT))?.Value;

                if (weightValue != null)
                {
                    var unit = details.FirstOrDefault(x =>
                        x.AttributeValue.Attribute.AttributeDetails.Any(a => a.Name.ToLower() == ATTRIBUTE_UNIT))?.Value?.ToLower();

                    var multiplier = unit == "g" ? 0.001 : 1;
                    totalWeightKg += Convert.ToDouble(weightValue) * multiplier * item.Quantity;
                }
            }

            if (totalWeightKg == 0)
                throw new Exception($"The Weight or Unit not found for ProductId: {item.ProductId}");

            return Math.Round(totalWeightKg, 2);
        }

        // ----------------------------------------------
        // Helper: Optimal Carton Combination
        // ----------------------------------------------
        public async Task<List<SelectedCarton>> GetOptimalCartonCombination(
        List<ResultCartonViewModel> selectedQuantityWithRelatedDimension,
        List<CartonDimensionsHandler> cartonDimensions)
        {
            var selectedCartons = new List<SelectedCarton>();

            foreach (var carton in cartonDimensions)
            {
                // 🔹 Get all products that use this carton dimension
                var relatedProducts = selectedQuantityWithRelatedDimension
                    .Where(x => x.DimensionId == carton.Id)
                    .ToList();

                if (!relatedProducts.Any())
                    continue;

                // 🔹 Sum total quantities for all related products
                int totalQuantity = relatedProducts.Sum(x => x.Quantity);

                // 🔹 Calculate how many cartons are needed (partial = full carton)
                int count = 0;
                if (carton.NumberOfPieces > 0)
                {
                    count = (int)Math.Ceiling((double)totalQuantity / carton.NumberOfPieces);
                }

                // 🔹 Add to result if at least one carton is needed
                if (count > 0)
                {
                    selectedCartons.Add(new SelectedCarton
                    {
                        Carton = carton,
                        Count = count
                    });
                }
            }

            return selectedCartons;
        }


        #endregion

    }
}


