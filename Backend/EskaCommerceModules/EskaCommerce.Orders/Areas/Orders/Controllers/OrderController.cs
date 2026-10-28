using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web;

using EskaCommerce.DinkToPdf;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using MediatR;
using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Entities.Entity.Catalog;
using System.Security.Cryptography;
using EskaCommerce.Orders.Services;
using SixLabors.ImageSharp.ColorSpaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCMS.Entities.Entity;
using MailKit.Search;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

using System.Text.Json;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using System.Web;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Enum;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Localization.Services.Interfaces;
using EskaCMS.Infrastructure.Models;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Route("api/Order")]
  [Authorize]
    public class OrderController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ProductSerialHistory> _productSerialHistory;
        private readonly ILocalizationServices localizationServices;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly ITestNotifier _testNotifier;
        private readonly IQrCodeService _qrCodeService;
        // private readonly IMediator _mediator;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IRepository<ProductSerial> _productSerialRepository;
        private readonly IApiMapper _ApiMapperService;
        private readonly IdcmsLogService _dcmsLogService;
        // private readonly IRSA_Helper _rSA_Helper;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IRepository<Order> orderRepository, /* IMediator mediator
            , */ IWorkContext workContext, ICurrencyService currencyService, IQrCodeService qrCodeService, ICartService cartService,
            IRepository<OrderItem> orderItemRepository, IRepository<ProductSerial> productSerialRepository,
            IRepository<ProductSerialHistory> productSerialHistory,
                        ILocalizationServices localizationServices,

            ITestNotifier testNotifier, IOrderService orderService,
            IApiMapper ApiMapperService, IdcmsLogService dcmsLogService, /* IRSA_Helper rSA_Helper, */
            ILogger<OrderController> logger
            )
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _workContext = workContext;
            _currencyService = currencyService;
            _qrCodeService = qrCodeService;
            // _mediator = mediator;
            _cartService = cartService;
            _testNotifier = testNotifier;
            _productSerialRepository = productSerialRepository;
            _productSerialHistory = productSerialHistory;
            this.localizationServices = localizationServices;
            _orderService = orderService;
            _ApiMapperService = ApiMapperService;
            _dcmsLogService = dcmsLogService;
            // _rSA_Helper = rSA_Helper;
            _logger = logger;
        }

        [HttpPost]
        [Route("User/OrderHistoryList")]
        public async Task<IActionResult> OrderHistoryList([FromBody] SmartSearchTableParam<OrderHistoryPaginationFillters> model)
        {
            var CultureId = _workContext.GetCurrentCultureId();
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var userId = await _workContext.GetCurrentUserId();

            // Base filter — no includes, used for counting and ID pagination
            var baseQuery = _orderRepository
                .Query()
                .Where(x => x.CustomerId == userId && x.ParentId == null &&
                    ((model.Search != null && model.Search.OrderStatuses != null && model.Search.OrderStatuses.Any())
                        ? model.Search.OrderStatuses.Contains(x.OrderStatus)
                        : true));

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)model.Pagination.Number);

            // Resolve only the IDs for the requested page — lightweight, no joins
            var pagedIds = await baseQuery
                .OrderByDescending(x => x.CreatedOn)
                .Skip(model.Pagination.Start * model.Pagination.Number)
                .Take(model.Pagination.Number)
                .Select(x => x.Id)
                .ToListAsync();

            // Load full entities for this page with all required navigation properties.
            // AsSplitQuery() is intentionally omitted: MySQL does not support CROSS APPLY /
            // OUTER APPLY that EF Core generates in split-query mode for correlated subqueries.
            var orderEntities = await _orderRepository.Query()
                .Where(x => pagedIds.Contains(x.Id))
                .Include(x => x.Shipment)
                .Include(x => x.Warehouse).ThenInclude(x => x.priceAndDestinations)
                .Include(x => x.OrderAdditionalFees).ThenInclude(x => x.Product).ThenInclude(x => x.ProductDetails)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.ProductDetails)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.ThumbnailImage)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.AttributeValues)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(oc => oc.Option).ThenInclude(o => o.optionDetails)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(oc => oc.Value).ThenInclude(v => v.productOptionValueDetails)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.Categories).ThenInclude(c => c.Category).ThenInclude(cat => cat.CategoryDetails)
                .Include(x => x.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.LinkedProductLinks).ThenInclude(l => l.Product).ThenInclude(p2 => p2.Categories).ThenInclude(c => c.Category).ThenInclude(cat => cat.CategoryDetails)
                .AsNoTracking()
                .ToListAsync();

            // Restore the sort order that was established by the ID query
            var orderedEntities = pagedIds
                .Select(id => orderEntities.FirstOrDefault(o => o.Id == id))
                .Where(o => o != null)
                .ToList();

            // Project to ViewModel in memory — avoids CROSS APPLY / OUTER APPLY entirely
            var orderVms = orderedEntities.Select(x => new OrderHistoryListItem(_currencyService)
            {
                Id = x.Id,
                CreatedOn = x.CreatedOn,
                SubTotal = x.OrderTotal,
                ShipmentId = x.Shipment?.FirstOrDefault()?.Id ?? 0,
                TrackingNumber = x.Shipment?.FirstOrDefault()?.TrackingNumber ?? "",
                ShippingDelivery = x.Shipment?.FirstOrDefault()?.ShippingDelivery ?? (DeliveryOptionEnum)0,
                PickupId = x.Shipment?.FirstOrDefault()?.PickupId,
                OrderStatus = x.OrderStatus.GetDisplayName(),
                EncryptedOrderId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(x.Id.ToString())),
                OrderItems = x.OrderItems?.Select(i => new OrderHistoryProductVm
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductDetails?.Where(d => d.CultureId == CultureId).Select(f => f.Name).FirstOrDefault(),
                    Quantity = i.Quantity,
                    Category = i.Product.AttributeValues == null
                        ? new List<CategoryValue>()
                        : i.Product.LinkedProductLinks?.Count() > 0
                            ? i.Product.LinkedProductLinks.SelectMany(l => l.Product.Categories).SelectMany(c => c.Category.CategoryDetails).Where(d => d.CultureId == CultureId).Select(d => new CategoryValue { Id = d.Category.Id, Name = d.Name, Description = d.Description }).ToList()
                            : i.Product.Categories?.SelectMany(c => c.Category.CategoryDetails).Where(d => d.CultureId == CultureId).Select(d => new CategoryValue { Id = d.Category.Id, Name = d.Name, Description = d.Description }).ToList() ?? new List<CategoryValue>(),
                    ThumbnailImage = i.Product.ThumbnailImage?.PublicUrl ?? "",
                    ProductOptions = i.Product.OptionCombinations == null
                        ? new List<OrderItemsProductOptionVM>()
                        : i.Product.OptionCombinations.Select(p => new OrderItemsProductOptionVM
                        {
                            Id = p.OptionId,
                            Name = p.Option.optionDetails?.Where(d => d.CultureId == CultureId).FirstOrDefault()?.Name ?? "",
                            Value = p.Value.productOptionValueDetails?.Where(d => d.CultureId == CultureId).Select(d => d.Value).FirstOrDefault()
                        }).ToList(),
                    Price = i.ProductPrice,
                    Currency = x.CurrencyCode
                }).ToList() ?? new List<OrderHistoryProductVm>(),
                OrderAdditionalFees = x.OrderAdditionalFees == null
                    ? new List<OrderAdditionalFeesVM>()
                    : x.OrderAdditionalFees.Select(o => new OrderAdditionalFeesVM
                    {
                        Amount = o.Amount,
                        Status = o.Status,
                        Note = o.Note,
                        ProductName = o.Product.ProductDetails?.Where(d => d.CultureId == CultureId).Select(d => d.Name).FirstOrDefault()
                    }).ToList()
            }).ToList();

            var allKeys = await localizationServices.GetLocalization(string.Empty, SiteId, CultureId);
            var sharedResources = allKeys.AsEnumerable()
                .Where(x => x.Key == "SharedResources")
                .FirstOrDefault();

            foreach (var item in orderVms)
            {
                item.OrderStatus = sharedResources.Value?
                    .Where(g => g.Key == item.OrderStatus)
                    .Select(X => X.Value)
                    .FirstOrDefault() ?? item.OrderStatus;
            }

            return Ok(new PaginationResult<OrderHistoryListItem>(orderVms, totalPages, totalCount));
        }
        [HttpGet]
        [Route("User/OrderSummary/{CartType}")]
        public async Task<IActionResult> OrderSummary([FromRoute] CartTypes CartType)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            var cart = await _cartService.GetActiveCartDetails(currentUserId, CartType, SiteId);
            if (cart == null)
            {
                cart = new CartVm(_currencyService);
            }
            return Ok(cart);
        }

        [HttpPost]
        [Route("User/OrderDetails")]
        public async Task<IActionResult> OrderDetails([FromBody] OrderIdVM Model)
        {
            try
            {
                await _dcmsLogService.AddError("From OrderDetails", null,  "Order Id: " + Model.orderId, null, null);

                string decryptedOrderId = EncryptDecryptHelper.Decrypt(Model.orderId);
                var OrderId = Convert.ToInt64(decryptedOrderId);
                var model = await _orderService.GetOrderDetailsById(OrderId);
                return Ok(model);
            }
            catch (Exception ex)
            {
                await _dcmsLogService.AddError("From OrderDetails", ex.Message, ex.Message + "Order Id: " + Model.orderId, null, null);

                return BadRequest(ex.Message);
            }
         
        }

        [HttpPost]
     
        [Route("User/GetOrderDetails")]
        public async Task<IActionResult> GetOrderDetails([FromBody] OrderIdVM Model)
        {
            try
            {

                var model = await _orderService.GetOrderDetailsById(int.Parse(Model.orderId));
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("User/PrintOrder/{id}")]
        public async Task<IActionResult> PrintOrder([FromRoute] long id)
        {
            try
            {


                var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var userId = await _workContext.GetCurrentUserId();

                var order = await _orderRepository
                    .Query()
                    //.Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                    //.Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                    //.Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                    .Include(x => x.OrderItems).ThenInclude(x => x.Product)
                    .ThenInclude(x => x.OptionCombinations)
                    .ThenInclude(x => x.Option)
                    .Include(x => x.Customer)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                if (order.CustomerId != userId)
                {
                    return BadRequest(new { error = "You don't have permission to view this order" });
                }

                var model = new OrderDetailVm(_currencyService)
                {
                    Id = order.Id,
                    IsMasterOrder = order.IsMasterOrder,
                    CreatedOn = order.CreatedOn,
                    OrderStatus = order.OrderStatus,
                    OrderStatusString = r.Replace(order.OrderStatus.ToString(), " "),
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.FullName,
                    CustomerEmail = order.Customer.Email,
                    ShippingMethod = order.ShippingMethod,
                    PaymentMethod = order.PaymentMethod,
                    PaymentFeeAmount = order.PaymentFeeAmount,
                    Subtotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    SubTotalWithDiscount = order.SubTotalWithDiscount,
                    TaxAmount = order.TaxAmount,
                    ShippingAmount = order.ShippingFeeAmount,
                    OrderTotal = order.OrderTotal,
                    OrderReferanceId = order.OrderReferanceId,
                    OrderNote = order.OrderNote,
                    OrderCurrencyCode = order.CurrencyCode,
                    OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                    {
                        Id = x.Id,
                        ProductId = x.Product.Id,
                        ProductName = x.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                        ProductPrice = x.ProductPrice,
                        Quantity = x.Quantity,
                        DiscountAmount = x.DiscountAmount,
                        ProductImage = x.Product.ThumbnailImage.PublicUrl,
                        TaxAmount = x.TaxAmount,
                        TaxPercent = x.TaxPercent,
                        OrderCurrencyCode = order.CurrencyCode,
                    }).ToList()
                };

                foreach (var item in model.OrderItems)
                {
                    string y = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string x = y + item.ProductImage;
                    byte[] imageArray = System.IO.File.ReadAllBytes(@x);
                    string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                    item.ProductImage = base64ImageRepresentation;
                }
                return Ok(model);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("User/GenerateQrCodeInvoice")]
        public async Task<IActionResult> CreateQrCodeInvoice([FromBody] QrCode model)
        {
            try
            {
                var GenerateQrCode = await _qrCodeService.GenerateQrCode(model.Url, model.OrderId);
                return Ok(GenerateQrCode);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost]
        [Route("User/RepushOrderNotification")]
        public async Task<IActionResult> RepushOrderNotification([FromBody] List<long> OrderIds)
        {
            var DecimalPlaces = Convert.ToInt32(_currencyService.GetDefaultDecimalPlaces(_workContext.GetCurrentSiteId()));
            var RoundDigit = Convert.ToInt32(Convert.ToInt32(_currencyService.GetDefaultRoundDigit()));
            RoundDigit = Convert.ToInt32(Math.Pow(10, RoundDigit));
            var format = $"F{DecimalPlaces}";

            for (int x = 0; x < OrderIds.Count(); x++)
            {
                long OrderId = OrderIds[x];

                try
                {
                    var siteId = _workContext.GetCurrentSiteId();

                    var OrderItem = _orderItemRepository.Query().Include(x => x.Order)
                        .ThenInclude(y => y.Customer).Include(x => x.Product)
                        .ThenInclude(x => x.AttributeValues)
                        .ThenInclude(x => x.AttributeValueDetails)
                        .Where(x => x.Order.Id == OrderId && x.Order.OrderStatus == OrderStatus.PaymentReceived && x.Product.IsProductSerial == true)
                        .FirstOrDefault();
                    var redirectURL = $"https://go.com.sa/user-order-invoice?orderId={OrderId}";
                    var emailTemplateId = "Aywa Purchase Admin Template";
                    var templateId = "Aywa SMS Order Arabic";
                    var Parameters = new List<EmailParamsVM>();

                    if (OrderItem == null)
                    {
                        return BadRequest("Order Was not Found");
                    }

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[SubtotalString]",
                        Value = _currencyService.FormatCurrency(OrderItem.Order.SubTotal)
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[RedirectURL]",
                        Value = redirectURL
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[name]",
                        Value = OrderItem.Order.Customer.FullName
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[ProductName]",
                        Value = OrderItem.Product.ProductDetails.Select(f => f.Name).FirstOrDefault()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[PaymentMethod]",
                        Value = OrderItem.Order.PaymentMethod.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[TaxAmountString]",
                        Value = OrderItem.TaxAmount.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[TaxPercent]",
                        Value = OrderItem.TaxPercent.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[OrderCurrencyCode]",
                        Value = OrderItem.Order.CurrencyCode.ToString()
                    });


                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[OrderTotal]",
                        Value = OrderItem.Order.OrderTotal.ToString()
                    });

                    var ReservedProductSerials = _productSerialRepository.Query().Where(x => x.OrderId == OrderId).ToList();
                    int reservedCount = ReservedProductSerials.Count();

                    List<ProductSerial> NewProductSerials = new();
                    if (reservedCount < OrderItem.Quantity)
                    {
                        NewProductSerials = _productSerialRepository.Query()
                            .Where(x => x.Status == EProductSerialStatus.Available &&
                                        x.ProductId == OrderItem.ProductId &&
                                        x.OrderId == null)
                            .Take(OrderItem.Quantity - reservedCount)
                            .ToList();

                    }

                    var productSerialStrings = "";

                    foreach (var productSerial in NewProductSerials)
                    {
                        productSerial.OrderId = OrderId;
                        productSerial.Status = EProductSerialStatus.Taken;
                        await _productSerialRepository.SaveChangesAsync();

                        var ProductSerialHistory = new ProductSerialHistory
                        {

                            ProductSerialId = productSerial.Id,
                            CreatedById = OrderItem.Order.CreatedById,
                            CreationDate = DateTimeOffset.Now,
                            OldStatus = productSerial.Status,
                            NewStatus = EProductSerialStatus.Taken,
                            OrderId = OrderId,
                            Note = "Repush and Change To Taken Value for OrderId=" + productSerial.OrderId.ToString()

                        };
                        _productSerialHistory.Add(ProductSerialHistory);
                        // Added by Israa
                        await _productSerialHistory.SaveChangesAsync();

                        productSerialStrings += productSerial.Code + " <br> ";


                    }

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

                    var qrCode = await _qrCodeService.GenerateQrCode(redirectURL, OrderId);

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[qrCode]",
                        Value = qrCode
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[productSerial]",
                        Value = OrderItem.Product.ProductDetails.Select(f => f.Name).FirstOrDefault().ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[CreatedOn]",
                        Value = DateTimeOffset.Now.ToString("dd/MM/yyyy")
                    });

                    var workflowInstanceId = string.Empty;
                    JObject cashSaleData = new JObject();

                    try
                    {


                        var cashSaleObj = new CashSaleVM();
                        var cash = new CashSaleAmountVM();
                        cash.totalAmount = OrderItem.Order.OrderTotal;
                        if (OrderItem.AdditionalDetails != null)
                        {
                            var additionalData = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(OrderItem.AdditionalDetails);
                            var personalInfo = additionalData["personalInfo"];
                            if (personalInfo != null)
                            {
                                personalInfo["Serials"] = Newtonsoft.Json.JsonConvert.SerializeObject(NewProductSerials.Concat(ReservedProductSerials).Select(x => x.Code).ToList());
                                personalInfo["DcsmsOrder"] = OrderItem.Order.Id.ToString();

                                var additionalDataString = Newtonsoft.Json.JsonConvert.SerializeObject(personalInfo);
                                cash.additionalInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(additionalDataString);
                            }

                        }

                        cashSaleObj.cashSale = cash;

                        var paymentList = new List<PaymentVM>();
                        var paymentObj = new PaymentVM();
                        paymentObj.paymentMethod = 3;
                        paymentObj.amount = OrderItem.Order.OrderTotal;
                        paymentList.Add(paymentObj);

                        cashSaleObj.payments = paymentList;

                        var productList = new List<BillingProductVM>();
                        var billingProductObj = new BillingProductVM();
                        billingProductObj.taxCode = string.Empty;
                        var attributeValue = OrderItem.Product.AttributeValues.SelectMany(x => x.AttributeValueDetails).OrderByDescending(x => x.CultureId).FirstOrDefault()?.Value ?? "AYWA_CARD";
                        billingProductObj.productCode = attributeValue;
                        billingProductObj.quantity = OrderItem.Quantity;
                        billingProductObj.taxAmount = Convert.ToDecimal(string.Format("{0:" + format + "}", Math.Round(OrderItem.Order.TaxAmount * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        billingProductObj.discountAmount = Convert.ToDecimal(string.Format("{0:" + format + "}", Math.Round(OrderItem.Order.DiscountAmount * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        billingProductObj.amount = Convert.ToDecimal(string.Format("{0:" + format + "}", Math.Round(OrderItem.ProductPrice * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture));

                        productList.Add(billingProductObj);

                        cashSaleObj.products = productList;

                        var serilizedObj = Newtonsoft.Json.JsonConvert.SerializeObject(cashSaleObj);
                        cashSaleData = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(serilizedObj);

                        var apiResult = await _ApiMapperService.HandleWebAPIRequests(28, cashSaleData, OrderItem.Order.CreatedById);

                        if (apiResult.Value == null)
                        {
                            throw new Exception("Something Went Wrong Calling Cash Sale API From Repush API");
                        }

                        workflowInstanceId = apiResult.Value.ToString();
                    }
                    catch (Exception ex)
                    {
                        await _dcmsLogService.AddError("Order Controller (Repush Notification)", ex.Message, "Error While Calling Cash Sale API From Repush Notification", $"Request:{Newtonsoft.Json.JsonConvert.SerializeObject(cashSaleData)}", null);
                    }

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[Id]",
                        Value = !string.IsNullOrEmpty(workflowInstanceId) ? workflowInstanceId : OrderId.ToString()
                    });

                    Parameters.Add(new EmailParamsVM
                    {
                        Param = "[orderId]",
                        Value = !string.IsNullOrEmpty(workflowInstanceId) ? workflowInstanceId : OrderId.ToString()
                    });


                    NotificationObject notificationObject = new NotificationObject
                    {
                        MessageBody = string.Empty,
                        MessageTitle = string.Empty,
                        SendVia = SendBy.MobileNumber,
                        SendValue = new List<string> { OrderItem.Order.Customer?.PhoneNumber },
                        NotificationType = NotificationType.SMS,
                        SMSProvider = SMSProviders.SMSUniFonic,
                        EmailOptions = new EmailSenderVM
                        {
                            Emails = new List<string>(),
                            TemplateId = templateId,
                            Params = Parameters
                        },
                        EmailTemplateId = emailTemplateId,

                    };




                    //notificationObject.SendValue[0] = "0512345678";
                    await _testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);
                    //notificationObject.SendValue[0] = "israa@gmail.com";
                    notificationObject.SendVia = SendBy.Email;
                    notificationObject.SendValue = new List<string> { OrderItem.Order.Customer?.Email };
                    notificationObject.NotificationType = NotificationType.Email;
                    notificationObject.EmailOptions.TemplateId = "Product Receipt Back End";
                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

                    //_orderItemRepository.Update(OrderItem);
                    await _orderItemRepository.SaveChangesAsync();


                }
                catch (Exception ex)
                {

                    return BadRequest($"Stopped at {OrderId} because of :{ex.Message}");
                }
            }
            return Ok();

        }


        [HttpPost("apply-coupon-for-admin-order")]
        public async Task<IActionResult> ApplyCouponForOrder([FromBody] ApplyCouponForOrderVM model)
        {
            try
            {

                var validationResult = await _orderService.ApplyCouponForOrder(model, true);

                return Json(validationResult);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ApplyCouponPlanForOrder")]
        public async Task<IActionResult> ApplyCouponPlanForOrder([FromBody] ApplyCouponForOrderVM model)
        {
            try
            {

                var validationResult = await _orderService.ApplyCouponPlanForOrder(model);

                return Json(validationResult);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("User/ChangeOrderCustomer")]
        public async Task<IActionResult> ChangeOrderCustomer([FromBody] UpdateOrderCustomerVM Model)
        {
            try
            {
                Model.NewUser = EncryptDecryptHelper.Decrypt(Model.NewUser);

                return Ok(await _orderService.UpdateOrderCustomer(long.Parse(Model.NewUser)));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Route("User/AddOrdersForBulkUsers/{UserGroupId}")]
        public async Task<IActionResult> AddOrdersForBulkUsers([FromForm] IFormFile UsersSheet, [FromRoute] long UserGroupId)
        {
            try
            {
                await _orderService.AddOrdersForBulkUsers(UsersSheet, 1, OrderStatus.PaymentReceived, UserGroupId);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpGet]
        [Route("User/GetTemplateOfAddingOrdersForBulkUsers/{ProductId}/{CultureId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTemplateOfAddingOrdersForBulkUsers([FromRoute] long ProductId, [FromRoute] long CultureId)
        {
            try
            {

                var Byte = await _orderService.CreateTemplate(ProductId, CultureId);


                return File(Byte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers Template.xlsx");


            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpGet]
        [Route("ReOrder")]
        public async Task<IActionResult> ReOrder(long OrderId)
        {
            try
            {
                long UserId =  await _workContext.GetCurrentUserId();
                long SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _orderService.ReOrder(OrderId, SiteId, UserId));
            }
            catch(Exception ex)
            {
                // Log the full exception (stack trace included) before flattening it to a
                // message-only 400 — otherwise the caller sees text like
                // "Value cannot be null. (Parameter 'source')" with no way to locate the throw site.
                _logger.LogError(ex, "ReOrder failed for OrderId {OrderId}.", OrderId);
                return BadRequest(ex.Message);
            }
        }

    }


}
