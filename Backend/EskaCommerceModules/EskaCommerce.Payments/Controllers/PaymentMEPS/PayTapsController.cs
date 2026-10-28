
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Filters.Helpers;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Models.PaymentMEPS;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.UserCatalog.Services;
using FirebaseAdmin;
using MailKit.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.Style;
using OpenQA.Selenium;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static System.Net.WebRequestMethods;

namespace EskaCommerce.Payments.Controllers.PaymentMEPS
{

    [Route("api/PayTaps")]
    // [Authorize]
    public class PayTapsController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Cart> _cartContext;
        // private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly ICurrencyService _currencyService;
        private readonly ISubscriptionServices subscriptionServices;
        // private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;

        // private readonly UserManager<User> _userManager;
        // private readonly IRSA_Helper _RSA_Helper;
        private readonly ISiteCultureService siteCultureService;
        // private IRepository<Warehouse> _warehouseRepository;
        private readonly IdcmsLogService idcmsLogService;
        private readonly IPaymentServices paymentServices;
        private readonly HttpClient httpClient;
        // private readonly IRepository<OrderAddress> _addressRepository;
        // Confirmed unused: never assigned (its assignment is already commented out below) and
        // never read (compiler warning CS0169).
        // private readonly ILogger _logger;
        private readonly IPaymentMepsService _IPaymentMepsService;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public PayTapsController(
            ICartService cartService,
            IOrderService orderService,
            IWorkContext workContext,
            /* IRSA_Helper rSA_Helper, */
            ISiteCultureService siteCultureService,
            /* IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository, */
            ICurrencyService currencyService,
            ISubscriptionServices subscriptionServices,
            /* IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, */
            /* UserManager<User> userManager, */
                   /* IRepository<OrderAddress> addressRepository, */
            /* IRepository<Warehouse> warehouseRepository, */
              /* ILoggerFactory loggerFactory, */
              IdcmsLogService idcmsLogService,
              IPaymentServices paymentServices,
              HttpClient httpClient,
              IPaymentMepsService IPaymentMepsService,
             IRepositoryWithTypedId<SiteSettings, string> SiteSettingsRepository,
              IRepository<Cart> cartContext,
              IHttpClientFactory httpClientFactory


             )

        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            // _RSA_Helper = rSA_Helper;
            this.siteCultureService = siteCultureService;
            // _paymentProviderRepository = paymentProviderRepository;
            _currencyService = currencyService;
            this.subscriptionServices = subscriptionServices;
            // _appSettingRepository = appSettingRepository;
            _cartContext=cartContext;
            // _userManager = userManager;

            // _addressRepository = addressRepository;
            // _warehouseRepository = warehouseRepository;
            this.idcmsLogService=idcmsLogService;
            this.paymentServices = paymentServices;
            this.httpClient=httpClient;
            // _logger = loggerFactory.CreateLogger<PayTapsController>();
            _IPaymentMepsService = IPaymentMepsService;
            siteSettingsRepository = SiteSettingsRepository;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("CreatePaymentPage")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentPage([FromQuery] CartTypes CartType, [FromQuery] string PaymentProviderId, [FromQuery] string ReturnUrl)
        {
            return await CreatePayment(CartType, PaymentProviderId, ReturnUrl);
        }

        [HttpPost("CPPaymentPage")]
        [Authorize]
        public async Task<IActionResult> CPPaymentPage([FromBody] RequestPayTabsViewModel model)
        {
            return await CreatePayment(model.CartType, model.PaymentProviderId, model.ReturnUrl);
        }



