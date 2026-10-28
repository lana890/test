using EskaCMS.Addresses.Models;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Core.Scripts.ECommerce;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.ShippingTableRate;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Helpers;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using EskaCMS.Localization.Services.Interfaces;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Tax.Services;
using EskaCommerce.Aramex.Services.Interfaces;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Discount.Services;
using EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCommerce.Inventory.Services.Interface;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Events;
// These namespaces now live in EskaCMS.Core (IPaymentServices, PaymentHelper), so this project
// no longer references EskaCommerce.Payments - that reference is what blocked Payments from
// using Orders.
using EskaCommerce.Payments.Helper;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.Pricing.Services;
using EskaCommerce.Shipments.Services.ShippingPricesService.Interfaces;
using EskaCommerce.ShippingPrices.Services;
using EskaCommerce.Shippments.ShippingPrices.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM;
using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services;
using Google.Api.Gax.ResourceNames;
using Google.Apis.Util;
using MailKit.Search;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeDetective.Storage.Xml.v2;
using Newtonsoft.Json;
using NUglify.JavaScript.Syntax;
using OfficeOpenXml;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Org.BouncyCastle.Asn1.X500;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DeliveryInformationVm = EskaCommerce.Orders.Areas.Orders.ViewModels.DeliveryInformationVm;
using ShippingAddressVm = EskaCommerce.Orders.Areas.Orders.ViewModels.ShippingAddressVm;

