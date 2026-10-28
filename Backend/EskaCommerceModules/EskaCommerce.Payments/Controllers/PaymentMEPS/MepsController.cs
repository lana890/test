using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using EskaCommerce.Payments.Models.PaymentMEPS;
using System;

using EskaCommerce.ShoppingCart.Services;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Services;
using EskaCMS.Core.Services;

using EskaCMS.Entities.Entity;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EskaCMS.Infrastructure.Helpers;
using System.Web;

using EskaCMS.Entities.Entity.Payments;
using EskaCommerce.Payments.Helper.PaymentMEPS;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Services.Interface;
using OpenQA.Selenium;
using EskaCMS.Entities.Entity.Orders;
using Google.Apis.Util;
using System.Dynamic;
using System.Text.Json;

namespace EskaCommerce.Payments.Controllers.PaymentMEPS
{
    [ApiController]
    [Route("api/MEPS")]
    public class MepsController : BaseController
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IPaymentServices paymentServices;
        private readonly IPaymentMepsService paymentMepsService;
        private readonly ICurrencyService _currencyService;
        private readonly MepsConfigFactory _mepsConfig;
        private readonly GatewayApiConfig GatewayApiConfig;
        public MepsController(
        ICartService cartService,
        IOrderService orderService,
        IWorkContext workContext, MepsConfigFactory mepsConfig,

        IPaymentServices paymentServices,
        IPaymentMepsService paymentMepsService,
        ICurrencyService currencyService,
 GatewayApiClient gatewayApiClient, NVPApiClient nvpApiClient, ILogger<PaymentController> logger) : base(gatewayApiClient, nvpApiClient, logger)
        {
            _mepsConfig = mepsConfig;

            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            this.paymentServices = paymentServices;
            this.paymentMepsService = paymentMepsService;
            _currencyService = currencyService;
            GatewayApiConfig=_mepsConfig.GetMepsConfig();

        }
        [HttpGet("hostedCheckout")]
        [Authorize]
        public async Task<IActionResult> hostedCheckout([FromQuery] CartTypes CartType)
        {
            try
            {

                var SiteId =_workContext.GetCurrentSiteId();

                var CultureId =_workContext.GetCurrentCultureId();

                var currentUserId = await _workContext.GetCurrentUserId();

                var DecimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);

                var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);

                if (cart == null)
                {
                    throw new Exception("Cart is empty");

                }

                var orderCreationResult = await _orderService.CreateOrder(cart.Id, "MEPS", 0, SiteId, OrderStatus.PendingPayment);


                if (!orderCreationResult.Success)
                    throw new Exception(orderCreationResult.Error);

                var ISOCurrencySymbol = _currencyService.GetCurrency(GatewayApiConfig.Allowed_Currencies, orderCreationResult.Value.CurrencyCodeCustomer);
               
                var CurrencyRate = await _currencyService.GetCurrencyByName(ISOCurrencySymbol);

               // await _orderService.UpdateCustomerCurrency(_currencyService.ConvertToCurrency(orderCreationResult.Value.OrderTotal, CurrencyRate, DecimalPlaces), ISOCurrencySymbol, orderCreationResult.Value);

                GatewayApiRequest gatewayApiRequest = CreateCheckoutSession(orderCreationResult.Value, ISOCurrencySymbol, GatewayApiConfig, CultureId);

