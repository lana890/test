using EskaCommerce.Payments.Models.PaymentQuickPay;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.Orders.Services;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;
using System.Reflection;
using EskaCMS.EmailSender.SMTP.Services;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Payments.Controllers.PaymentQuickPay
{
    [ApiController]
    [Route("api/Quickpay")]
    [Authorize]
    public class QuickPayController : ControllerBase
    {
        private readonly string _assembly;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Payment> _paymentRepository;
        // private readonly ICurrencyService _currencyService;
        // private readonly UserManager<User> _userManager;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        // private readonly IEmailTemplateService _emailTemplateService;
        // private readonly IEmailSender _emailSender;
        // private readonly ILogger _logger;
        // private readonly IdcmsLogService _dcmsLogService;

        public QuickPayController(ICartService cartService,
            IOrderService orderService,
            IWorkContext workContext,
            /* UserManager<User> userManager, */
             IRepository<Payment> paymentRepository,
            /* ICurrencyService currencyService, */
            IRepository<Order> orderRepository,
          IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository/* ,
          IEmailTemplateService emailTemplateService *//* ,
            IEmailSender emailSender *//* ,
            //   ILogger logger,
            IdcmsLogService dcmsLogService */

            )
        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _paymentRepository = paymentRepository;
            // _currencyService = currencyService;
            _orderRepository = orderRepository;
            // _userManager = userManager;
            _paymentProviderRepository = paymentProviderRepository;
            // _emailTemplateService = emailTemplateService;
            // _emailSender = emailSender;
            //   _logger = logger;
            // _dcmsLogService = dcmsLogService;
            _assembly = Assembly.GetExecutingAssembly().GetName().Name;
        }
        private string Sign(Dictionary<string, string> paramsList, string api_key)
        {
            var result = String.Join(" ", paramsList.OrderBy(c => c.Key).Select(c => c.Value).ToArray());
            var e = Encoding.UTF8;
            var hmac = new HMACSHA256(e.GetBytes(api_key));
            byte[] b = hmac.ComputeHash(e.GetBytes(result));
            var s = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                s.Append(b[i].ToString("x2"));
            }
            return s.ToString();
        }



        [HttpGet("quickPay-settings")]
        public async Task<IActionResult> QuickPay()
        {
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());
            var providers = await _paymentProviderRepository.Query().Where(x => x.SiteId == SiteId && x.Id == "Quickpay").FirstOrDefaultAsync();
            return Ok(providers);
        }


        [HttpPost("quick-pay")]
        public async Task<IActionResult> quickpayurl([FromBody] QPpaymentVM QPpayment)
        {
            long CartId = 0;
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());
            try
            {

                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, QPpayment.CartType, SiteId);

                if (cart == null)
                {
                    return NotFound();
                }
                CartId = cart.Id;
                var cartDetails = await _cartService.GetActiveCartDetails(currentUserId, currentUserId, QPpayment.CartType, SiteId);



                if (cart == null)
                {
                    return NotFound();
                }

                //var regionInfo = new RegionInfo(_currencyService.Currency.LCID);
                var orderCreationResult = await _orderService.CreateOrder(cart.Id, "Quickpay", 0, SiteId, OrderStatus.PendingPayment);
                if (!orderCreationResult.Success)
                {
                    return BadRequest(orderCreationResult.Error);

                }
                return Ok(new { order = orderCreationResult.Value, cartId = CartId });

            }
            catch (Exception exc)
            {
                await _orderService.ReActiveCart(CartId);

                //   _logger.LogError("Payment Error.");
                return BadRequest(exc.Message);
            }


        }

        [HttpPost("get-order/{orderId}")]
        public async Task<IActionResult> GetOrder(long orderId)
        {
            try
            {
                var order = await _orderService.GetOrderById(orderId);
                return Ok(order.OrderStatus);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }


        }

        [HttpGet("update/{cartType}/{orderId}/{status}/{paymentId}/{paymentStatus}")]
        [AllowAnonymous]
        public async Task<IActionResult> updateQuickPayStatus(CartTypes cartType, long orderId, int status, long paymentId, int paymentStatus)
        {
            try
            {
                long CartId = 0;
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                //var currentUser = await _workContext.GetCurrentUser(SiteId);

                var order = await _orderService.GetOrderById(orderId);
                var paymentQuick = new Payment()
                {
                    OrderId = orderId,
                    Amount = order.OrderTotal,
                    PaymentMethod = "QuickPay",
                    CreatedOn = DateTimeOffset.UtcNow
                };
                paymentQuick.GatewayTransactionId = Convert.ToString(paymentId);
                paymentQuick.Status = (PaymentStatus)paymentStatus;
                order.OrderStatus = (OrderStatus)status;
                _paymentRepository.Add(paymentQuick);
                await _paymentRepository.SaveChangesAsync();
                await _orderRepository.SaveChangesAsync();
                return Ok();
            }
            catch (Exception exc)
            {
                //   _logger.LogError("Payment Error.");
                return BadRequest(exc.Message);
            }


        }


        //[HttpGet("call-back")]
        //[AllowAnonymous]
        //public IActionResult Callback()
        //{
        //    string checkSum = Request.Heades["QuickPay-Checksum-Sha256"];
        //    //string checkSum = checksum;

        //    var bytes = new byte[Request.Body.Length];
        //    Request.Body.Read(bytes, 0, bytes.Length);
        //    Request.Body.Position = 0;
        //    string content = Encoding.UTF8.GetString(bytes);

        //    string compute = sign(content, "your_account_private_key");

        //    //if (checkSum.Equals(compute))
        //    //{
        //    //    // Request is authenticated
        //    //}
        //    //else
        //    //{
        //    //    // Request is NOT authenticated
        //    //}
        //    return Ok(checkSum);
        //}
        private string sign(string baseString, string api_key)
        {
            var e = Encoding.UTF8;

            var hmac = new HMACSHA256(e.GetBytes(api_key));
            byte[] b = hmac.ComputeHash(e.GetBytes(baseString));

            var s = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                s.Append(b[i].ToString("x2"));
            }

            return s.ToString();
        }

        private string EmailReciept(Order order, string username, List<OrderItem> cart, string EMailBody)
        {

            EMailBody = EMailBody.Replace("[FirstName] [LastName]", username);
            EMailBody = EMailBody.Replace("[OrderDate]", order.CreatedOn.ToString("yyyy-MM-dd"));
            EMailBody = EMailBody.Replace("[OVNO]", order.Id.ToString());
            EMailBody = EMailBody.Replace("[PayMethod]", order.PaymentMethod);
            EMailBody = EMailBody.Replace("[PaidAmount]", order.OrderTotal.ToString());
            string Items = "";
            foreach (var item in cart)
            {
               // Items = Items + "<tr><td style='border-bottom:solid 1px #1c3b78'>" + item.ProductId.ToString() + "</td><td style='border-bottom:solid 1px #1c3b78'>" + item.Product.Name + "</td><td style='border-bottom:solid 1px #1c3b78'>" + Math.Round(item.Product.Price, 2).ToString() + " USD" + "</td><td style='border-bottom:solid 1px #1c3b78'>" + item.Quantity.ToString() + "</td></tr>";
                //Items = Items + "<td>" + item.ProductId.ToString() + "</td><td>" + item.Product.Name + "</td><td>" + Math.Round(item.Product.Price, 2).ToString() + " USD" + "</td><td>" + item.Quantity.ToString() + "</td>";

            }
            EMailBody = EMailBody.Replace("[ITEMS]", Items);

            return EMailBody;
        }

    }
}

