using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Models.PaymentCoD;
using EskaCMS.Entities.Entity;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using EskaCMS.Core.Services;
using EskaCMS.Core.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Payments;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using System.Diagnostics;

namespace EskaCommerce.Payments.Controllers.PaymentCoD
{
    [Authorize]
    [Area("PaymentCoD")]
    [Route("api/PaymentCoD")]
 //   [ApiExplorerSettings(IgnoreApi = true)]
    public class CoDController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly ICartService _cartService;
     
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private Lazy<CoDSetting> _setting;

        public CoDController(
            ICartService cartService,
            IOrderService orderService,
            IWorkContext workContext,
         IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository)
        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _paymentProviderRepository = paymentProviderRepository;
            _setting = new Lazy<CoDSetting>(GetSetting());
        }


        [HttpPost("CoDCheckout")]
        public async Task<IActionResult> CoDCheckout([FromBody] OrderViewModel Model)
        {
            try
            {
           
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCartDetails(currentUserId, CartTypes.Shop, SiteId);
                if (cart == null)
                {
                    return NotFound("The Cart Is Empty");
                }

                if (!ValidateCoD(cart))
                {
                    return BadRequest("Payment Method is not eligible for this order.");
                }


                var result = await _orderService.PayCash(Model);

                try
                {
                    // Receipt is built now; the SMS/e-mails are sent after the response so the customer doesn't wait for them.
                    await _orderService.SendReceipt(result.OrderId, sendInBackground: true);
                }
                catch (Exception e)
                {
                    // to be loged 
                }

                return Ok(result.OrderCode);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);

            }
        }

        //[HttpPost("CoDCheckout")]
        //public async Task<IActionResult> CoDCheckout()
        //{
        //    try
        //    {
        //        var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //    var currentUserId = await _workContext.GetCurrentUserId();
        //        var cart = await _cartService.GetActiveCartDetails(currentUserId,CartTypes.Shop, SiteId);
        //    if (cart == null)
        //    {
        //        return NotFound();
        //    }

        //    if (!ValidateCoD(cart))
        //    {
        //        return BadRequest("Payment Method is not eligible for this order.");
        //    }

        //    var calculatedFee = CalculateFee(cart);
        //    var orderCreateResult = await _orderService.CreateOrder(cart.Id, "CashOnDelivery", calculatedFee, long.Parse(Request.Headers["SiteId"]));

        //    if (!orderCreateResult.Success)
        //    {
        //        return BadRequest(orderCreateResult.Error);
        //    }

        //    return Ok(orderCreateResult.Value.Id);

        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc.Message);

        //    }
        //}

        private CoDSetting GetSetting()
        {
            var coDProvider = _paymentProviderRepository.Query().FirstOrDefault(x => x.Id == PaymentProviderHelper.CODProviderId);
            if (string.IsNullOrEmpty(coDProvider.AdditionalSettings))
            {
                return new CoDSetting();
            }

            var coDSetting = JsonConvert.DeserializeObject<CoDSetting>(coDProvider.AdditionalSettings);

            return coDSetting;
        }

        private bool ValidateCoD(CartVm cart)
        {
            if (_setting.Value.MinOrderValue.HasValue && _setting.Value.MinOrderValue.Value > cart.OrderTotal)
            {
                return false;
            }

            if (_setting.Value.MaxOrderValue.HasValue && _setting.Value.MaxOrderValue.Value < cart.OrderTotal)
            {
                return false;
            }

            return true;
        }

        private decimal CalculateFee(CartVm cart)
        {
            var percent = _setting.Value.PaymentFee;
            return (cart.OrderTotal / 100) * percent;
        }
    }
}