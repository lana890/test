using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCommerce.Orders.Services;
using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.Pricing.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Entities.Entity.Orders;

using Microsoft.Extensions.Logging;

using EskaCMS.Infrastructure.Web;
using EskaCMS.Entities.Entity;
using EskaCMS.EmailSender.SMTP.Services;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using EskaCommerce.Payments.Models.PaymentHyperPay;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System.Text;
using System.Net;
using System.IO;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure;
using MailKit.Search;
using OfficeOpenXml.Style;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Filters.Helpers;
using System.Web;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Payments.Helper;

namespace EskaCommerce.Payments.Controllers.PaymentHyperPay
{

    [Route("api/PaymentHyperpay")]
    [Authorize]
    public class HyperpayController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IRepository<Payment> _paymentRepository;
        // private readonly ICurrencyService _currencyService;

        // private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;

        // private readonly UserManager<User> _userManager;
        // private readonly IRSA_Helper _RSA_Helper;
        // private IRepository<Warehouse> _warehouseRepository;
        // private readonly IRepository<OrderAddress> _addressRepository;
        private readonly ILogger _logger;

        public HyperpayController(
            ICartService cartService,
            IOrderService orderService,
            IWorkContext workContext,
            /* IRSA_Helper rSA_Helper, */
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IRepository<Payment> paymentRepository,
            /* ICurrencyService currencyService, */
            /* IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, */
            /* UserManager<User> userManager, */
                   /* IRepository<OrderAddress> addressRepository, */
            /* IRepository<Warehouse> warehouseRepository, */
              ILoggerFactory loggerFactory
             )

        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            // _RSA_Helper = rSA_Helper;
            _paymentProviderRepository = paymentProviderRepository;
            _paymentRepository = paymentRepository;
            // _currencyService = currencyService;

            // _appSettingRepository = appSettingRepository;

            // _userManager = userManager;