                CheckoutSessionVM checkoutSessionVM = new CheckoutSessionVM();
                try
                {
                    String response = GatewayApiClient.SendTransaction(gatewayApiRequest);

                    CheckoutSessionModel checkoutSessionModel = CheckoutSessionModel.toCheckoutSessionModel(response);

                    checkoutSessionVM.CheckoutJsUrl = $@"{GatewayApiConfig.GatewayUrl}/checkout/version/{GatewayApiConfig.Version}/checkout.js";
                    checkoutSessionVM.OrderId = orderCreationResult.Value.Id;

                    checkoutSessionVM.OrderCode = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderCreationResult.Value.Id.ToString()));

                    checkoutSessionVM.CheckoutSession = checkoutSessionModel;
                    checkoutSessionVM.Currency = ISOCurrencySymbol;

                    await paymentServices.CreatePayment(orderCreationResult.Value, checkoutSessionModel.Id, "MEPS", null, PaymentStatus.Initiated);
                }
                catch (Exception e)
                {
                    await paymentServices.CreatePayment(orderCreationResult.Value, null, "MEPS", e.Message, PaymentStatus.Failed);
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = e.InnerException != null ? e.InnerException.StackTrace : e.StackTrace,
                        Message = e.Message
                    });
                }

                return Ok(checkoutSessionVM);

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("hostedCheckout/{orderId}/{result}"), HttpGet("hostedCheckout/{orderId}/{result}/{sessionId}")]
        public async Task<IActionResult> HostedCheckoutReceipt([FromRoute(Name = "orderId")] long orderId, [FromRoute(Name = "result")] string result, [FromRoute(Name = "sessionId")] string sessionId)
        {

            try
            {
                Order Order = await _orderService.GetOrderById(orderId);

                if (Order == null)
                {
                    Logger.LogError($"Order Id is not found {orderId}");
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = "Order Id is not found",
                        Message = "Order Id is not exist"
                    });
                }

               
                if (Order.OrderStatus==OrderStatus.PaymentReceived)
                {
                    return Ok();
                }

                GatewayApiRequest gatewayApiRequest = new GatewayApiRequest(GatewayApiConfig)
                {
                    ApiOperation = "RETRIEVE_ORDER",
                    OrderId = GatewayApiConfig.GATEWAY_Enviroment + orderId.ToString(),
                    ApiMethod = GatewayApiClient.GET
                };

                gatewayApiRequest.buildOrderUrl();


                string response = GatewayApiClient.SendTransaction(gatewayApiRequest);

                Logger.LogInformation($"Hosted checkout retrieve order response {response}");
                JObject jObject = JObject.Parse(response);
                result = jObject["result"] != null ? jObject["result"].ToString() : "";
                TransactionResponseModel transactionResponseModel = null;


                if (result == "SUCCESS")
                {
                    try
                    {
                        transactionResponseModel = TransactionResponseModel.toTransactionResponseModel(response);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Hosted Checkout Receipt error : {JsonConvert.SerializeObject(e)}");

                        return BadRequest(new ErrorViewModel
                        {
                            RequestId = getRequestId(),
                            Cause = e.InnerException != null ? e.InnerException.StackTrace : e.StackTrace,
                            Message = e.Message
                        });
                    }
                    var cart = await _cartService.GetActiveCart(Order.CustomerId, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, Order.SiteId);

                    await paymentServices.UpdatePayment(Order, sessionId, "Meps", null, PaymentStatus.Succeeded);
                    await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentReceived, "MEPS Pay", Order.SiteId);
                    await _orderService.DeActiveCart(cart.Id);
                }
                else
                {
                    await paymentServices.UpdatePayment(Order, sessionId, "Meps", "Hosted Checkout Receipt error", PaymentStatus.Failed);

                    await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentFailed, "MEPS Pay", Order.SiteId);
                   
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = "Payment was unsuccessful",
                        Message = "There was a problem completing your transaction."
                    });


                }

                await _orderService.ReserveSerials(Order.Id);
                try
                {
                    await _orderService.SendReceipt(Order.Id);

                }
                catch (Exception e)
                {
                    // to be logged in dcms log
                }

                return Ok(transactionResponseModel);


            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);
                return BadRequest(e.Message);
            }

        }

        //private async  Task<CommenProperties> GetCommenData(dynamic result)
        //{
        //    try
        //    {

        //        var orderIdStr = result["order"]?["id"]?.ToString()?.Split('-')[1];
        //        var orderId = orderIdStr != null ? (long?)Convert.ToInt64(orderIdStr) : null;

        //        var cultureIdStr = result["order"]?["description"]?.ToString();
        //        var cultureId = cultureIdStr != null ? (long?)Convert.ToInt64(cultureIdStr) : null;

        //        var status = result["result"]?.ToString();

        //        var sessionId = result["authentication"]?["3ds"]?["transactionId"]?.ToString();

        //        return new CommenProperties
        //        {
        //            CultureId = cultureId.HasValue?0:cultureId.Value,
        //            OrderId = orderId.HasValue ? 0 : orderId.Value,
        //            SessionId = sessionId,
        //            Status = status
        //        };



        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}
        private GatewayApiRequest CreateCheckoutSession(Order order,string OrderCurrency, GatewayApiConfig gatewayApiConfig, long cultureId)
        {
            try
            {

                GatewayApiRequest gatewayApiRequest = new GatewayApiRequest(gatewayApiConfig);

                gatewayApiRequest.ApiOperation = "CREATE_CHECKOUT_SESSION";
                gatewayApiRequest.OrderId = gatewayApiConfig.GATEWAY_Enviroment + order.Id;
                gatewayApiRequest.OrderDescription = cultureId.ToString();
               // gatewayApiRequest.ReturnUrl = gatewayApiConfig.Return_Url;
                gatewayApiRequest.OrderCurrency = _currencyService.GetDefaultCurrencyCode();
                gatewayApiRequest.OrderAmount = decimal.Parse((Math.Round(order.CustomerCurrencyOrderTotal * 10) / 10).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                gatewayApiRequest.InteractionOperation = "PURCHASE";

                gatewayApiRequest.buildSessionRequestUrl();
                gatewayApiRequest.buildPayload();
                gatewayApiRequest.ApiMethod = GatewayApiClient.POST;

                return gatewayApiRequest;

            }
            catch (Exception)
            {

                throw;
            }
        }


    }
}