namespace EskaCommerce.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICouponService _couponService;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<StockAmount> stockproductRepository;
        private readonly IRepository<ProductLink> productLinkRepository;
        private readonly ITaxService _taxService;
        private readonly IRepository<Payment> paymentRepository;
        private readonly ILocalizationServices localizationServices;
        private readonly IProductPricingService productPricingService;
        private readonly IOrderAdditionalFeesServices orderAdditionalFeesServices;
        private readonly ISiteCultureService siteCultureService;
        private readonly ITopDeliveryServices topDeliveryServices;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly ICartService _cartService;
        private readonly IRepository<OrderHistory> orderHistoryRepository;
        private readonly IRepository<ProductSerial> productSerialRepository;
        private readonly IShippingPriceService _shippingPriceService;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IMediator _mediator;
        private readonly ISimsCreationServices simsCreationServices;
        private readonly IAramexShippingService aramexShippingService;
        private readonly IRepository<Product> productRepository;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IRepository<District> _districtRepository;
        private readonly IProductServices productServices;
        private readonly IRepository<CurrencyRates> _currencyratesRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> shipmentRepository;
        private readonly IRepository<ShipmentHistory> shipmentHistoryRepository;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<PriceAndDestination> priceAnddistinationRepository;
        private readonly IRepository<StateOrProvince> _stateRepository;
        private readonly IDiscountService _IDiscountService;
        private readonly IWarehouseServices warehouseServices;
        private readonly IRepository<Warehouse> warehouseRepository;
        private readonly IRepository<ProductSerialHistory> _productSerialHistory;
        private readonly IConfiguration _configuration;
        private readonly IdcmsLogService dcmsLogService;
        private readonly IUserService _UserSerice;
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        private readonly ISubscriptionServices subscriptionServices;
        private readonly ISiteSettingsService _SiteSettings;
        private readonly ITestNotifier _testNotifier;
        private readonly IPaymentServices _paymentServices;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IpdfConverter _ipdfConverter;
        private readonly IEmailTemplateService _emailTemplateService;

        private readonly EskaDCMSDbContext _context;


        private static Dictionary<long, int> SkipQueue = new();
        private static List<string> ReservedSerials = new();
        private readonly IRepository<Stock> _stockRepository;
        public OrderService(IRepository<Order> orderRepository,
            IRepository<Cart> cartRepository,
            ICouponService couponService,
            EskaDCMSDbContext context,
            IRepository<CartItem> cartItemRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<StockAmount> stockproductRepository,
            IRepository<ProductLink> ProductLinkRepository,
            ITaxService taxService,IRepository<Payment> paymentRepository,
                                               ILocalizationServices localizationServices,
                                               IProductPricingService productPricingService,
            IOrderAdditionalFeesServices orderAdditionalFeesServices,
            ISiteCultureService siteCultureService,
            ITopDeliveryServices topDeliveryServices,
            IRepositoryWithTypedId<SiteSettings, string> SiteSettingsRepository,
            ICartService cartService,
            IRepository<OrderHistory> orderHistoryRepository,
            IConfiguration configuration,
            IdcmsLogService dcmsLogService,
            IRepository<ProductSerial> ProductSerialRepository,
            IShippingPriceService shippingPriceService,
            IRepository<UserAddress> userAddressRepository,
            IRepository<ProductSerialHistory> productSerialHistory,
            IMediator mediator,
            ISimsCreationServices simsCreationServices,
            IAramexShippingService aramexShippingService,
           IRepository<Product> ProductRepository,
           IWorkContext workContext,
           ICurrencyService currencyService,
           IRepository<District> districtRepository,
           IProductServices productServices,
           IRepository<CurrencyRates> currencyratesRepository,
           IRepository<EskaCMS.Entities.Entity.Shipments.Shipment> shipmentRepository,
           IRepository<ShipmentHistory> shipmentHistoryRepository,
           IUserSiteService userSiteService,
           IRepository<PriceAndDestination> priceAnddistinationRepository,
           IRepository<StateOrProvince> stateRepository,
           IDiscountService IDiscountService,
           IWarehouseServices warehouseServices,
           IRepository<Warehouse> warehouseRepository,
           ILoyaltyWalletService loyaltyWalletService,
           ISubscriptionServices subscriptionServices,
           IRepository<Stock> stockRepository, IUserService userSerice
            , ISiteSettingsService siteSettings,
           ITestNotifier testNotifier,
           IpdfConverter ipdfConverter,
           IEmailTemplateService emailTemplateService,
           IPaymentServices paymentServices,
           IHttpContextAccessor httpContextAccessor)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _couponService = couponService;
            _context = context;
            _cartItemRepository = cartItemRepository;
            _orderItemRepository = orderItemRepository;
            this.stockproductRepository = stockproductRepository;
            productLinkRepository = ProductLinkRepository;
            _taxService = taxService;
            this.paymentRepository=paymentRepository;
            this.localizationServices = localizationServices;
            this.productPricingService = productPricingService;
            this.orderAdditionalFeesServices = orderAdditionalFeesServices;
            this.siteCultureService = siteCultureService;
            this.topDeliveryServices = topDeliveryServices;
            siteSettingsRepository = SiteSettingsRepository;
            _cartService = cartService;
            this.orderHistoryRepository = orderHistoryRepository;
            _configuration = configuration;
            this.dcmsLogService = dcmsLogService;
            productSerialRepository = ProductSerialRepository;
            _shippingPriceService = shippingPriceService;
            _userAddressRepository = userAddressRepository;
            _mediator = mediator;
            this.simsCreationServices = simsCreationServices;
            this.aramexShippingService = aramexShippingService;
            productRepository = ProductRepository;
            _workContext = workContext;
            _productSerialHistory = productSerialHistory;
            _currencyService = currencyService;
            _districtRepository = districtRepository;
            this.productServices = productServices;
            _currencyratesRepository = currencyratesRepository;
            this.shipmentRepository = shipmentRepository;
            this.shipmentHistoryRepository = shipmentHistoryRepository;
            this.userSiteService = userSiteService;
            this.priceAnddistinationRepository = priceAnddistinationRepository;
            _stateRepository = stateRepository;
            _IDiscountService = IDiscountService;
            this.warehouseServices = warehouseServices;
            this.warehouseRepository = warehouseRepository;
            _stockRepository = stockRepository;
            _UserSerice = userSerice;
            _SiteSettings = siteSettings;
            _testNotifier = testNotifier;
            _loyaltyWalletService = loyaltyWalletService;
            this.subscriptionServices = subscriptionServices;
            _paymentServices = paymentServices;
            _httpContextAccessor = httpContextAccessor;
            _ipdfConverter = ipdfConverter;
            _emailTemplateService = emailTemplateService;
        }


        public async Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, long SiteId, OrderStatus orderStatus = OrderStatus.New)
        {
            try
            {
             //   throw new Exception("Cannot create or pay for an order after 12 PM.");
                var cart = await _cartRepository
                   .Query()
                   .Include(y => y.Items)
                   .Where(x => x.Id == cartId).FirstOrDefaultAsync();


                if (cart == null)
                {
                    return Result.Fail<Order>($"Cart id {cartId} cannot be found");
                }
                var products = cart.Items.Select(x => new CheckProductQuantityVM { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();

                var CanAdd = await productServices.CheckAllowedQuantity(products, cart.CustomerId);
                if (!CanAdd.Item1)
                    return Result.Fail<Order>($"You Added More Than Allowed Quantity , The Ids are : {string.Join(", ", CanAdd.InValidProducts)}");



                var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);
                Address billingAddress = null;
                Address shippingAddress = null;

                if ((!(shippingData.IsPickuped.HasValue && shippingData.IsPickuped.Value && shippingData.WarehouseId.HasValue)))

                {
                    if (shippingData.ShippingAddressId == 0)
                    {

                        var address = new Address
                        {
                            AddressLine1 = shippingData.NewAddressForm.AddressLine1,
                            AddressLine2 = shippingData.NewAddressForm.AddressLine2,
                            ContactName = shippingData.NewAddressForm.ContactName,
                            CountryId = shippingData.NewAddressForm.CountryId,
                            StateOrProvinceId = shippingData.NewAddressForm.StateOrProvinceId,
                            DistrictId = shippingData.NewAddressForm.DistrictId,
                            City = shippingData.NewAddressForm.City,
                            ZipCode = shippingData.NewAddressForm.ZipCode,
                            Phone = shippingData.NewAddressForm.Phone,
                            Email = shippingData.NewAddressForm.Email,
                            SiteId = SiteId,
                            Latitude = shippingData.NewAddressForm.Latitude,
                            Longitude = shippingData.NewAddressForm.Longitude,

                        };
                        var userAddress = new UserAddress
                        {
                            Address = address,
                            AddressType = AddressType.Shipping,
                            UserId = cart.CustomerId
                        };

                        _userAddressRepository.Add(userAddress);

                        shippingAddress = address;
                    }
                    else
                    {
                        shippingAddress =await _userAddressRepository.Query().Include(x => x.Address).ThenInclude(x => x.StateOrProvince).Where(x => x.Id == shippingData.ShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();
                    }

                    if (shippingData.UseShippingAddressAsBillingAddress)
                    {
                        billingAddress = shippingAddress;
                    }
                    else if (shippingData.BillingAddressId == 0)
                    {
                        var address = new Address
                        {
                            AddressLine1 = shippingData.NewBillingAddressForm.AddressLine1,
                            AddressLine2 = shippingData.NewBillingAddressForm.AddressLine2,
                            ContactName = shippingData.NewBillingAddressForm.ContactName,
                            CountryId = shippingData.NewBillingAddressForm.CountryId,
                            StateOrProvinceId = shippingData.NewBillingAddressForm.StateOrProvinceId,
                            DistrictId = shippingData.NewBillingAddressForm.DistrictId,
                            City = shippingData.NewBillingAddressForm.City,
                            ZipCode = shippingData.NewBillingAddressForm.ZipCode,
                            Phone = shippingData.NewBillingAddressForm.Phone,
                            Email = shippingData.NewBillingAddressForm.Email,
                            Latitude = shippingData.NewAddressForm.Latitude,
                            Longitude = shippingData.NewAddressForm.Longitude

                        };

                        var userAddress = new UserAddress
                        {
                            Address = address,
                            AddressType = AddressType.Billing,
                            UserId = cart.CustomerId
                        };

                        _userAddressRepository.Add(userAddress);

                        billingAddress = address;
                    }
                    else
                    {
                        billingAddress = _userAddressRepository.Query().Include(x => x.Address).ThenInclude(x => x.StateOrProvince).Where(x => x.Id == shippingData.BillingAddressId).Select(x => x.Address).First();
                    }
                }
                if (cart.CartType == CartTypes.Plan)
                {
                    return await CreateOrderPlan(cartId, paymentMethod, paymentFeeAmount, shippingData.ShippingMethod, billingAddress, shippingAddress, orderStatus, SiteId);
                }
                return await CreateOrder(cartId, paymentMethod, paymentFeeAmount, shippingData, billingAddress, shippingAddress, orderStatus, SiteId);

            }

            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, DeliveryInformationVm AddressInfo, Address? billingAddress = null, Address? shippingAddress = null, OrderStatus orderStatus = OrderStatus.New, long SiteId = 0)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var CustomerCurrency = await _currencyService.GetDefaultUserCurrency();
                var DefaultCurrency = await _currencyService.DefaultSiteCurrencyRate();
                var DcimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);
                var UserId = await _workContext.GetCurrentUserId();
                var warehouseId =await _workContext.GetCurrentWarehouseId();
                var userCountry = await userSiteService.GetUserCountry();
                var SiteCountryId = await userSiteService.GetSiteCountry(SiteId);

                var IsLocalCountry = await userSiteService.IsLocalCountry();
                var orderShippingAddress = shippingAddress == null ? null : new OrderAddress();
                var orderBillingAddress = billingAddress == null ? null : new OrderAddress();
                var productSerial = new List<ProductSerial>();
                var OrderAdditionalFees = new List<OrderAdditionalFees>();
                var shippingmethodName = "";
                decimal allItemsTotalFees = 0;

                var cart = await _cartRepository
                           .Query()
                           .Include(c => c.Items)
                           .Include("Items.Product")
                           .Include("Items.Product.ProductPriceWarehouses")
                           .Include("Items.Product.ProductBranch")
                           .Include("Items.Product.ProductAdditionalFees")
                           .Include("Items.Product.ProductAvaliablity")
                           .Include("Items.Product.Stock")
                           .Where(x => x.Id == cartId).FirstOrDefaultAsync();

                if (cart == null)
                {
                    return Result.Fail<Order>($"Cart id {cartId} cannot be found");
                }

                var products = cart.Items.Select(x => new CheckProductQuantityVM { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();


                var checkingDiscountResult = await CheckForDiscountIfAny(cart);

                if (!checkingDiscountResult.Succeeded)
                {
                    return Result.Fail<Order>(checkingDiscountResult.ErrorMessage);
                }

                if (orderShippingAddress != null || orderBillingAddress != null)
                {
                    var validateShippingMethodResult = await ValidateShippingMethod(string.IsNullOrEmpty(AddressInfo.ShippingMethod) ? "Free" : AddressInfo.ShippingMethod, shippingAddress, cart);

                    if (!validateShippingMethodResult.Success)
                    {
                        return Result.Fail<Order>(validateShippingMethodResult.Error);
                    }

                    shippingmethodName = validateShippingMethodResult.Value.Name;

                    orderBillingAddress = fillAddress(billingAddress, UserId);
                    orderShippingAddress = fillAddress(shippingAddress, UserId);
                }
                var order = new Order
                {
                    CustomerId = cart.CustomerId,
                    CreatedOn = DateTimeOffset.Now,
                    CreatedById = cart.CreatedById,
                    LatestUpdatedOn = DateTimeOffset.Now,
                    LatestUpdatedById = cart.CreatedById,
                    BillingAddress = orderBillingAddress,
                    ShippingAddress = orderShippingAddress,
                    PaymentMethod = paymentMethod,
                    PaymentFeeAmount = paymentFeeAmount,
                    SiteId = SiteId,
                    Channel = _workContext.GetCurrentPlatform(),
                    CartId = cart.Id,
                    CurrencyCode = DefaultCurrency.Name,
                    CurrencyCodeCustomer = CustomerCurrency.Name,
                };


                foreach (var cartItem in cart.Items)
                {
                    decimal taxPercent = 0;

                    if (!cartItem.Product.IsAllowToOrder || cartItem.Product.Status != EStatus.Published)
                    {
                        return Result.Fail<Order>($"The product {cartItem.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault()} is not available any more");
                    }
                    if (cartItem.Product.StockTrackingIsEnabled)
                    {
                        await _cartService.WarehouseInventoryStatus(cartItem, shippingAddress, SiteId, cart.ShippingData, cartItem.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(), warehouseId);
                    }
                    if (shippingAddress != null || billingAddress != null)
                    {
                        taxPercent = await _taxService.GetTaxPercent(cartItem.Product.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId, shippingAddress.ZipCode);
                    }
                    else
                    {

                        if (warehouseId != 0)
                        {
                            Address WarehouseInfo = await warehouseServices.GetAddressByWarehouseId(warehouseId);
                            taxPercent = await _taxService.GetTaxPercent(cartItem.Product.TaxClassId, WarehouseInfo.CountryId, WarehouseInfo.StateOrProvinceId, WarehouseInfo.ZipCode);

                        }



                    }
                    var productName = cartItem.Product.ProductDetails
                          .Where(x => x.CultureId == CultureId)
                          .Select(x => x.Name)
                          .FirstOrDefault();
                    if (cartItem.Product?.ProductBranch != null
                          && cartItem.Product.ProductBranch.Any()
                          && !cartItem.Product.ProductBranch.Any(c => c.WarehouseId == warehouseId))
                    {
                        var warehouse = await warehouseRepository.Query()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == warehouseId);

                      

                        return Result.Fail<Order>($"The product {productName} is not available in {warehouse?.Name}");
                    }
                    if (cartItem.Product?.Stock.Any() == true)
                    {
                        if (warehouseId != 0 &&
                            cartItem.Product.Stock.Any(x =>
                                x.WarehouseId == warehouseId &&
                                (x.Status != EStatus.Published && x.Status != EStatus.Active)))
                        {
                            throw new Exception($"Item {productName} is not available in this warehouse.");
                        }
                    }

                    var ProductPrice = IsLocalCountry
                        ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", cartItem.Product.PriceType == EPriceType.ProductBase
                                                          ? cartItem.Product.Price.HasValue ? cartItem.Product.Price.Value : 0
                                                          : cartItem.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == warehouseId).Select(x => x.Price).FirstOrDefault()))
                        : cartItem.Product.ForeignPrice.HasValue
                                                    ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", cartItem.Product.ForeignPrice.Value))
                                                    : 0;


                    var PromoDiscount = await _IDiscountService.CalculateDiscount(SiteId, (ProductPrice * cartItem.Quantity), cartItem.ProductId, cart.CreatedById);
                    var PromoPerProductDiscount = await _IDiscountService.CalculateDiscount(SiteId, (ProductPrice), cartItem.ProductId, cart.CreatedById);

                    if (cart.IsProductPriceIncludeTax)
                    {
                        ProductPrice = ProductPrice / (1 + (taxPercent / 100));
                    }

                    decimal totalFees = 0;

                    if (cartItem.Product.ProductAdditionalFees != null)
                    {
                        var orderAdditionalFees = new OrderAdditionalFees();

                        foreach (var fee in cartItem.Product.ProductAdditionalFees.Where(x => x.Status != EStatus.Deleted).ToList())
                        {
                            decimal fees = 0;
                            if (fee.FeeType == DiscountTypes.Percentage)
                            {
                                fees = (fee.FeeValue / 100) * (cartItem.Quantity * ProductPrice);
                                totalFees += fees;
                            }
                            else if (fee.FeeType == DiscountTypes.FixedAmount)
                            {
                                fees = fee.FeeValue * cartItem.Quantity;
                                totalFees += fees;
                            }

                            OrderAdditionalFees.Add(new OrderAdditionalFees
                            {
                                Amount = fees,
                                FeesId = fee.Id,
                                Note = fee.Note,
                                ProductId = cartItem.ProductId,
                                Status = ProcessReversal.Paid,

                            });
                        }


                    }
                    allItemsTotalFees += totalFees;


                    await IsAvaliableProduct(cartItem.Product, userCountry, SiteCountryId);

                    var orderItem = new OrderItem
                    {
                        Product = cartItem.Product,
                        ProductPrice = ProductPrice,
                        Quantity = cartItem.Quantity,
                        TaxPercent = taxPercent,
                        AdditionalDetails = cartItem.AdditionalDetails,
                        TaxAmount = _currencyService.DcimalRound(cartItem.Quantity * ((ProductPrice - PromoPerProductDiscount) * taxPercent / 100))
                    };

                    orderItem.DiscountAmount += (PromoDiscount);

                    var discountedItem = checkingDiscountResult.DiscountedProducts.FirstOrDefault(x => x.Id == cartItem.ProductId);

                    if (discountedItem != null)
                    {
                        orderItem.DiscountAmount += _currencyService.DcimalRound(discountedItem.DiscountAmount);
                    }

                    order.AddOrderItem(orderItem);

                    productSerial = await ReserveSerials(cartItem);

                    await _cartItemRepository.SaveChangesAsync();
                    await productRepository.SaveChangesAsync();
                }
                if (warehouseId == 0)
                {
                    order.WarehouseId = await warehouseServices.GetMainWareHouse(order.SiteId);

                }
                else
                {
                    order.WarehouseId=warehouseId;

                }

                    order.SubTotal = _currencyService.DcimalRound(order.OrderItems.Sum(x => x.Quantity * x.ProductPrice)); ;
                order.OrderStatus = orderStatus;
                order.SiteId = SiteId;
                order.OrderNote = cart.OrderNote;
                order.CouponCode = checkingDiscountResult.CouponCode;
                order.CouponRuleName = cart.CouponRuleName;

                if (order.OrderItems.Select(x => x.DiscountAmount).Sum() == 0)
                {
                    order.DiscountAmount = _currencyService.DcimalRound(checkingDiscountResult.DiscountAmount);


                }
                else
                {
                    order.DiscountAmount = _currencyService.DcimalRound(order.OrderItems.Select(x => x.DiscountAmount).Sum());

                }


                var ShipmentAmount = cart.ShippingAmount == null || cart.ShippingAmount == 0 ? 0 : cart.ShippingAmount.Value;
                decimal shippingDiscount = 0;

                if (ShipmentAmount > 0 && await _couponService.QualifiesForFreeShipping(cart))
                {
                    shippingDiscount = ShipmentAmount;   
                    ShipmentAmount = 0;        
                }

                order.ShippingFeeAmount = ShipmentAmount;
                order.ShippingDiscountAmount = shippingDiscount;

                order.ShippingMethod = shippingmethodName;

                order.TaxAmount = _currencyService.DcimalRound(order.OrderItems.Sum(x => x.TaxAmount));
                order.PaymentFeeAmount = allItemsTotalFees;

                order.SubTotalWithDiscount = _currencyService.DcimalRound((order.SubTotal + allItemsTotalFees) - (order.DiscountAmount));

                order.RedeemAmount = cart.RedeemAmount;

                var OrderTotal = order.ShippingFeeAmount + order.PaymentFeeAmount + (order.SubTotal + order.TaxAmount  - order.DiscountAmount - order.RedeemAmount);

                order.OrderTotal = _currencyService.DcimalRound(OrderTotal);
                if (order.OrderTotal < 0)
                {
                    order.OrderTotal = 0;
                }

                var Currency = await _currencyService.GetCurrencyByName(order.CurrencyCodeCustomer);

                order.CustomerCurrencyOrderTotal = _currencyService.DcimalRound(_currencyService.ConvertToCurrency(OrderTotal, Currency, DcimalPlaces));

                _orderRepository.Add(order);

                var vendorIds = cart.Items.Where(x => x.Product.VendorId.HasValue).Select(x => x.Product.VendorId.Value).Distinct();

                if (vendorIds.Any())
                {
                    order.IsMasterOrder = true;
                }

                IList<Order> subOrders = new List<Order>();
                foreach (var vendorId in vendorIds)
                {
                    var subOrder = new Order
                    {
                        CustomerId = cart.CustomerId,
                        CreatedOn = DateTimeOffset.Now,
                        CreatedById = cart.CreatedById,
                        LatestUpdatedOn = DateTimeOffset.Now,
                        LatestUpdatedById = cart.CreatedById,
                        BillingAddress = orderBillingAddress,
                        ShippingAddress = orderShippingAddress,
                        VendorId = vendorId,
                        Parent = order,
                        Channel = _workContext.GetCurrentPlatform(),
                        CartId = cart.Id
                    };
                    foreach (var cartItem in cart.Items.Where(x => x.Product.VendorId == vendorId))
                    {
                        var taxPercent = await _taxService.GetTaxPercent(cartItem.Product.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId);
                        var productPrice = IsLocalCountry ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", cartItem.Product.Price)) : cartItem.Product.ForeignPrice.HasValue ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", cartItem.Product.ForeignPrice.Value)) : 0;
                        if (cart.IsProductPriceIncludeTax)
                        {
                            productPrice = productPrice / (1 + (taxPercent / 100));
                        }

                        var orderItem = new OrderItem
                        {
                            Product = cartItem.Product,
                            ProductPrice = productPrice,
                            Quantity = cartItem.Quantity,
                            TaxPercent = taxPercent,
                            TaxAmount = (cartItem.Quantity * productPrice) * (taxPercent / 100),
                            AdditionalDetails = cartItem.AdditionalDetails
                        };


                        if (cart.IsProductPriceIncludeTax)
                        {
                            orderItem.ProductPrice = Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", (orderItem.ProductPrice - orderItem.TaxAmount)));

                        }

                        subOrder.AddOrderItem(orderItem);
                    }

                    subOrder.SubTotal = _currencyService.DcimalRound(subOrder.OrderItems.Sum(x => x.ProductPrice * x.Quantity));

                    subOrder.TaxAmount = _currencyService.DcimalRound(subOrder.OrderItems.Sum(x => x.TaxAmount));

                    subOrder.OrderTotal = _currencyService.DcimalRound(subOrder.SubTotal + allItemsTotalFees + subOrder.TaxAmount + subOrder.ShippingFeeAmount - subOrder.DiscountAmount);

                    _orderRepository.Add(subOrder);

                    subOrders.Add(subOrder);
                }

                _orderRepository.SaveChanges();

                await orderAdditionalFeesServices.AddOrderAdditionalFees(OrderAdditionalFees, order.Id);

                _couponService.AddCouponUsage(cart.CustomerId, order.Id, checkingDiscountResult);

                _orderRepository.SaveChanges();

                if (productSerial.Count > 0)
                {
                    // Pre-load the reserved-status of all serials in ONE query instead of
                    // querying the DB per iteration (was an N+1). Behavior is unchanged:
                    // a missing serial yields null, exactly like the previous FirstOrDefault().
                    var serialIds = productSerial.Select(s => s.Id).ToList();
                    var reservedById = await productSerialRepository.Query()
                        .Where(ps => serialIds.Contains(ps.Id))
                        .AsNoTracking()
                        .ToDictionaryAsync(ps => ps.Id);

                    foreach (var Serial in productSerial)
                    {
                        reservedById.TryGetValue(Serial.Id, out var checkIfReserved);
                        if (checkIfReserved.OrderId == null && OrderService.ReservedSerials.Count(r => r == Serial.Code) == 1)   // Perf #15: was Where(...).ToList().Count - avoids allocating a list to count in-memory items
                        {
                            Serial.OrderId = order.Id;

                            var ProductSerialHistory = new ProductSerialHistory
                            {

                                ProductSerialId = Serial.Id,
                                CreatedById = order.CreatedById,
                                CreationDate = DateTimeOffset.Now,
                                OldStatus = EProductSerialStatus.Available,
                                NewStatus = EProductSerialStatus.Taken,
                                OrderId = Serial.OrderId,
                                Note = "Change To Taken Value for OrderId= " + Serial.OrderId.ToString() + " "

                            };
                            _productSerialHistory.Add(ProductSerialHistory);
                            await productSerialRepository.SaveChangesAsync();

                        }
                        else
                        {
                            OrderService.ReservedSerials.Remove(Serial.Code);

                            var SerialsSettings = await _SiteSettings.GetSiteSettingsByModule("Serials", await _workContext.GetCurrentSiteIdAsync());
                            // var ReserveSerialsAfterPayment = SerialsSettings.Where(x => x.Id == "ReserveSerialsAfterPayment").FirstOrDefault();
                            var GetSerialsFromOriginalProduct = SerialsSettings.Where(x => x.Id == "GetSerialsFromOriginalProduct").FirstOrDefault();
                            long TargetProductId = Serial.ProductId;


                            if (GetSerialsFromOriginalProduct != null &&
                               (bool.Parse(GetSerialsFromOriginalProduct.Value)))
                            {
                                TargetProductId = await productServices.GetOriginalProductId(Serial.ProductId);

                            }


                            var NewSerial = await productSerialRepository.Query().Where(ps => ps.Status == EProductSerialStatus.Available && ps.OrderId == null
                                                                            && ps.ProductId == TargetProductId)
                                                                        .FirstOrDefaultAsync();

                            NewSerial.OrderId = order.Id;
                            NewSerial.Status = EProductSerialStatus.Taken;

                            var ProductSerialHistory = new ProductSerialHistory
                            {

                                ProductSerialId = NewSerial.Id,
                                CreatedById = order.CreatedById,
                                CreationDate = DateTimeOffset.Now,
                                OldStatus = EProductSerialStatus.Available,
                                NewStatus = EProductSerialStatus.Taken,
                                OrderId = order.Id,
                                Note = "Replaced insted of Serial " + Serial.Id + "Change To Taken Value for OrderId= " + order.Id.ToString() + " "

                            };
                            _productSerialHistory.Add(ProductSerialHistory);
                            await productSerialRepository.SaveChangesAsync();


                        }
                        await _productSerialHistory.SaveChangesAsync();

                        await productSerialRepository.SaveChangesAsync();

                    }
                }

                await _context.Entry(order)
                         .Navigation("ProductSerials")
                         .LoadAsync();

                await PublishOrderCreatedEvent(order);
                foreach (var subOrder in subOrders)
                {
                    await PublishOrderCreatedEvent(subOrder);
                }
                if (orderBillingAddress != null || orderBillingAddress != null)
                {
                    await _workContext.UpdateAnynoumsUser(orderBillingAddress.ContactName, orderBillingAddress.Email, SiteId, orderBillingAddress.Phone);
                }
                else
                {
                    await _workContext.UpdateAnynoumsUser(AddressInfo.CustomerName, AddressInfo.Email, SiteId, AddressInfo.PhoneNumber);
                }

                if (productSerial != null)
                {
                    foreach (var item in productSerial)
                    {
                        OrderService.ReservedSerials.Remove(item.Code);
                    }
                }


                return Result.Ok(order);


            }
            catch (Exception ex)
            {
                //DBTrans.Rollback();
                throw ex;
            }
            // }
        }

        public async Task<PayCashResult> PayCash(OrderViewModel Model)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCartDetails(currentUserId, CartTypes.Shop, SiteId);
                if (cart == null)
                {
                    throw new Exception("No Active Cart.");
                }

                var order = await CreateOrder(cart.Id, "CashOnDelivery", Model.PaymentFees, SiteId, Model.OrderStatus);

                if (!order.Success)
                {
                    throw new Exception(order.Error);
                }





                await CheckAnyDiscountFromUserForPayment(order.Value, cart.RedeemDiscount, cart.CouponDiscount);
                if (order.Value.OrderTotal == 0)
                {
                    await OrderChangeStatus(order.Value.Id, OrderStatus.PaidByCoupon, string.Empty, SiteId);

                    await ReserveSerials(order.Value.Id);



                }
                else
                {
                    await OrderChangeStatus(order.Value.Id, OrderStatus.CashOnDelivery, string.Empty, SiteId);
                    await _paymentServices.CreatePayment(order.Value, order.Value.Id.ToString(), PaymentHelper.CashOnDelivery, null, PaymentStatus.Succeeded);


                }
                var CartResult = await DeActiveCart(cart.Id);

                return new PayCashResult()
                {
                    OrderCode = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(order.Value.Id.ToString())),
                    OrderId = order.Value.Id
                };
            }
            catch (Exception e)
            {

                throw e;
            }


        }

        private async Task IsAvaliableProduct(Product product, UserCountryVM userCountry, string siteCountryId)
        {
            var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

            if (!(AllowdProductBaseedOnPrice && (product.ProductAvaliablity?.Count() == 0 || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed))))
            {

                throw new Exception("Unavalible in your country");
            }

        }



        public async Task<bool> CheckAnyDiscountFromUserForPayment(Order order, decimal redeemAmount, decimal couponAmount)
        {

            if (redeemAmount > 0)
            {
                await _paymentServices.CreatePayment(order, order.Id.ToString(), PaymentHelper.RedeemAmount, null, PaymentStatus.Succeeded, null, redeemAmount);

            }
            if (couponAmount > 0)
            {
                await _paymentServices.CreatePayment(order, order.Id.ToString(), PaymentHelper.CouponAmount, null, PaymentStatus.Succeeded, null, couponAmount);

            }

            return true;

        }

        public async Task<bool> ReserveSerials(long orderId)
        {
            var order = await GetOrderById(orderId);
            var ReserveSerialsAfterPayment = await _SiteSettings.GetSiteSettingById("ReserveSerialsAfterPayment", await _workContext.GetCurrentSiteIdAsync());
            var GetSerialsFromOriginalProduct = await _SiteSettings.GetSiteSettingById("GetSerialsFromOriginalProduct", await _workContext.GetCurrentSiteIdAsync());

            if (ReserveSerialsAfterPayment == null ||
               (bool.Parse(ReserveSerialsAfterPayment.Value) &&
                (order.OrderStatus == OrderStatus.PaymentReceived || order.OrderStatus == OrderStatus.PaidByCoupon)))
            {
                foreach (var OrderItem in order.OrderItems)
                {
                    List<ProductSerial> Serials = new();
                    if (GetSerialsFromOriginalProduct != null && bool.Parse(GetSerialsFromOriginalProduct.Value))
                    {
                        Product Original = await productServices.GetOriginalProduct(OrderItem.Product.Id);
                        if (Original.StockTrackingIsEnabled && Original.IsProductSerial)
                        {
                            Serials = await ReserveSerials(OrderItem.Product.Id, OrderItem.Quantity);
                        }
                    }
                    else
                    {
                        if (OrderItem.Product.StockTrackingIsEnabled && OrderItem.Product.IsProductSerial)
                        {
                            Serials = await ReserveSerials(OrderItem.Product.Id, OrderItem.Quantity);
                        }

                    }

                    if (Serials.Count > 0)
                    {



                        // Perf #05: pre-load the reserved-status of all serials in ONE query instead
                        // of querying the DB per iteration (was an N+1). Same pattern already used
                        // for the productSerial loop earlier in this file. Behavior is unchanged:
                        // a missing serial yields null, exactly like the previous FirstOrDefault().
                        var serialIdsToCheck = Serials.Select(s => s.Id).ToList();
                        var reservedSerialsById = await productSerialRepository.Query()
                            .Where(ps => serialIdsToCheck.Contains(ps.Id))
                            .AsNoTracking()
                            .ToDictionaryAsync(ps => ps.Id);

                        foreach (var Serial in Serials)
                        {
                            reservedSerialsById.TryGetValue(Serial.Id, out var checkIfReserved);
                            if (checkIfReserved.OrderId == null && OrderService.ReservedSerials.Count(r => r == Serial.Code) == 1)   // Perf #15: was Where(...).ToList().Count
                            {
                                Serial.OrderId = order.Id;

                                var ProductSerialHistory = new ProductSerialHistory
                                {

                                    ProductSerialId = Serial.Id,
                                    CreatedById = order.CreatedById,
                                    CreationDate = DateTimeOffset.Now,
                                    OldStatus = EProductSerialStatus.Available,
                                    NewStatus = EProductSerialStatus.Taken,
                                    OrderId = Serial.OrderId,
                                    Note = "Change To Taken Value for OrderId= " + Serial.OrderId.ToString() + " "

                                };
                                _productSerialHistory.Add(ProductSerialHistory);
                                await _productSerialHistory.SaveChangesAsync();
                                await productSerialRepository.SaveChangesAsync();

                            }
                            else
                            {

                                var NewSerial = await productSerialRepository.Query()
                                                                            .Where(ps => ps.Status == EProductSerialStatus.Available && ps.OrderId == null
                                                                                && ps.ProductId == Serial.ProductId)
                                                                            .FirstOrDefaultAsync();
                                NewSerial.OrderId = order.Id;
                                NewSerial.Status = EProductSerialStatus.Taken;

                                var ProductSerialHistory = new ProductSerialHistory
                                {

                                    ProductSerialId = NewSerial.Id,
                                    CreatedById = order.CreatedById,
                                    CreationDate = DateTimeOffset.Now,
                                    OldStatus = EProductSerialStatus.Available,
                                    NewStatus = EProductSerialStatus.Taken,
                                    OrderId = order.Id,
                                    Note = "Replaced insted of Serial " + Serial.Id + "Change To Taken Value for OrderId= " + order.Id.ToString() + " "

                                };
                                _productSerialHistory.Add(ProductSerialHistory);
                                await productSerialRepository.SaveChangesAsync();


                            }




                            await _productSerialHistory.SaveChangesAsync();

                            await productSerialRepository.SaveChangesAsync();

                        }

                        if (Serials != null)
                        {
                            foreach (var Serial in Serials)
                            {
                                OrderService.ReservedSerials.Remove(Serial.Code);
                            }
                        }
                    }

                }
            }


            return true;
        }

        public async Task<Result<Cart>> ReActiveCart(long cartId)
        {
            var cart = await _cartRepository
              .Query()
              .Where(x => x.Id == cartId).FirstOrDefaultAsync();
            cart.IsActive = true;
            await _cartRepository.SaveChangesAsync();
            return Result.Ok(cart);
        }

        public async Task<Result<Cart>> DeActiveCart(long cartId)
        {
            var cart = await _cartRepository
              .Query()
              .Where(x => x.Id == cartId).FirstOrDefaultAsync();
            cart.IsActive = false;
            await _cartRepository.SaveChangesAsync();
            return Result.Ok(cart);
        }

        public async Task<List<long>> CancelOrder(Order order)
        {
            try
            {

                order.OrderStatus = OrderStatus.Canceled;
                order.LatestUpdatedOn = DateTimeOffset.Now;
                List<long> ProductSerilaId = new List<long>();
                var orderItems = _orderItemRepository.Query().Include(x => x.Order).Include(x => x.Product).Where(x => x.Order.Id == order.Id).ToList();
                foreach (var item in orderItems)
                {
                    var OriginalProductId = item.ProductId;

                    var IsProductSerial = item.Product.IsProductSerial;

                    var OriginalProduct = await productLinkRepository.Query().Include(x => x.Product).Where(x => x.LinkedProductId == item.ProductId).FirstOrDefaultAsync();

                    if (OriginalProduct != null)
                    {

                        OriginalProductId = OriginalProduct.ProductId;

                        IsProductSerial = OriginalProduct.Product.IsProductSerial;
                    }

                    if (IsProductSerial)
                    {
                        var ProductSerialForOrder = await productSerialRepository.Query().Where(ps => ps.ProductId == OriginalProductId && ps.OrderId == order.Id).ToListAsync();

                        if (ProductSerialForOrder.Count > 0)
                        {
                            await productSerialRepository.SQLQuery(ManageStockQueries.MakeProductSerialAvaliable, order.Id);

                            ProductSerilaId = ProductSerialForOrder.Select(x => x.Id).ToList();

                        }

                    }
                    await _orderItemRepository.SaveChangesAsync();

                }
                return ProductSerilaId;

            }
            catch (Exception e)
            {

                throw;
            }

        }

        public async Task<decimal> GetTax(long cartId, string countryId, long? stateOrProvinceId, string zipCode)
        {
            decimal taxAmount = 0;
            var IsLocalCountry = await userSiteService.IsLocalCountry();

            var cartItems = _cartItemRepository.Query()
            .Where(x => x.CartId == cartId)
            .Select(x => new Areas.Orders.ViewModels.CartItemVm
            {
                Quantity = x.Quantity,
                Price = x.Price.Value,
                TaxClassId = x.Product.TaxClass.Id
            }).ToList();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.TaxClassId.HasValue)
                {
                    var taxRate = await _taxService.GetTaxPercent(cartItem.TaxClassId, countryId, stateOrProvinceId);
                    taxAmount = taxAmount + ((cartItem.Quantity * cartItem.Price) * (taxRate / 100));
                }
            }

            var result = _currencyService.DcimalRound(taxAmount);
            return result;
        }

        public async Task<OrderTaxAndShippingPriceVm> UpdateTaxAndShippingPrices(long cartId, TaxAndShippingPriceRequestVm model, long SiteId)
        {
            var cart = await _cartRepository.Query().FirstOrDefaultAsync(x => x.Id == cartId);
            if (cart == null)
            {
                throw new ApplicationException($"Cart id {cartId} not found");
            }

            Address address;
            if (model.ExistingShippingAddressId != 0)
            {
                address = await _userAddressRepository.Query().Include(x => x.Address).ThenInclude(x => x.StateOrProvince).Where(x => x.Id == model.ExistingShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();
                if (address == null)
                {
                    throw new ApplicationException($"Address id {model.ExistingShippingAddressId} not found");
                }
            }
            else
            {
                address = new Address
                {
                    CountryId = model.NewShippingAddress.CountryId,
                    StateOrProvinceId = model.NewShippingAddress.StateOrProvinceId,
                    DistrictId = model.NewShippingAddress.DistrictId,
                    ZipCode = model.NewShippingAddress.ZipCode,
                    AddressLine1 = model.NewShippingAddress.AddressLine1,
                };
            }

            var orderTaxAndShippingPrice = new OrderTaxAndShippingPriceVm
            {
                Cart = await _cartService.GetActiveCartDetails(cart.CustomerId, cart.CreatedById, model.CartType, SiteId)
            };
            var currentCart = await _cartService.GetActiveCart(cart.CustomerId, cart.CreatedById, model.CartType, SiteId);
            cart.TaxAmount = orderTaxAndShippingPrice.Cart.TaxAmount = await GetTax(cartId, address.CountryId, address.StateOrProvinceId, address.ZipCode);

            var request = new GetShippingPriceRequest
            {
                OrderAmount = orderTaxAndShippingPrice.Cart.OrderTotal,
                ShippingAddress = address,
                CartItems = orderTaxAndShippingPrice.Cart.Items
            };
            orderTaxAndShippingPrice.ShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(request, currentCart);

            var selectedShippingMethod = string.IsNullOrWhiteSpace(model.SelectedShippingMethodName)
                ? orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault()
                : orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault(x => x.Name == model.SelectedShippingMethodName);

            if (selectedShippingMethod != null)
            {
                cart.ShippingAmount = orderTaxAndShippingPrice.Cart.ShippingAmount = selectedShippingMethod.Price;
                cart.ShippingMethod = orderTaxAndShippingPrice.SelectedShippingMethodName = selectedShippingMethod.Name;

            
            if (await _couponService.QualifiesForFreeShipping(cart))
            {
                orderTaxAndShippingPrice.Cart.ShippingAmount = 0;
                foreach (var shippingPrice in orderTaxAndShippingPrice.ShippingPrices)
                {
                    shippingPrice.Price = 0;
                }
            }
        }

            await _cartRepository.SaveChangesAsync();
            return orderTaxAndShippingPrice;
        }

        public async Task SendOrderStatus(long orderId, OrderStatus oldStatus, OrderStatus newStatus)
        {
            long siteId = await _workContext.GetCurrentSiteIdAsync();
            Order order = await GetOrderById(orderId);
            var settings = await _SiteSettings.GetSiteSettingsByModule("OrderStatusTemplates", siteId);
            string SMSOrderStatusTemplateId = null,
             EmailOrderStatusTemplateId = null;
            if (settings.Count > 0)
            {

                SMSOrderStatusTemplateId = settings.FirstOrDefault(x => x.Id == "SMSOrderStatusTemplateId")?.Value;
                EmailOrderStatusTemplateId = settings.FirstOrDefault(x => x.Id == "EmailOrderStatusTemplateId")?.Value;

            }
            var Parameters = new List<EmailParamsVM>();
            Parameters.Add(new EmailParamsVM
            {
                Param = "[OldStatus]",
                Value = oldStatus.GetDisplayName()
            });

            Parameters.Add(new EmailParamsVM
            {
                Param = "[NewStatus]",
                Value = newStatus.GetDisplayName()
            });

            Parameters.Add(new EmailParamsVM
            {
                Param = "[name]",
                Value = order.Customer.FullName
            });

            Parameters.Add(new EmailParamsVM
            {
                Param = "[Date]",
                Value = order.CreatedOn.ToString()
            });

            Parameters.Add(new EmailParamsVM
            {
                Param = "[OrderId]",
                Value = order.Id.ToString()
            });

            Parameters.Add(new EmailParamsVM
            {
                Param = "[OrderTotal]",
                Value = order.OrderTotal.ToString() + " " + order.CurrencyCode.ToString()
            });

            NotificationObject notificationObject = new NotificationObject
            {
                MessageBody = string.Empty,
                MessageTitle = "Order Status",
                SendVia = SendBy.MobileNumber,
                SendValue = new List<string> { order.Customer?.PhoneNumber },
                NotificationType = NotificationType.SMS,
                SMSProvider = SMSProviders.SMSUniFonic,
                EmailOptions = new EmailSenderVM
                {
                    Emails = new List<string>(),
                    TemplateId = SMSOrderStatusTemplateId,
                    Params = Parameters
                },
                EmailTemplateId = "",

            };



            if (!string.IsNullOrEmpty(order.Customer?.PhoneNumber) && !string.IsNullOrEmpty(SMSOrderStatusTemplateId))
            {
                await _testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);

            }

            notificationObject.SendVia = SendBy.Email;
            notificationObject.SendValue = new List<string> { order.Customer?.Email };
            notificationObject.NotificationType = NotificationType.Email;
            notificationObject.EmailOptions.TemplateId = EmailOrderStatusTemplateId;
            if (!string.IsNullOrEmpty(order.Customer?.Email) && !string.IsNullOrEmpty(EmailOrderStatusTemplateId))
            {
                await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

            }

        }

        public async Task SendReceipt(long orderId, bool sendInBackground = false)
        {
            try
            {


                long siteId = await _workContext.GetCurrentSiteIdAsync();

                Order order = await GetOrderById(orderId);

                var Shipment = order.Shipment.FirstOrDefault();


                var CultureId = _workContext.GetCurrentCultureId();

                var Culture = siteCultureService.GetCultureById(CultureId);

                var settings = await _SiteSettings.GetSiteSettingsByModule("OrderReceiptTemplates", siteId);

                var NotificationType = EskaCommerce.Notifications.Areas.Notifications.ViewModels.NotificationType.BothSMSEmail;

                List<string> AdminEmails = new List<string>();
                var (adminReceiptEmailTemplateId, smsReceiptTemplateId, emailReceiptTemplateId,
                deliveryReceiptEmailTemplate, designReceiptItems, DownloadInvoice, notificationType) = GetSettingsValues(settings, Culture.Culture);

                AdminEmails = GetAdminEmails(settings);

                var Parameters = new List<EmailParamsVM>();
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[SubtotalString]",
                    Value = _currencyService.FormatByUserCurrency(order.SubTotal)
                });


                Parameters.Add(new EmailParamsVM
                {
                    Param = "[name]",
                    Value = order.Customer.FullName
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[phonenumber]",
                    Value = order.ShippingAddressId.HasValue ? (string.IsNullOrEmpty(order.ShippingAddress.Phone) ? order.Customer.PhoneNumber : order.ShippingAddress.Phone) : order.Customer.PhoneNumber
                });
                var allKeys = await localizationServices.GetLocalization(string.Empty, siteId, CultureId);

                var ShipmentType = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.Cart.ShippingDelivery.GetDescription()).FirstOrDefault().Value;

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[ShipmentType]",
                    Value = string.IsNullOrEmpty(ShipmentType) ? order.Cart.ShippingDelivery.GetDescription() : ShipmentType
                });



                Parameters.Add(new EmailParamsVM
                {
                    Param = "[DiscountAmount]",
                    Value = _currencyService.FormatByUserCurrency(order.DiscountAmount)
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[EncryptedOrderId]",
                    Value = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderId.ToString()))
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[OrderId]",
                    Value = order.Id.ToString()
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Date]",
                    Value = order.CreatedOn.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[TotalFees]",
                    Value = _currencyService.FormatByUserCurrency(order.PaymentFeeAmount)
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[BranchName]",
                    Value = order.Warehouse.Name
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[ProductName]",
                    Value = string.Join(',', order.OrderItems.SelectMany(x => x.Product.ProductDetails).Where(x => x.CultureId == CultureId).Select(x => x.Name).ToList())
                });

                var PaymentMethod = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.PaymentMethod.ToString()).FirstOrDefault().Value;

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[PaymentMethod]",
                    Value = string.IsNullOrEmpty(PaymentMethod) ? order.PaymentMethod.ToString() : PaymentMethod
                });
                StringBuilder Items = new StringBuilder();
                var IsLocalCountry = await userSiteService.IsLocalCountry();

                long Id = 1;
                foreach (var item in order.OrderItems)
                {
                    var Quentity = item.Quantity;

                    var OrderId = orderId;

                    var ProductName = item.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault();

                    var customizeReceiptItemsStyle = designReceiptItems;

                    customizeReceiptItemsStyle = customizeReceiptItemsStyle.Replace("ItemNumber", Id.ToString())
                                        .Replace("ProductName", ProductName.ToString() + " " + item.Product?.OptionCombinations?
                                                    .SelectMany(o => o.Value?.productOptionValueDetails)
                                                    .Where(d => d.CultureId == CultureId)
                                                    .Select(d => d.Value).FirstOrDefault()
                                            )
                                        .Replace("Quentity", Quentity.ToString())
                                        .Replace("ProductPrice", _currencyService.FormatByUserCurrency(item.ProductPrice))
                                        .Replace("[PATH]", item.Product.ThumbnailImage == null ? "" : item.Product.ThumbnailImage.PublicUrl);
                    Items.Append(customizeReceiptItemsStyle + System.Environment.NewLine);
                    Id++;
                }

                if (order.PlansVersionId.HasValue)
                {

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RepeatAmount]",
                        Value = order.PlansVersion.RepeatAmount.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RepeatInterval]",
                        Value = order.PlansVersion.RepeatInterval.ToString()
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RepeatPeriod]",
                        Value = order.PlansVersion.RepeatPeriod.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RepeatStart]",
                        Value = order.PlansVersion.RepeatStart.ToString()
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RepeatFinal]",
                        Value = order.PlansVersion.RepeatFinal.ToString()
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RecurringType]",
                        Value = order.PlansVersion.Plan.RecurringType.GetDisplayName()
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[PlanType]",
                        Value = order.PlansVersion.Plan.PlanType.GetDisplayName()
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[PlanName]",
                        Value = order.PlansVersion.Plan.PlanDetails.Select(x => x.Name).FirstOrDefault()
                    });


                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[UserPlanId]",
                        Value = order.UserPlans.Count() == 0 ? "" : HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(order.UserPlans.Select(x => x.Id.ToString()).FirstOrDefault()))
                    });


                }
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Items]",
                    Value = Items.ToString()
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[OrderCurrencyCode]",
                    Value = order.CurrencyCode.ToString()
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[RedeemAmount]",
                    Value = order.RedeemAmount.ToString()
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[OrderTotal]",
                    Value = _currencyService.FormatByUserCurrency(order.OrderTotal)
                });

                var hasShippingDiscount = order.ShippingDiscountAmount > 0;
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Shipping-Fees]",
                    Value = _currencyService.FormatByUserCurrency(order.ShippingFeeAmount)
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Shipping-Fees-Before]",
                    Value = hasShippingDiscount
                      ? _currencyService.FormatByUserCurrency(order.ShippingFeeAmount + order.ShippingDiscountAmount)
                       : string.Empty
                });
                Parameters.Add(new EmailParamsVM
                { 
                    Param = "[Shipping-Discount]",
                    Value = hasShippingDiscount
                        ? _currencyService.FormatByUserCurrency(order.ShippingDiscountAmount)
                       : string.Empty
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[CouponCode]",
                    Value = order.CouponCode
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[CategoryName]",
                    Value = GetCategoryName(order.OrderItems.Select(x => x.Product).ToList(), CultureId)
                });

                var ReservedProductSerials = productSerialRepository.Query().Where(x => x.OrderId == orderId).ToList();

                string productSerialStrings = "";

                foreach (var ReservedSerial in ReservedProductSerials)
                {
                    productSerialStrings += ReservedSerial.Code + " <br> ";
                }

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[productSerialNumbers]",
                    Value = productSerialStrings
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[ProductSerialsString]",
                    Value = productSerialStrings
                });

                if (Shipment != null)
                {

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[LabelUrl]",
                        Value = Shipment.LabelUrl
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[TrackingNumber]",
                        Value = Shipment.TrackingNumber
                    });
                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[Warehouse]",
                        Value = Shipment.Warehouse.Name
                    });


                }

                var phoneNumber = !string.IsNullOrEmpty(order.Customer?.PhoneNumber) ? order.Customer?.PhoneNumber : order.ShippingAddress?.Phone;
                NotificationObject notificationObject = new NotificationObject
                {
                    MessageBody = string.Empty,
                    MessageTitle = "Order Receipt",
                    SendVia = SendBy.MobileNumber,
                    SendValue = new List<string> { phoneNumber },
                    NotificationType = NotificationType.SMS,

                    EmailOptions = new EmailSenderVM
                    {
                        Emails = new List<string>(),
                        TemplateId = smsReceiptTemplateId,
                        Params = Parameters
                    },
                    EmailTemplateId = adminReceiptEmailTemplateId,

                };

                var customerEmail = order.Customer?.Email;
                var isAramexShipment = Shipment != null && Shipment.ShippingDelivery == DeliveryOptionEnum.Aramex;

                async Task SendMessages(ITestNotifier testNotifier, IdcmsLogService logService)
                {
                    try
                    {

                        if (!string.IsNullOrEmpty(phoneNumber))
                        {
                            if (notificationType == NotificationType.SMS || notificationType == NotificationType.BothSMSEmail)
                            {
                                await testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);
                            }
                        }

                        notificationObject.SendVia = SendBy.Email;
                        notificationObject.SendValue = new List<string> { customerEmail };
                        notificationObject.NotificationType = NotificationType.Email;


                        if (isAramexShipment)
                        {
                            notificationObject.EmailOptions.TemplateId = deliveryReceiptEmailTemplate;
                        }
                        else
                        {
                            notificationObject.EmailOptions.TemplateId = emailReceiptTemplateId;
                        }
                        if (!string.IsNullOrEmpty(customerEmail))
                        {
                            if (notificationType == NotificationType.Email || notificationType == NotificationType.BothSMSEmail)
                            {
                                await testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);
                            }
                        }


                        if (AdminEmails.Count > 0 && (notificationType == NotificationType.BothSMSEmail || notificationType == NotificationType.Email))
                        {
                            notificationObject.SendValue = AdminEmails;
                            notificationObject.EmailOptions.TemplateId = adminReceiptEmailTemplateId;
                            await testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

                        }

                    }
                    catch (Exception ex)
                    {

                        await logService.AddError("SendReceipt Form Emails", ex.InnerException?.Message, ex.Message, null, siteId);
                    }
                }

                var httpContext = _httpContextAccessor?.HttpContext;

                if (!sendInBackground || httpContext == null)
                {
                    await SendMessages(_testNotifier, dcmsLogService);
                    return;
                }

                // Send the SMS/e-mails after the response instead of making the customer wait for them.
                // The receipt above is still built inside this request, so its content does not change.
                // The sending gets its own DI scope (this request's DbContext is released when the response ends)
                // and a copy of the request headers/user, because the notifier reads site, user and source
                // through IWorkContext.
                var scopeFactory = httpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
                var requestHeaders = httpContext.Request.Headers.ToList();
                var requestUser = httpContext.User;
                var culture = CultureInfo.CurrentCulture;
                var uiCulture = CultureInfo.CurrentUICulture;

                // SuppressFlow: assigning IHttpContextAccessor.HttpContext on a flowed ExecutionContext would
                // clear the HttpContext of this still-running request.
                using (ExecutionContext.SuppressFlow())
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await using var scope = scopeFactory.CreateAsyncScope();

                            CultureInfo.CurrentCulture = culture;
                            CultureInfo.CurrentUICulture = uiCulture;

                            var backgroundHttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = requestUser };
                            foreach (var header in requestHeaders)
                            {
                                backgroundHttpContext.Request.Headers[header.Key] = header.Value;
                            }

                            // Must be set before resolving anything that injects IWorkContext.
                            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = backgroundHttpContext;

                            await SendMessages(
                                scope.ServiceProvider.GetRequiredService<ITestNotifier>(),
                                scope.ServiceProvider.GetRequiredService<IdcmsLogService>());
                        }
                        catch
                        {
                            // Nothing may escape a detached task; SendMessages already logs send failures.
                        }
                    });
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Overwrites the inbound "cultureId" request header so that anything downstream which
        /// resolves culture through IWorkContext.GetCurrentCultureId() — which reads it from that
        /// header and otherwise yields 0 — sees the culture this receipt is being rendered in.
        ///
        /// OrderService is a plain service, not a Controller, so there is no inherited HttpContext
        /// property; the context has to come from IHttpContextAccessor. It is null whenever this
        /// runs outside a request (background job, detached scope), in which case there are no
        /// headers to override and the caller's explicit CultureId is already being passed along.
        /// </summary>
        private void AddCustomHeaders(long CultureId)
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            if (httpContext == null)
                return;

            httpContext.Request.Headers["cultureId"] = CultureId.ToString();
        }
		public async Task<EskaCMS.Infrastructure.Helpers.DinToPdf.PdfFile> PrintReceipt(long orderId, long? cultureId)
        {
            try
            {


                long siteId = await _workContext.GetCurrentSiteIdAsync();

                var plattform = _workContext.GetCurrentPlatform();

                Order order = await GetOrderById(orderId);

                var Shipment = order.Shipment.FirstOrDefault();





                var CultureId = cultureId.HasValue ? cultureId.Value : _workContext.GetCurrentCultureId();

                AddCustomHeaders(CultureId);

				var Culture = siteCultureService.GetCultureById(CultureId);

                var settings = await _SiteSettings.GetSiteSettingsByModule("OrderReceiptTemplates", siteId);


                var allKeys = await localizationServices.GetLocalization(string.Empty, siteId, CultureId);



                var (adminReceiptEmailTemplateId, smsReceiptTemplateId, emailReceiptTemplateId,
                deliveryReceiptEmailTemplate, designReceiptItems, DownloadInvoice, notificationType) = GetSettingsValues(settings, Culture.Culture);



                var Parameters = new List<PdfData>();
                Parameters.Add(new PdfData
                {
                    Key = "[SubtotalString]",
                    Value = _currencyService.FormatByUserCurrency(order.SubTotal)
                });


                Parameters.Add(new PdfData
                {
                    Key = "[name]",
                    Value = order.Customer.FullName
                });
                var ShipmentType = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.Cart.ShippingDelivery.GetDescription()).FirstOrDefault().Value;

                Parameters.Add(new PdfData
                {
                    Key = "[ShipmentType]",
                    Value = string.IsNullOrEmpty(ShipmentType) ? order.Cart.ShippingDelivery.GetDescription() : ShipmentType
                });


                Parameters.Add(new PdfData
                {
                    Key = "[DiscountAmount]",
                    Value =_currencyService.FormatByUserCurrency(order.DiscountAmount)
                });
                        var hasShippingDiscount = order.ShippingDiscountAmount > 0;
               Parameters.Add(new PdfData
                {
                    Key = "[Shipping-Fees]",
                    Value = _currencyService.FormatByUserCurrency(order.ShippingFeeAmount)
               });
               Parameters.Add(new PdfData
                {
                  Key = "[Shipping-Fees-Before]",
                   Value = hasShippingDiscount
                       ? _currencyService.FormatByUserCurrency(order.ShippingFeeAmount + order.ShippingDiscountAmount)
                       : string.Empty
                });
                Parameters.Add(new PdfData
                {
                    Key = "[Shipping-Discount]",  
                    Value = hasShippingDiscount
                       ? _currencyService.FormatByUserCurrency(order.ShippingDiscountAmount)
                       : string.Empty
                });

                Parameters.Add(new PdfData
                {
                    Key = "[CouponCode]",
                    Value = order.CouponCode
                });
                Parameters.Add(new PdfData
                {
                    Key = "[EncryptedOrderId]",
                    Value = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderId.ToString()))
                }); ;

                Parameters.Add(new PdfData
                {
                    Key = "[TotalFees]",
                    Value = _currencyService.FormatByUserCurrency(order.PaymentFeeAmount)
                }); ;
                Parameters.Add(new PdfData
                {
                    Key = "[OrderId]",
                    Value = order.Id.ToString()
                });

                Parameters.Add(new PdfData
                {
                    Key = "[Date]",
                    Value = order.CreatedOn.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")
                });


                Parameters.Add(new PdfData
                {
                    Key = "[ProductName]",
                    Value = string.Join(',', order.OrderItems.SelectMany(x => x.Product.ProductDetails).Where(x => x.CultureId == CultureId).Select(x => x.Name).ToList())
                });

                var PaymentMethod = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.PaymentMethod.ToString()).FirstOrDefault().Value;

                Parameters.Add(new PdfData
                {
                    Key = "[PaymentMethod]",
                    Value = string.IsNullOrEmpty(PaymentMethod) ? order.PaymentMethod.ToString() : PaymentMethod
                });


                StringBuilder Items = new StringBuilder();

                var IsLocalCountry = await userSiteService.IsLocalCountry();

                long Id = 1;

                foreach (var item in order.OrderItems)
                {
                    var Quentity = item.Quantity;

                    var OrderId = orderId;

                    var ProductName = item.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault();

                    var customizeReceiptItemsStyle = designReceiptItems;

                    customizeReceiptItemsStyle = customizeReceiptItemsStyle.Replace("ItemNumber", Id.ToString())
                                        .Replace("ProductName", ProductName.ToString()+ " " +  item.Product?.OptionCombinations?
                                                    .SelectMany(o => o.Value?.productOptionValueDetails)
                                                    .Where(d => d.CultureId==CultureId)
                                                    .Select(d => d.Value).FirstOrDefault())
                                        .Replace("Quentity", Quentity.ToString())
                                        .Replace("ProductPrice", _currencyService.FormatByUserCurrency(item.ProductPrice))
                                        .Replace("[PATH]", item.Product.ThumbnailImage == null ? "" : item.Product.ThumbnailImage.PublicUrl);

                    Items.Append(customizeReceiptItemsStyle + System.Environment.NewLine);

                    Id++;
                }

                if (order.PlansVersionId.HasValue)
                {

                    Parameters.Add(new PdfData
                    {
                        Key = "[RepeatAmount]",
                        Value = order.PlansVersion.RepeatAmount.ToString()
                    });

                    Parameters.Add(new PdfData
                    {
                        Key = "[RepeatInterval]",
                        Value = order.PlansVersion.RepeatInterval.ToString()
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[RepeatPeriod]",
                        Value = order.PlansVersion.RepeatPeriod.ToString()
                    });

                    Parameters.Add(new PdfData
                    {
                        Key = "[RepeatStart]",
                        Value = order.PlansVersion.RepeatStart.ToString()
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[RepeatFinal]",
                        Value = order.PlansVersion.RepeatFinal.ToString()
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[RecurringType]",
                        Value = order.PlansVersion.Plan.RecurringType.ToString()
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[PlanType]",
                        Value = order.PlansVersion.Plan.PlanType.ToString()
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[PlanName]",
                        Value = order.PlansVersion.Plan.PlanDetails.Select(x => x.Name).FirstOrDefault()
                    });

                    //if(order.PlansVersion.Plan)
                }




                Parameters.Add(new PdfData
                {
                    Key = "[Items]",
                    Value = Items.ToString()
                });



                Parameters.Add(new PdfData
                {
                    Key = "[OrderCurrencyCode]",
                    Value = order.CustomerCurrencyOrderTotal.ToString()
                });

                Parameters.Add(new PdfData
                {
                    Key = "[RedeemAmount]",
                    Value = order.RedeemAmount.ToString()
                });

                Parameters.Add(new PdfData
                {
                    Key = "[OrderTotal]",
                    Value = _currencyService.FormatByUserCurrency(order.OrderTotal)
                });
                Parameters.Add(new PdfData
                {
                    Key = "[PaymentMethod]",
                    Value = order.PaymentMethod.ToString()
                });

                Parameters.Add(new PdfData
                {
                    Key = "[CategoryName]",
                    Value = GetCategoryName(order.OrderItems.Select(x => x.Product).ToList(), CultureId)
                });

                var ReservedProductSerials = productSerialRepository.Query().Where(x => x.OrderId == orderId).ToList();

                string productSerialStrings = "";

                foreach (var ReservedSerial in ReservedProductSerials)
                {
                    productSerialStrings += ReservedSerial.Code + " <br> ";
                }

                Parameters.Add(new PdfData
                {
                    Key = "[productSerialNumbers]",
                    Value = productSerialStrings
                });

                Parameters.Add(new PdfData
                {
                    Key = "[ProductSerialsString]",
                    Value = productSerialStrings
                });

                if (Shipment != null)
                {

                    Parameters.Add(new PdfData
                    {
                        Key = "[LabelUrl]",
                        Value = Shipment.LabelUrl
                    });

                    Parameters.Add(new PdfData
                    {
                        Key = "[TrackingNumber]",
                        Value = Shipment.TrackingNumber
                    });
                    Parameters.Add(new PdfData
                    {
                        Key = "[Warehouse]",
                        Value = Shipment.Warehouse.Name
                    });


                }

                var emailTemplate = new EmailTemplateVM();

                if (Shipment != null && Shipment.ShippingDelivery == DeliveryOptionEnum.Aramex)
                {
                    emailTemplate = await _emailTemplateService.GetEmailTemplateById(deliveryReceiptEmailTemplate);
                }
                else
                {
                    emailTemplate = await _emailTemplateService.GetEmailTemplateById(DownloadInvoice);
                }

                if (emailTemplate is null)
                {
                    throw new Exception("Please add template for receipt");
                }
                var Invoice = await _ipdfConverter.Convert(emailTemplate.EmailBody, Parameters, "Invoice");

                return Invoice;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> SendFailedShipmentNotification(Order order)
        {
            try
            {

                var siteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                var Culture = siteCultureService.GetCultureById(CultureId);
                var settings = await _SiteSettings.GetSiteSettingsByModule("OrderReceiptTemplates", siteId);
                var allKeys = await localizationServices.GetLocalization(string.Empty, siteId, CultureId);

                var smsTemplate = new SiteSettingsVM();

                if (Culture.Culture.Contains("en"))
                {
                    smsTemplate = settings.Where(x => x.Id == "FailedShipmentTemplateId").FirstOrDefault();
                }
                else
                {

                    smsTemplate = settings.Where(x => x.Id == "FailedShipmentTemplateIdArabic").FirstOrDefault();

                }
                if (smsTemplate is null)
                {
                    await dcmsLogService.AddError($"Failed shipment", "Failed to find  template for failed shipment", "Failed to find sms template", null, siteId);
                    return true;
                }
                var smsReceiptTemplateId = smsTemplate.Value;




                var Parameters = new List<EmailParamsVM>();


                Parameters.Add(new EmailParamsVM
                {
                    Param = "[name]",
                    Value = order.Customer.FullName
                });
                var ShipmentType = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.Cart.ShippingDelivery.GetDisplayName()).FirstOrDefault().Value;

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[ShipmentType]",
                    Value = string.IsNullOrEmpty(ShipmentType) ? order.Cart.ShippingDelivery.GetDisplayName() : ShipmentType
                });

                NotificationObject notificationObject = new NotificationObject
                {
                    MessageBody = string.Empty,
                    MessageTitle = "Failed Shipment",
                    SendVia = SendBy.MobileNumber,
                    SendValue = new List<string> { order.Customer?.PhoneNumber },
                    NotificationType = NotificationType.SMS,
                    EmailOptions = new EmailSenderVM
                    {
                        Emails = new List<string>(),
                        TemplateId = smsReceiptTemplateId,
                        Params = Parameters
                    },

                };



                if (!string.IsNullOrEmpty(order.Customer?.PhoneNumber))
                {

                    await _testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);

                }

                return true;



            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> UpdateOrderCustomer(long CustomerId)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            long FromUserId = await _workContext.GetCurrentUserId();

            var Order = await _orderRepository.Query()
                .Where(x => x.CustomerId == FromUserId)
                .Include(o => o.OrderItems)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync();

            Order.CustomerId = CustomerId;
            await _orderRepository.SaveChangesAsync();
            return true;

        }

        public async Task<Order> GetOrderById(long id)
        {
            try
            {

                return await _orderRepository.Query().Where(x => x.Id == id).Include(x => x.OrderItems)
                                    .Include("OrderItems.Product")
                                    .Include("OrderItems.Product")
                                    .Include("OrderItems.Product.ProductDetails").Include("OrderItems.Product.Categories")
                                    .Include("OrderItems.Product.Categories.Category")
                                    .Include("OrderItems.Product.ThumbnailImage")
                                    .Include("OrderItems.Product.Categories.Category")
                                    .Include("OrderItems.Product.Categories.Category.CategoryDetails")
                                    .Include(x => x.ShippingAddress)
                                    .ThenInclude(x => x.StateOrProvince)
                                    .Include(x => x.Cart)
                                    .ThenInclude(x => x.Items)
                                    .Include(x => x.CreatedBy)
                                    .Include(o => o.Customer)
                                    .Include(x => x.PlansVersion)
                                    .ThenInclude(x => x.Plan)
                                    .ThenInclude(x => x.PlanDetails)
                                    .Include(x => x.Warehouse)
                                    .Include(x => x.Shipment)
                                    .Include(x => x.UserPlans)
                                    .Include(x => x.ShippingAddress)
                                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(x => x.Value).ThenInclude(x => x.productOptionValueDetails)
                                    .FirstOrDefaultAsync();

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<OrderDetailVm> GetOrderDetailsById(long id)
        {
            try
            {


                var r = new Regex(@"
                        (?<=[A-Z])(?=[A-Z][a-z]) |
                         (?<=[^A-Z])(?=[A-Z]) |
                         (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                var SiteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var CultureId = _workContext.GetCurrentCultureId();
                var CurrncyDecimal = Convert.ToInt32(_currencyService.GetDefaultDecimalPlaces(SiteId));
                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var RoundDigit = Convert.ToInt32(Math.Pow(10, Convert.ToInt32(_currencyService.GetDefaultRoundDigit())));
                var order = await _orderRepository
                    .Query()
                    .AsNoTracking()
                    .Include(x => x.ProductSerials)
                    .Include(x => x.ShippingAddress)
                    .Include("ShippingAddress.District")
                    .Include("ShippingAddress.District.DistrictDetails")
                    .Include("ShippingAddress.StateOrProvince")
                    .Include("ShippingAddress.StateOrProvince.StateOrProvinceDetails")
                    .Include("ShippingAddress.Country")
                    .Include("ShippingAddress.Country.CountryDetails")
                    .Include(x => x.Cart)
                    .ThenInclude(x => x.Items)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderItems)
                    .Include(x => x.OrderAdditionalFees)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .Include(x => x.Shipment)
                    .ThenInclude(x => x.Pickup)
                    .Include("OrderItems.Product")
                    .Include("OrderItems.Product.ProductDetails")
                    .Include("OrderItems.Product.TaxClass")
                    .Include("OrderItems.Product.TaxClass.TaxRate")
                    .Include("OrderItems.Product.ProductSerial")
                    .Include("OrderItems.Product.OptionCombinations")
                    .Include("OrderItems.Product.OptionCombinations.Value")
                    .Include("OrderItems.Product.OptionCombinations.Value.productOptionValueDetails")
                    .Include("OrderItems.Product.OptionCombinations.Option")
                    .Include("OrderItems.Product.OptionCombinations.Option.optionDetails")
                    .Include("OrderItems.Product.ThumbnailImage")
                    .Include("OrderItems.Product.OptionCombinations")
                    .Include("OrderItems.Product.OptionCombinations.Option")
                    .Include("OrderItems.Product.AttributeValues")
                    .Include("OrderItems.Product.AttributeValues.AttributeValueDetails")
                    .Include("OrderItems.Product.AttributeValues.Attribute")
                    .Include("OrderItems.Product.AttributeValues.Attribute.AttributeDetails")
                    .Include(x => x.Warehouse)
                    .ThenInclude(x => x.priceAndDestinations)
                    .Where(x => x.Id == id)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    throw new Exception("No Action will be taken");
                }
                var allKeys = await localizationServices.GetLocalization(string.Empty, SiteId, CultureId);

                var OrderStatus = allKeys.Where(x => x.Key == "SharedResources").FirstOrDefault().Value.Where(x => x.Key == order.OrderStatus.GetDisplayName()).FirstOrDefault().Value;


                var model = new OrderDetailVm(_currencyService);
                var product = order.OrderItems.Select(X => X.Product).FirstOrDefault();
                model.Id = order.Id;
                model.IsMasterOrder = order.IsMasterOrder;
                model.CreatedOn = order.CreatedOn;
                model.OrderStatus = order.OrderStatus;
                model.OrderStatusString = string.IsNullOrEmpty(OrderStatus) ? order.OrderStatus.GetDisplayName() : OrderStatus;
                model.CustomerId = order.CustomerId;
                model.CustomerName = order.Customer.FullName;
                model.CustomerEmail = order.Customer.Email;
                model.PhoneNumber = order.Customer.PhoneNumber;
                model.ShippingMethod = order.ShippingMethod;
                model.PaymentMethod = order.PaymentMethod;
                model.PaymentMethod = order.PaymentMethod;
                model.PaymentFeeAmount = order.PaymentFeeAmount;
                model.Subtotal = order.SubTotal;
                model.ShippingAmount =order.ShippingFeeAmount;
                model.ShippingDiscountAmount = (decimal)order.ShippingDiscountAmount;
                model.RedeemAmount = order.RedeemAmount;
                model.Channel = order.Channel;
                model.SiteId = order.SiteId;
                model.SiteId = order.SiteId;
                model.AdditionalDetails = order.Cart.Items.Select(x=>x.AdditionalDetails).FirstOrDefault();
                model.CartId = order.CartId;
                model.ShipmentId = order.Shipment.Count() > 0 ? order.Shipment.Select(i => i.Id).FirstOrDefault() : 0;
                model.TrackingNumber = order.Shipment.Count() > 0 ? order.Shipment.Select(i => i.TrackingNumber).FirstOrDefault() : "";
                model.ShippingDelivery = order.Shipment.Count() > 0 ? order.Shipment.Select(i => i.ShippingDelivery).FirstOrDefault() : 0;
                model.PickupId = order.Shipment.Count() > 0 ? order.Shipment.Select(i => i.PickupId).FirstOrDefault() : 0;
                model.DiscountAmount = order.DiscountAmount;
                model.SubTotalWithDiscount = order.SubTotalWithDiscount;
                model.TaxAmount = Convert.ToDecimal(string.Format("{0:N" + CurrncyDecimal + "}", Math.Round(order.TaxAmount * RoundDigit, MidpointRounding.AwayFromZero) / RoundDigit));
                model.OrderTotal = order.OrderTotal;
                model.OrderNote = order.OrderNote;
                model.OrderReferanceId = order.OrderReferanceId;
                model.OrderCurrencyCode = order.CurrencyCode;
                model.CouponCode = order.CouponCode;
                model.EstimatedDeliveryDate = GetEstimatedDeliveryDate(order.Warehouse, order.ShippingAddress, order.Shipment.Count() > 0 ? order.Shipment.Select(i => i.ShippingDelivery).FirstOrDefault() : 0);
                model.WareHouseName = order.Warehouse != null ? order.Warehouse.Name : null;
                model.OrderAdditionalFees = order.OrderAdditionalFees == null ? new List<OrderAdditionalFeesVM>() : order.OrderAdditionalFees.Select(o => new OrderAdditionalFeesVM
                {
                    Amount = o.Amount,
                    Status = o.Status,
                    Note = o.Note,
                    ProductName = o.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault()
                }).ToList();

                model.TotalFees =_currencyService.FormatByCurrentCurrency(order.PaymentFeeAmount);
                model.ProductSerial = order.OrderItems == null ? new ProductSerialViewModel() : order.OrderItems.Select(x => x.Product).Select(x => new ProductSerialViewModel
                {

                    PlanName = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                    ProductSerialNumbers = x.ProductSerial != null
                   ? x.ProductSerial.Where(x => x.OrderId == order.Id).Select(x => x.Id).ToList() == null ? new List<string>() : order.ProductSerials.Select(x => x.Code).ToList()
                   : new List<string>()
                }).FirstOrDefault();
                model.ShippingAddress = order.ShippingAddress == null ? new ShippingAddressVm() : new ShippingAddressVm
                {
                    AddressLine1 = order.ShippingAddress.AddressLine1,
                    CityName = order.ShippingAddress.City,
                    ZipCode = order.ShippingAddress.ZipCode,
                    ContactName = order.ShippingAddress.ContactName != null ? order.ShippingAddress.ContactName : order.Customer.FullName,
                    DistrictName = order.ShippingAddress.District == null ? "" : order.ShippingAddress.District.DistrictDetails.Where(x => CultureId == 0 || CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    StateOrProvinceName = order.ShippingAddress.StateOrProvince == null ? "" : order.ShippingAddress.StateOrProvince.StateOrProvinceDetails.Where(x => CultureId == 0 || CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    Phone = order.ShippingAddress.Phone != null ? order.ShippingAddress.Phone : order.Customer.PhoneNumber,
                    CountryId = order.ShippingAddress.CountryId
                };
                model.PaymentStatus=await paymentRepository.Query().AsNoTracking().Where(x => x.OrderId==id).OrderByDescending(x => x.Id).Select(x => x.Status).FirstOrDefaultAsync();
                model.OrderItems = order.OrderItems == null ? new List<OrderItemVm>() : order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                    ProductPrice = Convert.ToDecimal(string.Format("{0:N" + CurrncyDecimal + "}", x.ProductPrice)),

                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    ProductImage = x.Product.ThumbnailImage == null ? "" : x.Product.ThumbnailImage.PublicUrl,
                    TaxAmount = Convert.ToDecimal(string.Format("{0:N" + CurrncyDecimal + "}", Math.Round(x.TaxAmount * RoundDigit, MidpointRounding.AwayFromZero) / RoundDigit)),
                    TaxPercent = x.TaxPercent,
                    AdditionalDetails = x.AdditionalDetails,
                    OrderCurrencyCode = order.CurrencyCodeCustomer,
                    QrCode = x.QrCode,
                    VariationOptions = MapProductOptionToProductVm(x.Product, CultureId),
                    Attributes = x.Product.AttributeValues?.Select(av =>
                        new ItemDetailAttribute
                        {
                            Name = av.Attribute?.AttributeDetails?.Where(avd => avd.CultureId == CultureId)
                                .Select(avd => avd.Name)
                                .FirstOrDefault() ?? "",
                            Value = av.AttributeValueDetails
                                .Where(avd => avd.CultureId == CultureId)
                                .Select(avd => avd.Value)
                                .FirstOrDefault()
                        }).ToList() ?? new List<ItemDetailAttribute>()
                }).ToList();


                if (order.IsMasterOrder)
                {
                    model.SubOrderIds = _orderRepository.Query().Where(x => x.ParentId == order.Id).Select(x => x.Id).ToList();
                }
                if (order.OrderStatus == EskaCMS.Entities.Entity.OrderStatus.PaymentFailed)
                {
                    model.ShippingDelivery = DeliveryOptionEnum.None;
                }
                else model.ShippingDelivery = order.Cart?.ShippingDelivery??DeliveryOptionEnum.None;

                    return model;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public int? GetEstimatedDeliveryDate(Warehouse? warehouse, OrderAddress? address, DeliveryOptionEnum deliveryOption)
        {
            try
            {
                if (deliveryOption != DeliveryOptionEnum.Aramex && deliveryOption != DeliveryOptionEnum.Pickup)
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



        public async Task OrderChangeStatus(long id, OrderStatus StatusId, string Note, long SiteId, bool? FromApi = false)
        {
            try
            {

                var ReserveSerialsAfterPayment = await _SiteSettings.GetSiteSettingById("ReserveSerialsAfterPayment", await _workContext.GetCurrentSiteIdAsync());


                var order = _orderRepository
                              .Query()
                              .Where(x => x.Id == id)
                              .Include(x => x.Customer)
                              .Include(x => x.BillingAddress)
                              .Include(d => d.ProductSerials)
                              .Include(x => x.ShippingAddress)
                              .Include("ShippingAddress.District")
                              .Include("ShippingAddress.District.DistrictDetails")
                              .Include("ShippingAddress.StateOrProvince")
                              .Include("ShippingAddress.StateOrProvince.StateOrProvinceDetails")
                              .Include("ShippingAddress.Country")
                              .Include("ShippingAddress.Country.CountryDetails")
                              .Include(x => x.OrderItems)
                              .ThenInclude(x => x.Product)
                              .Include(x => x.Cart)
                              .Include(x => x.CartRuleUsage)
                              .FirstOrDefault();
                if (order == null)
                {
                    throw new Exception("Order Not found");
                }

                if (order.OrderStatus == StatusId)
                {
                    throw new Exception("The Order Is Already " + System.Enum.GetName<OrderStatus>(StatusId));
                }

                if (StatusId == OrderStatus.PaidByCoupon || StatusId == OrderStatus.PaymentReceived || StatusId == OrderStatus.CashOnDelivery)
                {

                    var deliveryOption = new DeliveryOption();

                    try
                    {
                        if (DeliveryOptionEnum.Aramex == order.Cart.ShippingDelivery)
                        {
                            try
                            {
                                await aramexShippingService.CreateShipmentAsync(order.Id);
                            }
                            catch (Exception ex)
                            {

                                await dcmsLogService.AddError("From Aramix", ex.Message, ex.Message + "Order Id: " + id, null, order.SiteId);
                                await SendFailedShipmentNotification(order);
                            }
                        }
                        else if (DeliveryOptionEnum.Domestic == order.Cart.ShippingDelivery || DeliveryOptionEnum.Pickup == order.Cart.ShippingDelivery)
                        {
                            try
                            {
                                await CreateDomesticShipment(order.Id, order.Cart.ShippingDelivery);
                            }
                            catch (Exception ex)
                            {
                                await dcmsLogService.AddError("From Pickuped", ex.Message, ex.Message + "Order Id: " + id, null, order.SiteId);
                            }
                        }
                        else if (DeliveryOptionEnum.TopDelivery == order.Cart.ShippingDelivery)
                        {
                            try
                            {
                                await topDeliveryServices.CreateOrder(order);
                            }
                            catch (Exception ex)
                            {
                                await dcmsLogService.AddError("From TopDelivery", ex.Message, ex.Message + "Order Id: " + id, null, order.SiteId);
                                await SendFailedShipmentNotification(order);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }

                    var warehouseSettings = await _SiteSettings.GetSiteSettingsByModule("OrderWarehouseSettings", SiteId);

                    if (warehouseSettings.Any())
                    {

                        await PushNotificationBasedOnWarehouse(warehouseSettings, order);
                    }

                    if (order.Cart.RedeemAmount > 0)
                    {
                        order.RedeemAmount = order.Cart.RedeemAmount;
                        await _loyaltyWalletService.AddRedeemToOrder(order.CustomerId, SiteId, order.Id, order.CartId.Value);

                    }


                    if (StatusId == OrderStatus.PaymentReceived)
                    {
                        var cartRuleUsage = order.CartRuleUsage.FirstOrDefault();

                        var copunDiscount = cartRuleUsage is null ? 0 : cartRuleUsage.DiscountAmount;
                        await CheckAnyDiscountFromUserForPayment(order, order.RedeemAmount, copunDiscount);
                    }





                    await _loyaltyWalletService.ProcessPurchesLoyaltySetups(order);
                }
                else if (StatusId == OrderStatus.Refunded || StatusId == OrderStatus.Canceled || StatusId == OrderStatus.PaymentFailed)
                {


                    if (order.RedeemAmount > 0)
                    {

                        await _loyaltyWalletService.RefundPoints(order.CustomerId, order.RedeemAmount, SiteId, order.Id, order.CartId);

                    }

                    await _loyaltyWalletService.RefundGainedPoints(order.CustomerId, SiteId, order.Id);

                }

                var oldStatus = order.OrderStatus;
                order.OrderStatus = (OrderStatus)StatusId;
                await _orderRepository.SaveChangesAsync();
                try
                {
                    if (FromApi.HasValue && FromApi.Value)
                    {
                        await SendOrderStatus(order.Id, oldStatus, order.OrderStatus);
                    }
                }
                catch (Exception e)
                {


                }
                var orderStatusChanged = new OrderChanged
                {
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = order.OrderStatus,
                    Order = order,
                    UserId = order.CreatedById,
                    Note = Note
                };


                if ((OrderStatus)StatusId != OrderStatus.PaymentReceived &&
                   (ReserveSerialsAfterPayment == null || !bool.Parse(ReserveSerialsAfterPayment.Value) ||
                   (OrderStatus)StatusId != OrderStatus.PendingPayment))
                {
                    var ProductSerials = await productSerialRepository.Query().Where(x => x.OrderId == id).ToListAsync();

                    if (ProductSerials.Count > 0)
                    {
                        var ProductSerialHistory = new List<ProductSerialHistory>();

                        foreach (var Serial in ProductSerials)
                        {
                            Serial.OrderId = null;

                            ProductSerialHistory.Add(new ProductSerialHistory
                            {


                                ProductSerialId = Serial.Id,
                                CreatedById = order.CreatedById,
                                CreationDate = DateTimeOffset.Now,
                                OldStatus = EProductSerialStatus.Taken,
                                NewStatus = EProductSerialStatus.Available,
                                OrderId = Serial.OrderId,
                                Note = "Change To Avaliable Value for OrderId= " + id + "By Change Status "

                            });
                        }
                        await productSerialRepository.SaveChangesAsync();
                        if (ProductSerialHistory.Count() > 0)
                        {
                            _productSerialHistory.AddRange(ProductSerialHistory);
                            await _productSerialHistory.SaveChangesAsync();
                        }

                    }



                }
                await _mediator.Publish(orderStatusChanged);


            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        public async Task<CartVm> GetOrderSummery(long UserId, CartTypes cartType)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var orderSummery = await _cartService.GetActiveCartDetails(UserId, cartType, SiteId);
                if (orderSummery == null)
                {

                    orderSummery = new CartVm(_currencyService);

                }


                return orderSummery;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateSerialProduct(List<long> SerialIds, long orderId)
        {
            try
            {

                foreach (var SerialId in SerialIds)
                {


                    _productSerialHistory.Add(new EskaCMS.Entities.Entity.Catalog.ProductSerialHistory()
                    {
                        OldStatus = EProductSerialStatus.Taken,
                        CreatedById = 1,
                        CreationDate = DateTimeOffset.Now,
                        NewStatus = EProductSerialStatus.Available,
                        ProductSerialId = SerialId,
                        OrderId = null,
                        Note = "Cancellation Order with OrderId= " + orderId.ToString()

                    });

                    await _productSerialHistory.SaveChangesAsync();

                }



                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<CouponValidationResult> ApplyCouponForOrder(ApplyCouponForOrderVM Model, bool FromApi)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();

            var order = _orderRepository.Query()
                .Include(x => x.OrderItems)
                .ThenInclude(OrderItem => OrderItem.Product)
                .FirstOrDefault(x => x.Id == Model.OrderId);




            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = order.OrderItems.Select(x => new CartItemForCoupon { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            };

            decimal totalFees = 0;
            decimal allItemsTotalFees = 0;

            var couponValidationResult = await _couponService.Validate(order.CustomerId, Model.CouponCode, cartInfoForCoupon, true);
            if (couponValidationResult.Succeeded)
            {

                _couponService.AddCouponUsage(order.CustomerId, order.Id, couponValidationResult);

    

                foreach (var OrderItem in order.OrderItems)
                {
                    var discountedItem = couponValidationResult.DiscountedProducts.FirstOrDefault(x => x.Id == OrderItem.ProductId);

                    if (discountedItem != null)
                    {
                        OrderItem.DiscountAmount = _currencyService.DcimalRound(discountedItem.DiscountAmount);
                    }
                    var IsLocalCountry = await userSiteService.IsLocalCountry();



                    var productPrice = Convert.ToDecimal(string.Format("{0:N" + 2 + "}", IsLocalCountry ? OrderItem.Product.Price : OrderItem.Product.ForeignPrice.HasValue ? OrderItem.Product.ForeignPrice.Value : 0));


                    if (OrderItem.Product.ProductAdditionalFees != null)
                        foreach (var fee in OrderItem.Product.ProductAdditionalFees)
                        {
                            if (fee.FeeType == DiscountTypes.Percentage)
                            {
                                totalFees += (fee.FeeValue / 100) * (OrderItem.Quantity * productPrice);
                            }
                            else if (fee.FeeType == DiscountTypes.FixedAmount)
                            {
                                totalFees += fee.FeeValue * OrderItem.Quantity;
                            }
                            allItemsTotalFees += totalFees;

                        }

                }

                order.OrderTotal = _currencyService.DcimalRound(order.SubTotal + allItemsTotalFees + order.TaxAmount + order.ShippingFeeAmount + order.PaymentFeeAmount - (order.DiscountAmount + couponValidationResult.DiscountAmount));
                couponValidationResult.TotalAmmountAfterDiscount = order.OrderTotal;
                await _orderRepository.SaveChangesAsync();

                if (order.OrderTotal <= 0)
                {
                    await this.OrderChangeStatus(order.Id, OrderStatus.PaidByCoupon, "", SiteId);
                    if (FromApi)
                    {
                        await ReserveSerials(order.Id);
                    }
                }
            }

            return couponValidationResult;
        }

        public async Task<CouponValidationResult> ApplyCouponPlanForOrder(ApplyCouponForOrderVM model)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();

                var order = _orderRepository.Query()
                    .Include(x => x.PlansVersion)
                    .FirstOrDefault(x => x.Id == model.OrderId);

                var couponValidationResult = await _couponService.ValidatePlan(order.CustomerId, model.CouponCode, order.SubTotal, order.PlansVersion.CartRuleId);
                if (couponValidationResult.Succeeded)
                {

                    _couponService.AddCouponUsage(order.CustomerId, order.Id, couponValidationResult);
                    order.OrderTotal = _currencyService.DcimalRound(order.SubTotal + order.TaxAmount + order.ShippingFeeAmount + order.DiscountAmount + order.PaymentFeeAmount - (order.DiscountAmount + couponValidationResult.DiscountAmount));
                    couponValidationResult.TotalAmmountAfterDiscount = order.OrderTotal;
                    await _orderRepository.SaveChangesAsync();

                    if (order.OrderTotal <= 0)
                    {
                        await this.OrderChangeStatus(order.Id, OrderStatus.PaidByCoupon, "", SiteId);
                        await ReserveSerials(order.Id);

                    }
                }

                return couponValidationResult;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task AddOrdersForBulkUsers(IFormFile UsersSheet, int Quantity, OrderStatus orderStatus, long UserGroupId)
        {

            var orderIds = new List<long>();

            try
            {
                List<CustomerVM> Customers = await ExcelHelper.ImportExcel<CustomerVM>(UsersSheet);

                var IsLocalCountry = await userSiteService.IsLocalCountry();


                var currency = _currencyService.GetDefaultCurrencyCode();
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                long CurrentUserId = await _workContext.GetCurrentUserId();
                var DcimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);
                foreach (var Customer in Customers)
                {
                    var Product = await productServices.GetProductById(Customer.ProductId);

                    Customer.Id = await _UserSerice.InsureUser(new EskaCMS.Core.BusinessModels.RegisterViewModel()
                    {
                        DateOfBirth = new DateTimeOffset(Customer.Year, Customer.Month, Customer.Day, 0, 0, 0, TimeSpan.Zero),
                        Email = string.IsNullOrEmpty(Customer.Email) ? "Customer@Customer.com" : Customer.Email,
                        FullName = Customer.FullName,
                        Password = "Customer@321" + Guid.NewGuid().ToString(),
                        RoleId = (long)ERoleTypes.Customer,
                        PhoneNumber = Customer.PhoneNumber,
                        SiteId = SiteId,
                        GroupId = UserGroupId,
                        Status = EStatus.Inactive,
                        UserName = Customer.UserName,
                        UserOptions = new List<EskaCMS.Core.BusinessModels.UserOptionsVM>()
                                    {
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.tShirtSize),
                                            Value=Customer.tShirtSize,
                                        },

                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.team),
                                            Value=Customer.team,
                                        },

                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.residenceCountry),
                                            Value=Customer.residenceCountry,
                                        },

                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.relationship),
                                            Value=Customer.relationship,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.raceCategory),
                                            Value=Customer.raceCategory,

                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.personofContact),
                                            Value=Customer.personofContact,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.occupation),
                                            Value=Customer.occupation,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.nationality),
                                            Value=Customer.nationality,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.gender),
                                            Value=Customer.gender,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.FirstName),
                                            Value=Customer.FirstName,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.LastName),
                                            Value=Customer.LastName,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.emergencyNumber),
                                            Value=Customer.emergencyNumber,
                                        },

                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.bloodType),
                                            Value=Customer.bloodType,
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.allergy),
                                            Value=Customer.allergy.ToString(),
                                        },
                                        new EskaCMS.Core.BusinessModels.UserOptionsVM()
                                        {
                                            PropertyName=nameof(CustomerVM.address),
                                            Value=Customer.address,
                                        }

                                    }
                    });


                    var product = new List<CheckProductQuantityVM> { new CheckProductQuantityVM { ProductId = Customer.ProductId, Quantity = 1 } };

                    var CanAdd = await productServices.CheckAllowedQuantity(product, Customer.Id);
                    if (!CanAdd.Item1)
                        throw new Exception($"You Added More Than Allowed Quantity ,{Customer.UserName} The Ids are : {string.Join(", ", CanAdd.InValidProducts)}");


                    var order = new Order
                    {
                        CustomerId = Customer.Id,
                        CreatedOn = DateTimeOffset.Now,
                        CreatedById = CurrentUserId,
                        LatestUpdatedOn = DateTimeOffset.Now,
                        LatestUpdatedById = CurrentUserId,
                        // PaymentMethod = paymentMethod,
                        SiteId = SiteId,
                        Channel = _workContext.GetCurrentPlatform()
                    };

                    order.CurrencyCode = _currencyService.GetUserCurrencyCode();
                    decimal allItemsTotalFees = 0;


                    if (!Product.IsAllowToOrder || Product.Status != EStatus.Published)
                    {
                        throw new Exception($"The product {Product.ProductDetails.OrderByDescending(c => c.CultureId).FirstOrDefault().Name} is not available any more");
                    }
                    bool StockTrackingIsEnabled = Product.StockTrackingIsEnabled;
                    long StockQuantity = Product.StockQuantity;

                    var GetSerialsFromOriginalProduct = await _SiteSettings.GetSiteSettingById("GetSerialsFromOriginalProduct", await _workContext.GetCurrentSiteIdAsync());

                    if (GetSerialsFromOriginalProduct != null &&
                                 (bool.Parse(GetSerialsFromOriginalProduct.Value)))
                    {
                        var Original = await productServices.GetOriginalProduct(Product.Id.Value);

                        if (Original != null)
                        {
                            var OriginalProduct = await productServices.GetProductById(Original.Id);

                            StockTrackingIsEnabled = Original.StockTrackingIsEnabled;
                            StockQuantity = OriginalProduct.StockQuantity;
                        }

                    }

                    if (StockTrackingIsEnabled && StockQuantity < Quantity)
                    {
                        throw new Exception($"There are only {Product.StockQuantity} items available for");
                    }

                    var productPrice = IsLocalCountry ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", Product.Price)) : Product.ForeignPrice.HasValue ? Convert.ToDecimal(string.Format("{0:N" + DcimalPlaces + "}", Product.ForeignPrice.Value)) : 0;



                    decimal totalFees = 0;

                    if (Product.ProductAdditionalFees != null)
                        foreach (var fee in Product.ProductAdditionalFees)
                        {
                            if (fee.FeeType == DiscountTypes.Percentage)
                            {
                                totalFees += (fee.FeeValue / 100) * (Quantity * productPrice);
                            }
                            else if (fee.FeeType == DiscountTypes.FixedAmount)
                            {
                                totalFees += fee.FeeValue * Quantity;
                            }
                        }

                    allItemsTotalFees += totalFees;

                    var orderItem = new OrderItem
                    {
                        ProductId = Customer.ProductId,
                        ProductPrice = productPrice,
                        Quantity = Quantity,
                        TaxPercent = 0,
                        TaxAmount = 0
                    };

                    order.AddOrderItem(orderItem);

                    await _cartItemRepository.SaveChangesAsync();
                    await productRepository.SaveChangesAsync();


                    var OrderSubTotal = _currencyService.DcimalRound(order.OrderItems.Sum(x => x.Quantity * x.ProductPrice));

                    order.OrderStatus = orderStatus;
                    order.SiteId = SiteId;
                    order.OrderNote = "";

                    order.TaxAmount = _currencyService.DcimalRound(order.OrderItems.Sum(x => x.TaxAmount));
                    order.SubTotal = OrderSubTotal;
                    order.Channel = _workContext.GetCurrentPlatform();
                    order.WarehouseId = await warehouseServices.GetMainWareHouse(order.SiteId);

                    order.OrderTotal = _currencyService.DcimalRound(order.SubTotal + order.TaxAmount + allItemsTotalFees + order.ShippingFeeAmount + order.PaymentFeeAmount - order.DiscountAmount);
                    _orderRepository.Add(order);

                    _orderRepository.SaveChanges();
                    orderIds.Add(order.Id);


                    var resultOfApplyCoupon = await ApplyCouponForOrder(new ApplyCouponForOrderVM()
                    {
                        CouponCode = Customer.CouponeCode,
                        OrderId = order.Id
                    }, false);

                    if (resultOfApplyCoupon.Succeeded && resultOfApplyCoupon.TotalAmmountAfterDiscount != 0)
                    {
                        var productSerials = await ReserveSerials(Customer.ProductId, Quantity);

                        if (productSerials.Count > 0)
                        {
                            foreach (var Serial in productSerials)
                            {
                                var checkIfReserved = productSerialRepository.Query().Where(ps => ps.Id == Serial.Id).AsNoTracking().FirstOrDefault();
                                if (checkIfReserved.OrderId == null)
                                {
                                    Serial.OrderId = order.Id;

                                    var ProductSerialHistory = new ProductSerialHistory
                                    {

                                        ProductSerialId = Serial.Id,
                                        CreatedById = order.CreatedById,
                                        CreationDate = DateTimeOffset.Now,
                                        OldStatus = EProductSerialStatus.Available,
                                        NewStatus = EProductSerialStatus.Taken,
                                        OrderId = Serial.OrderId,
                                        Note = "Change To Taken Value for OrderId= " + Serial.OrderId.ToString() + " "

                                    };
                                    _productSerialHistory.Add(ProductSerialHistory);
                                    await productSerialRepository.SaveChangesAsync();

                                }
                                else
                                {

                                    var NewSerial = await productSerialRepository.Query()
                                                                                .Where(ps => ps.Status == EProductSerialStatus.Available && ps.OrderId == null
                                                                                    && ps.ProductId == Serial.ProductId)
                                                                                .FirstOrDefaultAsync();
                                    NewSerial.OrderId = order.Id;
                                    NewSerial.Status = EProductSerialStatus.Taken;

                                    var ProductSerialHistory = new ProductSerialHistory
                                    {

                                        ProductSerialId = NewSerial.Id,
                                        CreatedById = order.CreatedById,
                                        CreationDate = DateTimeOffset.Now,
                                        OldStatus = EProductSerialStatus.Available,
                                        NewStatus = EProductSerialStatus.Taken,
                                        OrderId = order.Id,
                                        Note = "Replaced insted of Serial " + Serial.Id + "Change To Taken Value for OrderId= " + order.Id.ToString() + " "

                                    };
                                    _productSerialHistory.Add(ProductSerialHistory);
                                    await productSerialRepository.SaveChangesAsync();


                                }
                                await _productSerialHistory.SaveChangesAsync();

                                await productSerialRepository.SaveChangesAsync();

                            }


                        }

                    }
                    else
                    {
                        var productSerials = await ReserveSerials(Customer.ProductId, Quantity);

                        if (productSerials.Count > 0)
                        {
                            foreach (var Serial in productSerials)
                            {
                                var checkIfReserved = productSerialRepository.Query().Where(ps => ps.Id == Serial.Id).AsNoTracking().FirstOrDefault();
                                if (checkIfReserved.OrderId == null)
                                {
                                    Serial.OrderId = order.Id;

                                    var ProductSerialHistory = new ProductSerialHistory
                                    {

                                        ProductSerialId = Serial.Id,
                                        CreatedById = order.CreatedById,
                                        CreationDate = DateTimeOffset.Now,
                                        OldStatus = EProductSerialStatus.Available,
                                        NewStatus = EProductSerialStatus.Taken,
                                        OrderId = Serial.OrderId,
                                        Note = "Change To Taken Value for OrderId= " + Serial.OrderId.ToString() + " "

                                    };
                                    _productSerialHistory.Add(ProductSerialHistory);
                                    await productSerialRepository.SaveChangesAsync();

                                }
                                else
                                {

                                    var NewSerial = await productSerialRepository.Query()
                                                                                .Where(ps => ps.Status == EProductSerialStatus.Available && ps.OrderId == null
                                                                                    && ps.ProductId == Serial.ProductId)
                                                                                .FirstOrDefaultAsync();
                                    NewSerial.OrderId = order.Id;
                                    NewSerial.Status = EProductSerialStatus.Taken;

                                    var ProductSerialHistory = new ProductSerialHistory
                                    {

                                        ProductSerialId = NewSerial.Id,
                                        CreatedById = order.CreatedById,
                                        CreationDate = DateTimeOffset.Now,
                                        OldStatus = EProductSerialStatus.Available,
                                        NewStatus = EProductSerialStatus.Taken,
                                        OrderId = order.Id,
                                        Note = "Replaced insted of Serial " + Serial.Id + "Change To Taken Value for OrderId= " + order.Id.ToString() + " "

                                    };
                                    _productSerialHistory.Add(ProductSerialHistory);
                                    await productSerialRepository.SaveChangesAsync();


                                }
                                await _productSerialHistory.SaveChangesAsync();

                                await productSerialRepository.SaveChangesAsync();

                            }


                        }

                    }


                    await _context.Entry(order)
                          .Navigation("ProductSerials")
                          .LoadAsync();
                    await PublishOrderCreatedEvent(order);


                }

            }
            catch (Exception ex)
            {
                var Orders = await _orderRepository.Query().Where(x => orderIds.Any(t => t == x.Id)).ToListAsync();

                foreach (var order in Orders)
                {
                    var orderHistory = new OrderHistory
                    {
                        OrderId = order.Id,
                        CreatedOn = DateTimeOffset.Now,
                        OldStatus = order.OrderStatus,
                        CreatedById = order.CustomerId,
                        NewStatus = OrderStatus.Canceled,
                        Note = "Cancelled The Api AddBulkOrder Was Falied",
                    };


                    orderHistoryRepository.Add(orderHistory);
                    await orderHistoryRepository.SaveChangesAsync();

                    order.OrderStatus = OrderStatus.Canceled;
                }

                await _orderRepository.SaveChangesAsync();

                throw ex;
            }


        }

        public async Task<byte[]> CreateTemplate(long productId, long CultureId)
        {
            try
            {
                var ProductVariations = await productRepository.Query()
                                                     .Where(x => x.Id == productId)
                                                     .Include(x => x.ProductLinks)
                                                     .ThenInclude(x => x.LinkedProduct)
                                                     .Include("ProductLinks.LinkedProduct.ProductDetails")
                                                     .Include("ProductLinks.LinkedProduct.OptionCombinations")
                                                     .Include("ProductLinks.LinkedProduct.OptionCombinations.Option")
                                                     .Include("ProductLinks.LinkedProduct.OptionCombinations.Option.productOptionValues")
                                                     .Include("ProductLinks.LinkedProduct.OptionCombinations.Option.productOptionValues.productOptionValueDetails")
                                                     .SelectMany(x => x.ProductLinks.Where(

                                                         y => y.ProductId == productId
                                         && y.LinkType == ItemLinkType.Super && y.LinkedProduct.Status != EStatus.Deleted))
                                                     .Select(x => x.LinkedProduct)
                                                     .Where(x => x.Status != EStatus.Deleted)
                                                     .ToListAsync();

                var HandleVariations = await HandelValues(ProductVariations, CultureId);

                var FileByte = ExcelHelper.CreateTemplate<CustomerVM>(HandleVariations);


                return FileByte;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<Result<Order>> CreateOrderPlan(long cartId, string paymentMethod, decimal paymentFeeAmount, string shippingMethodName, Address billingAddress, Address shippingAddress, OrderStatus orderStatus = OrderStatus.New, long SiteId = 0)
        {
            try
            {
                var CulturId = _workContext.GetCurrentCultureId();
                var warehouseId = await _workContext.GetCurrentWarehouseId();

                var CustomerCurrency = await _currencyService.GetDefaultUserCurrency();
                var DefaultCurrency = await _currencyService.DefaultSiteCurrencyRate();

                var DcimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);

                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var UserId = await _workContext.GetCurrentUserId();

                var cart = await _cartRepository
                           .Query()
                           .Include(x => x.Items)
                           .Include("Items.Product")
                           .Include("Items.Product.ProductBranch")
                           .Include("Items.Product.ProductPriceWarehouses")
                           .Include("Items.Product.ProductDetails")
                           .Include(x => x.PlansVersion)
                           .Include("PlansVersion.TaxClass")
                           .Include("PlansVersion.Discount")
                           .Include("PlansVersion.PlanProductVersions")
                           .Include("PlansVersion.PlanProductVersions.Product")
                           .Include("PlansVersion.PlanProductVersions.Product.ProductDetails")
                           .Include("PlansVersion.Plan")
                           .Include("PlansVersion.Plan.PlanDetails")
                           .Where(x => x.Id == cartId).FirstOrDefaultAsync();

                if (cart == null)
                {
                    return Result.Fail<Order>($"Cart id {cartId} cannot be found");
                }

                var orderShippingAddress = new OrderAddress();
                var orderBillingAddress = new OrderAddress();
                var productSerial = new List<ProductSerial>();


                var validateShippingMethodResult = await ValidatePlanShippingMethod(string.IsNullOrEmpty(shippingMethodName) ? "Free" : shippingMethodName, shippingAddress, cart);

                if (!validateShippingMethodResult.Success)
                {
                    return Result.Fail<Order>(validateShippingMethodResult.Error);
                }

                var shippingMethod = validateShippingMethodResult.Value;

                orderBillingAddress = fillAddress(billingAddress, UserId);

                orderShippingAddress = fillAddress(shippingAddress, UserId);

                var order = new Order
                {
                    CustomerId = cart.CustomerId,
                    CreatedOn = DateTimeOffset.Now,
                    CreatedById = cart.CreatedById,
                    LatestUpdatedOn = DateTimeOffset.Now,
                    LatestUpdatedById = cart.CreatedById,
                    BillingAddress = orderBillingAddress,
                    ShippingAddress = orderShippingAddress,
                    PaymentMethod = paymentMethod,
                    PaymentFeeAmount = paymentFeeAmount,
                    SiteId = SiteId,
                    Channel = _workContext.GetCurrentPlatform(),
                    CartId = cart.Id,
                    PlansVersionId = cart.PlansVersionId,
                    CurrencyCode = DefaultCurrency.Name,
                    CurrencyCodeCustomer = CustomerCurrency.Name
                };
                foreach (var item in cart.Items)
                {
                    if (item.Product.StockTrackingIsEnabled)
                    {
                        order.WarehouseId = await _cartService.WarehouseInventoryStatus(item, shippingAddress, SiteId, cart.ShippingData, item.Product.ProductDetails.Where(x => x.CultureId == CulturId).Select(x => x.Name).FirstOrDefault(), warehouseId);
                    }
                    var orderItem = new OrderItem
                    {
                        Product = item.Product,
                        ProductPrice = item.Price.Value,
                        Quantity = item.Quantity,
                        TaxPercent = 0,
                        AdditionalDetails = item.AdditionalDetails,
                        TaxAmount = 0
                    };

                    order.AddOrderItem(orderItem);
                    await _cartItemRepository.SaveChangesAsync();
                    await productRepository.SaveChangesAsync();


                }

                if (order.WarehouseId == 0 || order.WarehouseId == null)
                {
                    var WareHouseId = await warehouseRepository.Query().Where(x => x.IsMain && x.SiteId == SiteId).Select(x => x.Id).FirstOrDefaultAsync();
                    if (WareHouseId != null)
                    {
                        order.WarehouseId = WareHouseId;
                    }
                    else
                    {
                        throw new Exception("Please At Lest Add One WareHouse");

                    }

                }
                order.SubTotal = _currencyService.DcimalRound(order.OrderItems.Sum(x => x.Quantity * x.ProductPrice)); ;

                if (cart.PlansVersion.DiscountId.HasValue)
                {
                    var Discount = _currencyService.DcimalRound(await _IDiscountService.CalculatePlanDiscount(cart.PlansVersion.DiscountId.Value, order.SubTotal));

                    order.DiscountAmount += Discount;

                }

                if (cart.PlansVersion.TaxClassId.HasValue)
                {
                    var TaxPercent = await _taxService.GetTaxPercent(cart.PlansVersion.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId, shippingAddress.ZipCode);

                    if (cart.IsProductPriceIncludeTax)
                    {
                        order.TaxAmount = (order.SubTotal - order.DiscountAmount) / (1 + (TaxPercent / 100));
                    }
                }


                var checkingDiscountResult = await CheckForDiscountPlanIfAny(cart, order.SubTotal, cart.PlansVersion.CartRuleId);

                if (!checkingDiscountResult.Succeeded)
                {
                    return Result.Fail<Order>(checkingDiscountResult.ErrorMessage);
                }
                order.OrderStatus = orderStatus;
                order.SiteId = SiteId;
                order.OrderNote = cart.OrderNote;
                order.CouponCode = checkingDiscountResult.CouponCode;
                order.CouponRuleName = cart.CouponRuleName;


                var ShippmentAmount = cart.ShippingAmount == null || cart.ShippingAmount == 0 ? 0 : cart.ShippingAmount.Value;

                order.ShippingFeeAmount = ShippmentAmount;

                order.ShippingMethod = shippingMethod.Name;

                order.SubTotalWithDiscount = _currencyService.DcimalRound(order.SubTotal - (order.DiscountAmount + checkingDiscountResult.DiscountAmount));

                order.RedeemAmount = cart.RedeemAmount;

                var OrderTotal = order.SubTotal + order.TaxAmount + order.ShippingFeeAmount - order.DiscountAmount - order.RedeemAmount;

                order.OrderTotal = _currencyService.DcimalRound(OrderTotal);

                var Currency = await _currencyService.GetCurrencyByName(order.CurrencyCodeCustomer);

                order.CustomerCurrencyOrderTotal = _currencyService.DcimalRound(_currencyService.ConvertToCurrency(OrderTotal, Currency, DcimalPlaces));

                _orderRepository.Add(order);


                _orderRepository.SaveChanges();

                _couponService.AddCouponUsage(cart.CustomerId, order.Id, checkingDiscountResult);

                _orderRepository.SaveChanges();

                await subscriptionServices.HandleRegularPlans(cart, SiteId, order.Id, new PlansVersionsOutput(), order);

                await PublishOrderCreatedEvent(order);


                await _workContext.UpdateAnynoumsUser(orderBillingAddress.ContactName, orderBillingAddress.Email, SiteId, orderBillingAddress.Phone);

                return Result.Ok(order);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateOrderReferanceId(long orderId, string orderReferanceId)
        {
            var order = await GetOrderById(orderId);
            order.OrderReferanceId = orderReferanceId;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateOrderStatus(long OrderId,OrderStatus status)
        {
            var order = await _orderRepository.Query().Where(x => x.Id == OrderId).FirstOrDefaultAsync();
            order.OrderStatus = status;
             _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();
            return true;
        }
        public async Task<Order> GetOrderByReferanceId(string id)
        {
            try
            {

                return await _orderRepository.Query().Where(x => x.OrderReferanceId.ToLower() == id.ToLower())
                                                     .Include(x => x.Cart)
                                                     .FirstOrDefaultAsync();

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        #region helpers


        private List<ProductVariationOptionVm> MapProductOptionToProductVm(Product product, long CultureId)
        {

            var Result = new List<ProductVariationOptionVm>();

            foreach (var item in product.OptionCombinations)
            {
                Result.Add(
                   new ProductVariationOptionVm
                   {
                       OptionName = item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault() == null ? "" : item.Option.optionDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                       Value = item.Value.productOptionValueDetails.Where(x => x.CultureId == CultureId).Select(x => x.Value).FirstOrDefault()
                   });

            }
            return Result;

        }
        private async Task<bool> PushNotificationBasedOnWarehouse(List<SiteSettingsVM> warehouseSettings, Order order)
        {
            var warehousePushNotificationSettings = warehouseSettings.Where(x => x.Id == "OrderWarehousePushNotification").FirstOrDefault();

            if (warehousePushNotificationSettings is not null)
            {
                var settings = JsonConvert.DeserializeObject<WarehousePushNotificationSettingsVM>(warehousePushNotificationSettings.Value);

                if (settings.IsActive)
                {
                    await _mediator.Publish(new OrderWarehouseNotification { Title = settings.Title, Body = settings.Body, Order = order });

                }

            }

            return true;

        }
        private async Task<string> HandelValues(List<Product> productVariations, long CultureId)
        {
            try
            {

                StringBuilder Variations = new StringBuilder();
                foreach (var product in productVariations)
                {

                    Variations.Append("Id" + "=" + product.Id + "  " + "ProductName " + "= " + product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name + "   OptionValue : " + " " + string.Join(",", product.OptionCombinations.SelectMany(x => x.Value.productOptionValueDetails).Where(x => x.CultureId == CultureId).Select(x => x.Value).Count() == 0 ? new List<string>() : product.OptionCombinations.SelectMany(x => x.Value.productOptionValueDetails).Where(x => x.CultureId == CultureId).Select(x => x.Value).ToList()) + System.Environment.NewLine);
                }

                return Variations.ToString();


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private (string, string, string, string, string, string, NotificationType) GetSettingsValues(List<SiteSettingsVM> settings, string Culture)
        {
            if (settings.Count > 0)
            {
                string adminReceiptEmailTemplateId = Culture.Contains("en") ? GetStringSettingValue(settings, "AdminReceiptEmailTemplateId") : GetStringSettingValue(settings, "AdminReceiptEmailTemplateIdArabic");
                string smsReceiptTemplateId = Culture.Contains("en") ? GetStringSettingValue(settings, "SMSReceiptTemplateId") : GetStringSettingValue(settings, "SMSReceiptTemplateIdArabic");
                string emailReceiptTemplateId = Culture.Contains("en") ? GetStringSettingValue(settings, "EmailReceipttemplateId") : GetStringSettingValue(settings, "EmailReceipttemplateIdArabic");
                string deliveryReceiptEmailTemplate = Culture.Contains("en") ? GetStringSettingValue(settings, "ARAMEXReceiptEmailTemplateId") : GetStringSettingValue(settings, "ARAMEXReceiptEmailTemplateArabic");
                string designReceiptItems = Culture.Contains("en") ? GetStringSettingValue(settings, "StyleReceipt") : GetStringSettingValue(settings, "StyleReceiptArabic");
                string DownloadInvoice = Culture.Contains("en") ? GetStringSettingValue(settings, "DownloadInvoice") : GetStringSettingValue(settings, "DownloadInvoiceArabic");


                NotificationType notificationType = settings?.FirstOrDefault(x => x.Id == "NotificationType")?.Value != null ?
                        (NotificationType)System.Enum.Parse(typeof(NotificationType), settings.First(x => x.Id == "NotificationType").Value, true) :
                        NotificationType.BothSMSEmail;

                return (adminReceiptEmailTemplateId, smsReceiptTemplateId, emailReceiptTemplateId,
                        deliveryReceiptEmailTemplate, designReceiptItems, DownloadInvoice, notificationType);
            }
            else
            {
                return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, NotificationType.BothSMSEmail);


            }
        }

        private List<string> GetAdminEmails(List<SiteSettingsVM> settings)
        {
            if (settings.Count > 0)
            {
                var adminEmailsSetting = settings.FirstOrDefault(x => x.Id == "AdminReceiptEmails");
                return adminEmailsSetting != null ? adminEmailsSetting.Value.Split(",").ToList() : new List<string>();
            }
            else
            {


                return new List<string>();
            }


        }

        private string GetStringSettingValue(List<SiteSettingsVM> settings, string settingId)
        {
            var setting = settings.FirstOrDefault(x => x.Id == settingId);
            return setting != null ? setting.Value : "";
        }

        private string GetCategoryName(IList<Product>? Product, long cultureId)
        {

            try
            {
                List<string> CategoryName = new List<string>();

                foreach (var product in Product)
                {

                    if (product.Categories == null || !product.Categories.Any())
                    {
                        continue;
                    }

                    var ParentCategory = product.Categories
                        .Where(x => x?.Category?.ParentId == null)
                        .Select(x => x.Category)
                        .FirstOrDefault();

                    if (ParentCategory != null)
                    {
                        ParentCategory = product.Categories
                            .Select(x => x.Category)
                            .FirstOrDefault();

                        if (ParentCategory != null)
                        {


                            var categoryDetail = ParentCategory.CategoryDetails
                               .Where(x => x.CultureId == cultureId)
                               .FirstOrDefault();

                            if (categoryDetail != null)
                            {
                                CategoryName.Add(categoryDetail.Name);
                            }
                        }
                    }
                    else
                    {
                        var categoryDetail = product.Categories.SelectMany(x => x.Category.CategoryDetails)
                            .Where(x => x.CultureId == cultureId)
                            .FirstOrDefault();

                        if (categoryDetail != null)
                        {
                            CategoryName.Add(categoryDetail.Name);
                        }
                    }

                }

                if (CategoryName.Count() > 0)
                {

                    return string.Join(",", CategoryName);

                }

                return "";
            }
            catch (Exception ex)
            {

                return "";
            }
        }

        private async Task<bool> CreateDomesticShipment(long id, DeliveryOptionEnum deliveryOption)
        {
            try
            {

                var Order = await _orderRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();

                var Shipment = new EskaCMS.Entities.Entity.Shipments.Shipment();

                Shipment.OrderId = id;

                Shipment.TrackingNumber = id.ToString();

                Shipment.ShippmentStatus = ShippmentStatus.Preparing;

                Shipment.ShippingDelivery = deliveryOption;

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

                shipmentHistoryRepository.Add(History);
                await shipmentHistoryRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<CouponValidationResult> CheckForDiscountIfAny(Cart cart)
        {
            //if (string.IsNullOrWhiteSpace(cart.CouponCode))
            //{
            //    return new CouponValidationResult { Succeeded = true, DiscountAmount = 0 };
            //}

            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = cart.Items.Select(x => new CartItemForCoupon { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            };

            var couponValidationResult = await _couponService.Validate(cart.CustomerId, cart.CouponCode, cartInfoForCoupon);
            return couponValidationResult;
        }
        private async Task<Result<ShippingPrice>> ValidateShippingMethod(string shippingMethodName, Address shippingAddress, Cart cart)
        {
            var IsLocalCountry = await userSiteService.IsLocalCountry();
            var warehouseId = await _workContext.GetCurrentWarehouseId();

            var orderTaxAndShippingPrice = new OrderTaxAndShippingPriceVm
            {
                Cart = await _cartService.GetActiveCartDetails(cart.CustomerId, cart.CreatedById, cart.CartType, cart.SiteId)
            };
            var applicableShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(new GetShippingPriceRequest
            {
                OrderAmount = cart.Items.Sum(x => (IsLocalCountry ? x.Product.PriceType == EPriceType.ProductBase
                                                          ? x.Product.Price.Value
                                                          : x.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == warehouseId).Select(x => x.Price).FirstOrDefault()

                                                           : x.Product.ForeignPrice.HasValue ? x.Product.ForeignPrice.Value : 0) * x.Quantity),
                ShippingAddress = shippingAddress,
                CartItems = orderTaxAndShippingPrice.Cart.Items
            }, cart);

            var shippingMethod = applicableShippingPrices.FirstOrDefault(x => x.Name == shippingMethodName);
            if (shippingMethod == null)
            {
                return Result.Fail<ShippingPrice>($"Invalid shipping method {shippingMethod}");
            }

            return Result.Ok(shippingMethod);
        }

        private async Task<string> ExecuteSQLQuery(long ProductId, long Quantity)
        {
            string Query = " SET @uids := \'\'; UPDATE catalog_productserial  SET Status = 2  WHERE  ProductId = " + ProductId.ToString() + " and Status = 1 and OrderId is null and ( SELECT @uids := CONCAT( Id,\',\', @uids) ) like '%%'   limit " + Quantity.ToString() + " ; SELECT @uids as Serials ;";

            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            using (var command = connection.CreateCommand())
            {

                command.CommandText = Query;
                command.CommandType = CommandType.Text;
                try
                {
                    if (shouldCloseConnection)
                    {
                        await connection.OpenAsync();
                    }

                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();


                }
                finally
                {
                    if (shouldCloseConnection)
                    {
                        await _context.Database.CloseConnectionAsync();
                    }
                }


            }

        }

        private async Task<List<ProductSerial>> ReserveSerials(CartItem cartItem)
        {
            var ReserveSerialsAfterPayment = await _SiteSettings.GetSiteSettingById("ReserveSerialsAfterPayment", await _workContext.GetCurrentSiteIdAsync());
            var GetSerialsFromOriginalProduct = await _SiteSettings.GetSiteSettingById("GetSerialsFromOriginalProduct", await _workContext.GetCurrentSiteIdAsync());

            if (ReserveSerialsAfterPayment != null &&
                bool.Parse(ReserveSerialsAfterPayment.Value))
            {
                return new List<ProductSerial>();
            }
            else
            {
                if (GetSerialsFromOriginalProduct != null && bool.Parse(GetSerialsFromOriginalProduct.Value))
                {
                    var OriginalProduct = await productServices.GetOriginalProduct(cartItem.Product.Id);
                    if (OriginalProduct.StockTrackingIsEnabled && OriginalProduct.IsProductSerial)
                    {
                        return await ReserveSerials(cartItem.Product.Id, cartItem.Quantity);
                    }
                }
                else
                {
                    if (cartItem.Product.StockTrackingIsEnabled && cartItem.Product.IsProductSerial)
                    {
                        return await ReserveSerials(cartItem.Product.Id, cartItem.Quantity);
                    }
                }
            }


            return new List<ProductSerial>();
        }

        private async Task<List<ProductSerial>> ReserveSerials(long ProductId, int Quantity)
        {
            try
            {
                var GetSerialsFromOriginalProduct = await _SiteSettings.GetSiteSettingById("GetSerialsFromOriginalProduct", await _workContext.GetCurrentSiteIdAsync());
                long TargetProductId = ProductId;

                if (GetSerialsFromOriginalProduct != null &&
                   (bool.Parse(GetSerialsFromOriginalProduct.Value)))
                {
                    TargetProductId = await productServices.GetOriginalProductId(ProductId);

                }



                var productSerial = new List<ProductSerial>();


                Random rnd = new Random();




                var CountOfAvailableSerials = productSerialRepository.Query()
                                        .Where(x => x.Status == EProductSerialStatus.Available && x.OrderId == null && x.ProductId == TargetProductId)
                                        .Select(x => x.Id)
                                        .Count();



                if (CountOfAvailableSerials != 0)
                {
                    var stock = await _stockRepository.Query()
                                            .Where(x => x.ProductId == TargetProductId)
                                            .FirstOrDefaultAsync();

                    if (stock != null)
                    {

                        var SerialsIds = await ExecuteSQLQuery(TargetProductId, Quantity);



                        SerialsIds = SerialsIds.Substring(0, SerialsIds.Length - 1);
                        List<string> ReservedIds = SerialsIds.Split(',').ToList();
                        List<long> ConvertedReservedIds = ReservedIds.Select(x => long.Parse(x)).ToList();

                        productSerial = await productSerialRepository.Query().Where(s => ConvertedReservedIds.Any(id => id == s.Id)).ToListAsync();



                        if (productSerial.Count() < Quantity)
                        {
                            throw new Exception("There are not enough Cards");

                        }
                        OrderService.ReservedSerials.AddRange(productSerial.Select(s => s.Code).ToList());

                    }
                    else
                    {
                        throw new Exception("there is no stock for Product With Id " + TargetProductId);

                    }

                    await _stockRepository.SaveChangesAsync();

                    return productSerial;
                }
                else
                {
                    throw new Exception("out of stock");
                }


                return new List<ProductSerial>();

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private OrderAddress fillAddress(Address InfoAddress, long UserId)
        {
            try
            {
                var infoAddress = new OrderAddress();

                if (InfoAddress != null)
                {
                    {
                        infoAddress.AddressLine1 = InfoAddress.AddressLine1;
                        infoAddress.AddressLine2 = InfoAddress.AddressLine2;
                        infoAddress.ContactName = InfoAddress.ContactName;
                        infoAddress.CountryId = InfoAddress.CountryId;
                        infoAddress.StateOrProvinceId = InfoAddress.StateOrProvinceId;
                        infoAddress.DistrictId = InfoAddress.DistrictId;
                        infoAddress.City = InfoAddress.City;
                        infoAddress.ZipCode = InfoAddress.ZipCode;
                        infoAddress.Phone = InfoAddress.Phone;
                        infoAddress.Email = InfoAddress.Email;
                        infoAddress.Latitude = InfoAddress.Latitude;
                        infoAddress.Longitude = InfoAddress.Longitude;

                    }
                    ;

                }
                return infoAddress;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task PublishOrderCreatedEvent(Order order)
        {
            var orderCreated = new OrderCreated
            {
                OrderId = order.Id,
                Order = order,
                UserId = order.CreatedById,
                Note = order.OrderNote,
                OrderStatus = order.OrderStatus

            };

            await _mediator.Publish(orderCreated);
        }

        private async Task<Result<ShippingPrice>> ValidatePlanShippingMethod(string shippingMethodParam, Address shippingAddress, Cart cart)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var orderTaxAndShippingPrice = new OrderTaxAndShippingPriceVm
                {
                    Cart = await _cartService.GetActiveCartDetails(cart.CustomerId, cart.CreatedById, cart.CartType, cart.SiteId)
                };
                var applicableShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(new GetShippingPriceRequest
                {
                    OrderAmount = await MappingPlanPrice(cart, IsLocalCountry),
                    ShippingAddress = shippingAddress,
                    CartItems = orderTaxAndShippingPrice.Cart.Items
                }, cart);

                var shippingMethod = applicableShippingPrices.FirstOrDefault(x => x.Name == shippingMethodParam);
                if (shippingMethod == null)
                {
                    return Result.Fail<ShippingPrice>($"Invalid shipping method {shippingMethod}");
                }

                return Result.Ok(shippingMethod);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<decimal> MappingPlanPrice(Cart cart, bool isLocalCountry)
        {
            try
            {
                decimal result = 0;
                var warehouseId = await _workContext.GetCurrentWarehouseId();

                if (isLocalCountry)

                {
                    result += cart.PlansVersion.PlanProductVersions.Sum(x => x.Product.PriceType == EPriceType.ProductBase
                                                          ? x.Product.Price.Value
                                                          : x.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == warehouseId).Select(x => x.Price).FirstOrDefault()
                                                           * x.Quantity);
                }
                else
                {
                    result += cart.PlansVersion.PlanProductVersions.Sum(x => x.Product.ForeignPrice.Value * x.Quantity);


                }


                return result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<CouponValidationResult> CheckForDiscountPlanIfAny(Cart cart, decimal SubTotal, long? CartRuleId = null)
        {
            if (string.IsNullOrWhiteSpace(cart.CouponCode))
            {
                return new CouponValidationResult { Succeeded = true, DiscountAmount = 0 };
            }

            var couponValidationResult = await _couponService.ValidatePlan(cart.CustomerId, cart.CouponCode, SubTotal, CartRuleId);
            return couponValidationResult;
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
                    var district = results.Where(rate => rate.DistrictId == shippingAddress.DistrictId).Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
                    return district ?? state.Where(x => !x.DistrictId.HasValue).Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
                }

                return results.Select(x => x.EstimatedDeliveryDate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task UpdateCustomerCurrency(decimal CustomerOrderTotal, string CustomerCurrencyCode, Order order)
        {
            try
            {
                order.CurrencyCodeCustomer = CustomerCurrencyCode;
                order.CustomerCurrencyOrderTotal = CustomerOrderTotal;
                await _orderRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<CartOrderChart>> GetCartOrderCharts(TimeIntervals time)
        {
            var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
            List<CartOrderChart> cart_order_charts = new();

            try
            {
                var cartsInPeriod = await _cartRepository.Query().Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToListAsync();
                var SuccessfullOrders = await SuccessfullOrder();
                SuccessfullOrders = SuccessfullOrders.Where(x => x.CreatedOn > currentStart && x.CreatedOn < currentEnd).ToList();
                //Filter through carts that haven't been purchased within the selected period
                var Abandoned_CartsTotal = cartsInPeriod.Count(x => !SuccessfullOrders.Any(y => y.CartId == x.Id));

                var OrderInPeriod = await _orderRepository.Query().Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToListAsync();

                var GroupOrdersByCarts = OrderInPeriod.GroupBy(x => x.CartId);

                //Filter through Carts that have been abondend then get purchased within the selected period but not in the same day
                var successStatuses = new[]
                       {
                        OrderStatus.CashOnDelivery,
                        OrderStatus.PaymentReceived,
                        OrderStatus.PaidByCoupon,
                        OrderStatus.Complete
                       };

                var Recovered_CartsTotal = GroupOrdersByCarts.Count(cart =>
                    // Has at least one successful order
                    cart.Any(o => successStatuses.Contains(o.OrderStatus))
                    // Has at least one non-successful order
                    && cart.Any(o => !successStatuses.Contains(o.OrderStatus))
                    // Earliest unsuccessful order is before the latest successful order
                    && cart.Where(o => !successStatuses.Contains(o.OrderStatus)).Min(o => o.CreatedOn)
                       >cart.Where(o => successStatuses.Contains(o.OrderStatus)).Max(o => o.CreatedOn)
                );

                //calculate Recovery_Rate
                var Cart_Recovery_Rate = (Recovered_CartsTotal / Abandoned_CartsTotal) * 100;

                cart_order_charts.Add(new CartOrderChart { label = "Abandoned Carts", value = Abandoned_CartsTotal });
                cart_order_charts.Add(new CartOrderChart { label = "Recovered Carts", value = Recovered_CartsTotal });
                cart_order_charts.Add(new CartOrderChart { label = "Recovery Rate (%)", value = Cart_Recovery_Rate });

                return cart_order_charts;


            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
        public async Task<CartVm> ReOrder(long orderId, long siteId,long userId)
        {
            try
            {

                var order = await _orderRepository.Query()
                    .AsNoTracking()
                     .Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince).ThenInclude(x => x.StateOrProvinceDetails)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.Country).ThenInclude(x => x.CountryDetails)
                .Include(x => x.BillingAddress).ThenInclude(x => x.StateOrProvince).ThenInclude(x => x.StateOrProvinceDetails)
                .Include(x => x.BillingAddress).ThenInclude(x => x.Country).ThenInclude(x => x.CountryDetails)
                .Include(x=>x.OrderItems).ThenInclude(x=>x.Product).ThenInclude(x=>x.ProductDetails)
                   .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ProductAvaliablity)

				    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ProductPriceWarehouses)

					.Where(x => x.Id == orderId)
                    .FirstOrDefaultAsync();

                if (order == null || order.CustomerId != userId)
                {
                    throw new Exception($"Order id {orderId} cannot be found");
                }

                if (!order.CartId.HasValue)
                {
                    throw new Exception("This order has no associated cart to reorder");
                }

                var sourceCart = await _cartRepository.Query()
                    .AsNoTracking()
                    .Include(x => x.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.ProductDetails)
                    .Include(x => x.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.ProductBranch)
                    .Include(x => x.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.ProductPriceWarehouses)
                                .Include("Items.Product.ProductAdditionalFees")
                           .Include("Items.Product.ProductAvaliablity")
                           .Include("Items.Product.Stock")
                    .Where(x => x.Id == order.CartId.Value)
                    .FirstOrDefaultAsync();

                if (sourceCart == null || !sourceCart.Items.Any())
                {
                     throw new Exception("The original cart for this order could not be found");
                }

                var IsLocalCountry = await userSiteService.IsLocalCountry();
                var UserCountry = await userSiteService.GetUserCountry();
               var siteCountryId = await userSiteService.GetSiteCountry(siteId);
                var WarehouseId = await _workContext.GetCurrentWarehouseId();
                var cultureId = _workContext.GetCurrentCultureId();

 
                // Deactivate currently active cart for this site
                var currentActiveCartId = await _cartRepository.Query()
                    .Where(x => x.CustomerId == userId &&
                                x.CreatedById == userId &&
                                x.IsActive &&
                                x.CartType == sourceCart.CartType &&
                                x.SiteId == siteId &&
                                x.Id != sourceCart.Id)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync();
                if (currentActiveCartId.HasValue)
                {
                  await DeActiveCart(currentActiveCartId.Value);
                }

                // Build the new cart from SourceCart
                
                var newCart = new Cart
                {
                    CustomerId = sourceCart.CustomerId,
                    CreatedById = userId,
                    IsProductPriceIncludeTax = sourceCart.IsProductPriceIncludeTax,
                    CartType = sourceCart.CartType,
                    SiteId = siteId,
                    PlansVersionId = sourceCart.PlansVersionId,
               
                };

                var skippedItems = new List<string>();

                if (sourceCart.CartType == CartTypes.Plan)
                {
                    _cartRepository.Add(newCart);
                    await _cartService.AddCartItemPlan(newCart, "");

                    if (!newCart.Items.Any())
                    {
                        throw new Exception("None of the items in this plan are currently available");
                    }
                }
                else
                {
                    foreach (var sourceItem in order.OrderItems)
                    {
                        var product = sourceItem.Product;
                        var productName = product?.ProductDetails
                            .Where(x => x.CultureId == cultureId)
                            .Select(x => x.Name)
                            .FirstOrDefault() ?? $"Product #{sourceItem.ProductId}";

                        if (product == null
                            || product.Status != EStatus.Published
                            || !product.IsAllowToOrder)
                        {
                            skippedItems.Add(productName);
                            continue;
                        }

                        try
                        {
                            await IsAvaliableProduct(product, UserCountry, siteCountryId);
                            await _cartService.CheckAvaliableStock(sourceItem.Quantity, sourceItem.ProductId);
                        }
                        catch (Exception)
                        {
                            skippedItems.Add(productName);
                            continue;
                        }

                        var freshPrice = IsLocalCountry
                            ? product.PriceType == EPriceType.ProductBase
                                ? product.Price ?? 0
                                : product.ProductPriceWarehouses?.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault() ?? 0
                            : product.ForeignPrice ?? 0;
                        newCart.IsActive = true;
                        newCart.Items.Add(new CartItem
                        {
                            Cart = newCart,
                            ProductId = sourceItem.ProductId,
                            Quantity = sourceItem.Quantity,
                            AdditionalDetails = sourceItem.AdditionalDetails,
                            CreatedOn = DateTimeOffset.Now,
                            Price = freshPrice
                        });
                    }

                    if (!newCart.Items.Any())
                    {
                        throw new Exception("None of the items in this order's cart are currently available");
                    }
                    newCart.ShippingData = sourceCart.ShippingData;
                    _cartRepository.Add(newCart);
                }

                await _cartRepository.SaveChangesAsync();

                var cartVm = await _cartService.GetActiveCartDetails(userId, newCart.CartType, siteId);

                if (skippedItems.Any())
                {
                    cartVm.ReOrderWarning = $"Not added (no longer available): {string.Join(", ", skippedItems)}";
                }
                if (order.ShippingAddress != null)
                {
                    var shippingDistrict = order.ShippingAddress is not null
               ? await _districtRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ShippingAddress.DistrictId)
               : null;
                    var shippingState = order.ShippingAddress is not null
                        ? await _stateRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ShippingAddress.StateOrProvinceId):null;

                    cartVm.ShippingAdress = order.ShippingAddress is null ? null : new ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM.ShippingAddressVm
                    {
                        AddressLine1 = order.ShippingAddress is null ? null : order.ShippingAddress.AddressLine1,
                        
                        AddressLine2Only = order.ShippingAddress.AddressLine2,
                        CityName =  order.ShippingAddress.City,
                        ZipCode = order.ShippingAddress.ZipCode,
                        ContactName =order.ShippingAddress.ContactName,
                        DistrictName =  shippingDistrict.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                        StateOrProvinceName = shippingState.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                        StateOrProvinceId =  shippingState.Id ,
                        Phone = order.ShippingAddress.Phone,
                        Email =  order.ShippingAddress.Email,
                        CountryName = order.ShippingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        CountryId =  order.ShippingAddress.Country.Id,
                          Longitude=order.ShippingAddress.Longitude,
                  Latitude=order.ShippingAddress.Latitude
    };
                }

                return cartVm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private bool CheckStatusOfOrder(Order order)
        {
            //if true then the order is success
            if (order.OrderStatus == OrderStatus.CashOnDelivery
           || order.OrderStatus == OrderStatus.PaymentReceived
           || order.OrderStatus == OrderStatus.PaidByCoupon
           || order.OrderStatus == OrderStatus.Complete)
                return true;
            return false;
        }
        private (DateTime start, DateTime end) GetDateRange(TimeIntervals period, DateTime referenceDate)
        {


            return period switch
            {
                TimeIntervals.Weekly =>
                (
                    referenceDate.AddDays(-(int)referenceDate.DayOfWeek),
                    referenceDate.AddDays(6 - (int)referenceDate.DayOfWeek)
                ),
                TimeIntervals.Monthly =>
                (
                    new DateTime(referenceDate.Year, referenceDate.Month, 1),
                    new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month))
                ),
                TimeIntervals.Yearly =>
                (
                    new DateTime(referenceDate.Year, 1, 1),
                    new DateTime(referenceDate.Year, 12, 31)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
            };




        }
        private async Task<List<Order>> SuccessfullOrder()
        {

            var SuccessfullOrders = await _orderRepository.Query().Where(x =>
               (x.OrderStatus == OrderStatus.CashOnDelivery
           || x.OrderStatus == OrderStatus.PaymentReceived
           || x.OrderStatus == OrderStatus.PaidByCoupon
           || x.OrderStatus == OrderStatus.Complete))
              .ToListAsync();
            return SuccessfullOrders;
        }

        public Task<Result<CartVm>> ReOrderAsync(long orderId, long siteId, long userId)
        {
            throw new NotImplementedException();
        }

    }


}


