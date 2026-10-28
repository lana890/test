using EskaCMS.Infrastructure;
using EskaCommerce.Pricing.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCommerce.LoyaltyPoints.Models.Response;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Addresses;

namespace EskaCommerce.ShoppingCart.Services
{
    public interface ICartService
    {
        Task<Result> AddToCart(long customerId, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanId = null, string ReferenceId = null);


		// Task<Result> AddCartItemOnlyOneProduct(long customerId, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanId = null);
		Task<long> AddToCartSingleProduct(long customerId, long createdById, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanId = null);

		Task<Result> AddToCart(long customerId, long createdById, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanId = null, string ReferenceId = null);

        IQueryable<Cart> Query();

        Task<DeliveryOptionEnum> CheckDeliveryAddress(string shippingData);
        Task<Cart> GetActiveCart(long customerId, CartTypes CartType, long SiteId,long? cartId=null, string ReferenceId = null);

        Task<Cart> GetActiveCart(long customerId, long createdById, CartTypes CartType, long SiteId, long? cartId = null , string? ReferenceId = null);

        Task<CartVm> GetActiveCartDetails(long customerId, CartTypes CartType, long SiteId);

        Task<CartVm> GetActiveCartDetails(long customerId, long createdById, CartTypes CartType, long SiteId);

        Task<CouponValidationResult> ApplyCoupon(long cartId, string couponCode);

        Task RemoveCoupon(Cart cart);

        Task MigrateCart(long fromUserId, long toUserId, long SiteId);
        Task DeActiveCart(long cartId);
        Task ReActiveCart(long cartId);
        Task<Result> BulkAddToCart(long customerId, long createdById, List<AddToCartModel> model, long SiteId);

        Task<Cart> GetCartById(long CartId);
      //  Task<decimal> GetDiscount(CartItem Item, long SiteId, decimal TiersAmount);

        Task updateAdditionalInfo(UpdateCartInfoVM cartInfoVM, long customerId, long SiteId,long? cartId);
        Task DeActiveAllCarts(long UserId, long CreatedBy);
        Task CheckAvaliableStock(int quantity, long productId);
        Task<RedeemPointsValidationDto> ApplyLoyaltyPoints(CartTypes CartType);
        Task<bool> RemovePoints(Cart cart, long userId, long siteId);
        Task<bool> CalculateRateAramix(long id);
        Task CalculateShippingRate(Cart cart);
        Task<bool> RemoveShippingAddress(long cartId);
        Task<CartVm> CheckInventoryItems(CartTypes cartTypes);
        Task<long> WarehouseInventoryStatus(CartItem cartItem, Address? shippingAddress, long siteId, string ShippingData, string productName,long WarehouseId);

        Task NotifyUserAfterPeriod(string BGTemplate, int cartNotificationLeadTimeInMinutes);
        Task AddCartItemPlan(Cart cart, string additionalDetails);


    }

}
