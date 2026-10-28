using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Addresses.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Helpers;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("ShoppingCart")]
    [Authorize]
    [Route("api/")]
    public class CheckoutApiController : Controller
    {
        private readonly IOrderService _orderService;
        // private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Cart> _cartRepository;

        public CheckoutApiController(
            IOrderService orderService,
            /* ICartService cartService, */
            IWorkContext workContext,
            IRepository<Cart> cartRepository)
        {
            _orderService = orderService;
            // _cartService = cartService;
            _workContext = workContext;
            _cartRepository = cartRepository;
        }

        [HttpPost("cart/{cartId}/update-tax-and-shipping-prices")]
        public async Task<IActionResult> UpdateTaxAndShippingPrices(long cartId, [FromBody] TaxAndShippingPriceRequestVm model)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();

            var cart = await _cartRepository.Query().FirstOrDefaultAsync(x => x.Id == cartId);
            if (cart == null)
            {
                return NotFound();
            }

            if (cart.CreatedById != currentUser.Id)
            {
                return Forbid();
            }

            var orderTaxAndShippingPrice = await _orderService.UpdateTaxAndShippingPrices(cart.Id, model, SiteId);
            return Ok(orderTaxAndShippingPrice);
        }

        [HttpPost("cart/{cartId}/order")]
        public async Task<IActionResult> CreateOrder(long cartId, [FromBody] DeliveryInformationVm deliveryInformationVm)
        {
            try
            {


                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUser = await _workContext.GetCurrentUser();
                var cart = await _cartRepository.Query().FirstOrDefaultAsync(x => x.Id == cartId);
                if (cart == null)
                {
                    return NotFound();
                }
                if (cart.CreatedById != currentUser.Id)
                {
                    return Forbid();
                }

                cart.ShippingData = JsonConvert.SerializeObject(deliveryInformationVm);
                cart.OrderNote = deliveryInformationVm.OrderNote;

                _cartRepository.SaveChanges();

                var orderCreateResult = await _orderService.CreateOrder(cart.Id, OrderStatus.CashOnDelivery.GetDisplayName(), 0, SiteId, OrderStatus.PendingPayment);

                if (!orderCreateResult.Success)
                {
                    return BadRequest(orderCreateResult.Error);
                }
                return Ok(orderCreateResult.Value);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // TODO might need to move to another place
        [HttpGet("users/{userId}/addresses")]
        public async Task<IActionResult> UserAddress(long userId, [FromServices] IRepository<UserAddress> userAddressRepository, [FromServices] IRepository<User> userRepository)
        {
            var user = await userRepository.Query().FirstOrDefaultAsync(x => x.Id == userId);
            var cultureId = _workContext.GetCurrentCultureId();
            if (user == null)
            {
                return NotFound();
            }

            var defaultAddressId = user.DefaultShippingAddressId.HasValue ? user.DefaultShippingAddressId : 0;
            var userAddress = await userAddressRepository
                .Query()
                .Where(x => (x.AddressType == EskaCMS.Entities.Enum.AddressType.Shipping) && (x.UserId == userId))
                .Select(x => new ShippingAddressVm
                {
                    UserAddressId = x.Id,
                    ContactName = x.Address.ContactName,
                    Phone = x.Address.Phone,
                    AddressLine1 = x.Address.AddressLine1,
                    CityName = x.Address.City,
                    ZipCode = x.Address.ZipCode,
                    DistrictName = x.Address.District.DistrictDetails.Where(x=> cultureId==0||cultureId==x.CultureId).Select(x=>x.Name).FirstOrDefault(),
                    StateOrProvinceName = x.Address.StateOrProvince.StateOrProvinceDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    CountryName = x.Address.Country.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    IsCityEnabled = x.Address.Country.IsCityEnabled,
                    IsZipCodeEnabled = x.Address.Country.IsZipCodeEnabled,
                    IsDistrictEnabled = x.Address.Country.IsDistrictEnabled
                }).ToListAsync();

            return Ok(new { Addresses = userAddress, DefaultShippingAddressId = defaultAddressId });
        }
    }

}
