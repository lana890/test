#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Sites.Services.Interfaces;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Models.PaymentPaypalExpress;
using EskaCommerce.Payments.Helper;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCommerce.Payments.Models.PaymentPaypalExpress.PaypalEnums;

namespace EskaCommerce.Payments.Controllers.PaymentPaypalExpress
{
    [Route("api/PaymentPaypal")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class PaypalController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Order> _orderRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICurrencyService _currencyService;
        // private readonly IEmailSender _emailService;
        // private readonly ISmsSender _smsService;
        // private readonly IConfiguration _config;
        private readonly ISites _siteServices;
        IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IPaymentServices _paymentServices;
        private readonly ILogger _logger;
        public PaypalController(
            ICartService cartService,
            IOrderService orderService,
            IWorkContext workContext,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IHttpClientFactory httpClientFactory,
            ICurrencyService currencyService,
            /* IEmailSender emailService, */
            /* ISmsSender smsService, */
            /* IConfiguration config, */
           IRepository<Order> orderRepository,
            ILoggerFactory loggerFactory,
           IPaymentServices paymentServices,
           ISites siteServices


            )
        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _httpClientFactory = httpClientFactory;
            _currencyService = currencyService;
            // _emailService = emailService;
            // _smsService = smsService;
            // _config = config;
            _logger = loggerFactory.CreateLogger<PaypalController>();
            _orderRepository = orderRepository;
            _paymentServices = paymentServices;
            _paymentProviderRepository = paymentProviderRepository;
            _siteServices = siteServices;

        }

        
        [Authorize]
        [HttpPost("CreatePayment")]
        public async Task<ActionResult> CreatePayment([FromBody] CreatePaymentDto model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var paypalSettings = await GetSetting(siteId);
                var accessToken = await GetAccessToken(paypalSettings);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, siteId);
                var cultureId = _workContext.GetCurrentCultureId();

                if (cart == null)
                {
                    return BadRequest("Cart is empty");
                }

                var orderCreationResult = await _orderService.CreateOrder(cart.Id, PaymentProviderHelper.PayPalProviderId, 0, siteId, OrderStatus.PendingPayment);

                if (!orderCreationResult.Success)
                {
                    return BadRequest(orderCreationResult.Error);
                }

                var OrderAmount = orderCreationResult.Value.CurrencyCode.Contains("USD") ? orderCreationResult.Value.OrderTotal : _currencyService.ConvertToDefault(orderCreationResult.Value.OrderTotal);

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var paymentCreateRequest =await  CreatePaymentRequest(OrderAmount, cultureId, model,siteId, paypalSettings);

                var response = await httpClient.PostAsJsonAsync($"https://api-m{paypalSettings.EnvironmentUrlPart}.paypal.com/v2/checkout/orders", paymentCreateRequest);

