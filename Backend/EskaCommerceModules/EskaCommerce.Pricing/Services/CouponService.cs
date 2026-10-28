using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.cart;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Pricing.Areas.Pricing.ViewModels;
using EskaCommerce.Pricing.Services.Interfaces;
using MailKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.DevTools.V106.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Pricing.Services
{
    public class CouponService : ICouponService
    {
        private readonly IRepository<Coupon> _couponRepository;
        private readonly IRepository<CartRuleUsage> _cartRuleUsageRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<CustomerGroup> customergrouprepository;
        private readonly IRepository<CartRuleCustomerGroup> cartRoleCustomerrepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<CartRuleCustomerGroup> cartRuleCustomerGroupRepository;
        private readonly ISendRuleSms sendSMs;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly ISiteSettingsService settingservice;
        private readonly IRepository<Order> orderRepository;
        private readonly UserManager<User> _userManager;
        private readonly ILoyalityService _loyalityService;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<Promotions> promotionsRepository;
        private readonly IRepository<CartRule> _cartRuleRepository;
        private readonly IRepository<Cart> _cartRepository;
        public CouponService(IRepository<Coupon> couponRepository, IRepository<CartRuleUsage> cartRuleUsageRepository,
            IRepository<Product> productRespository,
            IRepository<CustomerGroup> customergrouprepository,
            IRepository<CartRuleCustomerGroup> CartRoleCustomerrepository,
            IWorkContext workContext,
            IRepository<CartRuleCustomerGroup> CartRuleCustomerGroupRepository,
            ISendRuleSms SendSMs,
            IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
            ISiteSettingsService settingservice,
            IRepository<Order> orderRepository,
            UserManager<User> userManager,
            ILoyalityService loyalityService,
            IUserSiteService userSiteService,
            IRepository<Promotions>  promotionsRepository,
            IRepository<CartRule> cartRuleRepository,
            IRepository<Cart> cartRepository
            )
        {
            _couponRepository = couponRepository;
            _cartRuleUsageRepository = cartRuleUsageRepository;
            _productRepository = productRespository;
            this.customergrouprepository = customergrouprepository;
            cartRoleCustomerrepository = CartRoleCustomerrepository;
            this.workContext = workContext;
            cartRuleCustomerGroupRepository = CartRuleCustomerGroupRepository;
            sendSMs = SendSMs;
            this.siteSettingsRepository = siteSettingsRepository;
            this.settingservice = settingservice;
            this.orderRepository=orderRepository;
            _userManager = userManager;
            _loyalityService = loyalityService;
            this.userSiteService = userSiteService;
            this.promotionsRepository=promotionsRepository;
            _cartRuleRepository = cartRuleRepository;
            _cartRepository=cartRepository;

        }

        public async Task CheckProductsDiscount(long SiteId, long ProductId)
        {
            var coupons = await _cartRuleRepository.Query().Include(x => x.Products)
                .Include(x => x.Categories)
                .Where(x => x.SiteId == SiteId
                && x.IsActive == true
                && x.StartOn > DateTimeOffset.Now
                && x.EndOn <= DateTimeOffset.Now
                && x.Products.Select(y => y.ProductId).Contains(ProductId)).ToListAsync();

        }

        public async Task<CouponValidationResult> Validate
            (long customerId, string couponCode,
            CartInfoForCoupon cart,bool? afterAddorder=false
            )
        {
            try
            {
                var siteId =  workContext.GetCurrentSiteId();

                var cartRuleQuery = _cartRuleRepository.Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Include(c => c.Products)
                    .Include(c => c.Categories)
                    .Include(x => x.Coupons)
                    .Where(x => x.Status == EStatus.Active || x.Status == EStatus.Published);

                // ---- Case 1: Auto Promotions ----
                if (string.IsNullOrEmpty(couponCode))
                {
                    var orders = await orderRepository.Query()
                                 .Where(x => x.CustomerId == customerId &&
                                             x.OrderStatus != OrderStatus.PaymentFailed &&
                                             x.OrderStatus != OrderStatus.Canceled &&
                                             x.OrderStatus != OrderStatus.PendingPayment&&
                                             x.OrderStatus != OrderStatus.PaymentReceived&&
                                             x.OrderStatus != OrderStatus.Complete)
                                 .ToListAsync();
                 

                    var promotions = await promotionsRepository.Query()
                        .Include(x => x.CartRule).ThenInclude(x => x.CustomerGroups)
                        .Include(x => x.CartRule).ThenInclude(x => x.Products)
                        .Where(x => x.SiteId == siteId &&x.CartRule.Status!=EStatus.Deleted)
                        .ToListAsync();

                    if (promotions?.Any() == true)
                    {
                        var validationResult = await ValidateAutoDiscount(customerId, cart, orders, promotions, afterAddorder);
                         validationResult.IsAutoCoupon = true;
                         validationResult.Succeeded = true;
                        return validationResult;
                    }

                    return new CouponValidationResult
                    {
                        Succeeded = true                    };
                }

                // ---- Case 2: Manual Coupon ----
                var rule = await cartRuleQuery
                    .FirstOrDefaultAsync(x => x.Coupons.Any(c => c.Code.ToLower() == couponCode.ToLower() && c.IsActive));
                if (rule == null)
                {
                    return new CouponValidationResult
                    {
                        Succeeded = false,
                        ErrorMessage = "The coupon doesn’t exist."
                    };
                }

                return await ValidateCoupon(customerId, cart, rule, couponCode);
            }
            catch
            {
                throw;
            }
        }

        public async Task<CouponValidationResult> ValidateAutoDiscount(long customerId, CartInfoForCoupon cart, List<Order> orders, List<Promotions> promotions,bool?AfterAddOrder=false)
        {
            try
            {
                var warehouseId = await workContext.GetCurrentWarehouseId();
                var cultureId =  workContext.GetCurrentCultureId();
                var isLocalCountry = await userSiteService.IsLocalCountry();

                var result = new CouponValidationResult { Succeeded = false };

                foreach (var promotion in promotions)
                {
                    // Free shipping is resolved by QualifiesForFreeShipping, not as a product discount.
                    if (promotion.EPromotion == PromotionsEnum.ShippingFees)
                        continue;
                    if (!await PassesPromotionChecks(promotion, customerId, orders, cart, AfterAddOrder))
                        continue;

                    var validationResult = await ApplyCartRule(promotion.CartRule, cart, isLocalCountry, warehouseId, cultureId);
                
                    //if (validationResult.IsFreeShipping)
                    //{
                    //    result.Succeeded = true;
                    //    result.IsAutoCoupon = true;
                    //    result.IsFreeShipping = true;
                    //    continue;
                    //}
                    if (!validationResult.Succeeded || !validationResult.DiscountedProducts.Any())
                        continue;
                    result.DiscountType = validationResult.DiscountType;
                   result.DiscountAmount += validationResult.DiscountAmount;
                       result.Succeeded=true;
                   result.DiscountedProducts=validationResult.DiscountedProducts;
                   result.IsAutoCoupon=true;
                   

                }

                return result;
            }
            catch
            {
               return new CouponValidationResult { Succeeded = true };
            }
        }

        private async Task<bool> PassesPromotionChecks(Promotions promotion, long customerId, List<Order> orders, CartInfoForCoupon cart,bool? AfterAddOrder = false)
        {
            if ( promotion.EPromotion == PromotionsEnum.Registration)
            {
                if (promotion.UserState == UserState.AnonymousUser)
                    return false;
                   var user = await _userManager.Users
                        .Where(x => x.Id == customerId && x.Type != EUsersTypes.GuestClient)
                        .FirstOrDefaultAsync();
                //orders =new List<Order>();
                return user!=null && (orders?.Count == 0);


            }

            if (promotion.EPromotion == PromotionsEnum.NumberOfOrders)
            {
                if (orders == null || !orders.Any()) return false;
                
                var orderCount = AfterAddOrder.Value ? orders.Count+1 : orders.Count;
                return orderCount == promotion.QuantityOfOrders ? true : false;
            }

            if (promotion.EPromotion == PromotionsEnum.Shipping)
            {
                var current_cart = await _cartRepository.Query().Where(x => x.CustomerId == customerId && x.IsActive).OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync();
                if (current_cart == null || string.IsNullOrEmpty(current_cart.ShippingData)) return false;
                  return CheckDelivaryStatus(current_cart);
                
            }

            return false;
        }

        private async Task<CouponValidationResult> ApplyCartRule(CartRule cartRule,
            CartInfoForCoupon cart,
            bool isLocalCountry,
            long warehouseId,
            long cultureId)
        {
            var validationResult = new CouponValidationResult { Succeeded = false };

            // Check Customer Group
            if (cartRule.IsCustomerGroup)
            {
                var userId = await workContext.GetCurrentUserId();

                var cartRuleCustomerGroups = await cartRoleCustomerrepository.Query()
                    .Include(x => x.CustomerGroup)
                    .ThenInclude(x => x.Users)
                    .Where(x => x.CartRuleId == cartRule.Id && x.CartRule.Status != EStatus.Deleted)
                    .AsNoTrackingWithIdentityResolution()
                    .ToListAsync();

                var users = cartRuleCustomerGroups.SelectMany(x => x.CustomerGroup.Users).Select(x => x.UserId).ToList();

                if (!users.Contains(userId))
                    return validationResult; // fails silently
            }

            // Date validation
            if (cartRule.StartOn.HasValue && cartRule.StartOn > DateTimeOffset.Now)
                return validationResult;

            if (cartRule.EndOn.HasValue && cartRule.EndOn <= DateTimeOffset.Now)
                return validationResult;
     
            if (cartRule.RuleToApply == "free_shipping")
                return validationResult;
            // Discountable Products
            var discountableProducts = await GetDiscountableProducts(cartRule, cart, isLocalCountry, warehouseId, cultureId);

            if (!discountableProducts.Any())
                return validationResult;

            // Apply discounts
            foreach (var item in cart.Items)
            {
                var discountableProduct = discountableProducts.FirstOrDefault(x => x.Id == item.ProductId);
                if (discountableProduct == null) continue;

                validationResult.DiscountedProducts.Add(new DiscountedProduct
                {
                    Id = discountableProduct.Id,
                    Name = discountableProduct.Name,
                    Price = discountableProduct.Price,
                    Quantity = item.Quantity
                });
            }

            if (!validationResult.DiscountedProducts.Any())
                return validationResult;

            var totalCartAmount = cart.Items.Sum(x => x.Price * x.Quantity);
            // Calculate discount
            switch (cartRule.RuleToApply)
            {
                case "cart_fixed":
                    validationResult.DiscountType = "cart_fixed";
                    validationResult.DiscountValue = cartRule.DiscountAmount.ToString();
					validationResult.DiscountAmount =Math.Min(cartRule.DiscountAmount, totalCartAmount);
                    validationResult.Succeeded = true;
                    return validationResult;

                case "by_percent":
					validationResult.DiscountType = "by_percent";
					validationResult.DiscountValue = cartRule.DiscountAmount.ToString();

					foreach (var item in validationResult.DiscountedProducts)
                    {
                        item.DiscountAmount += (item.Price * cartRule.DiscountAmount / 100) * item.Quantity;
                    }
                    var totalDiscount=validationResult.DiscountedProducts.Sum(x=>x.DiscountAmount);    
                    if (cartRule.MaxDiscountAmount.HasValue)
                    {
                        totalDiscount = Math.Min(totalDiscount, cartRule.MaxDiscountAmount.Value);
                       
                    }
                    validationResult.Succeeded = true;
                    validationResult.DiscountAmount = totalDiscount;
                    return validationResult;


                default:
                    throw new InvalidOperationException($"{cartRule.RuleToApply} is not supported");

            }

        }

        private async Task<IList<DiscountableProduct>> GetDiscountableProducts(CartRule cartRule, CartInfoForCoupon cart, bool isLocalCountry, long warehouseId, long cultureId)
        {
            if (!cartRule.Products.Any() && !cartRule.Categories.Any())
            {
                var productIds = cart.Items.Select(x => x.ProductId);

                return await _productRepository.Query()
                    .Include(x => x.ProductPriceWarehouses)
                    .Where(x => productIds.Contains(x.Id))
                    .Select(x => new DiscountableProduct
                    {
                        Id = x.Id,
                        Name = x.ProductDetails.Where(d => d.CultureId == cultureId).FirstOrDefault().Name ?? "",
                        Price = isLocalCountry
                            ? (x.PriceType == EPriceType.ProductBase
                                ? x.Price.Value
                                : x.ProductPriceWarehouses.FirstOrDefault(p => p.WarehouseId == warehouseId).Price)
                            : x.ForeignPrice ?? 0
                    }).ToListAsync();
            }

            // With product/category restrictions
            return GetDiscountableProduct(cartRule.Products, cartRule.Categories, isLocalCountry, warehouseId);
        }


        public async Task<CouponValidationResult> ValidateCoupon(long customerId, CartInfoForCoupon cart, CartRule CartRule, string coupon)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();
                var warehouseId = await workContext.GetCurrentWarehouseId();
                var CultureId = workContext.GetCurrentCultureId();
                var validationResult = new CouponValidationResult { Succeeded = false };
                var Coupon = CartRule.Coupons.Where(x => x.Code.ToLower() == coupon.ToLower()).FirstOrDefault();
                if (Coupon==null)
                {
                    return new CouponValidationResult
                    {
                        Succeeded = false,
                        ErrorMessage = "The coupon doesn’t exist."
                    };
                }


                if (CartRule.IsCustomerGroup)
                {
                    var userId = await workContext.GetCurrentUserId();

                    var cartRuleCustomerGroups = await cartRoleCustomerrepository
                                                             .Query()
                                                             .Include(x => x.CustomerGroup)
                                                             .ThenInclude(x => x.Users)
                                                             .Where(x => x.CartRuleId == CartRule.Id && x.CartRule.Status != EStatus.Deleted)
                                                             .AsNoTrackingWithIdentityResolution()
                                                             .ToListAsync();


                    var Users = cartRuleCustomerGroups.SelectMany(x => x.CustomerGroup.Users).ToList().ConvertAll(x => x.UserId);

                    if (!Users.Any(x => x == userId))
                    {
                        var Massage = $"This coupon is not permitted for You";
                        validationResult.ErrorMessage = Massage;
                        return validationResult;
                    }
                }

                if (CartRule.StartOn.HasValue && CartRule.StartOn > DateTimeOffset.Now)
                {
                    validationResult.ErrorMessage = "The coupon not active yet.";
                    return validationResult;
                }

                if (CartRule.EndOn.HasValue && CartRule.EndOn <= DateTimeOffset.Now)
                {
                    validationResult.ErrorMessage = "The coupon is expired.";
                    return validationResult;
                }

                var couponUsageCount = await _cartRuleUsageRepository.Query().CountAsync(x => x.CouponId == Coupon.Id);
                if (CartRule.UsageLimitPerCoupon.HasValue && couponUsageCount >= CartRule.UsageLimitPerCoupon)
                {
                    validationResult.ErrorMessage = "The coupon is all used.";
                    return validationResult;
                }

                var couponUsageByCustomerCount = await _cartRuleUsageRepository.Query().CountAsync(x => x.CouponId == Coupon.Id && x.UserId == customerId);
                if (CartRule.UsageLimitPerCustomer.HasValue && couponUsageByCustomerCount >= CartRule.UsageLimitPerCustomer)
                {
                    validationResult.ErrorMessage = $"You have used this coupon more than once.";
                    return validationResult;
                }


                IList<DiscountableProduct> discountableProducts = new List<DiscountableProduct>();
                if (!CartRule.Products.Any() && !CartRule.Categories.Any())
                {
                    var productIds = cart.Items.Select(x => x.ProductId);
                    discountableProducts = _productRepository.Query()
                        .Include(x => x.ProductPriceWarehouses)
                       .Where(x => productIds.Contains(x.Id))
                       .Select(x => new DiscountableProduct
                       {
                           Id = x.Id,
                           Name = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name ?? "",
                           Price = IsLocalCountry ? x.PriceType == EskaCMS.Entities.Enum.EPriceType.ProductBase
                                                          ? x.Price.HasValue?x.Price.Value:0
                                                          : x.ProductPriceWarehouses.Where(x => x.WarehouseId == warehouseId).Select(x => x.Price).FirstOrDefault()

                                               : x.ForeignPrice.HasValue ? x.ForeignPrice.Value : 0
                       })
                       .ToList();

                }
                else
                {
                    discountableProducts = GetDiscountableProduct(CartRule.Products, CartRule.Categories, IsLocalCountry, warehouseId);

                    var discountProducts = cart.Items.Where(x => discountableProducts.Select(x => x.Id).Contains(x.ProductId)).ToList();

                    if (discountProducts.Count != 0)
                    {
                        var ProdQuantity = discountProducts.Sum(x => x.Quantity);
                        if (!(CartRule.Quantity == 0) && ProdQuantity < CartRule.Quantity)
                        {
                            validationResult.ErrorMessage = $"You can use this coupon when buy more than one product";
                            return validationResult;
                        }
                    }
                    else
                    {
                        validationResult.ErrorMessage = $"That coupon doesn't apply to the selected product(s)";
                        return validationResult;
                    }
                }

                foreach (var item in cart.Items)
                {
                    var discountableProduct = discountableProducts.FirstOrDefault(x => x.Id == item.ProductId);
                    if (discountableProduct != null)
                    {
                        var discountedProduct = new DiscountedProduct { Id = discountableProduct.Id, Name = discountableProduct.Name, Price = discountableProduct.Price, Quantity = 1 };
                        couponUsageCount = couponUsageCount + 1;
                        couponUsageByCustomerCount = couponUsageByCustomerCount + 1;
                        for (var i = 1; i < item.Quantity; i++)
                        {
                            discountedProduct.Quantity = discountedProduct.Quantity + 1;
                            couponUsageCount = couponUsageCount + 1;
                            couponUsageByCustomerCount = couponUsageByCustomerCount + 1;
                        }

                        validationResult.DiscountedProducts.Add(discountedProduct);
                    }
                }

                if (!validationResult.DiscountedProducts.Any())
                {
                    validationResult.ErrorMessage = $"The coupon doesn't apply to any products in your cart";
                    return validationResult;
                }

                validationResult.Succeeded = true;
                validationResult.CouponId = Coupon.Id;
                validationResult.CouponCode = Coupon.Code;
                validationResult.CouponRuleName = CartRule.Name;
                validationResult.CartRule = CartRule;

                switch (CartRule.RuleToApply)
                {
                    case "cart_fixed":
						validationResult.DiscountType = "cart_fixed";

						validationResult.DiscountValue = CartRule.DiscountAmount.ToString(); 
                        return validationResult;

                    case "by_percent":
						validationResult.DiscountType = "by_percent";
						validationResult.DiscountValue = CartRule.DiscountAmount.ToString();

						foreach (var item in validationResult.DiscountedProducts)
                        {
                            item.DiscountAmount = (item.Price * CartRule.DiscountAmount / 100) * item.Quantity;
                        }
                        if (CartRule.MaxDiscountAmount.HasValue && validationResult.DiscountedProducts.Sum(x => x.DiscountAmount) > CartRule.MaxDiscountAmount)
                        {
                            validationResult.DiscountAmount = CartRule.MaxDiscountAmount.Value;
                            return validationResult;
                        }

                        validationResult.DiscountAmount = validationResult.DiscountedProducts.Sum(x => x.DiscountAmount);

                        return validationResult;

                    default:
                        throw new InvalidOperationException($"{CartRule.RuleToApply} is not supported");

                }
            }
            catch (Exception ex)
            {

                throw ex;
            }



        }

        private IList<DiscountableProduct> GetDiscountableProduct(IList<CartRuleProduct> cartRuleProducts, IList<CartRuleCategory> cartRuleCategorys, bool IsLocalCountry, long WarehouseId)
        {
            var CultureId = workContext.GetCurrentCultureId();

            IList<DiscountableProduct> discountedProducts = new List<DiscountableProduct>();

            if (cartRuleProducts.Any())
            {
                var productIds = cartRuleProducts.Select(x => x.ProductId).ToList();
                discountedProducts = _productRepository.Query()
                     .Include(x => x.ProductPriceWarehouses)
                    .Where(x => productIds.Contains(x.Id))
                    .Select(x => new DiscountableProduct
                    {
                        Id = x.Id,
                        Name = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name ?? "",
                        Price = IsLocalCountry ? x.PriceType == EPriceType.ProductBase
                                                       ? x.Price.HasValue?x.Price.Value:0
                                                       : x.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

                                               : x.ForeignPrice.HasValue ? x.ForeignPrice.Value : 0
                    }).ToList();
            }

            if (cartRuleCategorys.Any())
            {
                var categoryIds = cartRuleCategorys.Select(x => x.CategoryId);
                var discountedProductByCategories = _productRepository.Query()
                    .Where(x => x.Categories.Any(c => categoryIds.Contains(c.Id)))
                   .Select(x => new DiscountableProduct
                   {
                       Id = x.Id,
                       Name = x.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name ?? "",
                       Price = IsLocalCountry ? x.PriceType == EPriceType.ProductBase
                                                       ? x.Price.Value
                                                       : x.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

                                               : x.ForeignPrice.HasValue ? x.ForeignPrice.Value : 0
                   }).ToList();
                discountedProducts = discountedProducts.Concat(discountedProductByCategories).ToList();
            }

            return discountedProducts;
        }

        public void AddCouponUsage(long customerId, long orderId, CouponValidationResult couponValidationResult)
        {
            if (!couponValidationResult.Succeeded || couponValidationResult.CartRule == null)
            {
                return;
            }

            CartRuleUsage couponUsage;
            switch (couponValidationResult.CartRule.RuleToApply)
            {
                case "cart_fixed":
                    couponUsage = new CartRuleUsage
                    {
                        UserId = customerId,
                        OrderId = orderId,
                        CouponId = couponValidationResult.CouponId,
                        CartRuleId = couponValidationResult.CartRule.Id,
                        DiscountAmount = couponValidationResult.DiscountAmount
                    };

                    _cartRuleUsageRepository.Add(couponUsage);
                    break;

                case "by_percent":
                    foreach (var item in couponValidationResult.DiscountedProducts)
                    {
                        for (var i = 0; i < item.Quantity; i++)
                        {
                            couponUsage = new CartRuleUsage
                            {
                                UserId = customerId,
                                OrderId = orderId,
                                CouponId = couponValidationResult.CouponId,
                                CartRuleId = couponValidationResult.CartRule.Id,
                                DiscountAmount = couponValidationResult.DiscountAmount
                            };

                            _cartRuleUsageRepository.Add(couponUsage);
                        }
                    }

                    break;

                default:
                    throw new InvalidOperationException($"{couponValidationResult.CartRule.RuleToApply} is not supported");
            }
        }

        public async Task<long> AddCoupon(CouponFormVM model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var existingActiveCoupons = await _couponRepository.Query().Where(x => x.Code == model.Coupon && x.IsActive).ToListAsync();

                if (existingActiveCoupons != null && existingActiveCoupons.Count() > 0)
                {
                    throw new Exception("coupon Is Already Exist");

                }
                var coupon = new Coupon
                {
                    CartRuleId = model.CartRuleId,
                    Code = model.Coupon,
                    CreatedOn = DateTimeOffset.Now,
                    IsActive = model.IsActive


                };
                _couponRepository.Add(coupon);
                await _couponRepository.SaveChangesAsync();

                if (model.IsActive)
                {
                    var CartRule = await _cartRuleRepository.Query().Where(x => x.Id == model.CartRuleId)
                                                                              .Include(x => x.CustomerGroups)
                                                                              .FirstOrDefaultAsync();

                    var CartRuleCustomerGroupId = CartRule.CustomerGroups.Select(x => x.CustomerGroupId).ToList();

                    if (CartRuleCustomerGroupId != null && CartRuleCustomerGroupId.Count() > 0)
                    {


                        var customerGroup = await customergrouprepository
                                                              .Query()
                                                              .Include(x => x.Users)
                                                              .ThenInclude(x => x.User)
                                                              .Where(x => CartRuleCustomerGroupId.Contains(x.Id))
                                                              .ToListAsync();

                        var PhoneNumbers = customerGroup.SelectMany(x => x.Users)
                                                        .Select(x => x.User)
                                                        .Where(x => x.Status != EStatus.Deleted && !string.IsNullOrEmpty(x.PhoneNumber))
                                                        .Select(x => x.PhoneNumber)
                                                        .ToList();


                        NotificationObject notificationObject = await HandlerNotification(PhoneNumbers, model.Coupon, CartRule);



                        try
                        {
                            await sendSMs.SendMessageAsync(NotificationType.SMS, null, notificationObject, SiteId);
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                    }
                }
                return coupon.Id;


            }

            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<ExcelFile> BulkAddition(long CartRuleId, List<CouponVM> Coupons)
        {
            await _couponRepository.ExecuteInTransactionAsync(async () =>
            {
                foreach (var Coupon in Coupons)
                {
                    await this.AddCoupon(new CouponFormVM(CartRuleId, Coupon.Code));
                }
            });
            return await ExcelHelper.ExportExcelTemplate<CouponVM>("Coupons Template");
        }

        public async Task<CouponValidationResult> ValidatePlan(long customerId, string couponCode, decimal SubTotal, long? CartRuleId = null)
        {
            try
            {
                var coupon = await _couponRepository.Query()
                    .Include(x => x.CartRule).ThenInclude(c => c.Products)
                    .Include(x => x.CartRule).ThenInclude(c => c.Categories)
                    .Include(x => x.CartRule)
                    .FirstOrDefaultAsync(x => x.Code == couponCode && x.CartRule.Status != EStatus.Deleted);

                var validationResult = await ValidatePlanApplyCoupon(customerId, SubTotal, coupon, CartRuleId);

                return validationResult;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<CouponValidationResult> ValidatePlanApplyCoupon(long customerId, decimal SubTotal, Coupon? coupon, long? CartRuleId = null)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();
                var CultureId = workContext.GetCurrentCultureId();
                var validationResult = new CouponValidationResult { Succeeded = false };

                if (!CartRuleId.HasValue)
                {

                    validationResult.ErrorMessage = "The coupon doesn’t exist.";
                    return validationResult;
                }



                if (coupon == null || !coupon.IsActive || !coupon.CartRule.IsActive || !(coupon.CartRule.Status != EStatus.Deleted))
                {
                    validationResult.ErrorMessage = "The coupon doesn’t exist.";
                    return validationResult;
                }

                if (coupon.CartRule.IsCustomerGroup)
                {
                    var userId = await workContext.GetCurrentUserId();

                    var cartRuleCustomerGroups = await cartRoleCustomerrepository
                                                             .Query()
                                                             .Include(x => x.CustomerGroup)
                                                             .ThenInclude(x => x.Users)
                                                             .Where(x => x.CartRuleId == coupon.CartRule.Id)
                                                             .AsNoTrackingWithIdentityResolution()
                                                             .ToListAsync();


                    var Users = cartRuleCustomerGroups.SelectMany(x => x.CustomerGroup.Users).ToList().ConvertAll(x => x.UserId);

                    //   var CustomerGroupName = cartRuleCustomerGroups.Select(x => x.CustomerGroup).FirstOrDefault()?.Name;

                    if (!Users.Any(x => x == userId))
                    {
                        var Massage = $"This coupon is not permitted for You";
                        validationResult.ErrorMessage = Massage;
                        return validationResult;
                    }
                }

                if (coupon.CartRule.StartOn.HasValue && coupon.CartRule.StartOn > DateTimeOffset.Now)
                {
                    validationResult.ErrorMessage = "The coupon not active yet.";
                    return validationResult;
                }

                if (coupon.CartRule.EndOn.HasValue && coupon.CartRule.EndOn <= DateTimeOffset.Now)
                {
                    validationResult.ErrorMessage = "The coupon is expired.";
                    return validationResult;
                }

                var couponUsageCount = await _cartRuleUsageRepository.Query().CountAsync(x => x.CouponId == coupon.Id);
                if (coupon.CartRule.UsageLimitPerCoupon.HasValue && couponUsageCount >= coupon.CartRule.UsageLimitPerCoupon)
                {
                    validationResult.ErrorMessage = "The coupon is all used.";
                    return validationResult;
                }

                var couponUsageByCustomerCount = await _cartRuleUsageRepository.Query().CountAsync(x => x.CouponId == coupon.Id && x.UserId == customerId);
                if (coupon.CartRule.UsageLimitPerCustomer.HasValue && couponUsageByCustomerCount >= coupon.CartRule.UsageLimitPerCustomer)
                {
                    validationResult.ErrorMessage = $"You have used this coupon more than once.";
                    return validationResult;
                }
                validationResult.Succeeded = true;
                validationResult.CouponId = coupon.Id;
                validationResult.CouponCode = coupon.Code;
                validationResult.CouponRuleName = coupon.CartRule.Name;
                validationResult.CartRule = coupon.CartRule;

                switch (coupon.CartRule.RuleToApply)
                {
                    case "cart_fixed":
                        validationResult.DiscountAmount = coupon.CartRule.DiscountAmount;
                        //if (coupon.CartRule.MaxDiscountAmount.HasValue && validationResult.DiscountAmount > coupon.CartRule.MaxDiscountAmount)
                        //{
                        //    validationResult.DiscountAmount = coupon.CartRule.MaxDiscountAmount.Value;
                        //    return validationResult;
                        //}
                        return validationResult;
                    case "by_percent":
                        validationResult.DiscountAmount = (SubTotal * coupon.CartRule.DiscountAmount / 100);
                        if (coupon.CartRule.MaxDiscountAmount.HasValue && validationResult.DiscountedProducts.Sum(x => x.DiscountAmount) > coupon.CartRule.MaxDiscountAmount)
                        {
                            validationResult.DiscountAmount = coupon.CartRule.MaxDiscountAmount.Value;
                            return validationResult;
                        }
                        return validationResult;

                    default:
                        throw new InvalidOperationException($"{coupon.CartRule.RuleToApply} is not supported");

                }
            }
            catch (Exception ex)
            {

                throw ex;
            }



        }
        public SmartTableResult<CouponFormVM> CouponList(SmartTableParam param)
        {
            try
            {

                dynamic Search = param.Search.PredicateObject;
                var CartRuleId = Search.CartRuleId;
                long RuleId = Convert.ToInt64(CartRuleId.ToString());


                IQueryable<Coupon> query = _couponRepository
                    .Query().Include(x => x.CartRule).Where(x => x.CartRuleId == RuleId && x.CartRule.Status != EStatus.Deleted).Include(x => x.CartRule);




                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;


                    if (search.CouponCode != null)
                    {
                        string couponCode = search.CouponCode;
                        query = query.Where(x => x.Code.Contains(couponCode));
                    }


                    if (search.CreatedOn != null)
                    {
                        if (search.CreatedOn.before != null)
                        {
                            DateTimeOffset before = search.CreatedOn.before;
                            query = query.Where(x => x.CreatedOn <= before);
                        }

                        if (search.CreatedOn.after != null)
                        {
                            DateTimeOffset after = search.CreatedOn.after;
                            query = query.Where(x => x.CreatedOn >= after);
                        }
                    }
                }


                var coupons = query.ToSmartTableResult(
                    param,
                    coupon => new CouponFormVM
                    {
                        Id = coupon.Id,
                        CartRuleName = coupon.CartRule.Name,
                        CartRuleId = coupon.CartRuleId,
                        Coupon = coupon.Code,
                        CreatedOn = coupon.CreatedOn,
                        IsActive = (bool)coupon.IsActive,
                        //UseAsWallet = coupon.UseAsWallet,
                        IsConsumed = _cartRuleUsageRepository.Query().Where(y => y.CouponId == coupon.Id).Count() > 0 ? true : false

                    });


                return coupons;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<CouponFormVM> GetCouponById(long id)
        {
            try
            {
                var coupon = await _couponRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
                var model = new CouponFormVM
                {
                    Id = coupon.Id,
                    Coupon = coupon.Code,
                    CreatedOn = coupon.CreatedOn,
                    IsActive = (bool)coupon.IsActive,
                    CartRuleId = coupon.CartRuleId,
                    IsConsumed = _cartRuleUsageRepository.Query().Where(y => y.CouponId == coupon.Id).Count() > 0 ? true : false
                };
                return model;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task EditCoupon(long id, CouponFormVM model)
        {
            var siteId = workContext.GetCurrentSiteId();
            var coupon = await _couponRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (coupon == null)
            {
                throw new Exception("No coupon found");
            }
            coupon.Code = model.Coupon;
            coupon.IsActive = model.IsActive;

            var existingActiveCoupons = await _couponRepository.Query().Where(x => x.Code == model.Coupon && x.IsActive).ToListAsync();

            if (existingActiveCoupons.Count() > 1 && model.IsActive)
            {
                throw new Exception("coupon Is Already Exist");

            }

            await _couponRepository.SaveChangesAsync();

            if (model.IsActive)
            {
                var CartRule = await _cartRuleRepository.Query().Where(x => x.Id == model.CartRuleId)
                                                                          .Include(x => x.CustomerGroups)
                                                                          .FirstOrDefaultAsync();

                var CartRuleCustomerGroupId = CartRule.CustomerGroups.Select(x => x.CustomerGroupId).ToList();

                if (CartRuleCustomerGroupId != null && CartRuleCustomerGroupId.Count() > 0)
                {


                    var customerGroup = await customergrouprepository
                                                          .Query()
                                                          .Include(x => x.Users)
                                                          .ThenInclude(x => x.User)
                                                          .Where(x => CartRuleCustomerGroupId.Contains(x.Id))
                                                          .ToListAsync();

                    var PhoneNumbers = customerGroup.SelectMany(x => x.Users)
                                                    .Select(x => x.User)
                                                    .Where(x => x.Status != EStatus.Deleted && !string.IsNullOrEmpty(x.PhoneNumber))
                                                    .Select(x => x.PhoneNumber)
                                                    .ToList();


                    //NotificationObject notificationObject = await HandlerNotification(PhoneNumbers, model.Coupon, CartRule);



                    //Task.Run(async () =>
                    //{
                    //    try
                    //    {
                    //        await sendSMs.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        throw ex;
                    //    }
                    //}).ConfigureAwait(false);

                }
            }



        }

        public async Task DeleteCoupon(long CouponId)
        {
            var Coupon = await _couponRepository.Query().FirstOrDefaultAsync(x => x.Id == CouponId);
            if (Coupon == null)
            {
                throw new Exception("Coupon is not found");
            }

            try
            {
                _couponRepository.Remove(Coupon);
                await _couponRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new Exception($"The coupon code {Coupon.Code} can't not be deleted because it has been used");
            }

        }
        private async Task<NotificationObject> HandlerNotification(List<string> PhoneNumbers, string CouponCode, CartRule cartRule)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var siteId = workContext.GetCurrentSiteId();
                var TemplateId = (await settingservice.GetSiteSettingById("CouponTemplate", siteId))?.Value;

                NotificationObject data = new NotificationObject();
                List<long> Ids = new List<long>();
                List<string> SendValues = new List<string>();
                var defaultSmsProvider = (await settingservice.GetSiteSettingById("SmsProvider", siteId))?.Value;


                if (defaultSmsProvider is null)
                    throw new Exception("Please add default provider");

                var smsProvider = (SMSProviders)Enum.Parse(typeof(SMSProviders), defaultSmsProvider, true);
                Ids.Add(userId);
                SendValues.AddRange(PhoneNumbers);
                data.UserId = Ids.ToArray();
                data.MessageTitle = "";
                data.MessageBody = "";
                data.EmailOptions = new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailSenderVM
                {
                    TemplateId = TemplateId,
                    Params = new List<EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM>() {

                            new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM{ Param="[CouponCode]",Value=CouponCode},
                            new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM{ Param="[CouponValue]",Value=cartRule.DiscountAmount.ToString() + (cartRule.RuleToApply.ToLower() == "by_percent"
                                                                            ?" %":"")}

                    },
                    Emails = SendValues

                };
                data.SendValue = SendValues;
                data.SendVia = SendBy.MobileNumber;
                data.NotificationType = NotificationType.SMS;
                data.SMSProvider = smsProvider;
                ;
                data.Severity = 0;

                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> addCouponToGroup(long couponId,long GroupId)
        {
            try
            {
                CartRuleCustomerGroup coupongroup = new CartRuleCustomerGroup
                {
                    CartRuleId = couponId,
                    CustomerGroupId = GroupId
                };
                 cartRoleCustomerrepository.Add(coupongroup);
                await cartRoleCustomerrepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<bool> QualifiesForFreeShipping(Cart cart)
        {
            //    // Manual Coupon Code
            //    if (!string.IsNullOrWhiteSpace(couponCode))
            //    {
            //        var couponResult = await Validate(customerId, couponCode, cart);
            //        if (couponResult.Succeeded && couponResult.IsFreeShipping)
            //            return true;
            //    }

            //    // Auto Coupon
            //    var autoResult = await Validate(customerId, null, cart);
            //    return autoResult.Succeeded && autoResult.IsFreeShipping;
               if (cart == null || cart.CartType == CartTypes.Plan)
                     return false;
        
                 if (IsPickupSelected(cart.ShippingData))
                    return false;
            
 
                if (!CheckDelivaryStatus(cart) && string.IsNullOrWhiteSpace(cart.ShippingMethod))
                     return false;
            
                var promotions = await promotionsRepository.Query()
                .Include(x => x.CartRule)
                .Where(x => x.SiteId == cart.SiteId
                && x.EPromotion == PromotionsEnum.ShippingFees
                && x.CartRule.Status == EStatus.Published||x.CartRule.Status==EStatus.Active)
                .AsNoTracking()
                .ToListAsync();
            foreach(var promotion in promotions)
            {
                if (await IsFreeShippingPromotionEligible(promotion, cart.CustomerId))
                    return true;
            }
            return false;
        }
        private async Task<bool> IsFreeShippingPromotionEligible(Promotions promotion, long customerId)
        {
            var rule = promotion.CartRule;


            if (rule == null || !rule.IsActive)
                return false;
            if (rule.StartOn.HasValue && rule.StartOn > DateTimeOffset.Now)
                return false;
            if (rule.EndOn.HasValue && rule.EndOn <= DateTimeOffset.Now)
                return false;

            if (promotion.UserState == UserState.RegisteredUser)
            {
                var isRegistered = await _userManager.Users
                    .AnyAsync(x => x.Id == customerId && x.Type != EUsersTypes.GuestClient);
                if (!isRegistered)
                    return false;
            }

            if (rule.IsCustomerGroup)
            {
               var inGroup = await cartRoleCustomerrepository.Query()
                    .AnyAsync(x => x.CartRuleId == rule.Id
                                   && x.CartRule.Status != EStatus.Deleted
                                   && x.CustomerGroup.Users.Any(u => u.UserId == customerId));
                if (!inGroup)
                    return false;
            }

            return true;
        }
        private static bool IsPickupSelected(string shippingData)
       {
            if(string.IsNullOrEmpty(shippingData)) return false;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(shippingData);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "IsPickuped", StringComparison.OrdinalIgnoreCase))
                        return prop.Value.ValueKind == System.Text.Json.JsonValueKind.True;
                }
                return false;
            }
            catch (System.Text.Json.JsonException)
            {
                    return true;
              }
        }

        #region Helper
        private bool CheckDelivaryStatus(Cart cart)
        {
            try
            {
                switch (cart.ShippingDelivery)
                {
                    case DeliveryOptionEnum.TopDelivery:
                    case DeliveryOptionEnum.Domestic:
                    case DeliveryOptionEnum.Aramex:
                    case DeliveryOptionEnum.Dhl:
                        return true;
                    default: return false;

                }

            }catch(Exception ex)
            {
                throw;
            }
        }

    

        #endregion
    }
}
