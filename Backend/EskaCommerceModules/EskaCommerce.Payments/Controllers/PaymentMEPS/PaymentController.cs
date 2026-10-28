
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using EskaCommerce.Payments.Models.PaymentMEPS;
using System;
using EskaCommerce.Payments;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using EskaCommerce.ShoppingCart.Services;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Services;
using EskaCMS.Core.Services;

using EskaCMS.Entities.Entity;
using EskaCMS.EmailSender.SMTP.Services;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EskaCMS.Infrastructure.Helpers;
using System.Web;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity.Payments;
using EskaCommerce.Payments.Helper.PaymentMEPS;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using System.Net.Http;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Helper;
using System.Globalization;
using Humanizer;
using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.Payments.Services;
using EskaCommerce.Payments.Services.Interface;

namespace EskaCommerce.Payments.Controllers.PaymentMEPS
{
    [ApiController]
    [Route("api/PaymentMEPS")]
    // [Authorize]
    public class PaymentController : BaseController
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IPaymentServices paymentServices;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpHelper _HttpHelper;
        private readonly IPaymentMepsService _paymentMepsService;






        public PaymentController(
        ICartService cartService,
        IOrderService orderService,
        IWorkContext workContext,
        IRepository<Payment> paymentRepository,
        IPaymentServices paymentServices,
        ICurrencyService currencyService,
        IPaymentMepsService paymentMepsService,
         IHttpHelper httpHelper, GatewayApiClient gatewayApiClient, NVPApiClient nvpApiClient, ILogger<PaymentController> logger) :base(gatewayApiClient, nvpApiClient, logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _paymentRepository = paymentRepository;
            this.paymentServices = paymentServices;
            _currencyService = currencyService;
            _HttpHelper = httpHelper;
            _paymentMepsService = paymentMepsService;
        }



        [HttpPost("EmpededHostedCheckout")]
        [Authorize]

        public async Task<IActionResult> EmpededHostedCheckout( [FromBody] CreateNetworkPaymentDto model)
        {
            try
            {

                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                var cultureId = _workContext.GetCurrentCultureId();

                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);

                var networkConfiguration = await _paymentMepsService.GetNetowrkPaymentSettings(SiteId);

                if (cart == null)
                {
                    throw new Exception("Cart is empty");
                }
                var orderCreationResult = await _orderService.CreateOrder(cart.Id, PaymentProviderHelper.NetworkProviderId, 0, SiteId, OrderStatus.PendingPayment);

                if (!orderCreationResult.Success)
                    throw new Exception(orderCreationResult.Error);


                var DecimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);
                var ISOCurrencySymbol =_currencyService.GetCurrency(networkConfiguration.AllowedCurrencies, orderCreationResult.Value.CurrencyCodeCustomer);
                var CurrencyRate =await _currencyService.GetCurrencyByName(ISOCurrencySymbol);


