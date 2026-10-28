using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCommerce.Payments.Models;
using EskaCommerce.ShoppingCart.Services;
using Newtonsoft.Json;
//using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Controllers
{
    [Area("Payments")]
    [Route("checkout")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CheckoutController : Controller
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly ICartService _cartService;
        //private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        // private readonly IRepository<UserAddress> _userAddressRepository;
      

        public CheckoutController(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
          
            ICartService cartService,
            //IOrderService orderService,
            IWorkContext workContext/* ,
            IRepository<UserAddress> userAddressRepository */
            )
        {
            _paymentProviderRepository = paymentProviderRepository;
            _cartService = cartService;
            //_orderService = orderService;
            _workContext = workContext;
            // _userAddressRepository = userAddressRepository;
         
        }

        [HttpGet("payment")]
        public async Task<IActionResult> Payment(CartTypes CartType)
        {
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());
            var currentUserId = await _workContext.GetCurrentUserId();
            var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);
            if (cart == null)
            {
                return Redirect("~/");
            }

            cart.LockedOnCheckout = true;
            await _paymentProviderRepository.SaveChangesAsync();
            var checkoutPaymentForm = new CheckoutPaymentForm();
            //var shippingdata = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);
            var country = "";//_userAddressRepository.Query().Where(x => x.Id == shippingdata.ShippingAddressId).Select(x => x.Address.CountryId).FirstOrDefault();
            if (country == "JO")
            {
                checkoutPaymentForm.PaymentProviders = await _paymentProviderRepository.Query()
    .Where(x => x.IsEnabled && x.SiteId == SiteId)
    .Select(x => new PaymentProviderVm
    {
        Id = x.Id,
       Name=x.Name
    }).ToListAsync();
            }
            else
            {
                checkoutPaymentForm.PaymentProviders = await _paymentProviderRepository.Query()
    .Where(x => x.IsEnabled && x.SiteId == SiteId && x.Id != "CoD")
    .Select(x => new PaymentProviderVm
    {
        Id = x.Id,
        Name = x.Name,
     
    }).ToListAsync();
            }

            return View(checkoutPaymentForm);
        }
    }
}