                return await HandlePaymentResponse(response, orderCreationResult.Value);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }


        }


        [AllowAnonymous]
        [HttpGet("CapturePayment")]
        public async Task<ActionResult> CapturePayment([FromQuery] string frontUrl, [FromQuery] string cultureId, [FromQuery] string token, [FromQuery] string PayerID)
        {
            try
            {


                var order = await _orderService.GetOrderByReferanceId(token);
                var paypalSettings = await GetSetting(order.SiteId);

                var accessToken = await GetAccessToken(paypalSettings);

                var payment = await _paymentServices.GetPaymentByOrderId(order.Id);

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var emptyBody = "{}";
                var content = new StringContent(emptyBody, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"https://api-m{paypalSettings.EnvironmentUrlPart}.paypal.com/v2/checkout/orders/{token}/capture", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                var capturePayment = JObject.Parse(responseBody);

                var status = capturePayment["status"]?.ToString();

                if (response.IsSuccessStatusCode && status == "COMPLETED")
                {
                    var cart = await _cartService.GetActiveCart(order.CustomerId, order.Cart.CartType, order.SiteId);

                    var gatewayTransactionId = capturePayment["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["id"]?.ToString();
                    //var cultureId = capturePayment["purchase_units"]?[0]?["custom_id"]?.ToString();

                    await AddHeaders(order.SiteId.ToString(), order.CustomerId.ToString(), cultureId);

                    await _paymentServices.UpdatePayment(order, gatewayTransactionId, PaymentProviderHelper.PayPalProviderId, string.Empty, PaymentStatus.Succeeded);

                    await _orderService.OrderChangeStatus(order.Id, OrderStatus.PaymentReceived, PaymentProviderHelper.PayPalProviderId, order.SiteId);

                    await _orderService.DeActiveCart(cart.Id);

                    try
                    {
                        await _orderService.SendReceipt(order.Id);
                    }
                    catch (Exception e)
                    {

                    }

                    //var host = await GetHostName(order.SiteId);

                    return RedirectPermanent(frontUrl + "?orderId=" + HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(order.Id.ToString())));
                }
                else
                {

                    await _paymentServices.UpdatePayment(order, string.Empty, PaymentProviderHelper.PayPalProviderId, status, PaymentStatus.Failed);
                    await _orderService.OrderChangeStatus(order.Id, OrderStatus.PaymentFailed, PaymentProviderHelper.PayPalProviderId, order.SiteId);


                    return BadRequest("Faild");
                }

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }


        }


        [AllowAnonymous]
        [HttpGet("CancelPayment")]
        public async Task<ActionResult> CancelPayment([FromQuery] string token, [FromQuery] string cancelUrl, [FromQuery] string cultureId)
        {
            try
            {

                var order = await _orderService.GetOrderByReferanceId(token);

                var paypalSettings = await GetSetting(order.SiteId);

                await AddHeaders(order.SiteId.ToString(), order.CustomerId.ToString(), cultureId);

                await _orderService.OrderChangeStatus(order.Id, OrderStatus.Canceled, PaymentProviderHelper.PayPalProviderId, order.SiteId);

                var payment = await _paymentServices.GetPaymentByOrderId(order.Id);

                await _paymentServices.UpdatePayment(order, string.Empty, PaymentProviderHelper.PayPalProviderId, PaymentStatus.Cancled.ToString(), PaymentStatus.Cancled);

                //var host = await GetHostName(order.SiteId);

                return RedirectPermanent(cancelUrl +"?orderId=" +HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(order.Id.ToString())));

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);

            }


        }
        
 

        #region CreatePaymentHelpers


        private async Task<PaymentCreateRequest> CreatePaymentRequest(decimal orderAmount, long cultureId, CreatePaymentDto model, long siteId , PaypalExpressConfigForm settings )
        {
            var host = await  GetHostName(siteId);
            return new PaymentCreateRequest
            {
                intent = "CAPTURE",
                purchase_units = new PurchesUnits[]
                {
            new PurchesUnits {
                amount = new Amount
                {
                    value = Math.Round(orderAmount, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    currency_code = "USD",
                },
                custom_id= cultureId.ToString()
                }
                },
                payment_source = new PaymentSource
                {
                    paypal = new Paypal
                    {
                        experience_context = new ExperienceContext
                        {
                            return_url = $"{settings.ApiReturnUrl}?frontUrl={model.ReturnUrl}&cultureId={cultureId.ToString()}",
                            cancel_url = $"{settings.ApiCancelUrl}?cancelUrl={model.CancelUrl}&cultureId={cultureId.ToString()}",
                            shipping_preference = ShippingPreference.NO_SHIPPING.ToString(),
                            user_action= UserAction.PAY_NOW.ToString(),
                       
                        }
                    }
                }
            };
        }
        private async Task<ActionResult> HandlePaymentResponse(HttpResponseMessage response, Order order)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var payment = JObject.Parse(responseBody);
            var status = payment["status"]?.ToString();

            if (response.IsSuccessStatusCode && status == "PAYER_ACTION_REQUIRED")
            {
                await _paymentServices.CreatePayment(order, string.Empty, PaymentProviderHelper.PayPalProviderId, string.Empty, PaymentStatus.Initiated);

                var paypalOrderId = payment["id"]?.ToString();
                await UpdateReferanceOrderId(order.Id, paypalOrderId);

                var links = payment["links"] as JArray;
                var payerLink = links?.FirstOrDefault(link => link["rel"]?.ToString() == "payer-action")?["href"]?.ToString();

                return Ok(new RedirectVM { RedirectUrl = payerLink});
            }
            else
            {
                await _paymentServices.CreatePayment(order, string.Empty, PaymentProviderHelper.PayPalProviderId, string.Empty, PaymentStatus.Failed);
                return BadRequest(responseBody);
            }
        }


        private async Task<bool> UpdateReferanceOrderId(long orderId, string ReferanceId)
        {

            var order = await _orderService.GetOrderById(orderId);

            order.OrderReferanceId = ReferanceId;

            _orderRepository.Update(order);
            _orderRepository.SaveChanges();
            return true;
        }



        #endregion


        #region CapturePaymentHelpers

        private async Task<Order> GetOrderByReferanceOrderId(string ReferanceId)
        {

            var order = await _orderRepository.Query()
                                        .Where(x => x.OrderReferanceId == ReferanceId)
                                        .Include(x => x.Cart)
                                        .FirstOrDefaultAsync();
            if (order is null)
            {
                throw new Exception("there is no order associated with this referance Id");
            }

            return order;
        }

        #endregion


        #region GeneralHelpers

        private async Task<PaypalExpressConfigForm> GetSetting(long siteId)
        {

            var paypalProvider = _paymentProviderRepository.Query().AsNoTracking().FirstOrDefault(x => x.Id == PaymentProviderHelper.PayPalProviderId && x.SiteId == siteId);
            if (paypalProvider is null)
            {
                throw new Exception("Please Add Paypal Configuration");
            }
            var paypalExpressSetting = JsonConvert.DeserializeObject<PaypalExpressConfigForm>(paypalProvider.AdditionalSettings);
            return paypalExpressSetting;
        }

        private async Task<bool> AddHeaders(string siteId, string customerId, string cultureId)
        {
            HttpContext.Request.Headers.Add("siteId", siteId);
            HttpContext.Request.Headers.Add("cultureId", cultureId);
            HttpContext.Request.Headers.Add("userId", customerId);

            return true;


        }

        private async Task<string> GetAccessToken(PaypalExpressConfigForm model)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{model.ClientId}:{model.ClientSecret}")));
            var requestBody = new StringContent("grant_type=client_credentials");
            requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync($"https://api-m{model.EnvironmentUrlPart}.paypal.com/v1/oauth2/token", requestBody);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic token = JObject.Parse(responseBody);
            string accessToken = token.access_token;
            return accessToken;
        }
        
        
        private async Task<string> GetHostName(long siteId)
        {
            var hotName = await _siteServices.GetActiveSiteHostName(siteId);

            return hotName.EndsWith('/') ? ("https://" + hotName) : ("https://" + hotName + "/");
        }




        #endregion





    }
}