                var zeroDecimalOrderAmount = _currencyService.ConvertToCurrency(orderCreationResult.Value.OrderTotal, CurrencyRate, DecimalPlaces);
                try
                {
                    //custom data you can get it from order details 
                    var customData = new CustomizedObj() { cultureId = cultureId.ToString(), frontReturnUrl = model.RedirectUrl };

                    EmbeddedCheckoutPayloadVM Payload = new EmbeddedCheckoutPayloadVM(networkConfiguration.MERCHANT_NAME, orderCreationResult.Value.Id.ToString(), (double)zeroDecimalOrderAmount, ISOCurrencySymbol, "New Order", networkConfiguration.RETURN_URL, customData);
                    var result = await _HttpHelper.Post(new PostRequestVM()
                    {
                        BaseUrl = networkConfiguration.BASE_URL,
                        SerializedContent = JsonConvert.SerializeObject(Payload),
                        Url = networkConfiguration.VERSION + "/merchant/" + networkConfiguration.MERCHANT_ID + "/session",
                        BasicAuthCredinatials = new BasicAuth()
                        {
                            ClientId = "merchant." + networkConfiguration.MERCHANT_ID,
                            ClientSecret = networkConfiguration.MASTER_API_PASSWORD
                        }

                    });


                    CheckoutSessionVM checkoutSessionVM = new CheckoutSessionVM()
                    {
                        OrderId = orderCreationResult.Value.Id,
                        OrderCode = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderCreationResult.Value.Id.ToString())),
                        CheckoutSession = CheckoutSessionModel.toCheckoutSessionModel(result),

                    };
                    var cc = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt("1777"));
                    await paymentServices.CreatePayment(orderCreationResult.Value, checkoutSessionVM.OrderId.ToString(), PaymentProviderHelper.NetworkProviderId, null, PaymentStatus.Initiated);
                    await _orderService.UpdateOrderReferanceId(orderCreationResult.Value.Id,checkoutSessionVM.CheckoutSession.SuccessIndicator);

                    return Ok(checkoutSessionVM);

                }
                catch (Exception e)
                {
                    await paymentServices.CreatePayment(orderCreationResult.Value, null, "MEPS", e.Message, PaymentStatus.Failed);


                    return BadRequest(e.Message);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

         
        [HttpGet("EmpededHostedReceipt")]
        [AllowAnonymous]
        public async Task<IActionResult> EmpededHostedReceipt([FromQuery] string resultIndicator)
        {
            try
            {
                var Order = await _orderService.GetOrderByReferanceId(resultIndicator);
                long SiteId = Order.SiteId;
                Logger.LogInformation($"PaymentApiController HostedCheckoutReceipt action orderId {Order.Id} ");

                if (Order == null)
                {
                    Logger.LogError($"Order Id is not found ");
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = "Order Id is not found",
                        Message = "Order Id is not exist"
                    });
                }
                var payment = await _paymentRepository.Query().Where(x => x.OrderId == Order.Id).FirstOrDefaultAsync();
                if (payment == null)
                {
                    Logger.LogError($"Payment for order is not found ");
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = "Payment for order is not found",
                        Message = "Payment for order is not found"
                    });
                }


                var networkConfiguration = await _paymentMepsService.GetNetowrkPaymentSettings(SiteId);

                string response = await _HttpHelper.Get(new GetRequestVM()
                {
                    BaseUrl = networkConfiguration.BASE_URL,
                    Url = networkConfiguration.VERSION + "/merchant/" + networkConfiguration.MERCHANT_ID + "/order/" + Order.Id,
                    BasicAuthCredinatials = new BasicAuth()
                    {
                        ClientId = "merchant." + networkConfiguration.MERCHANT_ID,
                        ClientSecret = networkConfiguration.MASTER_API_PASSWORD
                    }
                });

                Logger.LogInformation($"Hosted checkout retrieve order response {response}");
                JObject jObject = JObject.Parse(response);
                string result = jObject["result"] != null ? jObject["result"].ToString() : "";
                TransactionResponseModel transactionResponseModel = null;

                           
                CustomizedObj data = jObject["transaction"][0]["order"]["custom"].ToObject<CustomizedObj>();
                           

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

                    await AddHeaders(Order.SiteId.ToString(), Order.CustomerId.ToString(),data.cultureId);

                    var cart = await _cartService.GetActiveCart(Order.CustomerId, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, SiteId);
                    await _paymentRepository.ExecuteInTransactionAsync(async () =>
                    {
                        payment.GatewayTransactionId = transactionResponseModel.OrderId;
                        payment.Status = PaymentStatus.Succeeded;
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentReceived, PaymentProviderHelper.NetworkProviderId, SiteId);
                        await _orderService.DeActiveCart(cart.Id);

                        await _orderService.ReserveSerials(Order.Id);
                        try
                        {
                            await SendReceipt(Order.Id);
                        }
                        catch (Exception e)
                        {
                            // to be logged in dcms log
                        }
                    });

                }
                else
                {
                    await _paymentRepository.ExecuteInTransactionAsync(async () =>
                    {
                        payment.GatewayTransactionId = transactionResponseModel.OrderId;
                        payment.Status = PaymentStatus.Failed;
                        payment.FailureMessage = "Hosted Checkout Receipt error";
                        _paymentRepository.Update(payment);

                        await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentFailed, PaymentProviderHelper.NetworkProviderId, SiteId);

                        await _paymentRepository.SaveChangesAsync();
                    });


                    Logger.LogError($"The payment was unsuccessful {result}");
                    return BadRequest(new ErrorViewModel
                    {
                        RequestId = getRequestId(),
                        Cause = "Payment was unsuccessful",
                        Message = "There was a problem completing your transaction."
                    });


                }

                return RedirectPermanent($"{data.frontReturnUrl}?orderId={HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(Order.Id.ToString()))}");


            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);
                return BadRequest(e.Message);
            }

        }


        [NonAction]

        private async Task SendReceipt(long orderId)
        {
            await _orderService.SendReceipt(orderId);
        }

        [HttpGet("SendReceipt/{orderId}")]

        public async Task<IActionResult> SendReceipts([FromRoute] long orderId)
        {
            await _orderService.SendReceipt(orderId);
            return Ok();
        }


        #region helper

        private async Task<bool> AddHeaders(string siteId, string customerId, string cultureId)
        {
            HttpContext.Request.Headers.Add("siteId", siteId);
            HttpContext.Request.Headers.Add("cultureId", cultureId);
            HttpContext.Request.Headers.Add("userId", customerId);

            return true;


        }
        #endregion
    }
}
