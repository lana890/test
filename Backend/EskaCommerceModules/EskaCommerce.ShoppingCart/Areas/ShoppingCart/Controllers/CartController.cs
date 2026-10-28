

using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Tax.Services;
using EskaCMS.Entities.Entity.ShoppingCart;
using Newtonsoft.Json;
using EskaCMS.Entities.Enum;
using Microsoft.VisualBasic;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Route("api/cart")]

    [Authorize]

    public class CartController : Controller
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Cart> _cartRepository;
        private readonly ICartService _cartService;
        // private readonly IMediaService _mediaService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IdcmsLogService _dcmsLogService;
        // private readonly ITaxService _taxService;


        public CartController(
            IRepository<CartItem> cartItemRepository,
            IRepository<Cart> cartRepository,
            ICartService cartService,
            /* IMediaService mediaService, */
            IWorkContext workContext,
            ICurrencyService currencyService,
            IdcmsLogService dcmsLogService/* ,
            ITaxService taxService */)
        {
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            // _mediaService = mediaService;
            _workContext = workContext;
            _currencyService = currencyService;
            _cartRepository = cartRepository;
            _dcmsLogService = dcmsLogService;
            // _taxService = taxService;

        }


        [HttpPost("add-item")]

        public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Cart item is required.");
                }

                if (model.ProductId <= 0 || model.Quantity <= 0)
                {
                    return BadRequest("ProductId and Quantity are required.");
                }

                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                //var userId = Convert.ToInt64(Request.Headers["userId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                var result = await _cartService.AddToCart(currentUserId, model.ProductId, model.Quantity, model.CartType, SiteId, model.AdditionalDetails,model.PlanVersionId,model.ReferenceId);
                if (result.Success)
                {
                    var cart =await  _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                    if(cart.RedeemAmount > 0)
                    {
                        await _cartService.ApplyLoyaltyPoints(model.CartType);
                    }
                    return await AddItemToCartResult(model.ProductId, model.CartType);
                }
                else
                {
                    return Ok(new { Error = true, Message = result.Error });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("add-list-item")]
        public async Task<IActionResult> AddToCart([FromBody] List<AddToCartModel> model)
        {
            try
            {
                if (model == null || !model.Any())
                {
                    return BadRequest("Cart items are required.");
                }

                if (model.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
                {
                    return BadRequest("ProductId and Quantity are required.");
                }

                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null || currentUser.Id <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                await _dcmsLogService.AddInfo("currentUser : ", currentUser.FullName.ToString() + " id : " + currentUser.Id.ToString(), null, null);

                var result = await _cartService.BulkAddToCart(currentUser.Id, currentUser.Id, model, SiteId);
                if (result.Success)
                {

                    return await ListJson(CartTypes.Plan);
                }
                else
                {
                    return Ok(new { Error = true, Message = result.Error });
                }
            }
            catch (Exception ex)
            {
                await _dcmsLogService.AddInfo("ex : ", ex.ToString(), null, null);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        [HttpGet("add-item-result")]
        public async Task<IActionResult> AddToCartResult(long productId, CartTypes CartType)
        {
            try
            {
                return await AddItemToCartResult(productId, CartType);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        private async Task<IActionResult> AddItemToCartResult(long productId, CartTypes CartType)
        {
            try
            {

           
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                      

                var cartDetails = await _cartService.GetActiveCartDetails(currentUserId, CartType, SiteId);
                 

                var model = new AddToCartResult(_currencyService)
                {
                    CartItemCount = cartDetails.Items.Count,
                    CartAmount = cartDetails.SubTotal,
                    CartId = cartDetails.Id
                };


                var addedProduct = cartDetails.Items.FirstOrDefault(x => x.ProductId == productId);

                if (addedProduct != null)
                {
                    model.ProductName = addedProduct.ProductName;
                    model.Item = addedProduct;
                    model.ProductImage = addedProduct.ProductImage;
                    model.ProductPrice = addedProduct.ProductPrice;
                    model.Quantity = addedProduct.Quantity;
                    model.ProductAdditionalFees = addedProduct.ProductAdditionalFees;
                   
                }

                return Ok(model);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        [HttpGet("cart-list/{CartType}")]
        public async Task<IActionResult> List(CartTypes CartType)
        {
            try
            {
                return await ListJson(CartType);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }



		[HttpPost("GetCartDetailsByCartId")]
		public async Task<IActionResult> GetCartDetailsByCartId([FromBody] GetCartByCartIdVM getCartByCartIdVM)
		{
			try
			{

				var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
				var currentUserId = await _workContext.GetCurrentUserId();

				

				var cart = await _cartService.GetActiveCart(currentUserId, getCartByCartIdVM.cartType, SiteId, getCartByCartIdVM.cartId,getCartByCartIdVM.ReferenceId);

                return Ok(cart);
				// return await ListJson(CartType);
			}
			catch (Exception ex)
			{

				return BadRequest(ex.Message);
			}
		}

          
        [HttpPost("GetCartDetailsByReferenceId")]
        public async Task<IActionResult> GetCartDetailsByReferenceId([FromBody] GetCartByCartIdVM getCartByCartIdVM)
        {
            try
            {

                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, getCartByCartIdVM.cartType, SiteId, null, getCartByCartIdVM.ReferenceId);

                return Ok(cart);
                // return await ListJson(CartType);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }




        private async Task<IActionResult> ListJson(CartTypes CartType)
        {

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUserId = await _workContext.GetCurrentUserId();
            var cart = await _cartService.GetActiveCartDetails(currentUserId, CartType, SiteId);
            return Json(cart);
        }

        [HttpPost("update-tax")]
        public async Task<IActionResult> UpdateTax([FromBody] TaxUpdateVM model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCartDetails(currentUserId, model.CartType, SiteId);

                if (cart != null)
                {
                    var checkingDiscountResult = await _cartService.ApplyCoupon(cart.Id, cart.CouponCode);

                    decimal taxAmount = 0;

                    if (model.IsTaxIncluded)
                    {
                        var itemsSubTotal = cart.Items.Sum(x => x.Quantity * x.ProductPrice);

                        foreach (var item in cart.Items)
                        {
                            //var data = await _cartItemRepository.Query().Include(x => x.Product).ThenInclude(p => p.ProductAdditionalFees).Include(x => x.Product)
                            //    .Include(x => x.Product.TaxClass).ThenInclude(x => x.TaxRate).Where(x => x.Product.SiteId == SiteId && x.Product.TaxClass.TaxRate.CountryId == model.CountryId
                            //    && x.ProductId == item.ProductId && (x.Product.TaxClass.TaxRate.StateOrProvinceId == model.StateOrProvinceId || x.Product.TaxClass.TaxRate.StateOrProvinceId == null)
                            //    && (x.Product.TaxClass.TaxRate.ZipCode == model.ZipCode || x.Product.TaxClass.TaxRate.ZipCode == string.Empty))
                            //    .FirstOrDefaultAsync();
                           // var data
                            decimal discountAmount = 0;

                            if (checkingDiscountResult.Succeeded)
                            {
                                var discountedItem = checkingDiscountResult.DiscountedProducts.FirstOrDefault(x => x.Id == item.ProductId);
                                if (discountedItem != null)
                                {
                                    if (checkingDiscountResult.CartRule.RuleToApply == "cart_fixed")
                                    {
                                        discountAmount = discountedItem.DiscountAmount + ((checkingDiscountResult.DiscountAmount * item.Quantity * item.ProductPrice) / itemsSubTotal);
                                    }
                                    else
                                    {
                                        discountAmount = discountedItem.DiscountAmount;
                                    }
                                }
                                else
                                {
                                    if (checkingDiscountResult.CartRule.RuleToApply == "cart_fixed")
                                    {
                                        discountAmount = ((checkingDiscountResult.DiscountAmount * item.Quantity * item.ProductPrice) / itemsSubTotal);
                                    }
                                }
                            }

                            var tax = 0; // data.Product.TaxClass.TaxRate.Rate;
                            if (tax != 0)
                            {
                                decimal totalFees = 0;
                                if (item.ProductAdditionalFees != null)
                                    foreach (var fee in item.ProductAdditionalFees)
                                    {
                                        if (fee.FeesType == DiscountTypes.Percentage)
                                        {
                                            totalFees += (fee.FeesValue / 100) * (item.Quantity * item.ProductPrice);
                                        }
                                        else if (fee.FeesType == DiscountTypes.FixedAmount)
                                        {
                                            totalFees += fee.FeesValue * item.Quantity;
                                        }
                                    }
                                taxAmount += tax * (item.Quantity * item.ProductPrice + totalFees - discountAmount - cart.Discount);
                                item.TaxPercent = tax;
                            }

                        }
                    }

                    cart.TaxAmount = taxAmount;
                    _cartRepository.SaveChanges();
                }

                return Json(cart);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("update-item-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartQuantityUpdate model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);

                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }

                var cartItem = _cartItemRepository.Query().Include(x => x.Product)
                    .FirstOrDefault(x => x.Id == model.CartItemId && x.Cart.CreatedById == currentUserId);
                if (cartItem == null)
                {
                    return NotFound();
                }

                if (cart != null)
                {
                    await _cartService.ApplyCoupon(cart.Id, cart.CouponCode);
                }

                await _cartService.CheckAvaliableStock(model.Quantity, cartItem.ProductId);


                cartItem.Quantity = model.Quantity;
                _cartItemRepository.SaveChanges();

                if(cart.RedeemAmount > 0)
                {
                    await _cartService.RemovePoints(cart,currentUserId,SiteId);
                    await _cartService.ApplyLoyaltyPoints(model.CartType);
                }

                await _cartRepository.SaveChangesAsync();
                return await List(model.CartType);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Check-Inventory-Items")]

        public async Task<IActionResult> CheckInventoryItems([FromQuery] CartTypes cartTypes)
        {
            try
            {
                return Ok(await _cartService.CheckInventoryItems(cartTypes));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("update-cart-info")]
        public async Task<IActionResult> UpdateCartInfo([FromBody] UpdateCartInfoVM model)
        {
            try  
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var currentUserId = await _workContext.GetCurrentUserId();
				//var cartId = await _workContext.GetCartId();
				await _cartService.updateAdditionalInfo(model, currentUserId, SiteId , model.CartId);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponForm model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }

                var validationResult = await _cartService.ApplyCoupon(cart.Id, model.CouponCode);
                if (validationResult.Succeeded)
                {

                    if(cart.RedeemAmount > 0)
                    {
                        await _cartService.RemovePoints(cart, currentUserId, SiteId);
                        await _cartService.ApplyLoyaltyPoints(model.CartType);
                    }

                    var cartVm = await _cartService.GetActiveCartDetails(currentUserId, model.CartType, SiteId);


					var response = cartVm.GetType()
	.GetProperties()
	.ToDictionary(
		p => p.Name,
		p => p.GetValue(cartVm)
	);

					response["DiscountType"] = validationResult.DiscountType;
					response["DiscountValue"] = validationResult.DiscountValue;


					return Json(response);
                }

                return Json(validationResult);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ApplyLoyaltyPoints/{CartType}")]
        public async Task<IActionResult> ApplyLoyaltyPoints(CartTypes CartType)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId,CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }
                
                var validationResult = await _cartService.ApplyLoyaltyPoints(CartType);
                if (validationResult.Succeeded)
                {
                    var cartVm = await _cartService.GetActiveCartDetails(currentUserId, CartType, SiteId);
                    return Json(cartVm);
                }

                return BadRequest(validationResult.ErrorMessage);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("remove-coupon")]
        public async Task<IActionResult> RemoveCoupon([FromBody] ApplyCouponForm model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }

                await _cartService.RemoveCoupon(cart);
                if (cart.RedeemAmount > 0)
                {
                    await _cartService.RemovePoints(cart, currentUserId, SiteId);
                    await _cartService.ApplyLoyaltyPoints(model.CartType);
                }
                var cartVm = await _cartService.GetActiveCartDetails(currentUserId, model.CartType, SiteId);
                return Json(cartVm);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("RemoveLoyaltyPoints/{CartType}")]
        public async Task<IActionResult> RemoveLoyaltyPoints(CartTypes CartType)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }

                if(cart.RedeemAmount > 0 )
                await _cartService.RemovePoints(cart,currentUserId,SiteId);
                var cartVm = await _cartService.GetActiveCartDetails(currentUserId, CartType, SiteId);
                return Json(cartVm);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("cart/SaveOrderNote")]
        public async Task<IActionResult> SaveOrderNote([FromBody] SaveOrderNote model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                if (cart == null)
                {
                    return BadRequest("Cart is empty");
                }
                cart.OrderNote = model.OrderNote;
                await _cartRepository.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("remove-item/{CartType}")]
        public async Task<IActionResult> Remove(CartTypes CartType, [FromBody] long itemId)
        {
            return await _RemoveCartItem(CartType, itemId);
        }

        [HttpPost("remove-item")]
        public async Task<IActionResult> RemoveCartItem([FromBody] RemoveCartItemVM model)
        {
            return await _RemoveCartItem(model.CartType, model.itemId);
        }

        [NonAction]
        private async Task<IActionResult> _RemoveCartItem(CartTypes CartType,long itemId)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                if (cart.LockedOnCheckout)
                {
                    return CreateCartLockedResult();
                }

                var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUserId);
                if (cartItem == null)
                {
                    throw new Exception("The Item Not Avaliable Any More");
                }


                _cartItemRepository.Remove(cartItem);
                _cartItemRepository.SaveChanges();
                if (cart.RedeemAmount > 0)
                {
                    await _cartService.RemovePoints(cart, currentUserId, SiteId);
                    await _cartService.ApplyLoyaltyPoints(CartType);
                }
                return Json(cart); ;
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPost("deactivate-cart")]
        public async Task DeActiveCart([FromBody] long cartId)
        {
            try
            {
                await _cartService.DeActiveCart(cartId);
                Ok();
            }
            catch (Exception ex)
            {

                BadRequest(ex.Message);
            }


        }


        [HttpPost("deactivate-all-carts")]
        public async Task DeActiveCart()
        {
            try
            {
                long user = await _workContext.GetCurrentUserId();
                await _cartService.DeActiveAllCarts(user, user);
                Ok();
            }
            catch (Exception ex)
            {

                BadRequest(ex.Message);
            }

        }

        [HttpPost("reactivate-cart")]
        public async Task ReActiveCart([FromBody] long cartId)
        {
            try
            {
                await _cartService.ReActiveCart(cartId);
                Ok();
            }
            catch (Exception ex)
            {
                BadRequest(ex);
            }

        }

        [HttpDelete("Remove-Shipping-Address/{CartType}")]
        public async Task<IActionResult> RemoveShippingAddress([FromRoute] CartTypes CartType)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }
                await _cartService.RemoveShippingAddress(cart.Id);
                return  Ok();
            }
            catch (Exception ex)
            {
             return   BadRequest(ex);
            }

        }



        private IActionResult CreateCartLockedResult()
        {
            return Ok(new { Error = true, Message = "Cart is locked for checkout. Please complete or cancel the checkout first" });
        }
    }

}
