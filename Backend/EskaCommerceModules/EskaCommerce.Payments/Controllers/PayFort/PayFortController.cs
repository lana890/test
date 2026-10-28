#nullable enable
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Services.Interfaces;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Services.PayFort;
using EskaCommerce.Payments.Services.PayFort.Interfaces;
using EskaCommerce.Payments.Models.PayFort;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.UserCatalog.Services;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Controllers.PayFort
{
    [ApiController]
    [Route("api/PayFort")]
    public class PayFortController : ControllerBase
    {
        private readonly IPayFortServices _payFortServices;
        private readonly IOrderService _orderService;
        // private readonly HttpClient _httpClient;
        private readonly IPayFortConfigurationServices _payFortConfigurationServices;
        // private readonly ISubscriptionServices _subscriptionServices;
        private readonly IPaymentServices _paymentServices;
        // private readonly IIntegrationServices _IntegrationServices;
        // private readonly ICartService _cartService;
        private readonly IRepository<Order> orderRepository;
        private readonly IServiceScopeFactory scopeFactory;
        // private readonly ICurrencyService _currencyService;
        private readonly IdcmsLogService _idcmsLogService;
        // private readonly IRepository<Cart> _cartContext;
       
	 

		private readonly IWorkContext _workContext;

        public PayFortController(
            IWorkContext workContext,
			 
		IPayFortServices payFortServices,
            IOrderService orderService,
            /* HttpClient httpClient, */
            IdcmsLogService idcmsLogService,
            /* IIntegrationServices IntegrationServices, */
        IPayFortConfigurationServices payFortConfigurationServices,
            /* ISubscriptionServices subscriptionServices, */
            IPaymentServices paymentServices,
            /* ICurrencyService currencyService, */
            /* ICartService cartService, */
            IRepository<Order> orderRepository,
            IServiceScopeFactory scopeFactory/* ,
                          IRepository<Cart> cartContext */
)
        {
            _workContext = workContext;
 			_payFortServices = payFortServices;
            _orderService = orderService;
            // _IntegrationServices = IntegrationServices;
            // _httpClient = httpClient;
            _payFortConfigurationServices =payFortConfigurationServices;
            // _subscriptionServices=subscriptionServices;
            _paymentServices=paymentServices;
            // _cartService = cartService;
            this.orderRepository=orderRepository;
            this.scopeFactory=scopeFactory;
            // _currencyService = currencyService;
            _idcmsLogService = idcmsLogService;
            // _cartContext = cartContext;


        }

        [HttpPost]
        [Authorize]

        [Route("PayFortTransaction")]
        public async Task<IActionResult> PayFortTransaction([FromBody] PayFortWebRequest request)
        {

			SemaphoreSlim userLock = new SemaphoreSlim(1);
            long currentUserId = 0;
			try
            {

                var SiteId = _workContext.GetCurrentSiteId();

                 currentUserId = await _workContext.GetCurrentUserId();

				 userLock = UserPaymentLockHelper.GetLock(currentUserId);

				await userLock.WaitAsync();

				var Model = await _payFortServices.PayFortTransaction(request.CartType, SiteId, request.CP_URL, request.Type,request.CartId);
                return Ok(Model);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
			finally
			{
				userLock.Release();                  // 🔓 Unlock
				UserPaymentLockHelper.ReleaseLock(currentUserId);
			}
		}
        [HttpPost]
        [Authorize]
        [Route("PayFortTransactionMobile")]
        public async Task<IActionResult> PayFortTransactionMobile([FromBody] PayFortMobilPayload payRequest)
        {

            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var currentUser = await _workContext.GetCurrentUser();

                var Model = await _payFortServices.GenerateSdkTokenAsync(payRequest.CartType, SiteId, payRequest.device_id, payRequest.Type,payRequest.CartId);
                return Ok(Model);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [AllowAnonymous]
        [HttpPost("PayFortResponseCallBack")]
        public async Task<IActionResult> PayFortResponseCallBack()
        {
            try
            {
                bool isValidSignature = false;
                var form = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                var json = JsonConvert.SerializeObject(form);
                var model = JsonConvert.DeserializeObject<PayFortResultViewModel>(json);
                long OrderId = model.GetOrderId();
				var cartId = model.GetCartId();
				var orderObj = await _orderService.GetOrderById(OrderId);
                var cultureId = model.GetCulture();
                int type = (int)model.GetType();

                if (orderObj == null)
                {
                    // Previously an NRE on the next line, surfaced only as a 400 body.
                    await _idcmsLogService.AddError("PayFort.Callback",
                        $"No order found for merchant_extra1 '{OrderId}'.", json, null, 0);
                    return BadRequest($"Order {OrderId} not found.");
                }

                AddCustomHeaders(orderObj.SiteId, orderObj.CustomerId, Convert.ToInt64(cultureId.Trim()), 0 , cartId);
                var settings = await _payFortServices.GetPayFortSettingsAsync(model.GetSiteId(), orderObj.PaymentMethod);
                if (orderObj.Channel!=EPlatformSource.Mobile)
                    isValidSignature =  _payFortConfigurationServices.VerifyResponseSigneture(Request.Form, settings.ResponsePhrase);
                if (isValidSignature|| orderObj.Channel== EPlatformSource.Mobile)
                {
                    // Claimed in one statement: the customer's return (payFortTransactionRedirection)
                    // carries the same result, and only the first of the two may act on it.
                    if (orderObj.OrderStatus == OrderStatus.PaymentReceived || orderObj.OrderStatus == OrderStatus.PaymentFailed
                        || !await TryClaimAsync(orderObj.Id))
                    {
                        return Ok();
                    }
                    // 14 / 04 success; everything else (19 pending, 15, 10 incomplete, 13 purchase
                    // failed, 45 3DS failed, 05 capture failed…) is recorded as a failure
                    await ApplyResultAsync(orderObj, model, settings, Convert.ToInt64(cultureId.Trim()), cartId);
                    return Ok();
                }
                else
                {
                    // A silent bare 400 here is indistinguishable from "never called".
                    // The usual cause is a ResponsePhrase that does not match the one
                    // configured in the PayFort merchant account for this environment.
                    await _idcmsLogService.AddError("PayFort.Callback",
                        $"Signature verification failed for order {OrderId}.", json, null, orderObj.SiteId);
                    return BadRequest("Invalid signature.");
                }



            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        private async Task HandleSuccessAsync(Order orderObj, PayFortResultViewModel model, ConfigurePayFort settings,
            string ResponseStatus, string ResponseMessage, int type, long cultureId, string cartId)
        {
            // Recording the payment is a fast local write and MUST happen before we
            // acknowledge PayFort — the money is already taken at this point.
            await _paymentServices.UpdatePayment(orderObj, model.fort_id, orderObj.PaymentMethod,
                ResponseMessage, PaymentStatus.Succeeded);

            await _idcmsLogService.AddInfo("PayFort.Callback",
                $"order {orderObj.Id} paid (fort_id {model.fort_id}) — queueing completion",
                null, orderObj.SiteId);

            // Everything else is detached. The policy journey routinely runs longer than
            // PayFort's callback read-timeout; when PayFort gives up, the request is
            // aborted and every remaining await dies silently mid-flight. That is how
            // orders were being left unfinalised with no exception in the logs.
            QueueOrderCompletion(orderObj.Id, orderObj.SiteId, orderObj.CustomerId,
                cultureId, cartId, type, orderObj.Cart?.Id);
        }

        /// <summary>
        /// Runs the order-completion journey on a detached DI scope so it survives the
        /// PayFort webhook connection being closed.
        ///
        /// Two things make this safe:
        ///  * only the order ID crosses the scope boundary — never a tracked entity,
        ///    which would silently no-op on the new scope's DbContext;
        ///  * a synthetic HttpContext carries the same siteId/userId/cultureId/CartId
        ///    headers that AddCustomHeaders fabricates today, because WorkContext reads
        ///    the site exclusively from those headers and otherwise silently yields 0.
        /// </summary>
        private void QueueOrderCompletion(long orderId, long siteId, long customerId,
            long cultureId, string cartId, int type, long? orderCartId)
        {
            // Not inheriting this request's execution context: IHttpContextAccessor is AsyncLocal,
            // and installing the synthetic context in an inherited flow clears the HttpContext of
            // the request that is still answering PayFort.
            using var noFlow = ExecutionContext.IsFlowSuppressed() ? default(AsyncFlowControl?) : ExecutionContext.SuppressFlow();
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunOrderCompletion(orderId, siteId, customerId, cultureId, cartId, type, orderCartId);
                }
                catch
                {
                    // Nothing above this point may throw: an unobserved exception on a
                    // detached task has no handler and can take the process down.
                }
            });
        }

        private async Task RunOrderCompletion(long orderId, long siteId, long customerId,
            long cultureId, string cartId, int type, long? orderCartId)
        {
            var status = OrderStatus.PaymentFailed;
            var sendReceipt = false;
            {
                using var scope = scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                InstallWorkContext(sp, siteId, customerId, cultureId, cartId);

                var log = sp.GetRequiredService<IdcmsLogService>();
                var orders = sp.GetRequiredService<IOrderService>();
                var integration = sp.GetRequiredService<IIntegrationServices>();
                var orderRepo = sp.GetRequiredService<IRepository<Order>>();

                try
                {
                    await log.AddInfo("PayFort.Complete", $"order {orderId} start", null, siteId);

                    if (type == (int)PaymentType.DCMS || type == (int)PaymentType.CP)
                    {
                        // Throws only when no policy exists; a created-but-partly-finalised
                        // policy returns normally and is treated as paid.
                        await integration.CreatePolicyAsync(orderId);
                    }
                    else
                    {
                        // Sent once the order is marked paid, from its own scope. Sent from here, a
                        // failing receipt (no SMS provider for the site) left this scope's database
                        // session unable to save anything, so the status, the cart and the logs below
                        // were all lost and the paid order stayed pending (order 102020, 2026-10-20).
                        sendReceipt = true;
                    }

                    status = OrderStatus.PaymentReceived;
                }
                catch (Exception ex)
                {
                    status = OrderStatus.PaymentFailed;
                    await log.AddError("PayFort.Complete", ex.Message, ex.StackTrace, null, siteId);
                }
                finally
                {
                    // Each of these must run regardless of what happened above.
                    try { await orders.OrderChangeStatus(orderId, status, "", siteId); }
                    catch (Exception ex)
                    {
                        await log.AddError("PayFort.Complete.OrderChangeStatus",
                            ex.Message, ex.StackTrace, null, siteId);
                    }

                    try
                    {
                        if (orderCartId.HasValue)
                            await orders.DeActiveCart(orderCartId.Value);
                    }
                    catch (Exception ex)
                    {
                        await log.AddError("PayFort.Complete.DeActiveCart",
                            ex.Message, ex.StackTrace, null, siteId);
                    }

                    // Release the in-flight marker so a repush can retry a failed order.
                    // The final OrderStatus above is what makes duplicate callbacks idempotent.
                    try
                    {
                        var fresh = await orderRepo.Query().FirstOrDefaultAsync(x => x.Id == orderId);
                        if (fresh != null)
                        {
                            fresh.IsProcessed = false;
                            await orderRepo.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await log.AddError("PayFort.Complete.ClearIsProcessed",
                            ex.Message, ex.StackTrace, null, siteId);
                    }

                    await log.AddInfo("PayFort.Complete",
                        $"order {orderId} finished with status {status}", null, siteId);
                }
            }

            if (sendReceipt && status == OrderStatus.PaymentReceived)
                await SendReceiptIsolatedAsync(orderId, siteId, customerId, cultureId, cartId);
        }

        /// <summary>
        /// WorkContext caches IHttpContextAccessor.HttpContext in its CONSTRUCTOR, so the
        /// synthetic context has to be installed before anything in the scope that
        /// transitively resolves IWorkContext.
        /// </summary>
        private static void InstallWorkContext(IServiceProvider sp, long siteId, long customerId, long cultureId, string cartId)
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var synthetic = new DefaultHttpContext { RequestServices = sp };
            synthetic.Request.Headers["siteId"] = siteId.ToString();
            synthetic.Request.Headers["userId"] = customerId.ToString();
            synthetic.Request.Headers["cultureId"] = cultureId.ToString();
            if (!string.IsNullOrEmpty(cartId))
                synthetic.Request.Headers["CartId"] = cartId;
            accessor.HttpContext = synthetic;
        }

        /// <summary>
        /// The receipt (e-mail + SMS) in a scope of its own: whatever it leaves behind in its
        /// database session cannot reach the order. Its failure is logged from a fresh scope too.
        /// </summary>
        private async Task SendReceiptIsolatedAsync(long orderId, long siteId, long customerId, long cultureId, string cartId)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                InstallWorkContext(scope.ServiceProvider, siteId, customerId, cultureId, cartId);
                await scope.ServiceProvider.GetRequiredService<IOrderService>().SendReceipt(orderId);
            }
            catch (Exception ex)
            {
                try
                {
                    using var logScope = scopeFactory.CreateScope();
                    InstallWorkContext(logScope.ServiceProvider, siteId, customerId, cultureId, cartId);
                    await logScope.ServiceProvider.GetRequiredService<IdcmsLogService>()
                        .AddError("PayFort.Complete.SendReceipt", ex.Message, ex.StackTrace, null, siteId);
                }
                catch
                {
                    // The log itself is unavailable.
                }
            }
        }

        /// <summary>
        /// Takes an order for processing in one UPDATE (IsProcessed 0 → 1, while not yet paid or
        /// failed). PayFort's server call and the customer's return both carry the result; the one
        /// that gets the row acts, the other leaves it.
        /// </summary>
        private async Task<bool> TryClaimAsync(long orderId) =>
            await orderRepository.Query()
                .Where(o => o.Id == orderId && !o.IsProcessed
                    && o.OrderStatus != OrderStatus.PaymentReceived && o.OrderStatus != OrderStatus.PaymentFailed)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsProcessed, true)) == 1;

        private Task<int> ReleaseClaimAsync(long orderId) =>
            orderRepository.Query()
                .Where(o => o.Id == orderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsProcessed, false));

        /// <summary>
        /// Records a verified result on a claimed order: 14 / 04 paid, anything else failed.
        /// A failure while recording hands the claim back, so a later delivery can still settle it.
        /// </summary>
        private async Task ApplyResultAsync(Order orderObj, PayFortResultViewModel model, ConfigurePayFort settings, long cultureId, string cartId)
        {
            var statusCode = int.TryParse(model.status, out var code) ? code : -1;
            try
            {
                if (statusCode == 14 || statusCode == 4)
                    await HandleSuccessAsync(orderObj, model, settings, model.status, model.response_message,
                        (int)model.GetType(), cultureId, cartId);
                else
                    await HandleFailureAsync(orderObj, model, settings, model.status, model.response_message);
            }
            catch
            {
                await ReleaseClaimAsync(orderObj.Id);
                throw;
            }
        }

        /// <summary>
        /// The result PayFort posts with the customer's return (Return URL Type POST), signed like
        /// the server call. Recording it settles the order even when PayFort's server call never
        /// arrives. Pending results (19 pending, 15 uncertain, 20 on hold) are left to that call.
        /// Never throws: the customer is redirected whatever happens here.
        /// </summary>
        private async Task RecordReturnAsync()
        {
            long? siteId = null;
            try
            {
                if (!Request.HasFormContentType) return;
                var form = await Request.ReadFormAsync();
                if (string.IsNullOrEmpty(form["signature"]) || string.IsNullOrEmpty(form["status"])
                    || !long.TryParse(form["merchant_extra1"], out var orderId))
                    return;

                var model = JsonConvert.DeserializeObject<PayFortResultViewModel>(
                    JsonConvert.SerializeObject(form.ToDictionary(x => x.Key, x => x.Value.ToString())));
                var orderObj = await _orderService.GetOrderById(orderId);
                if (orderObj == null || orderObj.Channel == EPlatformSource.Mobile) return;
                siteId = orderObj.SiteId;

                var cultureId = Convert.ToInt64(model.GetCulture().Trim());
                var cartId = model.GetCartId();
                AddCustomHeaders(orderObj.SiteId, orderObj.CustomerId, cultureId, 0, cartId);
                var settings = await _payFortServices.GetPayFortSettingsAsync(orderObj.SiteId, orderObj.PaymentMethod);
                if (!_payFortConfigurationServices.VerifyResponseSigneture(form, settings.ResponsePhrase))
                {
                    await _idcmsLogService.AddError("PayFort.Return",
                        $"Signature verification failed for order {orderId}.", ResultSummary(form), null, siteId);
                    return;
                }

                var statusCode = int.TryParse(model.status, out var code) ? code : -1;
                if (statusCode == 19 || statusCode == 15 || statusCode == 20)
                {
                    await _idcmsLogService.AddInfo("PayFort.Return",
                        $"order {orderId} returned with status {model.status}: left to PayFort's server call", ResultSummary(form), siteId);
                    return;
                }
                if (orderObj.OrderStatus == OrderStatus.PaymentReceived || orderObj.OrderStatus == OrderStatus.PaymentFailed
                    || !await TryClaimAsync(orderObj.Id))
                    return;

                await ApplyResultAsync(orderObj, model, settings, cultureId, cartId);
                await _idcmsLogService.AddInfo("PayFort.Return",
                    $"order {orderId} recorded from the customer's return (status {model.status})", ResultSummary(form), siteId);
            }
            catch (Exception ex)
            {
                try { await _idcmsLogService.AddError("PayFort.Return", ex.Message, ex.StackTrace, null, siteId); }
                catch { }
            }
        }

        /// <summary>The result fields worth logging; card, holder and contact fields stay out.</summary>
        private static string ResultSummary(IFormCollection form) =>
            JsonConvert.SerializeObject(new[] { "status", "response_code", "response_message", "fort_id", "merchant_reference", "amount", "currency", "merchant_extra1" }
                .ToDictionary(k => k, k => form[k].ToString()));

        private async Task HandleFailureAsync(Order orderObj, PayFortResultViewModel model, ConfigurePayFort settings, string ResponseStatus, string ResponseMessage)
        {
            await _paymentServices.UpdatePayment(orderObj, model.fort_id, orderObj.PaymentMethod, ResponseMessage, PaymentStatus.Failed);
            await _orderService.OrderChangeStatus(orderObj.Id, OrderStatus.PaymentFailed, ResponseMessage, orderObj.SiteId);

            // Was left true forever, which permanently short-circuited every later
            // callback for this order at the IsProcessed guard above.
            await ReleaseClaimAsync(orderObj.Id);

            await _idcmsLogService.AddInfo("PayFort.Callback",
                $"order {orderObj.Id} failed: {ResponseStatus} {ResponseMessage}", null, orderObj.SiteId);
        }
        private void AddCustomHeaders(long siteId, long customerId, long CultureId, long warehouseId,string cartId)
        {
            try
            {
                // Indexer, not Add: Add throws on a duplicate key, and a proxy in front of
                // the app injecting any of these would have taken down the whole callback.
                HttpContext.Request.Headers["siteId"] = siteId.ToString();
                HttpContext.Request.Headers["userId"] = customerId.ToString();
                HttpContext.Request.Headers["cultureId"] = CultureId.ToString();

				if (!string.IsNullOrEmpty( cartId))
				{
                    HttpContext.Request.Headers["CartId"] = cartId;
				}


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        [AllowAnonymous]
        [HttpPost("payFortTransactionRedirection")]
        public async Task<IActionResult> payFortTransactionRedirection([FromQuery] string baseUrl, [FromQuery] long OrderId, [FromQuery] long? CartId)
        {
            await RecordReturnAsync();
            try
            {
                var encryptedOrderId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(OrderId.ToString()));
                var fullUrl = baseUrl + $"?OrderId={encryptedOrderId}";

				return Redirect(fullUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
