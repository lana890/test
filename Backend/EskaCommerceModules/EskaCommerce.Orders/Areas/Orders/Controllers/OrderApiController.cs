using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Events;
using EskaCommerce.Orders.Services;
using Hangfire.MemoryStorage.Database;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static Google.Apis.Requests.BatchRequest;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize]
    [Route("api/orders")]
    public class OrderApiController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        private readonly ICurrencyService _currencyService;
        private readonly IOrderApiService _orderApiService;
        // private readonly IEmailSender _emailService;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<StateOrProvince> _stateRepository;
        // private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<ProductLink> productlinkRepository;
        private readonly IRepository<ProductSerial> productserialRepository;
        private readonly IRepository<ProductSerialHistory> productSerialHistory;
        private readonly IRepository<ProductCategory> productCategoryRepository;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IUserSiteService userSiteService;
        private readonly IOrderService _orderService;
        private readonly ISiteSettingsService _SiteSettings;
        private readonly IRepository<SitesCultures> _SitesCulturesRepository;
        private readonly IOrderAdditionalFeesServices orderAdditionalFeesServices;
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        private readonly ITopDeliveryServices topDeliveryServices;

        public OrderApiController(IRepository<Order> orderRepository, IWorkContext workContext,
            IMediator mediator,
            ICurrencyService currencyService, /* IEmailSender emailService, */ IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IRepository<District> districtRepository, IRepository<StateOrProvince> stateRepository,
            /* IRepository<OrderItem> orderItemRepository, */
            IRepository<ProductLink> productlinkRepository,
            IRepository<ProductSerial> productserialRepository,
            IRepository<ProductSerialHistory> ProductSerialHistory,
            IRepository<SitesCultures> SitesCulturesRepository,
            IOrderAdditionalFeesServices orderAdditionalFeesServices,
            IRepository<ProductCategory> productCategoryRepository,
            IRepositoryWithTypedId<Country, string> countryRepository, IRepository<StateOrProvince> stateOrProvinceRepository,
            IUserSiteService userSiteService,
            ILoyaltyWalletService loyaltyWalletService,
            ITopDeliveryServices topDeliveryServices,
            IOrderApiService orderApiService, IOrderService orderService
, ISiteSettingsService siteSettings)
        {
            _orderService = orderService;
            _orderRepository = orderRepository;
            _workContext = workContext;
            _mediator = mediator;
            _currencyService = currencyService;
            // _emailService = emailService;
            _appSettingRepository = appSettingRepository;
            _districtRepository = districtRepository;
            _stateRepository = stateRepository;
            // _orderItemRepository = orderItemRepository;
            this.productlinkRepository = productlinkRepository;
            this.productserialRepository = productserialRepository;
            productSerialHistory = ProductSerialHistory;
            this.productCategoryRepository = productCategoryRepository;
            _orderApiService = orderApiService;
            _countryRepository = countryRepository;
            _stateOrProvinceRepository = stateOrProvinceRepository;
            this.userSiteService = userSiteService;
            _SiteSettings = siteSettings;
            _SitesCulturesRepository = SitesCulturesRepository;
            this.orderAdditionalFeesServices = orderAdditionalFeesServices;
            _loyaltyWalletService = loyaltyWalletService;
            this.topDeliveryServices=topDeliveryServices;
        }

        [HttpGet]
        public async Task<ActionResult> Get(int status, int numRecords)
        {
            try
            {
                var orderStatus = (OrderStatus)status;
                if ((numRecords <= 0) || (numRecords > 100))
                {
                    numRecords = 5;
                }

                var query = _orderRepository.Query();
                if (orderStatus != 0)
                {
                    query = query.Where(x => x.OrderStatus == orderStatus);
                }

                var SiteId = _workContext.GetCurrentSiteId();
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin"))
                {
                    query = query.Where(x => x.VendorId == currentUser.VendorId);
                }

                var model = query.OrderByDescending(x => x.CreatedOn)
                    .Take(numRecords)
                    .Select(x => new
                    {
                        x.Id,
                        CustomerName = x.Customer.FullName,
                        x.OrderTotal,
                        OrderTotalString = _currencyService.FormatCurrency(x.OrderTotal),
                        OrderStatus = x.OrderStatus.ToString(),
                        x.CreatedOn,
                        x.WarehouseId,
                        x.Warehouse.Name
                    });

                return Json(model);
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }

        }

        [HttpPost("grid")]
        public async Task<ActionResult> List([FromBody] SmartTableParam param)
        {
            try
             {
                var orders = await GetOrders(param);

                return Ok(orders);
            }
            catch (Exception EX)
            {

                return BadRequest(EX.Message);
            }

        }


        [HttpPost("GetCompletedOrders")]
        public async Task<ActionResult> GetCompletedOrders([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await GetOrders(param));
            }
            catch (Exception EX)
            {

                return BadRequest(EX.Message);
            }

        }


        [HttpGet("GetExportProperties")]
        public async Task<IActionResult> GetExportProperties()
        {
            try
            {
                var orderExportVm = new OrderExportVm();

                var properties = orderExportVm.GetType().GetProperties();

                var propertyNames = properties.Select(prop => prop.Name).ToList();

                return Ok(propertyNames);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }
        [HttpPost("GetOrdersByUserId")]
        public IActionResult GetOrdersByUserId(long userId, [FromBody] SearchTableParam<OrderListSearchVM> param)
        {
            try
            {
                GridOutputVM<OrderListOutputVM> orders = _orderApiService.GetFilteredUserOrders(userId, param);
                return Ok(orders);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{id}")]
     
        public async Task<IActionResult> Get(long id)
        {
            try
            {

                var order = await _orderRepository
                    .Query()
                    .Where(x => x.Id == id)
                    .Include(x => x.ProductSerials)
                    .Include(x => x.ShippingAddress)
                    .Include(x => x.Cart)
                    .Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince).ThenInclude(x => x.StateOrProvinceDetails)
                    .Include(x => x.ShippingAddress).ThenInclude(x => x.Country).ThenInclude(x => x.CountryDetails)
                    .Include(x => x.BillingAddress).ThenInclude(x => x.StateOrProvince).ThenInclude(x => x.StateOrProvinceDetails)
                    .Include(x => x.BillingAddress).ThenInclude(x => x.Country).ThenInclude(x => x.CountryDetails)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ProductDetails)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option).ThenInclude(x => x.optionDetails)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Value).ThenInclude(x => x.productOptionValueDetails)
                    .Include(x => x.OrderAdditionalFees)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                     .Include(x => x.OrderAdditionalFees)
                    .ThenInclude(x => x.Fees)
                    .Include(x => x.OrderAdditionalFees)
                    .ThenInclude(x => x.OrderAdditionalFeesHistories)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.ProductPriceWarehouses)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.ProductAdditionalFees)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.ProductSerial)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.LinkedProductLinks).ThenInclude(x => x.Product)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(x => x.Value).ThenInclude(x=>x.productOptionValueDetails)
                    .Include(x => x.Customer)
                    .Include(x => x.Warehouse)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound();
                }

                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var shippingDistrict = order.ShippingAddress is not null
                    ? await _districtRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ShippingAddress.DistrictId)
                    : null;
                var shippingState = order.ShippingAddress is not null
                    ? await _stateRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ShippingAddress.StateOrProvinceId)
                    : null;

                var billingDistrict = new District();
                var billingState = new StateOrProvince();

                if (order.BillingAddress != null)
                {
                    billingDistrict = await _districtRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.BillingAddress.DistrictId);
                    billingState = await _stateRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.BillingAddress.StateOrProvinceId);
                }
                var SiteId = _workContext.GetCurrentSiteId();
                var DecimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);
                var currentUser = await _workContext.GetCurrentUser();
                if (order.VendorId != currentUser.VendorId)
                {
                    return BadRequest(new { error = "You don't have permission to manage this order" });
                }
                var maxCultureId = order.OrderItems.SelectMany(x=>x.Product.ProductDetails)
                                     .Max(pd => (int?)pd.CultureId) ?? 0;
                var model = new OrderDetailVm(_currencyService)
                {
                    Id = order.Id,
                    IsMasterOrder = order.IsMasterOrder,
                    CreatedOn = order.CreatedOn,
                    OrderStatus = order.OrderStatus,
                    OrderStatusString = order.OrderStatus.GetDisplayName(),
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.FullName,
					UserName = order.Customer.UserName,

					PhoneNumber = order.Customer.PhoneNumber,
                    CustomerEmail = order.Customer.Email,
                    ShippingMethod = order.ShippingMethod,
                    PaymentMethod = order.PaymentMethod,
                    Subtotal = order.SubTotal,
                    RedeemAmount = order.RedeemAmount,
                    DiscountAmount = order.DiscountAmount,
                    SubTotalWithDiscount = order.SubTotalWithDiscount,
                    TaxAmount = order.TaxAmount,
                    OrderReferanceId = order.OrderReferanceId,
                    //ShippingAmount = order.Cart == null ? 0 : order.Cart.ShippingAmount ?? 0,
                    ShippingAmount=order.ShippingFeeAmount,
                    ShippingDiscountAmount=order.ShippingDiscountAmount,
                    OrderTotal = _currencyService.DcimalRound(order.OrderTotal),
                    OrderNote = order.OrderNote,
                    CouponCode=order.CouponCode,
                    Channel = order.Channel,
                    TotalFees = order.PaymentFeeAmount,
                    OrderAdditionalFees = order.OrderAdditionalFees == null ? new List<OrderAdditionalFeesVM>() : order.OrderAdditionalFees.Select(o => new OrderAdditionalFeesVM
                    {
                        Id = o.Id,
                        ProductId = o.ProductId,
                        Amount = o.Amount,
                        Status = o.Status,
                        Note = o.Note,
                        FeeName = o.Fees.Name,
                        Refundable = o.Fees.Refundable,
                        ProductName = o.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),
                        History = o.OrderAdditionalFeesHistories.Select(x => new OrderAdditionalFeesHistoryVM
                        {

                            NewStatus = x.NewStatus,
                            OldStatus = x.OldStatus,
                            Note = x.Note

                        }).ToList()

                    }).ToList(),
                    EncryptedOrderId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(order.Id.ToString())),
                    ShippingDelivery = order.Cart.ShippingDelivery,
                    WareHouseName = order.Warehouse != null ? order.Warehouse.Name : "",
                    OrderCurrencyCode = order.CurrencyCode,
                    ShippingAddress = order.ShippingAddress is null ? null : new ShippingAddressVm
                    {
                        AddressLine1 = order.ShippingAddress is null ? null : order.ShippingAddress.AddressLine1,
                        AddressLine2Only = order.ShippingAddress is null ? null : order.ShippingAddress.AddressLine2,
                        CityName = order.ShippingAddress is null ? null : order.ShippingAddress.City,
                        ZipCode = order.ShippingAddress is null ? null : order.ShippingAddress.ZipCode,
                        ContactName = order.ShippingAddress is null ? null : order.ShippingAddress.ContactName,
                        DistrictName = shippingDistrict != null ? shippingDistrict.DistrictDetails.Select(x => x.Name).FirstOrDefault() : "",
                        StateOrProvinceName = shippingState != null ? shippingState.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault() : "",
                        StateOrProvinceId = shippingState != null ? shippingState.Id : 0,
                        Phone = order.ShippingAddress is null ? null : order.ShippingAddress.Phone,
                        Email = order.ShippingAddress is null ? null : order.ShippingAddress.Email,
                        CountryName = order.ShippingAddress is null ? null : order.ShippingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        CountryId = order.ShippingAddress is null ? null : order.ShippingAddress.Country.Id
                    },
                    ProductSerial = order.OrderItems == null ? new ProductSerialViewModel() : order.OrderItems.Select(x => x.Product).Select(x => new ProductSerialViewModel
                    {

                        PlanName = x.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                        ProductSerialNumbers = x.ProductSerial != null
                        ? x.ProductSerial.Where(x => x.OrderId == order.Id).Select(x => x.Id).ToList() == null ? new List<string>() : order.ProductSerials.Select(x => x.Code).ToList()
                        : new List<string>(),



                    }).FirstOrDefault(),
                    OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                    {
                        Id = x.Id,
                        ProductId = x.Product.Id,
                        ProductName =
                              (x.Product?.ProductDetails?.Where(x=>x.CultureId==maxCultureId).Select(f => f.Name).FirstOrDefault() ?? string.Empty)
                                + " " +
                                string.Join("_",
                                    x.Product?.OptionCombinations?
                                        .SelectMany(o => o.Value?.productOptionValueDetails )
                                        .Where(d => d.CultureId ==
                                           maxCultureId
                                        )
                                        .Select(d => d.Value).ToList()
                                    ?? Enumerable.Empty<string>()
                                ),

                        ProductDate = x.Product.LinkedProductLinks.Count > 0 ? x.Product.LinkedProductLinks.FirstOrDefault(y => y.LinkedProductId == x.Product.Id).Product.SpecialPriceStart : x.Product.CreationDate,
                        ProductPrice = x.ProductPrice,
                        Quantity = x.Quantity,
                        DiscountAmount = _currencyService.DcimalRound(x.DiscountAmount),
                        TaxAmount = _currencyService.DcimalRound(x.TaxAmount),
                        QrCode = x.QrCode,
                        TaxPercent = x.TaxPercent,
                        AdditionalDetails = x.AdditionalDetails,
                        OrderCurrencyCode = order.CurrencyCode,
                        ProductAdditionalFees = x.Product.ProductAdditionalFees.Where(x => x.Status != EStatus.Deleted).Select(x => new ProductFeesVM
                        {
                            Name = x.Name,
                            FeesValue = x.FeeValue,
                            FeesType = x.FeeType,
                            ProductId = x.ProductId
                        }).ToList()
                    }).ToList()
                };

                if (order.BillingAddress != null)
                {
                    model.BillingAddress = new ShippingAddressVm
                    {
                        AddressLine1 = order.BillingAddress.AddressLine1,
                        AddressLine2Only = order.BillingAddress.AddressLine2,
                        CityName = order.BillingAddress.City,
                        ZipCode = order.BillingAddress.ZipCode,
                        ContactName = order.BillingAddress.ContactName,
                        DistrictName = billingDistrict != null ? billingDistrict.DistrictDetails.Select(x => x.Name).FirstOrDefault() : "",
                        StateOrProvinceName = billingState != null ? billingState.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault() : "",
                        StateOrProvinceId = billingState != null ? billingState.Id : 0,
                        Phone = order.BillingAddress.Phone,
                        Email = order.BillingAddress.Email,
                        CountryName = order.BillingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        CountryId = order.BillingAddress.Country.Id

                    };
                }

                model.ShipableContries = _countryRepository.Query().Include(x => x.CountryDetails)

                    .Where(x => x.IsShippingEnabled)
                    .Select(x => new SelectListItem
                    {
                        Text = x.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        Value = x.Id.ToString()
                    }).OrderBy(x => x.Text)
                       .ToList();

                if (model.ShipableContries.Count == 1)
                {
                    var onlyShipableCountryId = model.ShipableContries.First().Value;

                    model.StateOrProvinces = _stateOrProvinceRepository
                    .Query().Include(x => x.StateOrProvinceDetails)
                    .Where(x => x.CountryId == onlyShipableCountryId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                        Value = x.Id.ToString()
                    })
                    .OrderBy(x => x.Text)
                    .ToList();
                }


                if (order.IsMasterOrder)
                {
                    model.SubOrderIds = _orderRepository.Query().Where(x => x.ParentId == order.Id).Select(x => x.Id).ToList();
                }


                return Json(model);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost("change-order-status/{id}")]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] OrderStatusForm model)
        {
            try
            {
                var ReserveSerialsAfterPayment = await _SiteSettings.GetSiteSettingById("ReserveSerialsAfterPayment", await _workContext.GetCurrentSiteIdAsync());

                var order =await _orderRepository.Query().Include(x=>x.Cart).Include(x=>x.Shipment).FirstOrDefaultAsync(x => x.Id == id);
                if (order == null)
                {
                    return NotFound();
                }

                var SiteId = _workContext.GetCurrentSiteId();
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
                {
                    return BadRequest(new { error = "You don't have permission to manage this order" });
                }

                if (model.OrderReferanceIdInfo is not null)
                {
                    order.OrderReferanceId = model.OrderReferanceIdInfo;
                }


                if (System.Enum.IsDefined(typeof(OrderStatus), model.StatusId))
                {


                    if ((order.OrderStatus != OrderStatus.Canceled || order.OrderStatus != OrderStatus.Refunded) && ((OrderStatus)model.StatusId == OrderStatus.Canceled || (OrderStatus)model.StatusId == OrderStatus.Refunded))
                    {
                        if (order.RedeemAmount > 0)
                        {
                            await _loyaltyWalletService.RefundPoints(order.CustomerId, order.RedeemAmount, SiteId, order.Id, order.CartId);

                        }

                        await _loyaltyWalletService.RefundGainedPoints(order.CustomerId, SiteId, order.Id);
                    }
                    var oldStatus = order.OrderStatus;
                    order.OrderStatus = (OrderStatus)model.StatusId;
                    await _orderRepository.SaveChangesAsync();


                    var orderStatusChanged = new OrderChanged
                    {
                        OrderId = order.Id,
                        OldStatus = oldStatus,
                        NewStatus = order.OrderStatus,
                        Order = order,
                        UserId = currentUser.Id,
                        Note = model.Note
                    };

                    try
                    {
                        await _orderService.SendOrderStatus(order.Id, orderStatusChanged.OldStatus.Value, orderStatusChanged.NewStatus);
                    }
                    catch (Exception)
                    {

                    }

                    if ((OrderStatus)model.StatusId != OrderStatus.PaymentReceived &&
                        (ReserveSerialsAfterPayment == null || !bool.Parse(ReserveSerialsAfterPayment.Value) ||
                        ((OrderStatus)model.StatusId == OrderStatus.PendingPayment || (OrderStatus)model.StatusId == OrderStatus.Canceled)))
                    {
                        var ProductSerials = await productserialRepository.Query().Where(x => x.OrderId == id).ToListAsync();

                        if (ProductSerials.Count > 0)
                        {
                            var ProductSerialHistory = new List<ProductSerialHistory>();

                            foreach (var Serial in ProductSerials)
                            {
                                Serial.OrderId = null;
                                Serial.Status = EProductSerialStatus.Available;
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
                            await productserialRepository.SaveChangesAsync();
                            if (ProductSerialHistory.Count() > 0)
                            {
                                productSerialHistory.AddRange(ProductSerialHistory);
                                await productSerialHistory.SaveChangesAsync();
                            }

                        }



                    }



                    if ((OrderStatus)model.StatusId==OrderStatus.Canceled)
                    {
                        if (order.Cart!=null && order.Cart.ShippingDelivery==DeliveryOptionEnum.TopDelivery)
                        {
                            if (order.Shipment!=null && order.Shipment.Count()>0)
                            {
                                var shipment = order.Shipment.FirstOrDefault();
                                try
                                {
                                  await topDeliveryServices.CancelOrder(shipment.TrackingNumber, order.Id, shipment.Id);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    
                    
                    }    

                    await _orderService.ReserveSerials(order.Id);
                    await _mediator.Publish(orderStatusChanged);

                    return Accepted();
                }

                return BadRequest(new { Error = "unsupported order status" });


            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("order-status")]
        public IActionResult GetOrderStatus()
        {
            var model = EnumHelper.ToDictionary(typeof(OrderStatusAdmin)).Select(x => new { Id = x.Key, Name = x.Value });
            return Json(model);
        }

        [HttpPost("export")]
        public async Task<ActionResult<OrderExportVm>> Export([FromBody] SmartExportTableParam param)
        {
            try
            {


                var Currency = _currencyService.GetUserCurrencyCodeForAdmin();
                var SiteId = _workContext.GetCurrentSiteId();
                var plattform = _workContext.GetCurrentPlatform();
                var DecimalPlaces = Convert.ToInt32(_currencyService.GetDefaultDecimalPlaces(_workContext.GetCurrentSiteId()));
                var RoundDigit = Convert.ToInt32(Convert.ToInt32(_currencyService.GetDefaultRoundDigit()));
                RoundDigit = Convert.ToInt32(Math.Pow(10, RoundDigit));
                long cultureId = 0;
                if (plattform == EPlatformSource.Admin)
                {
                    cultureId = await _SitesCulturesRepository.Query().Where(x => x.CultureId == "en-US" && x.SiteId == SiteId).Select(x => x.Id).FirstOrDefaultAsync();
                }
                else
                {
                    cultureId = _workContext.GetCurrentCultureId();
                }



                var query = _orderRepository.Query()
                            .Where(x => x.SiteId == SiteId)
                            .Include(x => x.ProductSerials)
                            .Include(x => x.BillingAddress)
                            .Include(x => x.ShippingAddress)
                            .Include(x => x.Customer)
                            .ThenInclude(x => x.UserOptions)
                            .Include(x => x.OrderItems)
                            .Include(x => x.CartRuleUsage)
                            .ThenInclude(x => x.Coupon)
                            .Include(x => x.Warehouse)
                            .ThenInclude(x => x.priceAndDestinations)
                            .Include("OrderItems.Product")
                            .Include("OrderItems.Product.Categories")
                            .Include("OrderItems.Product.Categories.Category")
                            .Include("OrderItems.Product.Categories.Category.CategoryDetails")
                            .Include("OrderItems.Product.LinkedProductLinks")
                            .Include("OrderItems.Product.LinkedProductLinks.Product")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories.Category")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories.Category.CategoryDetails")
                            .Include("OrderItems.Product.ProductDetails")
                            .AsNoTracking()
                            .AsSplitQuery();

                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin"))
                {
                    query = query.Where(x => x.VendorId == currentUser.VendorId);
                }


                if (param.Search.PredicateObject != null)
                {
                    query = ApplyFiltersOrder(param.Search.PredicateObject, query);
                }


                var orders = await query
                    .Select(x => new OrderExportVm
                    {
                        Id = x.Id,
                        OrderStatus = (int)x.OrderStatus,
                        IsMasterOrder = x.IsMasterOrder,
                        DiscountAmount = x.DiscountAmount,
                        CreatedOn = x.CreatedOn.DateTime.ToLocalTime(),
                        OrderStatusString = x.OrderStatus.GetDisplayName(),
                        Channel = x.Channel.GetDisplayName(),
                        Gender = x.Customer.UserOptions == null ? "" : x.Customer.UserOptions.Where(x => x.PropertyName.ToLower() == "gender").Select(x => x.Value).FirstOrDefault(),
                        Nationality = x.Customer.UserOptions.Where(x => x.PropertyName.Contains("nationality")).Select(x => x.Value).FirstOrDefault(),
                        RaceCategory = x.Customer.UserOptions.Where(x => x.PropertyName.Contains("raceCategory")).Select(x => x.Value).FirstOrDefault(),
                        TShirtSize = x.Customer.UserOptions.Where(x => x.NormalizedPropertyName.Contains("TSHIRTSIZE")).Select(x => x.Value).FirstOrDefault(),
                        VIP = x.Customer.UserOptions.Where(x => x.PropertyName.Contains("VIP")).Select(x => x.Value).FirstOrDefault(),
                        YearOfBirth = x.Customer.DateOfBirth.HasValue ? x.Customer.DateOfBirth.Value.Year : 0,
                        MonthOfBirth = x.Customer.DateOfBirth.HasValue ? x.Customer.DateOfBirth.Value.Month : 0,
                        DayOfBirth = x.Customer.DateOfBirth.HasValue ? x.Customer.DateOfBirth.Value.Day : 0,
                        CategoryNames = x.OrderItems.Select(x => x.Product).SelectMany(x => x.LinkedProductLinks).Count() == 0 ? string.Join(',', x.OrderItems.Select(x => x.Product).SelectMany(x => x.Categories).SelectMany(y => y.Category.CategoryDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList()) : string.Join(',', x.OrderItems.Select(x => x.Product).SelectMany(x => x.LinkedProductLinks).SelectMany(x => x.Product.Categories).SelectMany(y => y.Category.CategoryDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList()),
                        PaymentFeeAmount = x.PaymentFeeAmount,
                        OrderTotal = Convert.ToDecimal((Math.Round(x.OrderTotal * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        Subtotal = Convert.ToDecimal((Math.Round(x.SubTotal * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        SubtotalWithDiscount = Convert.ToDecimal((Math.Round(x.SubTotalWithDiscount * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        PaymentMethod = x.PaymentMethod,
                        ShippingAmount = x.ShippingFeeAmount,
                        ShippingMethod = x.ShippingMethod,
                        TaxAmount = x.TaxAmount,
                        CustomerId = x.CustomerId,
                        NationalId = x.Customer.UserName,
                        CustomerName = x.Customer.FullName,
                        CustomerEmail = x.Customer.Email,
                        LatestUpdatedOn = x.LatestUpdatedOn.DateTime.ToLocalTime(),
                        Coupon = x.CartRuleUsage.Count() == 0 ? "" : x.CartRuleUsage.Select(x => x.Coupon).Count() == 0 ? "" : x.CartRuleUsage.Select(x => x.Coupon).FirstOrDefault().Code,
                        ProductName = x.OrderItems.SelectMany(x => x.Product.ProductDetails).Count() == 0 ? "" : x.OrderItems.SelectMany(x => x.Product.ProductDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                        Quentity = x.OrderItems.Sum(x => x.Quantity),
                        ProductSerial = x.ProductSerials.Count() == 0 ? "" : string.Join("/", x.ProductSerials.Where(p => p.OrderId == x.Id).Select(x => x.Code).ToList()),
                        Items = x.OrderItems.Count(),

                        BillingAddressId = x.BillingAddressId,
                        BillingAddressAddressLine1 = x.BillingAddress.AddressLine1,
                        BillingAddressAddressLine2 = x.BillingAddress.AddressLine2,
                        BillingAddressContactName = x.BillingAddress.ContactName,
                        BillingAddressCountryName = x.BillingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        BillingAddressDistrictName = x.BillingAddress.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                        BillingAddressZipCode = x.BillingAddress.ZipCode,
                        BillingAddressPhone = x.BillingAddress.Phone,
                        BillingAddressStateOrProvinceName = x.BillingAddress.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                        ShippingAddressAddressLine1 = x.ShippingAddress.AddressLine1,
                        ShippingAddressAddressLine2 = x.ShippingAddress.AddressLine2,
                        ShippingAddressId = x.ShippingAddressId,
                        ShippingAddressContactName = x.ShippingAddress.ContactName,
                        ShippingAddressCountryName = x.ShippingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                        ShippingAddressDistrictName = x.ShippingAddress.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                        ShippingAddressPhone = x.ShippingAddress.Phone,
                        ShippingAddressStateOrProvinceName = x.ShippingAddress.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                        ShippingAddressZipCode = x.ShippingAddress.ZipCode,
                        SubtotalString = FormatRound(Currency, x.SubTotal, DecimalPlaces, RoundDigit),
                        DiscountAmountString = FormatRound(Currency, x.DiscountAmount, DecimalPlaces, RoundDigit),
                        SubtotalWithDiscountString = FormatRound(Currency, x.SubTotalWithDiscount, DecimalPlaces, RoundDigit),
                        TaxAmountString = FormatRound(Currency, x.TaxAmount, DecimalPlaces, RoundDigit),
                        ShippingAmountString = FormatRound(Currency, x.ShippingFeeAmount, DecimalPlaces, RoundDigit),
                        PaymentFeeAmountString = FormatRound(Currency, x.PaymentFeeAmount, DecimalPlaces, RoundDigit),
                        OrderTotalString = FormatRound(Currency, x.OrderTotal, DecimalPlaces, RoundDigit),
                        TeamName = x.Customer.UserOptions.Where(x => x.NormalizedPropertyName == "TEAM").FirstOrDefault().Value,
                        EstimatedDate = x.Shipment.Count() > 0 ? _orderService.GetEstimatedDeliveryDate(x.Warehouse, x.ShippingAddress, x.Shipment.Select(i => i.ShippingDelivery).FirstOrDefault()) : 0,
                        WareHouseName = x.Warehouse != null ? x.Warehouse.Name : null
                    }).OrderByDescending(x => x.Id)
                    .ToListAsync();



                //var csvString = CsvConverter.ExportCsv(orders, includedProperties: param.Properties);
                //var csvBytes = Encoding.UTF8.GetBytes(csvString);
                //// MS Excel need the BOM to display UTF8 Correctly
                //var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
                //return File(csvBytesWithUTF8BOM, "text/csv", "orders-export.csv");


                var file = await ExcelHelper.ExportExcelFile(orders, "OrderFile", ExcludedProperties: param.Properties);
                return File(file.FileBytes, file.ContentType, file.Name);



            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost("lines-export")]
        public async Task<ActionResult<OrderLineExportVm>> OrderLinesExport([FromBody] SmartExportTableParam param, [FromServices] IRepository<OrderItem> orderItemRepository)
        {
            try
            {



                var Currency = _currencyService.GetUserCurrencyCodeForAdmin();
                ; var DecimalPlaces = Convert.ToInt32(_currencyService.GetDefaultDecimalPlaces(_workContext.GetCurrentSiteId()));
                var RoundDigit = Convert.ToInt32(Convert.ToInt32(_currencyService.GetDefaultRoundDigit()));
                RoundDigit = Convert.ToInt32(Math.Pow(10, RoundDigit));

                var query = orderItemRepository.Query()
                                                  .AsNoTrackingWithIdentityResolution();

                var SiteId = _workContext.GetCurrentSiteId();

                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin"))
                {
                    query = query.Where(x => x.Order.VendorId == currentUser.VendorId);
                }

                if (param.Search.PredicateObject != null)
                {
                    query = ApplyFiltersItem(param.Search.PredicateObject, query);
                }


                var orderItems = await query
                            .Select(x => new OrderLineExportVm()
                            {
                                Id = x.Order.Id,

                                OrderStatus = (int)x.Order.OrderStatus,
                                IsMasterOrder = x.Order.IsMasterOrder,
                                DiscountAmount = x.Order.DiscountAmount,
                                CreatedOn = x.Order.CreatedOn.DateTime.ToLocalTime(),
                                OrderStatusString = x.Order.OrderStatus.GetDisplayName(),
                                Channel = x.Order.Channel.GetDisplayName(),
                                PaymentFeeAmount = x.Order.PaymentFeeAmount,
                                OrderTotal = Convert.ToDecimal((Math.Round(x.Order.OrderTotal * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                Subtotal = Convert.ToDecimal((Math.Round(x.Order.SubTotal * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                SubtotalWithDiscount = Convert.ToDecimal((Math.Round(x.Order.SubTotalWithDiscount * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                PaymentMethod = x.Order.PaymentMethod,
                                ShippingAmount = x.Order.ShippingFeeAmount,
                                ShippingMethod = x.Order.ShippingMethod,
                                Nationality = x.Order.Customer.UserOptions.Where(x => x.PropertyName.Contains("nationality")).Select(x => x.Value).FirstOrDefault(),
                                RaceCategory = x.Order.Customer.UserOptions.Where(x => x.PropertyName.Contains("raceCategory")).Select(x => x.Value).FirstOrDefault(),
                                VIP = x.Order.Customer.UserOptions.Where(x => x.PropertyName.Contains("VIP")).Select(x => x.Value).FirstOrDefault(),
                                TShirtSize = x.Order.Customer.UserOptions.Where(x => x.NormalizedPropertyName.Contains("TSHIRTSIZE")).Select(x => x.Value).FirstOrDefault(),

                                TaxAmount = x.Order.TaxAmount,
                                CustomerId = x.Order.CustomerId,
                                CustomerName = x.Order.Customer.FullName,
                                CustomerEmail = x.Order.Customer.Email,
                                LatestUpdatedOn = x.Order.LatestUpdatedOn.DateTime.ToLocalTime(),
                                Coupon = x.Order.CartRuleUsage.Count() == 0 ? "" : x.Order.CartRuleUsage.Select(x => x.Coupon).Count() == 0 ? "" : x.Order.CartRuleUsage.Select(x => x.Coupon).FirstOrDefault().Code,
                                Items = x.Order.OrderItems.Count(),
                                ProductSerial = string.Join("/", x.Order.ProductSerials.Where(p => p.OrderId == x.Order.Id).Select(x => x.Id).ToList()),
                                Gender = x.Order.Customer.UserOptions == null ? "" : x.Order.Customer.UserOptions.Where(x => x.PropertyName == "gender").Select(x => x.Value).FirstOrDefault(),
                                YearOfBirth = x.Order.Customer.DateOfBirth.HasValue ? x.Order.Customer.DateOfBirth.Value.Year : null,
                                MonthOfBirth = x.Order.Customer.DateOfBirth.HasValue ? x.Order.Customer.DateOfBirth.Value.Month : null,
                                DayOfBirth = x.Order.Customer.DateOfBirth.HasValue ? x.Order.Customer.DateOfBirth.Value.Day : null,
                                CategoryNames = string.Join(',', x.Product.Categories.SelectMany(x => x.Category.CategoryDetails).Select(x => x.Name).ToList()),
                                NationalId = x.Order.Customer.UserName,
                                BillingAddressId = x.Order.BillingAddressId,
                                BillingAddressAddressLine1 = x.Order.BillingAddress.AddressLine1 ?? "",
                                BillingAddressAddressLine2 = x.Order.BillingAddress.AddressLine2,
                                BillingAddressContactName = x.Order.BillingAddress.ContactName,
                                BillingAddressCountryName = x.Order.BillingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                                BillingAddressDistrictName = x.Order.BillingAddress.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                                BillingAddressZipCode = x.Order.BillingAddress.ZipCode,
                                BillingAddressPhone = x.Order.BillingAddress.Phone,
                                BillingAddressStateOrProvinceName = x.Order.BillingAddress.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                                ShippingAddressAddressLine1 = x.Order.ShippingAddress.AddressLine1,
                                ShippingAddressAddressLine2 = x.Order.ShippingAddress.AddressLine2,
                                ShippingAddressId = x.Order.ShippingAddressId.Value,
                                ShippingAddressContactName = x.Order.ShippingAddress.ContactName,
                                ShippingAddressCountryName = x.Order.ShippingAddress.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                                ShippingAddressDistrictName = x.Order.ShippingAddress.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                                ShippingAddressPhone = x.Order.ShippingAddress.Phone,
                                ShippingAddressStateOrProvinceName = x.Order.ShippingAddress.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                                ShippingAddressZipCode = x.Order.ShippingAddress.ZipCode,
                                OrderLineDiscountAmount = x.DiscountAmount,
                                OrderLineQuantity = x.Quantity,
                                OrderLineTaxAmount = x.TaxAmount,
                                OrderLineTaxPercent = x.TaxPercent,
                                Quentity = x.Quantity,
                                OrderLineId = x.Order.Id,
                                ProductId = x.ProductId,
                                ProductName = x.Product.ProductDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                ProductPrice = x.ProductPrice,
                                SubtotalString = FormatRound(Currency, x.Order.SubTotal, DecimalPlaces, RoundDigit),
                                DiscountAmountString = FormatRound(Currency, x.Order.DiscountAmount, DecimalPlaces, RoundDigit),
                                SubtotalWithDiscountString = FormatRound(Currency, x.Order.SubTotalWithDiscount, DecimalPlaces, RoundDigit),
                                TaxAmountString = FormatRound(Currency, x.Order.TaxAmount, DecimalPlaces, RoundDigit),
                                ShippingAmountString = FormatRound(Currency, x.Order.ShippingFeeAmount, DecimalPlaces, RoundDigit),
                                PaymentFeeAmountString = FormatRound(Currency, x.Order.PaymentFeeAmount, DecimalPlaces, RoundDigit),
                                OrderTotalString = FormatRound(Currency, x.Order.OrderTotal, DecimalPlaces, RoundDigit),

                                OrderLineTaxAmountString = FormatRound(Currency, x.Order.TaxAmount, DecimalPlaces, RoundDigit),
                                OrderLineProductPriceString = FormatRound(Currency, x.ProductPrice, DecimalPlaces, RoundDigit),
                                OrderLineDiscountAmountString = FormatRound(Currency, x.DiscountAmount, DecimalPlaces, RoundDigit),
                                OrderLineTotalString = FormatRound(Currency, x.Quantity * x.ProductPrice, DecimalPlaces, RoundDigit),
                                OrderLineRowTotalString = FormatRound(Currency, (x.Quantity * x.ProductPrice) + x.TaxAmount - x.DiscountAmount, DecimalPlaces, RoundDigit)
                             ,
                                EstimatedDate = x.Order.Shipment.Count() > 0 ? _orderService.GetEstimatedDeliveryDate(x.Order.Warehouse, x.Order.ShippingAddress, x.Order.Shipment.Select(i => i.ShippingDelivery).FirstOrDefault()) : 0,
                                WareHouseName = x.Order.Warehouse != null ? x.Order.Warehouse.Name : null
                            })
                            .OrderByDescending(x => x.Id)
                            .ToListAsync();



                var file = await ExcelHelper.ExportExcelFile(orderItems, "OrderFile", ExcludedProperties: param.Properties);
                return File(file.FileBytes, file.ContentType, file.Name);



            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPost("RequestOrder")]
        [EnableCors("ApiCorsPolicy")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOrder([FromBody] RequestOrder data)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var adminemails = await _appSettingRepository.Query().Where(x => x.Id == "AdminEmail").Select(x => x.Value).FirstOrDefaultAsync();
                var emails = JsonConvert.DeserializeObject<EmailsVM>(adminemails);
                if (data == null)
                    return Ok(new { ErrorCode = 999, ErrorEDesc = "invalid data", ErrorADesc = "invalid data" });

                DateTime? orderDate = Convert.ToDateTime(data.OrderTime);
                data.OrderTime = orderDate.Value.ToString("dd/MMM/yyyy hh:mm tt");
                var emailpath = "wwwroot/modules/core/email-template/requestorder.html";
                var EMailBody = System.IO.File.ReadAllText(emailpath);
                EMailBody = EMailBody.Replace("[FirstName]", data.FirstName);
                EMailBody = EMailBody.Replace("[LastName]", data.LastName);
                EMailBody = EMailBody.Replace("[OrderDate]", data.OrderTime);
                string Items = "";
                foreach (var item in data.products)
                {
                    Items = Items + "<tr><td style='border-bottom:solid 1px #1c3b78'>" + item.Id.ToString() + "</td><td style='border-bottom:solid 1px #1c3b78'>" + item.Name + "</td><td style='border-bottom:solid 1px #1c3b78'>" + Math.Round(item.Price, 2).ToString() + " USD" + "</td><td style='border-bottom:solid 1px #1c3b78'>" + item.Quantity.ToString() + "</td></tr>";
                    //Items = Items + "<td>" + item.ProductId.ToString() + "</td><td>" + item.Product.Name + "</td><td>" + Math.Round(item.Product.Price, 2).ToString() + " USD" + "</td><td>" + item.Quantity.ToString() + "</td>";

                }
                EMailBody = EMailBody.Replace("[ITEMS]", Items);

                //await _emailService.SendEmailAsync(emails.Emails, "Request Order", EMailBody, siteId, true);


                return Ok(new { ErrorCode = 0, ErrorEDesc = "success", ErrorADesc = "success" });


            }
            catch
            {
                return Ok(new { ErrorCode = 999, ErrorEDesc = "an error occurred please try again later", ErrorADesc = "an error occurred please try again later" });

            }

        }


        [HttpPost("GetOrderStatistics")]

        public async Task<IActionResult> GetOrderStatistics([FromBody] GetOrderStatistcsVM model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var allRequestedMonths = new List<DateTimeOffset>();

                var currentDate = model.From;
                while (currentDate <= model.To)
                {
                    allRequestedMonths.Add(new DateTimeOffset(currentDate.Year, currentDate.Month, 1, 0, 0, 0, TimeSpan.Zero));
                    currentDate = currentDate.AddMonths(1);
                }






                var query = await _orderRepository.Query()
                            .Where(x => x.SiteId == siteId && ((x.CreatedOn.Year >= model.From.Year || x.CreatedOn.Year <= model.From.Year) && (x.CreatedOn.Month >= model.To.Month || x.CreatedOn.Month <= model.To.Month)) && (model.UserId.HasValue ? x.CustomerId == model.UserId.Value : true))
                            .Where(x => x.OrderStatus == OrderStatus.CashOnDelivery || x.OrderStatus == OrderStatus.PaidByCoupon || x.OrderStatus == OrderStatus.Complete || x.OrderStatus == OrderStatus.PaymentReceived)
                            .Where(x => model.WarehouseId.HasValue ? x.WarehouseId == model.WarehouseId : true)
                            .GroupBy(x => x.CreatedOn.Month)
                            .Select(x => new GetOrderStatistcsDto
                            {
                                OrderCount = x.Count(),   // Perf #15: was x.ToList().Count - counts in SQL like the sibling Min/Sum aggregates instead of materialising every order in the group
                                FormatedDate = x.Min(o => o.CreatedOn).ToString("yyyy-MM"),
                                Date = x.Min(o => o.CreatedOn),
                                TotalOrders = _currencyService.DcimalRound(x.Sum(x => x.OrderTotal))
                            })
                            .ToListAsync();


                foreach (var item in allRequestedMonths)
                {
                    if (query.Any(x => x.Date.Year == item.Date.Year && x.Date.Month == item.Date.Month))
                    {
                        continue;
                    }
                    query.Add(new GetOrderStatistcsDto { OrderCount = 0, FormatedDate = item.ToString("yyyy-MM"), Date = item, TotalOrders = 0 });
                }

                query = query.OrderBy(x => x.Date).ToList();





                return Ok(query);


            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }


        [HttpGet("DailyOrderStatistics")]
        [HttpGet("DailyOrderStatistics/{WarehouseId}")]

        public async Task<IActionResult> GetDailyOrderStatistics([FromRoute] long? WarehouseId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var query = await _orderRepository.Query().AsNoTracking()
                            .Where(x => x.SiteId == siteId && (x.CreatedOn.Year == DateTimeOffset.Now.Year && x.CreatedOn.Month == DateTimeOffset.Now.Month && x.CreatedOn.Day == DateTimeOffset.Now.Day))
                            .Where(x => x.OrderStatus == OrderStatus.CashOnDelivery || x.OrderStatus == OrderStatus.PaidByCoupon || x.OrderStatus == OrderStatus.Complete || x.OrderStatus == OrderStatus.PaymentReceived)
                            .Where(x => WarehouseId.HasValue ? x.WarehouseId == WarehouseId : true)
                            .ToListAsync();


                var statistics = new DailyOrderStatistics { OrderCount = query.Count, TotalOrdersAmount = _currencyService.DcimalRound(query.Sum(x => x.OrderTotal)) };




                return Ok(statistics);


            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }



        [HttpGet("OrderStatisticsForUser/{UserId}")]

        public async Task<IActionResult> OrderStatisticsForUser([FromRoute] long UserId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var query = await _orderRepository.Query()
                            .Where(x => x.SiteId == siteId && x.CustomerId == UserId)
                            .Where(x => x.OrderStatus == OrderStatus.CashOnDelivery || x.OrderStatus == OrderStatus.PaidByCoupon || x.OrderStatus == OrderStatus.Complete || x.OrderStatus == OrderStatus.PaymentReceived)
                            .ToListAsync();


                var statistics = new DailyOrderStatistics { OrderCount = query.Count, TotalOrdersAmount = _currencyService.DcimalRound(query.Sum(x => x.OrderTotal)) };




                return Ok(statistics);


            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }

        [HttpGet("PrintReceipt")]
        [AllowAnonymous]
        public async Task<IActionResult> PrintReceipt([FromQuery] string orderId, [FromQuery] long? cultureId, [FromQuery] long? siteId)
        {
            try
            {
                string decryptedOrderId = EncryptDecryptHelper.Decrypt(orderId);
                var OrderId = Convert.ToInt64(decryptedOrderId);



                if (siteId.HasValue)
                {
                    await _workContext.SetRequestHeader("siteId", siteId.Value.ToString());
                }

                var result = await _orderService.PrintReceipt(OrderId, cultureId);

                return File(result.FileBytes, result.ContentType, result.Name);


            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }


        [HttpPost("MakeOrder")]
        [AllowAnonymous]
        [EnableCors("ApiCorsPolicy")]
        public async Task<IActionResult> MakeOrder([FromBody] MakeOrder data)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var adminemails = await _appSettingRepository.Query().Where(x => x.Id == "AdminEmail").Select(x => x.Value).FirstOrDefaultAsync();
                var emails = JsonConvert.DeserializeObject<EmailsVM>(adminemails);
                if (data == null)
                    return Ok(new { ErrorCode = 999, ErrorEDesc = "invalid data", ErrorADesc = "invalid data" });

                DateTime? Date = Convert.ToDateTime(data.date);
                DateTime? Time = Convert.ToDateTime(data.time);
                data.time = Time.Value.ToString("t");
                data.date = Date.Value.ToString("dd/MMM/yyyy");
                var emailpath = "wwwroot/modules/core/email-template/makeorder.html";
                var EMailBody = System.IO.File.ReadAllText(emailpath);
                EMailBody = EMailBody.Replace("[CustomerName]", data.CustomerName);
                EMailBody = EMailBody.Replace("[CustomerPhone]", data.CustomerPhone);
                EMailBody = EMailBody.Replace("[Date]", data.date);
                EMailBody = EMailBody.Replace("[Time]", data.time);
                EMailBody = EMailBody.Replace("[Branch]", data.branch);
                EMailBody = EMailBody.Replace("[Address]", data.address);
                EMailBody = EMailBody.Replace("[Note]", data.Note);
                string Items = "";
                foreach (var item in data.products)
                {
                    Items = Items + " <tr><td width= '20%' >" + item.Id + "</td><td width= '30%'>" + item.Name + "</td><td width= '30%'>" + item.Description + "</td><td width= '20%'>" + item.Quantity + "</td></tr>";
                    //Items = Items + "<td>" + item.ProductId.ToString() + "</td><td>" + item.Product.Name + "</td><td>" + Math.Round(item.Product.Price, 2).ToString() + " USD" + "</td><td>" + item.Quantity.ToString() + "</td>";

                }
                EMailBody = EMailBody.Replace("[ITEMS]", Items);

                //await _emailService.SendEmailAsync(emails.Emails, "Make Order", EMailBody, siteId, true);

                return Ok(new { ErrorCode = 0, ErrorEDesc = "success", ErrorADesc = "success" });


            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = 999, ErrorEDesc = "an error occurred please try again later", ErrorADesc = "an error occurred please try again later" });

            }

        }


        [HttpPost("update-user-addresses/{id}")]
        [EnableCors("ApiCorsPolicy")]
        public async Task<IActionResult> UpdateUserAddresses(long id, [FromBody] UpdateAddressVM addressesVM)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var currentUserId = await _workContext.GetCurrentUserId();

            if (addressesVM.IsUpdateOrderAddress)
                await _orderApiService.updateAddresses(id, addressesVM, currentUserId);

            if (addressesVM.IsUpdateAdditionalAddresses)
                await _orderApiService.updateAdditionalAddresses(id, addressesVM, currentUserId);

            return Ok();
        }

        [HttpPost("GetTopOrderPlacingUsers")]
        public async Task<IActionResult> GetTopOrderPlacingUsers([FromBody] SmartTableParam param)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var query = _orderRepository.Query()
                                       .Where(x => x.SiteId == SiteId)
                                       .Where(x => x.OrderStatus == OrderStatus.PaymentReceived
                                       || x.OrderStatus == OrderStatus.CashOnDelivery
                                       || x.OrderStatus == OrderStatus.Complete
                                       || x.OrderStatus == OrderStatus.PaidByCoupon)
                                       .Include(c => c.Customer)
                                       .AsNoTracking();

                var Result = await query.GroupBy(x => new { x.Customer.Id, x.Customer.FullName, x.Customer.UserName }).Select(x => new GetTopOrderPlacingUsersVM
                {

                    FullName = x.Key.FullName,
                    UserId = x.Key.Id,
                    UserName = x.Key.UserName,
                    OrdersCount = x.Count()

                }).OrderByDescending(x => x.OrdersCount).PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return Ok(Result);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        [HttpPost("UpdateFeesToRefaund")]
        public async Task<IActionResult> UpdateFeesToRefaund([FromBody] UpdateOrderAdditionalFeesVM param)
        {
            try
            {
                return Ok(await orderAdditionalFeesServices.UpdateFeesToRefaund(param));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        public static List<OrderStatus> ConvertToOrderStatusList(dynamic dynamicObject)
        {
            if (dynamicObject is IList<OrderStatus> orderStatusCollection)
            {
                return orderStatusCollection.ToList();
            }

            // Handle invalid conversion
            throw new InvalidOperationException("The dynamic object cannot be converted to List<OrderStatus>.");
        }

        private IQueryable<OrderItem> ApplyFiltersItem(dynamic search, IQueryable<OrderItem> query)
        {
            if (search.Id != null)
            {
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }
            }

            if (search.CustomerName != null)
            {
                string customerName = search.CustomerName;
                query = query.Where(x => x.Order.Customer.FullName.Contains(customerName));
            }
            if (search.Email != null)
            {
                string Email = search.Email;
                query = query.Where(x => x.Order.Customer.Email.Contains(Email));
            }
            if (search.CreatedOn != null)

            {
                if (search.CreatedOn.before != null)
                {
                    DateTimeOffset Date = search.CreatedOn.before;
                    DateTimeOffset before = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 0, 0, 0, Date.Offset);
                    query = query.Where(x => x.Order.CreatedOn >= before.ToUniversalTime());
                }

                if (search.CreatedOn.after != null)
                {
                    DateTimeOffset Date = search.CreatedOn.after;
                    DateTimeOffset after = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 23, 59, 59, Date.Offset);
                    query = query.Where(x => x.Order.CreatedOn <= after.ToUniversalTime());
                }



            }
            if (search.Status != null)
            {

                List<OrderStatus> statusList = new List<OrderStatus>();

                foreach (dynamic status in search.Status)
                {
                    OrderStatus parsedStatus = (OrderStatus)(status);
                    statusList.Add(parsedStatus);
                }


                query = query.Where(x => statusList.Contains(x.Order.OrderStatus));
            }


            if (search.ProductSerial != null)
            {
                string ProductSerial = search.ProductSerial;
                query = query.Where(x => x.Order.ProductSerials.Any(y => y.Code.Contains(ProductSerial) && x.Order.ProductSerials.Any(p => p.OrderId == x.Id)));
            }
            if (search.Channel != null)
            {
                EPlatformSource Channel = (EPlatformSource)search.Channel;
                query = query.Where(x => x.Order.Channel == Channel);
            }
            if (search.CategoryId != null)
            {
                long CategoryId = Convert.ToInt64(search.CategoryId);

                var ProductCategory = productCategoryRepository.Query().Include(x => x.Product).ThenInclude(x => x.ProductLinks).Where(x => x.CategoryId == CategoryId).SelectMany(x => x.Product.ProductLinks).AsNoTracking().ToList();
                if (ProductCategory.Count > 0)
                {


                    query = query.Where(x => x.Order.OrderItems.Any(d => ProductCategory.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductCategory.Select(x => x.ProductId).Contains(d.ProductId)));

                }



            }

            if (search.ProductId != null)
            {

                long ProductId = Convert.ToInt64(search.ProductId);

                var ProductLikes = productlinkRepository.Query().Where(x => x.ProductId == ProductId).AsNoTracking().ToList();

                query = query.Where(x => x.Order.OrderItems.Any(d => ProductLikes.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductLikes.Select(x => x.ProductId).Contains(d.ProductId)));
            }
            if (search.orderReferanceId != null)
            {
                string OrderReferanceId = search.orderReferanceId.ToString();

                query = query.Where(x => x.Order.OrderReferanceId.Contains($"\"{OrderReferanceId}\""));
            }
            var warehouseIds = new List<long>();

            if (search.WarehouseId is IEnumerable<object> warehouseList)
            {
                foreach (var id in warehouseList)
                {
                    if (long.TryParse(id.ToString(), out var parsedId))
                        warehouseIds.Add(parsedId);
                }
            }

            if (warehouseIds.Any())
            {
                query = query.Where(x => warehouseIds.Contains(x.Order.WarehouseId));
            }




            return query;
        }

        private IQueryable<Order> ApplyFiltersOrder(dynamic search, IQueryable<Order> query)
        {
            if (search.Id != null)
            {
                long id = search.Id;
                query = query.Where(x => x.Id == id);
            }



            if (search.CustomerName != null)
            {
                string customerName = search.CustomerName;
                query = query.Where(x => x.Customer.FullName.Contains(customerName));
            }

            if (search.CreatedOn != null)

            {

                if (search.CreatedOn.before != null)
                {
                    DateTimeOffset Date = search.CreatedOn.before;
                    DateTimeOffset before = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 0, 0, 0, Date.Offset);
                    query = query.Where(x => x.CreatedOn >= before.ToUniversalTime());
                }

                if (search.CreatedOn.after != null)
                {
                    DateTimeOffset Date = search.CreatedOn.after;
                    DateTimeOffset after = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 23, 59, 59, Date.Offset);
                    query = query.Where(x => x.CreatedOn <= after.ToUniversalTime());
                }



            }

            if (search.Status != null)
            {

                List<OrderStatus> statusList = new List<OrderStatus>();

                foreach (dynamic status in search.Status)
                {
                    OrderStatus parsedStatus = (OrderStatus)(status);
                    statusList.Add(parsedStatus);
                }


                query = query.Where(x => statusList.Contains(x.OrderStatus));
            }




            if (search.ProductSerial != null)
            {
                string ProductSerial = search.ProductSerial;
                query = query.Where(x => x.ProductSerials.Any(y => y.Code.Contains(ProductSerial) && x.ProductSerials.Any(p => p.OrderId == x.Id)));
            }
            if (search.Channel != null)
            {
                EPlatformSource Channel = (EPlatformSource)search.Channel;
                query = query.Where(x => x.Channel == Channel);
            }
            if (search.CategoryId != null)
            {
                var categoryIds = new List<long>();

                if (search.CategoryId is IEnumerable<object> categoryList)
                {
                    foreach (var id in categoryList)
                    {
                        if (long.TryParse(id.ToString(), out var parsedId))
                        {
                            categoryIds.Add(parsedId);
                        }
                    }
                }


                var ProductCategory =  productCategoryRepository.Query().Include(x => x.Product).ThenInclude(x => x.ProductLinks).Where(x => categoryIds.Contains(x.CategoryId)).SelectMany(x => x.Product.ProductLinks).AsNoTracking().ToList();
                if (ProductCategory.Count > 0)
                {


                    query = query.Where(x => x.OrderItems.Any(d => ProductCategory.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductCategory.Select(x => x.ProductId).Contains(d.ProductId)));

                }



            }


            if (search.ProductId != null)
            {

                var productIds = new List<long>();

                if (search.ProductId is IEnumerable<object> productList)
                {
                    foreach (var id in productList)
                    {
                        if (long.TryParse(id.ToString(), out var parsedId))
                        {
                            productIds.Add(parsedId);
                        }
                    }
                }

                var ProductLikes =  productlinkRepository.Query().Where(x => productIds.Contains(x.ProductId)).AsNoTracking().ToList();

                query = query.Where(x => x.OrderItems.Any(d => ProductLikes.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductLikes.Select(x => x.ProductId).Contains(d.ProductId)));
            }
            if (search.orderReferanceId != null)
            {
                string OrderReferanceId = search.orderReferanceId.ToString();

                query = query.Where(x => x.OrderReferanceId.Contains($"\"{OrderReferanceId}\""));
            }
            var warehouseIds = new List<long>();

            if (search.WarehouseId is IEnumerable<object> warehouseList)
            {
                foreach (var id in warehouseList)
                {
                    if (long.TryParse(id.ToString(), out var parsedId))
                        warehouseIds.Add(parsedId);
                }
            }

            if (warehouseIds.Any())
            {
                query = query.Where(x => warehouseIds.Contains(x.WarehouseId));
            }

            return query;
        }
        private static string FormatRound(string Currency, decimal value, int DecimalPlace, int RoundDigit)
        {
            try
            {
                var format = $"F{DecimalPlace}";

                return string.Format("{0:" + format + "}", Math.Round((value) * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Currency;
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        private async Task<SmartTableResult<OrderDto>> GetOrders(SmartTableParam param)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                //var cultureId = _workContext.GetCurrentCultureId();
                var DecimalPlaces = Convert.ToInt32(_currencyService.GetDefaultDecimalPlaces(_workContext.GetCurrentSiteId()));
                var RoundDigit = Convert.ToInt32(Convert.ToInt32(_currencyService.GetDefaultRoundDigit()));
                RoundDigit = Convert.ToInt32(Math.Pow(10, RoundDigit));

                var currentUserId = await _workContext.GetCurrentUserId();

                var userRoles = await userSiteService.GetUserRoles(SiteId, currentUserId);

                var plattform = _workContext.GetCurrentPlatform();

                long cultureId = 0;
                if (plattform == EPlatformSource.Admin)
                {
                    cultureId = await _SitesCulturesRepository.Query().Where(x => x.CultureId == "en-US" && x.SiteId == SiteId).Select(x => x.Id).FirstOrDefaultAsync();
                }
                else
                {
                    cultureId = _workContext.GetCurrentCultureId();
                }

                var query = _orderRepository.Query()
                            .Where(x => x.SiteId == SiteId)
                            .Where(x => x.Warehouse.WarehouseRoles.Any() ? x.Warehouse.WarehouseRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                            .Include(c => c.Customer)
                            .ThenInclude(x => x.UserSites)
                            .ThenInclude(u => u.Currency)
                            .Include(x => x.ProductSerials)
                            .Include(x => x.OrderItems)
                            .ThenInclude(x => x.Product)
                            .ThenInclude(x => x.ProductLinks)
                            .ThenInclude(x => x.LinkedProduct)
                            .ThenInclude(x => x.Categories)
                            .Include(x => x.Warehouse)
                             
                            

                            .Include("OrderItems.Product")
                            .Include("OrderItems.Product.Categories")
                            .Include("OrderItems.Product.Categories.Category")
                            .Include("OrderItems.Product.Categories.Category.CategoryDetails")
                            .Include("OrderItems.Product.LinkedProductLinks")
                            .Include("OrderItems.Product.LinkedProductLinks.Product")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories.Category")
                            .Include("OrderItems.Product.LinkedProductLinks.Product.Categories.Category.CategoryDetails")
                            .Include("OrderItems.Product.ProductDetails")
                            .AsNoTracking();

                //var currentUser = await _workContext.GetCurrentUser();
                //if (!User.IsInRole("admin"))
                //{
                //    query = query.Where(x => x.VendorId == currentUser.VendorId);
                //}
                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Id != null)
                    {
                        long id = search.Id;
                        query = query.Where(x => x.Id == id);
                    }
                    if (search.UserId != null)
                    {
                        long userId = search.UserId;
                        query = query.Where(x => x.CustomerId == userId);
                    }

                    if (search.Status != null)
                    {

                        List<OrderStatus> statusList = new List<OrderStatus>();

                        foreach (dynamic status in search.Status)
                        {
                            OrderStatus parsedStatus = (OrderStatus)(status);
                            statusList.Add(parsedStatus);
                        }


                        query = query.Where(x => statusList.Contains(x.OrderStatus));
                    }

                    if (search.CustomerName != null)
                    {
                        string customerName = search.CustomerName;
                        query = query.Where(x => x.Customer.FullName.Contains(customerName));
                    }
                    if (search.shipmentType != null)
                    {
                        DeliveryOptionEnum ShipmentType =(DeliveryOptionEnum) Convert.ToInt64(search.shipmentType);
                        query = query.Where(x => x.Cart.ShippingDelivery==ShipmentType);
                    }
                    if (search.Email != null)
                    {
                        string Email = search.Email;
                        query = query.Where(x => x.Customer.Email.Contains(Email));
                    }

                    if (search.Phone != null)
                    {
                        string Phone = search.Phone;
                        query = query.Where(x => x.ShippingAddress.Phone.Contains(Phone) || x.Customer.PhoneNumber.Contains(Phone));
                    }

                    if (search.ProductSerial != null)
                    {
                        string ProductSerial = search.ProductSerial;
                        query = query.Where(x => x.ProductSerials.Any(y => y.Code.Contains(ProductSerial) && x.ProductSerials.Any(p => p.OrderId == x.Id)));
                    }
                    if (search.CategoryId != null)
                    {
                        var categoryIds = new List<long>();

                        if (search.CategoryId is IEnumerable<object> categoryList)
                        {
                            foreach (var id in categoryList)
                            {
                                if (long.TryParse(id.ToString(), out var parsedId))
                                {
                                    categoryIds.Add(parsedId);
                                }
                            }
                        }


                        var ProductCategory = await productCategoryRepository.Query().Include(x => x.Product).ThenInclude(x => x.ProductLinks).Where(x => categoryIds.Contains(x.CategoryId)).SelectMany(x => x.Product.ProductLinks).AsNoTracking().ToListAsync();
                        if (ProductCategory.Count > 0)
                        {


                            query = query.Where(x => x.OrderItems.Any(d => ProductCategory.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductCategory.Select(x => x.ProductId).Contains(d.ProductId)));

                        }



                    }


                    if (search.ProductId != null)
                    {

                        var productIds = new List<long>();

                        if (search.ProductId is IEnumerable<object> productList)
                        {
                            foreach (var id in productList)
                            {
                                if (long.TryParse(id.ToString(), out var parsedId))
                                {
                                    productIds.Add(parsedId);
                                }
                            }
                        }

                        var ProductLikes = await productlinkRepository.Query().Where(x => productIds.Contains( x.ProductId)).AsNoTracking().ToListAsync();

                        query = query.Where(x => x.OrderItems.Any(d => ProductLikes.Select(x => x.LinkedProductId).Contains(d.ProductId) || ProductLikes.Select(x => x.ProductId).Contains(d.ProductId)));
                    }


                    if (search.CreatedOn != null)

                    {
                        if (search.CreatedOn.before != null)
                        {
                            DateTimeOffset Date = search.CreatedOn.before;
                            DateTimeOffset before = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 0, 0, 0, Date.Offset);
                            query = query.Where(x => x.CreatedOn >= before.ToUniversalTime());
                        }

                        if (search.CreatedOn.after != null)
                        {
                            DateTimeOffset Date = search.CreatedOn.after;
                            DateTimeOffset after = new DateTimeOffset(Date.Year, Date.Month, Date.Day, 23, 59, 59, Date.Offset);
                            query = query.Where(x => x.CreatedOn <= after.ToUniversalTime());
                        }


                    }
                    if (search.Channel != null)
                    {
                        EPlatformSource Channel = (EPlatformSource)search.Channel;
                        query = query.Where(x => x.Channel == Channel);
                    }

                    if (search.orderReferanceId != null)
                    {
                        string OrderReferanceId = search.orderReferanceId.ToString();

                        query = query.Where(x => x.OrderReferanceId.Contains($"\"{OrderReferanceId}\""));
                    }

                    var warehouseIds = new List<long>();

                    if (search.WarehouseId is IEnumerable<object> warehouseList)
                    {
                        foreach (var id in warehouseList)
                        {
                            if (long.TryParse(id.ToString(), out var parsedId))
                                warehouseIds.Add(parsedId);
                        }
                    }

                    if (warehouseIds.Any())
                    {
                        query = query.Where(x => warehouseIds.Contains(x.WarehouseId));
                    }
                }

                var DefaultCurrencyCulture = _currencyService.GetDefaultCurrencyCulture();
                var DefaultDecimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);
                var format = $"F{DecimalPlaces}";
                var Source = _workContext.GetCurrentPlatform();
                var orders = query.ToSmartTableResult(
                    param,
                    order => new OrderDto
                    {
                     Id=   order.Id,
                        CustomerName = order.Customer.FullName,
                     
						OrderTotal =_currencyService.FormatRound(order.CurrencyCode, order.OrderTotal),
                        serials = order.ProductSerials.Count() > 0 ? order.ProductSerials.Select(x => x.Code).ToList() : new List<string>(),
                        OrderTotalString = _currencyService.FormatRound(order.CurrencyCodeCustomer, order.OrderTotal),
                        OrderStatus = order.OrderStatus.GetDisplayName(),
                        OrderStatusId = order.OrderStatus,
                        CreatedOn = new DateTimeOffset(order.CreatedOn.DateTime, TimeSpan.Zero),
                        Phone = order.ShippingAddress == null || order.ShippingAddress.Phone == null ? order.Customer.PhoneNumber : order.ShippingAddress.Phone,
                        Channel = order.Channel,
                        Gender = order.Customer.UserOptions.Count() > 0 ? "" : order.Customer.UserOptions.Where(x => x.NormalizedPropertyName == "GENDER").FirstOrDefault() == null ? "" : order.Customer.UserOptions.Where(x => x.NormalizedPropertyName == "GENDER").FirstOrDefault().Value,
                        CategoryNames = order.OrderItems.Select(x => x.Product).SelectMany(x => x.LinkedProductLinks).Count() == 0 ?
                        string.Join(',', order.OrderItems.Select(x => x.Product).SelectMany(x => x.Categories).SelectMany(y => y.Category.CategoryDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList()) :
                        string.Join(',', order.OrderItems.Select(x => x.Product).SelectMany(x => x.LinkedProductLinks).SelectMany(x => x.Product.Categories).SelectMany(y => y.Category.CategoryDetails).Where(x => x.CultureId == cultureId).Select(x => x.Name).ToList()),
                        Email = order.Customer.Email,
                        WareHouseName = order.Warehouse != null ? order.Warehouse.Name : "",
                        ShipmentType = order.Cart != null ? order.Cart.ShippingDelivery.GetDisplayName() : string.Empty

                    });
                return orders;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<IActionResult> GetCartOrderCharts(TimeIntervals time)
        {
            try
            {
                List<CartOrderChart> results = await _orderService.GetCartOrderCharts(time);
                return Ok(results);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

}
