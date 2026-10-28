using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore.Internal;
using EskaCMS.Entities.Entity.Orders;
using MailKit.Search;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Authorize]
    public class CartApiController : Controller
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        public CartApiController(
            IRepository<Cart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            ICurrencyService currencyService,
            IWorkContext workContext)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _workContext = workContext;
            _currencyService = currencyService;
        }


        [HttpGet("api/customers/{customerId}/cart/{CartType}")]
        public async Task<IActionResult> List(long customerId, CartTypes CartType)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();

                var cart = await _cartService.GetActiveCartDetails(customerId, currentUserId, CartType, SiteId);

                return Ok(cart);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost("api/GetUnCompletedCarts/{UserId}")]
        public async Task<IActionResult> GetUnCompletedCarts([FromBody] SmartTableParam param, [FromRoute] long UserId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();


                var query = _cartRepository.Query()
                    .Where(x => x.SiteId == SiteId && x.CustomerId == UserId && !x.Orders.Any() && x.Items.Any())
                    .Include(x => x.Customer)
                    .Include(x => x.Items)


                    .Include("Items.Product.ThumbnailImage")
                    .Include("Items.Product.ProductDetails")

                    .AsNoTracking();

                var carts = query.ToSmartTableResult(param, cart => new
                {
                    Id = cart.Id,
                    Amount = _currencyService
                                           .DcimalRound(cart.Items.Sum(x => x.Quantity * (x.Price.HasValue ? x.Price.Value : x.Product.Price.Value))),
                    IsActive = cart.IsActive,
                    ShippingAmount = cart.ShippingAmount.HasValue ? cart.ShippingAmount : 0,
                    TaxAmount = cart.TaxAmount.HasValue ? cart.TaxAmount : 0,
                    CartType = cart.CartType,
                    RedeemAmount = cart.RedeemAmount,
                    CouponCode = !string.IsNullOrEmpty(cart.CouponCode) ? cart.CouponCode : "",
                    ShippingDelivery = cart.ShippingDelivery,
                    Items = cart.Items.Select(x => new
                    {
                        ProductName = x.Product.ProductDetails.FirstOrDefault().Name,
                        ProductPrice = x.Product.Price,
                        ProductImage = x.Product.ThumbnailImageId.HasValue ? x.Product.ThumbnailImage.PublicUrl : "",
                        Quantity = x.Quantity,
                    })
                });

                return Ok(carts);
            }
            catch (Exception ex)
            {
                var deepEx = ExceptionHelper.ThrowDeepException(ex);

                return BadRequest(deepEx.Message);
            }

        }

        [HttpPost("api/customers/{customerId}/add-cart-item")]
        public async Task<IActionResult> AddToCart(long customerId, [FromBody] AddToCartModel model)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            await _cartService.AddToCart(customerId, currentUserId, model.ProductId, model.Quantity, model.CartType, SiteId, model.AdditionalDetails);

            return Accepted();
        }

         
		[HttpPost("api/carts/AddToCartSingleProduct")]
		public async Task<long> AddToCartSingleProduct([FromBody] AddToCartModel model)
		{
			var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
			var currentUserId = await _workContext.GetCurrentUserId();
			
			var result=	await _cartService.AddToCartSingleProduct(currentUserId, currentUserId, model.ProductId, model.Quantity, model.CartType, SiteId, model.AdditionalDetails);

			return result;
		}



		[HttpPut("api/carts/items/{itemId}")]
        public async Task<IActionResult> UpdateQuantity(long itemId, [FromBody] CartQuantityUpdate model)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUserId);
            if (cartItem == null)
            {
                return NotFound();
            }

            cartItem.Quantity = model.Quantity;
            _cartItemRepository.SaveChanges();

            return Accepted();
        }

        [HttpPost("api/carts/{cartId}/apply-coupon")]
        public async Task<ActionResult> ApplyCoupon(long cartId, [FromBody] ApplyCouponForm model)
        {
            try
            {


                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId =  await _workContext.GetCurrentUserId();
                var cart = await _cartService.Query().FirstOrDefaultAsync(x => x.Id == cartId && x.CreatedById == currentUserId);
                if (cart == null)
                {
                    return NotFound();
                }

                var validationResult = await _cartService.ApplyCoupon(cart.Id, model.CouponCode);
                if (validationResult.Succeeded)
                {
                    var cartVm = await _cartService.GetActiveCartDetails(currentUserId, model.CartType, SiteId);
                  
                    return Json(cartVm);
                }

                return Json(validationResult);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost("api/carts/apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponFormApi model)
        {
            try
            {

                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();

                var result = await _cartService.AddToCart(currentUserId, model.ProductId, model.Quantity, model.CartType, SiteId, model.AdditionalDetails);
                if (!result.Success)
                    return Ok(new { Error = true, Message = result.Error });
                else
                {
                    var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                    if (cart == null)
                    {
                        return NotFound("Cart not found");
                    }

                    if (cart.LockedOnCheckout)
                    {
                        return Ok(new { Error = true, Message = "Cart is locked for checkout. Please complete or cancel the checkout first" });
                    }

                    var validationResult = await _cartService.ApplyCoupon(cart.Id, model.CouponCode);
                    if (validationResult.Succeeded)
                    {
                    
                        var cartVm = await _cartService.GetActiveCartDetails(currentUserId, model.CartType, SiteId);
                      
                          return Ok(cartVm);
                    }


                    return Ok(validationResult);
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost("api/carts/{cartId}/save-ordernote")]
        public async Task<IActionResult> SaveOrderNote(long cartId, [FromBody] SaveOrderNote model)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            var cart = await _cartService.Query().FirstOrDefaultAsync(x => x.Id == cartId && x.CreatedById == currentUserId);
            if (cart == null)
            {
                return NotFound();
            }

            cart.OrderNote = model.OrderNote;
            await _cartItemRepository.SaveChangesAsync();
            return Accepted();
        }

        [HttpDelete("api/carts/items/{itemId}")]
        public async Task<IActionResult> Remove(long itemId)
        {
            //var currentUser = await _workContext.GetCurrentUser();
            //var currentUser = Convert.ToInt64(Request.Headers["userId"]);
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUserId);
            if (cartItem == null)
            {
                return NotFound();
            }

            _cartItemRepository.Remove(cartItem);
            _cartItemRepository.SaveChanges();

            return NoContent();
        }
    }

}