        [HttpGet("CreatePaymentMobile")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentMobile([FromQuery] CartTypes CartType, [FromQuery] string PaymentProviderId)
        {
            return await CreatePayment(CartType, PaymentProviderId, null);
        }

        private async Task<IActionResult> CreatePayment(CartTypes CartType, string PaymentProviderId, string ReturnUrl)
        {
            try
            {
                var providerConfigurations = await _IPaymentMepsService.GetPayTabsPaymentSettings(_workContext.GetCurrentSiteId(), PaymentProviderId);
                var currentUser = await _workContext.GetCurrentUser();
                var cart = await _cartService.GetActiveCart(currentUser.Id, CartType, _workContext.GetCurrentSiteId());
                var source = _workContext.GetCurrentPlatform();

                if (cart == null)
                    return NotFound();

                var cartDetails = await _cartService.GetActiveCartDetails(currentUser.Id, currentUser.Id, CartType, _workContext.GetCurrentSiteId());
                var orderCreationResult = await _orderService.CreateOrder(cart.Id, PaymentProviderId, 0, _workContext.GetCurrentSiteId(), OrderStatus.PendingPayment);

                if (!orderCreationResult.Success)
                    return BadRequest(orderCreationResult.Error);

                var isoCurrencySymbol = _currencyService.GetCurrency(providerConfigurations.AllowedCurrencies, orderCreationResult.Value.CurrencyCodeCustomer);

                try
                {
                    object result;
                    if (source == EPlatformSource.Mobile)
                    {
                        var mobileResult = await PrepareRequestMobile(currentUser, cartDetails, isoCurrencySymbol, orderCreationResult, providerConfigurations, CartType, PaymentProviderId);
                        await paymentServices.CreatePayment(orderCreationResult.Value, null, PaymentProviderId, null, PaymentStatus.Initiated);
                        return Ok(mobileResult);
                    }
                    else
                    {
                        var webResult = await PrepareRequest(currentUser, cartDetails, isoCurrencySymbol, orderCreationResult, providerConfigurations, CartType, PaymentProviderId, ReturnUrl);
                        await paymentServices.CreatePayment(orderCreationResult.Value, null, PaymentProviderId, null, PaymentStatus.Initiated);
                        return Ok(new RedirectUrlDto { RedirectUrl = webResult["redirect_url"] });
                    }
                }
                catch (Exception ex)
                {
                    await paymentServices.CreatePayment(orderCreationResult.Value, ex.Message, PaymentProviderId, null, PaymentStatus.Failed);
                    throw;
                }
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("VerifyResponseCallBack")]
        public async Task<IActionResult> VerifyResponseCallBack([FromBody] IPNCallBack content)
        {
            try
            {

                string requestSignature = Request.Headers["signature"].ToString();
                long OrderId = content.GetOrderId();
                var orderObj = await _orderService.GetOrderById(OrderId);
                var settings = await _IPaymentMepsService.GetPayTabsPaymentSettings(content.GetSiteId(), content.GetProvider());
                if (await _IPaymentMepsService.IsExternalRequestAuthorised(HttpContext, content.GetSiteId(), requestSignature, orderObj.PaymentMethod))
                {

                    if (orderObj.OrderStatus == OrderStatus.PaymentReceived || orderObj.OrderStatus == OrderStatus.PaymentFailed)
                    {
                        return Ok();
                    }

                    AddCustomHeaders(orderObj.SiteId, orderObj.CustomerId, content.GetCulture(), content.GetWarehouse());
                    string AcquirerResponse = content.payment_result.acquirer_message;
                    string ResponseStatus = content.payment_result.response_status;
                    string ResponseCode = content.payment_result.response_code;

                    string ResponseMessage = content.payment_result.response_message;
                    if (ResponseStatus == "A")
                    {
                        if (orderObj.Cart.CartType == CartTypes.Plan)
                        {
                            await subscriptionServices.UpdateUserPlanStatus(orderObj.Id, content.token, content.tran_ref);
                        }
                        await paymentServices.UpdatePayment(orderObj, content.tran_ref, orderObj.PaymentMethod, ResponseMessage, PaymentStatus.Succeeded);
                    //    await _orderService.OrderChangeStatus(Convert.ToInt64(OrderId), OrderStatus.PaymentReceived, string.Empty, orderObj.SiteId);
                        //await _orderService.DeActiveCart(orderObj.Cart.Id);
                        try
                        {
                            if (settings.enableEmail.Value)
                            {
                                await _orderService.SendReceipt(orderObj.Id);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception(e.Message);
                        }


                        if (!string.IsNullOrEmpty(settings.CP_URL))
                        {
                            try
                            {
                                var queryParams = new Dictionary<string, string>
                                                {
                                                    { "stepId",content.GeStep() },
                                                    { "userPropertiesId",content.GetSessionId() },
                                                    {"paymentStatus",ResponseStatus }
                                                };
                                 await SendGetAsync(settings.CP_URL, queryParams, orderObj, content.tran_ref);
                            }
                            catch (Exception ex)
                            {
                                //throw ex;
                            }

                        }
                        else
                        {
                            await _orderService.OrderChangeStatus(Convert.ToInt64(OrderId), OrderStatus.PaymentReceived, string.Empty, orderObj.SiteId);
                            await _orderService.DeActiveCart(orderObj.Cart.Id);
                        }
                        return Ok();

                    }
                    else
                    {

                        await paymentServices.UpdatePayment(orderObj, content.tran_ref, orderObj.PaymentMethod, ResponseMessage, PaymentStatus.Failed);
                        await _orderService.OrderChangeStatus(orderObj.Id, OrderStatus.PaymentFailed, ResponseMessage, orderObj.SiteId);
                        await _orderService.DeActiveCart(orderObj.Cart.Id);
                        return BadRequest("Payment Error: " + ResponseMessage);
                    }
                }
                else
                {

                    return BadRequest("Invalid Request!");
                }

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        #region Helpers
        private void AddCustomHeaders(long siteId, long customerId, long CultureId, long warehouseId)
        {
            try
            {
                HttpContext.Request.Headers.Add("siteId", siteId.ToString());
                HttpContext.Request.Headers.Add("userId", customerId.ToString());
                HttpContext.Request.Headers.Add("cultureId", CultureId.ToString());
                HttpContext.Request.Headers.Add("warehouseId", CultureId.ToString());

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<Dictionary<string, dynamic>> PrepareRequest(User currentUser, CartVm cart, string ISOCurrencySymbol, Result<EskaCMS.Entities.Entity.Orders.Order> Order, PayTabsConfig PayTabsSetting, CartTypes cartTypes, string PaymentProviderId, string ReturnUrl)
        {
            try
            {
                var warehouseId = _workContext.GetCurrentSiteId();
                var additionalDetails = cart.Items.Where(x => x.AdditionalDetails!=null).Select(x => x.AdditionalDetails).FirstOrDefault();
                var (order, currencySymbol, amount, decimalPlaces, defaultAddress, culture) = await PrepareCommonRequestData(Order, PayTabsSetting);
                var encryptedOrderId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(Order.Value.Id.ToString()));
                PayTabsTransaction objReq = new PayTabsTransaction
                {
                    profile_id = int.Parse(PayTabsSetting.ProfileId),
                    tran_type = "sale",
                    tran_class = "ecom",
                    cart_amount = float.Parse((Math.Round(amount * 100) / 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    cart_currency = currencySymbol,
                    hide_shipping = true,
                    cart_id = $"{PayTabsSetting.Environment}{Order.Value.Id}",
                    cart_description = $"{PayTabsSetting.CartDesc}&{cart.SiteId}&{culture.Id}&{warehouseId}&{PaymentProviderId}"
                    ,
                    paypage_lang = culture.CultureCodeId.Split("-")[0],
                    customer_details = await PrepareAddress(currentUser, order, defaultAddress),
                    @return = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl+$"&OrderId={encryptedOrderId}" : (culture.CultureCodeId.ToLower().Contains("en") ?
                        PayTabsSetting.ReturnURLEN + $"?OrderId={encryptedOrderId}" :
                        PayTabsSetting.ReturnURLAR + $"?OrderId={encryptedOrderId}"),
                    callback = PayTabsSetting.CallBackURL
                };

                if (additionalDetails!=null)
                {
                    var obj = JsonConvert.DeserializeObject<dynamic>(additionalDetails);


                    if (obj["stepId"]!=null)
                    {
                        objReq.cart_description= objReq.cart_description+$"&{obj["stepId"]}&{obj["userPropertiesId"]}";

                    }


                }

                if (!string.IsNullOrEmpty(PayTabsSetting.Tokenize) && cartTypes == CartTypes.Plan)
                {
                    objReq.tokenise = Convert.ToInt32(PayTabsSetting.Tokenize);
                }

                objReq.customer_details.Street1 = order.ShippingAddress?.AddressLine1 ?? defaultAddress.AddressLine;
                objReq.customer_details.Country = order.ShippingAddress?.CountryId ?? defaultAddress.CountryCode;

                string json = JsonConvert.SerializeObject(objReq);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("authorization", PayTabsSetting.ServerKey);

                    HttpResponseMessage response = await client.PostAsync(PayTabsSetting.PayTapsURL, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                        return responseData;
                    }
                    else
                    {
                        throw new Exception(result);
                    }
                }
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        private async Task<(OrderDetailVm order, string ISOCurrencySymbol, decimal Amount, string DecimalPlaces, BillingDetailsViewModel DefaultAddress, SitesCulturesGetAllVM Culture)> PrepareCommonRequestData(Result<EskaCMS.Entities.Entity.Orders.Order> Order, PayTabsConfig PayTabsSetting)
        {
            try
            {


                var order = await _orderService.GetOrderDetailsById(Order.Value.Id);
                var ISOCurrencySymbol = _currencyService.GetCurrency(PayTabsSetting.AllowedCurrencies, Order.Value.CurrencyCodeCustomer);
                // Await the query here. Leaving it un-awaited started a hot EF Task on the request's
                // shared (scoped) DbContext, and the synchronous GetDefaultDecimalPlaces / async
                // BillingAddressId queries below then ran on that same context concurrently ->
                // "A second operation was started on this context instance...". Awaiting also removes
                // the sync-over-async .Result blocking call.
                var CurrencyRate = await _currencyService.GetCurrencyByName(ISOCurrencySymbol);
                var DecimalPlaces = _currencyService.GetDefaultDecimalPlaces(Order.Value.SiteId);
                var DefaultAddressSettings = await siteSettingsRepository.Query().Where(x => x.Id == "BillingAddressId").Select(x => x.Value).FirstOrDefaultAsync();
                var DefaultAddress = JsonConvert.DeserializeObject<BillingDetailsViewModel>(DefaultAddressSettings);
                var CultureId = _workContext.GetCurrentCultureId();
                var Culture = siteCultureService.GetCultureById(CultureId);
                var Amount = _currencyService.ConvertToCurrency(Order.Value.OrderTotal, CurrencyRate, DecimalPlaces);

                return (order, ISOCurrencySymbol, Amount, DecimalPlaces, DefaultAddress, Culture);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private async Task<PaymentConfigurationMobileViewModel> PrepareRequestMobile(User currentUser, CartVm cart, string ISOCurrencySymbol, Result<EskaCMS.Entities.Entity.Orders.Order> Order, PayTabsConfig PayTabsSetting, CartTypes cartTypes, string PaymentProviderId)
        {
            try
            {
                var warehouseId = _workContext.GetCurrentSiteId();

                var additionalDetails = cart.Items.Where(x => x.AdditionalDetails!=null).Select(x => x.AdditionalDetails).FirstOrDefault();

                var (order, currencySymbol, amount, decimalPlaces, defaultAddress, culture) = await PrepareCommonRequestData(Order, PayTabsSetting);

                PaymentConfigurationMobileViewModel objReq = new PaymentConfigurationMobileViewModel
                {
                    ProfileID = int.Parse(PayTabsSetting.ProfileId),
                    Amount = float.Parse((Math.Round(amount * 100) / 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    Currency = currencySymbol,
                    EncryptedOrderId = EncryptDecryptHelper.Encrypt(Order.Value.Id.ToString()),
                    CartID = $"{PayTabsSetting.Environment}{Order.Value.Id}",
                    CartDescription = $"{PayTabsSetting.CartDesc}&{cart.SiteId}&{culture.Id}&{warehouseId}&{PaymentProviderId}",
                    languageCode = culture.CultureCodeId.Split("-")[0],
                    ClientKey = PayTabsSetting.ClientKey,
                    ServerKey = PayTabsSetting.ServerKey,
                    MerchantCountryCode = PayTabsSetting.MerchantCountryCode,
                    BillingDetails = await PrepareAddress(currentUser, order, defaultAddress)
                };
                if (additionalDetails!=null)
                {
                    var obj = JsonConvert.DeserializeObject<dynamic>(additionalDetails);
                    if (obj["stepId"]!=null)
                    {
                        objReq.CartDescription= objReq.CartDescription+$"&{obj["stepId"]}&{obj["userPropertiesId"]}";
                    }
                }
                objReq.BillingDetails.countryCode = order.ShippingAddress?.CountryId ?? defaultAddress.CountryCode;

                return objReq;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        private async Task<long> GetDefaultCulture()
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

        private async Task<dynamic> PrepareAddress(User currentUser, OrderDetailVm order, BillingDetailsViewModel BillingAddress)
        {
            dynamic address = new ExpandoObject();
            address.name = currentUser.FullName;
            address.email = currentUser.Email;
            address.phone = order.ShippingAddress?.Phone ?? currentUser.PhoneNumber;
            address.city =string.IsNullOrEmpty(order.ShippingAddress?.StateOrProvinceName) ? BillingAddress.State : order.ShippingAddress?.StateOrProvinceName;
            address.state = string.IsNullOrEmpty(order.ShippingAddress?.StateOrProvinceName) ? BillingAddress.State : order.ShippingAddress?.StateOrProvinceName;
            address.zip = order.ShippingAddress?.ZipCode ?? BillingAddress.Zip;
            address.addressLine = order.ShippingAddress?.AddressLine1 ?? BillingAddress.AddressLine;

            return address;
        }

        private async Task SendGetAsync(
      string baseUrl,
      Dictionary<string, string> queryParams,
      Order orderObj,
      string trans_ref)
        {
            var Message = "";
            var query = queryParams != null && queryParams.Any()
                ? "?" + string.Join("&", queryParams.Select(p =>
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"))
                : string.Empty;
            try
            {

                var fullUrl = baseUrl + query;
                var currentUserId = await _workContext.GetCurrentUserId();

                await idcmsLogService.AddInfo(
                    currentUserId,
                    "API Invoke Started",
                    fullUrl,
                    null,
                    null);

                httpClient.Timeout = TimeSpan.FromMinutes(10);

                var start = DateTime.UtcNow;
                using var response = await httpClient.GetAsync(fullUrl);
                var end = DateTime.UtcNow;

                string body = await response.Content.ReadAsStringAsync();

                Message=body;
                // 🚨 API ALWAYS RETURNS 200 → parse JSON
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int businessStatus = root.GetProperty("status").GetInt32();

                // ❌ BUSINESS ERROR
                if (businessStatus == 0)
                {
                    await _orderService.OrderChangeStatus(
                        Convert.ToInt64(orderObj.Id),
                        OrderStatus.PaymentFailed,
                        "CP business error",
                        orderObj.SiteId);

                    await _orderService.DeActiveCart(orderObj.Cart.Id);

                    await idcmsLogService.AddInfo(
                        currentUserId,
                        "CP Business Error",
                        "status = 0",
                        body,
                        null);

                    return;
                }


                if (root.TryGetProperty("data", out var dataArray)
                    && dataArray.ValueKind == JsonValueKind.Array
                    && dataArray.GetArrayLength() > 0)
                {
                    var firstItem = dataArray[0];

                    if (firstItem.TryGetProperty("formValue", out var formValueElement))
                    {
                        var item = orderObj.Cart.Items.FirstOrDefault();
                        if (item != null)
                        {
                            JObject additionalDetails;

                            if (string.IsNullOrWhiteSpace(item.AdditionalDetails))
                            {
                                additionalDetails = new JObject();
                            }
                            else
                            {
                                try
                                {
                                    additionalDetails = JObject.Parse(item.AdditionalDetails);
                                }
                                catch
                                {
                                    additionalDetails = new JObject();
                                }
                            }

                            JObject formValueObject =
                                JObject.Parse(formValueElement.GetRawText());

                            additionalDetails["formValue"] = formValueObject;

                            item.AdditionalDetails =
                                additionalDetails.ToString(Newtonsoft.Json.Formatting.None);

                            await _cartContext.SaveChangesAsync();
                        }
                    }
                }


              

                await _orderService.OrderChangeStatus(
                    Convert.ToInt64(orderObj.Id),
                    OrderStatus.PaymentReceived,
                    string.Empty,
                    orderObj.SiteId);
                await _orderService.DeActiveCart(orderObj.Cart.Id);

                await idcmsLogService.AddInfo(
                    currentUserId,
                    "API Response",
                    $"BusinessStatus: {businessStatus} | Duration: {(end - start).TotalSeconds} sec",
                    body,
                    null);
            }
            catch (TaskCanceledException ex)
            {
                await idcmsLogService.AddError(
                    "SendGetAsync",
                    "Timeout",
                    ex.Message,
                    $"Request {query} -----  Reponse {Message}",
                    null);
            }
            catch (Exception ex)
            {
                await idcmsLogService.AddError(
                    "SendGetAsync",
                    ex.Message,
                    ex.ToString(),
                    $"Request {query} -----  Reponse {Message}",
                    null);
            }
        }



        #endregion

    }
}