            // _addressRepository = addressRepository;
            // _warehouseRepository = warehouseRepository;
            _logger = loggerFactory.CreateLogger<HyperpayController>();

        }

        [HttpGet("CreateHyperpaySession/{EntityType}")]

        public async Task<IActionResult> CreateHyperpaySession(HyperPayEntityType EntityType)
        {
            try
            {

                var stripeProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.HyperProviderId);

                var stripeSetting = JsonConvert.DeserializeObject<HyperConfigForm>(stripeProvider.AdditionalSettings);
                var SelectedEntityId = stripeSetting.EntitiesTypes.Where(x => x.EntityType == EntityType.ToString()).Select(y => y.EntityId).FirstOrDefault();
                var currentUser = await _workContext.GetCurrentUser();
                var cart = await _cartService.GetActiveCart(currentUser.Id, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, _workContext.GetCurrentSiteId());
                var cartDetails = await _cartService.GetActiveCartDetails(currentUser.Id, currentUser.Id, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, _workContext.GetCurrentSiteId());
                if (cart == null)
                { 
                    return NotFound();
                }
                var orderCreationResult = await _orderService.CreateOrder(cart.Id, EntityType.ToString(), 0, _workContext.GetCurrentSiteId(), OrderStatus.PendingPayment);
                if (!orderCreationResult.Success)
                {
                    return BadRequest(orderCreationResult.Error);
                }

                var res = request(currentUser, cartDetails, "SAR", orderCreationResult, stripeSetting, SelectedEntityId);
                HyperpayRequestViewModel model = new HyperpayRequestViewModel();
                model.id = res.Where(x => x.Key == "id").Select(x => x.Value).FirstOrDefault().ToString();
                model.url = stripeSetting.url + "/paymentWidgets.js?checkoutId=" + model.id;
                // model.redirectUrl = stripeSetting.BaseURL + stripeSetting.ReturnPath + model.id;
                model.redirectUrl = stripeSetting.BaseURL + "/api/PaymentHyperpay/HyperpayRecieptNew/";
                return Ok(model);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpGet("HyperpayReciept/{id}/{EntityType}")]
        public async Task<IActionResult> Reciept(string id, HyperPayEntityType EntityType)
        {
            try
            {
                var stripeProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.HyperProviderId);

                var stripeSetting = JsonConvert.DeserializeObject<HyperConfigForm>(stripeProvider.AdditionalSettings);
                var SelectedEntityId = stripeSetting.EntitiesTypes.Where(x => x.EntityType == EntityType.ToString()).Select(y => y.EntityId).FirstOrDefault();
                var checkpaymentsuccess = CheckPaymnetSuccess(id, stripeSetting, SelectedEntityId);
                var currentUser = await _workContext.GetCurrentUser();
                var cart = await _cartService.GetActiveCart(currentUser.Id, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, _workContext.GetCurrentSiteId());
                string orderIdString = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "merchantTransactionId").Select(x => x.Value).FirstOrDefault());
                string orderId = string.IsNullOrEmpty(orderIdString) ? null : orderIdString.Split("-")[1];
                var orderObj = await _orderService.GetOrderDetailsById(Convert.ToInt64(orderId));
                string AcquirerResponse = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "AcquirerResponse").Select(x => x.Value).FirstOrDefault());
                var resultobj = checkpaymentsuccess.Where(x => x.Key == "result").Select(x => x.Value).FirstOrDefault();
                string result = Convert.ToString(resultobj["description"]);
                string statusCode = Convert.ToString(resultobj["code"]);
                string descCode = Convert.ToString(resultobj["description"]);
                if (statusCode == stripeSetting.SuccessResponseCode)// if (statusCode == "000.000.000" && descCode == "Transaction succeeded")
                {
                    var payment = new Payment()
                    {
                        OrderId = Convert.ToInt64(orderId),
                        Amount = orderObj.OrderTotal,
                        PaymentMethod = EntityType.ToString(),
                        CreatedOn = DateTimeOffset.UtcNow,

                    };

                    payment.GatewayTransactionId = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "id").Select(x => x.Value).FirstOrDefault());
                    payment.Status = PaymentStatus.Succeeded;
                    payment.FailureMessage = result;
                    orderObj.OrderStatus = OrderStatus.PaymentReceived;
                    _paymentRepository.Add(payment);
                    await _paymentRepository.SaveChangesAsync();
                    await _orderService.OrderChangeStatus(Convert.ToInt64(orderId), OrderStatus.PaymentReceived, string.Empty, _workContext.GetCurrentSiteId());
                    await _orderService.DeActiveCart(cart.Id);


                    return Ok(orderObj);

                }
                else
                {
                    var payment = new Payment()
                    {
                        OrderId = orderObj.Id,
                        Amount = Convert.ToDecimal(orderObj.OrderTotal),
                        PaymentMethod = EntityType.ToString(),
                        CreatedOn = DateTimeOffset.UtcNow
                    };

                    payment.GatewayTransactionId = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "id").Select(x => x.Value).FirstOrDefault());
                    payment.FailureMessage = result;
                    payment.Status = PaymentStatus.Failed;
                    orderObj.OrderStatus = OrderStatus.PaymentFailed;
                    _paymentRepository.Add(payment);
                    await _paymentRepository.SaveChangesAsync();

                    await _orderService.OrderChangeStatus(orderObj.Id, orderObj.OrderStatus, payment.FailureMessage, _workContext.GetCurrentSiteId());

                    await _orderService.DeActiveCart(cart.Id);
                    return BadRequest("Payment Error: " + result);
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [AllowAnonymous]
        [HttpGet("HyperpayRecieptNew/{EntityType}")]
        public async Task<IActionResult> HyperpayRecieptNew([FromRoute] HyperPayEntityType EntityType, [FromQuery] string fronturl, [FromQuery] string id, [FromQuery] string resourcePath)
        {
            try
            {
                var stripeProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.HyperProviderId);

                var stripeSetting = JsonConvert.DeserializeObject<HyperConfigForm>(stripeProvider.AdditionalSettings);
                var SelectedEntityId = stripeSetting.EntitiesTypes.Where(x => x.EntityType == EntityType.ToString()).Select(y => y.EntityId).FirstOrDefault();
                var checkpaymentsuccess = CheckPaymnetSuccess(id, stripeSetting, SelectedEntityId);

                string orderIdString = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "merchantTransactionId").Select(x => x.Value).FirstOrDefault());
                string orderId = string.IsNullOrEmpty(orderIdString) ? null : orderIdString.Split("-")[1];
                var orderObj = await _orderService.GetOrderById(Convert.ToInt64(orderId));

                var OrderUserId = orderObj.CustomerId;
                var OrderSiteId = orderObj.SiteId;
           

                string AcquirerResponse = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "AcquirerResponse").Select(x => x.Value).FirstOrDefault());
                var resultobj = checkpaymentsuccess.Where(x => x.Key == "result").Select(x => x.Value).FirstOrDefault();
                string result = Convert.ToString(resultobj["description"]);
                string statusCode = Convert.ToString(resultobj["code"]);
                string descCode = Convert.ToString(resultobj["description"]);
                if (statusCode == stripeSetting.SuccessResponseCode)// if (statusCode == "000.000.000" && descCode == "Transaction succeeded")
                {
                    var payment = new Payment()
                    {
                        OrderId = Convert.ToInt64(orderId),
                        Amount = orderObj.OrderTotal,
                        PaymentMethod = EntityType.ToString(),
                        CreatedOn = DateTimeOffset.UtcNow,

                    };

                    payment.GatewayTransactionId = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "id").Select(x => x.Value).FirstOrDefault());
                    payment.Status = PaymentStatus.Succeeded;
                    payment.FailureMessage = result;
                    orderObj.OrderStatus = OrderStatus.PaymentReceived;
                    _paymentRepository.Add(payment);
                    await _paymentRepository.SaveChangesAsync();
                    await _orderService.OrderChangeStatus(Convert.ToInt64(orderId), OrderStatus.PaymentReceived, string.Empty, OrderSiteId);

                }
                else
                {
                    var payment = new Payment()
                    {
                        OrderId = orderObj.Id,
                        Amount = Convert.ToDecimal(orderObj.OrderTotal),
                        PaymentMethod = EntityType.ToString(),
                        CreatedOn = DateTimeOffset.UtcNow
                    };

                    payment.GatewayTransactionId = Convert.ToString(checkpaymentsuccess.Where(x => x.Key == "id").Select(x => x.Value).FirstOrDefault());
                    payment.FailureMessage = result;
                    payment.Status = PaymentStatus.Failed;
                    orderObj.OrderStatus = OrderStatus.PaymentFailed;


                    _paymentRepository.Add(payment);
                    await _paymentRepository.SaveChangesAsync();

                    await _orderService.OrderChangeStatus(orderObj.Id, orderObj.OrderStatus, payment.FailureMessage, _workContext.GetCurrentSiteId());

                    
                  //  return BadRequest("Payment Error: " + result);

                }

                var cart = await _cartService.GetActiveCart(OrderUserId, EskaCMS.Entities.Enum.GeneralEnums.CartTypes.Shop, OrderSiteId);
                if (cart != null)
                {
                    await _orderService.DeActiveCart(cart.Id);
                }
                return RedirectPermanent(fronturl + "?orderId=" + HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderId)));

            }
            catch (Exception ex)
            {


                return BadRequest(ex.Message);
            }

        }
        private Dictionary<string, dynamic> request(User currentUser, CartVm cart, string ISOCurrencySymbol, Result<Order> Order, HyperConfigForm stripeSetting, string SelectedEntityId)
        {

            Dictionary<string, dynamic> responseData;
            string data = "entityId=" + SelectedEntityId +
            "&amount=" + (Math.Round(cart.OrderTotal * 10) / 10).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
            "&currency=" + ISOCurrencySymbol +
            "&paymentType=DB" +
            "&merchantTransactionId=" + $" {stripeSetting.Environment}-{Order.Value.Id}" +
            "&customer.email=" + currentUser.Email +
            "&customer.mobile=" + currentUser.PhoneNumber +
                "&customer.givenName=" + currentUser.FullName
                ;
            string url = stripeSetting.url + "checkouts";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "POST";
            request.Headers["Authorization"] = "Bearer " + stripeSetting.token;
            request.ContentType = "application/x-www-form-urlencoded";
            Stream PostData = request.GetRequestStream();
            PostData.Write(buffer, 0, buffer.Length);
            PostData.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string result = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = reader.ReadToEnd();
            }
            responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
            return responseData;
        }


        private Dictionary<string, dynamic> CheckPaymnetSuccess(string id, HyperConfigForm stripeSetting, string SelectedEntityId)
        {
            try
            {


                Dictionary<string, dynamic> responseData;
                string data = "entityId=" + SelectedEntityId;
                string url = stripeSetting.url + "checkouts/" + id + "/payment?" + data;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + stripeSetting.token;
                WebResponse httpResponse = (HttpWebResponse)request.GetResponse();

                string result = "";
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = reader.ReadToEnd(); // do something fun...
                }
                responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                return responseData;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

    }
}
