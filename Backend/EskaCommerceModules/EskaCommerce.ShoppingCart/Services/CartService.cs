using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Pricing.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Discount.Services;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;

using EskaCMS.Entities.Entity;


using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure;
using EskaCommerce.Pricing.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity.Discount;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.LoyaltyPoints.Models.Response;

using EskaCMS.Entities.Entity.Addresses;
using Google.Apis.Storage.v1.Data;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM;
using EskaCMS.Tax.Services;
using EskaCMS.Core.Services.Interfaces;
using Google.Api.Gax.ResourceNames;
using Microsoft.VisualBasic;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCommerce.Discount.Models;
using EskaCMS.Entities.Entity.ShippingTableRate;
using OfficeOpenXml.Drawing.Chart;
using EskaCMS.Core.Models;
using EskaCMS.DataAccess.Data;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCommerce.Module.Notifications.Notifiers;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using System.Text.Json;
using System.Text.Json.Serialization;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using EskaCommerce.Aramex.Services.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;

namespace EskaCommerce.ShoppingCart.Services
{
    public class CartService : ICartService
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<TaxRate> _taxrateRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ITopDeliveryServices topDeliveryServices;
        private readonly IRepository<Stock> stockRepository;
        private readonly IConfiguration configuration;
        private readonly IEmailTemplateService emailTemplateService;
        private readonly ITestNotifier testNotifier;
        private readonly IRepository<Warehouse> warehouseRepository;
        private readonly IRepository<StockAmount> stockproductRepository;
        private readonly IRepository<UserAddress> userAddressRepository;
        private readonly IRepository<DiscountCustomerGroup> discountCustomerGroupRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<PriceAndDestination> priceAndDestinationRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductServices _productService;
        private readonly IRepository<EskaCMS.Entities.Entity.Discount.Discount> discountRepository;
        private readonly IRepository<PlansVersions> planVersionsrepository;
        private readonly EskaDCMSDbContext context;
        private readonly ICouponService _couponService;
        private readonly bool _isProductPriceIncludeTax;
        private readonly ICurrencyService _currencyService;
        private readonly UserManager<User> _userManager;
        private readonly IProductPricingService productPricingService;
        private readonly IAramexShippingService aramexShippingService;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingRepository;
        private readonly IDiscountService _IDiscountService;
        private readonly IWorkContext workContext;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<ProductSerial> _productserailRepository;
        private readonly ISiteSettingsService _SiteSettings;
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        private readonly ITaxService _taxService;

        public CartService(IRepository<Cart> cartRepository, IRepository<OrderItem> orderRepository, IRepository<PriceAndDestination> PriceAndDestinationRepository, IRepository<CartItem> cartItemRepository, IRepository<TaxRate> taxRateRepository, ICouponService couponService,
            IMediaService mediaService, IConfiguration config, ICurrencyService currencyService,
            UserManager<User> userManager,
             ILoyalityService IUserPlansService,
             IProductPricingService productPricingService,
             IAramexShippingService aramexShippingService,
             IRepositoryWithTypedId<SiteSettings, string> siteSettingRepository,
             IRepository<Product> productRepository,
             ITopDeliveryServices topDeliveryServices,
             IRepository<Stock> StockRepository,
             IConfiguration configuration,
             IEmailTemplateService emailTemplateService,
             ITestNotifier testNotifier,
             IRepository<Warehouse> WarehouseRepository,
             IRepository<StockAmount> StockproductRepository,
             IRepository<UserAddress> UserAddressRepository,
             IRepository<DiscountCustomerGroup> DiscountCustomerGroupRepository,
             IDiscountService IDiscountService,
             IProductServices productService,
               IRepository<EskaCMS.Entities.Entity.Discount.Discount> discountRepository,
               IRepository<PlansVersions> planVersionsrepository,
             ILoyaltyWalletService loyaltyWalletService,
             EskaDCMSDbContext context,
             IWorkContext workContext,
             IUserSiteService userSiteService,
             ITaxService taxService,
            IRepository<ProductSerial> productserailRepository, ISiteSettingsService siteSettings)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _taxrateRepository = taxRateRepository;
            _couponService = couponService;
            _mediaService = mediaService;
            _currencyService = currencyService;
            _isProductPriceIncludeTax = config.GetValue<bool>("Catalog.IsProductPriceIncludeTax");
            _userManager = userManager;
            this.productPricingService = productPricingService;
            this.aramexShippingService = aramexShippingService;
            this.siteSettingRepository = siteSettingRepository;
            _productRepository = productRepository;
            this.topDeliveryServices = topDeliveryServices;
            stockRepository = StockRepository;
            this.configuration = configuration;
            this.emailTemplateService = emailTemplateService;
            this.testNotifier = testNotifier;
            warehouseRepository = WarehouseRepository;
            stockproductRepository = StockproductRepository;
            userAddressRepository = UserAddressRepository;
            discountCustomerGroupRepository = DiscountCustomerGroupRepository;
            _loyaltyWalletService = loyaltyWalletService;
            _IDiscountService = IDiscountService;
            this.workContext = workContext;
            this.userSiteService = userSiteService;
            _productserailRepository = productserailRepository;
            _orderItemRepository = orderRepository;
            priceAndDestinationRepository = PriceAndDestinationRepository;
            _productService = productService;
            this.discountRepository = discountRepository;
            this.planVersionsrepository = planVersionsrepository;
            this.context = context;
            _SiteSettings = siteSettings;
            _taxService = taxService;

        }

        public IQueryable<Cart> Query()
        {
            return _cartRepository.Query();
        }

        public Task<Cart> GetActiveCart(long customerId, CartTypes CartType, long SiteId , long? cartId = null, string ReferenceId = null)
        {
            return GetActiveCart(customerId, customerId, CartType, SiteId , cartId , ReferenceId);
        }

        private bool IsOracleProvider()
        {
            return context.Database.ProviderName?.Contains("Oracle", StringComparison.OrdinalIgnoreCase) == true;
        }

        private async Task<long?> GetOracleActiveCartId(long customerId, long createdById, CartTypes cartType, long siteId)
        {
            var activeCartIds = await context.Database
                .SqlQuery<long>($@"
                    SELECT ID
                    FROM SHOPPINGCART_CART
                    WHERE CUSTOMERID = {customerId}
                      AND CREATEDBYID = {createdById}
                      AND ISACTIVE = 1
                      AND CARTTYPE = {(int)cartType}
                      AND SITEID = {siteId}")
                .ToListAsync();

            return activeCartIds.FirstOrDefault();
        }

        private IQueryable<Cart> GetActiveCartDetailsQuery()
        {
            return _cartRepository.Query()
                .Include(x => x.Items)
                .Include(x => x.Customer)
                .Include(c => c.Orders)
                .Include("Items.Product")
                .Include("Items.Product.ProductBranch")
                .Include("Items.Product.ProductPriceWarehouses")
                .Include("Items.Product.ProductAvaliablity")
                .Include("Items.Product.ProductDetails")
                .Include("Items.Product.ProductAdditionalFees")
                .Include("Items.Product.DiscountProducts")
                .Include("Items.Product.DiscountProducts.Discount")
                .Include("Items.Product.DiscountProducts.Discount.DiscountTiers")
                .Include("Items.Product.DiscountProducts.Discount.CustomerGroups")
                .Include("Items.Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup")
                .Include("Items.Product.DiscountProducts.Discount.CustomerGroups.CustomerGroup.Users")
                .Include(x => x.PlansVersion)
                .Include("PlansVersion.TaxClass")
                .Include("PlansVersion.Discount")
                .Include("PlansVersion.PlanProductVersions")
                .Include("PlansVersion.PlanProductVersions.Product")
                .Include("PlansVersion.PlanProductVersions.Product.ProductDetails")
                .Include("PlansVersion.Plan")
                .Include("PlansVersion.Plan.PlanDetails")
                .Include("Items.Product.AttributeValues")
                .Include("Items.Product.AttributeValues.AttributeValueDetails")
                .Include("Items.Product.AttributeValues.Attribute")
                .Include("Items.Product.AttributeValues.Attribute.AttributeDetails")
                .AsSplitQuery();
        }

        public async Task<Cart> GetActiveCart(long customerId, long createdById, CartTypes CartType, long SiteId , long? cartId = null ,string ReferenceId = null)
        {
            long? activeCartId;

            if (cartId != null)
            {
                activeCartId = await _cartRepository.Query()
                    .Where(x => x.Id == cartId)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrWhiteSpace(ReferenceId))
            {
                var cart = await _cartRepository.Query()
                    .FirstOrDefaultAsync(x => x.ReferenceId != null && x.ReferenceId.Contains(ReferenceId));

                if (cart == null)
                {
                    return null;
                }

                if (!cart.IsActive)
                {
                    cart.IsActive = true;
                    _cartRepository.Update(cart);
                    await _cartRepository.SaveChangesAsync();
                }

                activeCartId = cart.Id;
            }
            else if (IsOracleProvider())
            {
                activeCartId = await GetOracleActiveCartId(customerId, createdById, CartType, SiteId);
            }
            else
            {
                activeCartId = await _cartRepository.Query()
                    .Where(x => x.CustomerId == customerId &&
                                x.CreatedById == createdById &&
                                x.IsActive &&
                                x.CartType == CartType &&
                                x.SiteId == SiteId)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync();
            }

            if (!activeCartId.HasValue || activeCartId.Value == 0)
            {
                return null;
            }

            return await GetActiveCartDetailsQuery()
                .FirstOrDefaultAsync(x => x.Id == activeCartId.Value);
        }

        private async Task<Cart> GetActiveCartForMutation(long customerId, long createdById, CartTypes cartType, long siteId)
        {
            long? activeCartId;

            if (IsOracleProvider())
            {
                activeCartId = await GetOracleActiveCartId(customerId, createdById, cartType, siteId);
            }
            else
            {
                activeCartId = await _cartRepository.Query()
                    .Where(x => x.CustomerId == customerId &&
                                x.CreatedById == createdById &&
                                x.IsActive &&
                                x.CartType == cartType &&
                                x.SiteId == siteId)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync();
            }

            if (!activeCartId.HasValue || activeCartId.Value == 0)
            {
                return null;
            }

            return await _cartRepository.Query()
                .Include(x => x.Items)
                .Include("Items.Product")
                .FirstOrDefaultAsync(x => x.Id == activeCartId.Value);
        }

        public async Task<Cart> GetCartById(long CartId)
        {
            return await _cartRepository.Query()
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .ThenInclude(x => x.ProductDetails)

                .Where(x => x.Id == CartId).FirstOrDefaultAsync();
        }

        public async Task<Result> AddToCart(long customerId, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanVersionId = null, string ReferenceId = null)
        {
            return await AddToCart(customerId, customerId, productId, quantity, CartType, SiteId, additionalDetails, PlanVersionId, ReferenceId);
        }

        public async Task<Result> AddToCart(long customerId, long createdById, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanVersionId = null,  string ReferenceId = null)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var UserCountry = await userSiteService.GetUserCountry();

                var siteCountryId = await userSiteService.GetSiteCountry(SiteId);
                var WarehouseId = await workContext.GetCurrentWarehouseId();

                var cart = await GetActiveCartForMutation(customerId, createdById, CartType, SiteId);

                var products = new List<CheckProductQuantityVM>() { new CheckProductQuantityVM { ProductId = productId, Quantity = quantity } };

                var CanAdd = await _productService.CheckAllowedQuantity(products, customerId);


                if (!CanAdd.Item1)
                    return Result.Fail($"You Added More Than Allowed Quantity , The Ids are : {string.Join(", ", CanAdd.InValidProducts)}");

                if (cart == null)
                {
                    var IsCartPriceIncludeFees = await siteSettingRepository.Query()
                       .Where(x => x.Id == "Catalog.IsProductPriceIncludeTax" && x.SiteId == SiteId)
                       .Select(x => x.Value)
                       .FirstOrDefaultAsync();

       
                    cart = new Cart
                    {
                        CustomerId = customerId,
                        CreatedById = createdById,
                        IsProductPriceIncludeTax = IsCartPriceIncludeFees != null && Convert.ToBoolean(IsCartPriceIncludeFees),
                        CartType = CartType,
                        SiteId = SiteId,
                        PlansVersionId = (PlanVersionId == 0 || PlanVersionId == null) ? null : PlanVersionId.Value,
                        ReferenceId = ToJson(ReferenceId)
                    };

                    _cartRepository.Add(cart);
                }
                else
                {
                    if (cart.LockedOnCheckout)
                    {
                        return Result.Fail("Cart is being locked for checkout. Please complete the checkout first");
                    }

                }

                if (cart.CartType == CartTypes.Plan)
                {
                    await AddCartItemPlan(cart, additionalDetails);

                }
                else
                {
                    var product = await _productRepository.Query()
                        .AsNoTracking()
                                                .Where(x => x.Id == productId)
                                                .Include(x => x.ProductBranch)
                                                .Include(x => x.ProductPriceWarehouses)
                                                .Include(x => x.ProductAvaliablity)
                                                .FirstOrDefaultAsync(); ;

                    await IsAvaliableProduct(productId, UserCountry, siteCountryId);

                    var cartItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);
                    if (cartItem == null)
                    {
                        await CheckAvaliableStock(quantity, productId);
                        cartItem = new CartItem
                        {
                            Cart = cart,
                            ProductId = productId,
                            Quantity = quantity,
                            CreatedOn = DateTimeOffset.Now,
                            AdditionalDetails = additionalDetails,
                            Price = IsLocalCountry ? product.PriceType == EPriceType.ProductBase
                                                          ? product.Price.Value
                                                          : product.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

                                               : product.ForeignPrice.HasValue ? product.ForeignPrice.Value : 0,


                        };

                        cart.Items.Add(cartItem);
                    }
                    else
                    {
                        cartItem.Quantity = cartItem.Quantity + quantity;

                        await CheckAvaliableStock(cartItem.Quantity, productId);

                    }
                }
                await _cartRepository.SaveChangesAsync();

                return Result.Ok();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }




		public async Task<long> AddToCartSingleProduct(long customerId, long createdById, long productId, int quantity, CartTypes CartType, long SiteId, string additionalDetails = "", long? PlanVersionId = null)
		{
			try
			{
				var IsLocalCountry = await userSiteService.IsLocalCountry();

				var UserCountry = await userSiteService.GetUserCountry();

				var siteCountryId = await userSiteService.GetSiteCountry(SiteId);
				var WarehouseId = await workContext.GetCurrentWarehouseId();

				//var cart = await GetActiveCart(customerId, createdById, CartType, SiteId);

				var products = new List<CheckProductQuantityVM>() { new CheckProductQuantityVM { ProductId = productId, Quantity = quantity } };

	var IsCartPriceIncludeFees = await siteSettingRepository.Query()
					   .Where(x => x.Id == "Catalog.IsProductPriceIncludeTax" && x.SiteId == SiteId)
					   .Select(x => x.Value)
					   .FirstOrDefaultAsync();
                	var product = await _productRepository.Query()
						.AsNoTracking()
						.Where(x => x.Id == productId)
						.FirstOrDefaultAsync(); 
				
				

				var	cart = new Cart
					{
						CustomerId = customerId,
						CreatedById = createdById,
						IsProductPriceIncludeTax = IsCartPriceIncludeFees != null && Convert.ToBoolean(IsCartPriceIncludeFees),
						CartType = CartType,
						SiteId = SiteId,
						PlansVersionId = (PlanVersionId == 0 || PlanVersionId == null) ? null : PlanVersionId.Value,

					};

					_cartRepository.Add(cart);
               
              
				
					var cartItem = new CartItem
						{
							Cart = cart,
							ProductId = productId,
							Quantity = quantity,
							CreatedOn = DateTimeOffset.Now,
							AdditionalDetails = additionalDetails,
							Price = IsLocalCountry ? product.PriceType == EPriceType.ProductBase
														  ? product.Price.Value
														  : product.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

											   : product.ForeignPrice.HasValue ? product.ForeignPrice.Value : 0

						};

						cart.Items.Add(cartItem);
				await _cartItemRepository.SaveChangesAsync();
				await _cartRepository.SaveChangesAsync();
				return cart.Id;

			}
			catch (Exception ex)
			{

				throw ex;
			}
		}





		public async Task AddCartItemPlan(Cart cart, string additionalDetails)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();

                var UserCountry = await userSiteService.GetUserCountry();

                var siteCountryId = await userSiteService.GetSiteCountry(cart.SiteId);
                var WarehouseId = await workContext.GetCurrentWarehouseId();

                if (cart.PlansVersionId.HasValue)
                {
                    var LatestProductVersions = await planVersionsrepository.Query().Where(x => x.Id == cart.PlansVersionId).SelectMany(x => x.PlanProductVersions).ToListAsync();

                    foreach (var productversion in LatestProductVersions)
                    {
                        var product = await _productRepository.Query()
                      .AsNoTracking()
                                              .Where(x => x.Id == productversion.ProductId)
                                              .Include(x => x.ProductBranch)
                                              .Include(x => x.ProductPriceWarehouses)
                                              .Include(x => x.ProductAvaliablity)
                                              .FirstOrDefaultAsync(); ;

                        var cartItem = cart.Items.FirstOrDefault(x => x.ProductId == productversion.ProductId);
                        if (cartItem == null)
                        {
                            cartItem = new CartItem
                            {
                                Cart = cart,
                                ProductId = productversion.ProductId,
                                Quantity = productversion.Quantity,
                                CreatedOn = DateTimeOffset.Now,
                                AdditionalDetails = additionalDetails
                                ,
                                Price = IsLocalCountry ? product.PriceType == EPriceType.ProductBase
                                                          ? product.Price.Value
                                                          : product.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

                                               : product.ForeignPrice.HasValue ? product.ForeignPrice.Value : 0
                            };

                            cart.Items.Add(cartItem);
                            await CheckAvaliableStock(productversion.Quantity, productversion.ProductId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<DeliveryOptionEnum> CheckDeliveryAddress(string shippingData)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var DeliveryOption = new DeliveryOption();

                var ShippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(shippingData);

                var shippingAddress = await userAddressRepository.Query().Where(x => x.Id == ShippingData.ShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();

                if (shippingAddress != null)
                {

                    var CountryId = await userSiteService.GetSiteCountry(SiteId);
                    if (CountryId == shippingAddress.CountryId)
                    {

                        var DomesticValue = await siteSettingRepository.Query().AsNoTracking().Where(x => x.Id == "DomesticIntegration" && x.SiteId == SiteId).FirstOrDefaultAsync();

                        if (DomesticValue.Value == "TopDelivery")
                        {
                            return DeliveryOptionEnum.TopDelivery;

                        }
                        else
                        {

                            return DeliveryOptionEnum.Domestic;

                        }

                    }
                    else
                    {
                        var DeliveryIntegration = await siteSettingRepository.Query().AsNoTracking().Where(x => x.Id == "DeliveryOption" && x.SiteId == SiteId).FirstOrDefaultAsync();

                        return (DeliveryOptionEnum)Convert.ToInt32(DeliveryIntegration.Value);

                    }
                }
                else
                {
                    return DeliveryOptionEnum.None;


                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<Result> BulkAddToCart(long customerId, long createdById, List<AddToCartModel> model, long SiteId)
        {

            var products = model.Select(x => new CheckProductQuantityVM { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();

            var CanAdd = await _productService.CheckAllowedQuantity(products, customerId);
            if (!CanAdd.Item1)
                return Result.Fail($"You Added More Than Allowed Quantity , The Ids are : {string.Join(", ", CanAdd.InValidProducts)}");


            var cart = await GetActiveCart(customerId, createdById, model[0].CartType, SiteId);

            if (cart != null)
                await DeActiveCart(cart.Id);
 

            cart = new Cart
            {
                CustomerId = customerId,
                CreatedById = createdById,
                IsProductPriceIncludeTax = _isProductPriceIncludeTax,
                CartType = model[0].CartType, 
                SiteId = SiteId,
                PlansVersionId = model[0].PlanVersionId.HasValue ? model[0].PlanVersionId.Value : null,
                ReferenceId = ToJson( model[0].ReferenceId)
            };

            _cartRepository.Add(cart);
            if (cart.CartType == CartTypes.Plan)
            {
                await AddCartItemPlan(cart, model[0].AdditionalDetails);
            }
            else
            {
                foreach (var item in model)
                {

                    var cartItem = cart.Items.FirstOrDefault(x => x.ProductId == item.ProductId);
                    if (cartItem == null)
                    {
                        cartItem = new CartItem
                        {
                            Cart = cart,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            CreatedOn = DateTimeOffset.Now,
                            AdditionalDetails = item.AdditionalDetails,

                        };

                        cart.Items.Add(cartItem);
                    }
                    else
                    {
                        cartItem.Quantity = cartItem.Quantity + item.Quantity;
                    }
                }
            }
            await _cartRepository.ExecuteInTransactionAsync(async () =>
            {
                await _cartRepository.SaveChangesAsync();
            });

            return Result.Ok();
        }


        public async Task<CartVm> GetActiveCartDetails(long customerId, CartTypes CartType, long SiteId)
        {
            return await GetActiveCartDetails(customerId, customerId, CartType, SiteId);
        }


        // TODO separate getting product thumbnail, varation options from here
        public async Task<CartVm> GetActiveCartDetails(long customerId, long createdById, CartTypes CartType, long SiteId)
        {
            try
            {
                var IsLocalCountry = await userSiteService.IsLocalCountry();
                var WarehouseId = await workContext.GetCurrentWarehouseId();
                var userCountry = await userSiteService.GetUserCountry();
                var Currency = await _currencyService.GetUserCurrencyByCulture();

                var cart = await GetActiveCart(customerId, createdById, CartType, SiteId);

                var CultureId = workContext.GetCurrentCultureId();

                if (cart == null)
                {
                    return null;
                }

                await CountrySpecificItemRemoval(userCountry, cart, WarehouseId);
                await UpdateSoecficItemPrice(userCountry, cart, WarehouseId, IsLocalCountry);

                var cartVm = new CartVm(_currencyService)
                {
                    Id = cart.Id,
                    CouponCode = cart.CouponCode,
                    IsProductPriceIncludeTax = cart.IsProductPriceIncludeTax,
                    TaxAmount = cart.TaxAmount,
                    ShippingAmount = cart.ShippingAmount,
                    OrderNote = cart.OrderNote,
                    LockedOnCheckout = cart.LockedOnCheckout,
                    SiteId = cart.SiteId,
                    RedeemDiscount = cart.RedeemAmount,
                    ShippingData = cart.ShippingData,
                    ReferenceId = FromJson(cart.ReferenceId)
                };
                if (cartVm.ShippingAmount.GetValueOrDefault() > 0 && await _couponService.QualifiesForFreeShipping(cart))
                {
                   cartVm.ShippingAmount = 0;           
                }
                var cartItems = await _cartItemRepository
               .Query()
               .AsNoTracking()
               .Include(x => x.Product).ThenInclude(p => p.Stock)
               .Include(x => x.Product).ThenInclude(p => p.ThumbnailImage)
               .Include(x => x.Product).ThenInclude(p => p.ProductDetails)
               .Include(x => x.Product).ThenInclude(p => p.ProductPriceWarehouses)
               .Include("Product.OptionCombinations")
               .Include("Product.OptionCombinations.Option")
               .Include("Product.OptionCombinations.Value")
               .Include("Product.OptionCombinations.Value.productOptionValueDetails")
               .Include("Product.OptionCombinations.Option.optionDetails")
               .Include(x => x.Product)
               .ThenInclude(p => p.ProductAdditionalFees)
               .Include(x => x.Product)
               .ThenInclude(x => x.StockAmounts)
               .ThenInclude(x => x.WareHouse)
               .Include("Product.AttributeValues")
               .Include("Product.AttributeValues.AttributeValueDetails")
               .Include("Product.AttributeValues.Attribute")
               .Include("Product.AttributeValues.Attribute.AttributeDetails")
               .AsSplitQuery()
               .Where(x => x.CartId == cart.Id)
               .ToListAsync();

                cartVm.Items = cartItems
               .Select(x => new CartItemVm(_currencyService)
               {
                   Id = x.Id,
                   ProductId = x.ProductId,
                   ProductName = x.Product.ProductDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                   IsSerailProduct = x.Product.IsProductSerial,
                   ProductPrice = IsLocalCountry ? x.Product.PriceType == EPriceType.ProductBase
                                                          ? x.Product.Price.Value
                                                          : x.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault()

                                               : x.Product.ForeignPrice.HasValue ? x.Product.ForeignPrice.Value : 0,
                   AdditionalDetails = x.AdditionalDetails,
                   ProductStockQuantity = x.Product.StockAmounts != null && x.Product.StockAmounts.Count() > 0 ? x.Product.StockAmounts.Sum(x => x.Quantity) : 0,
                   ProductStockTrackingIsEnabled = x.Product.StockTrackingIsEnabled,
                   IsProductAvailabeToOrder = x.Product.IsAllowToOrder && x.Product.Status != EStatus.Deleted,
                   ProductImage = x.Product.ThumbnailImage?.PublicUrl ?? string.Empty,
                   Quantity = x.Quantity,
                   TaxClassId = x.Product.TaxClassId,
                   VariationOptions = CartItemVm.GetVariationOption(x.Product, CultureId),
                   ProductAdditionalFees = x.Product.ProductAdditionalFees.Where(x => x.Status != EStatus.Deleted).Select(t => new ProductFeesVM
                   {
                       Name = t.Name,
                       FeesValue = t.FeeValue,
                       FeesType = t.FeeType,
                       ProductId = t.ProductId,
                       Note = t.Note,
                       Refundable = t.Refundable
                   }).ToList(),
                   Attributes = x.Product.AttributeValues.Count() > 0 ? x.Product.AttributeValues
                                                                                 .Select(x => new ItemDetailAttribute
                                                                                 {
                                                                                     Name = x.Attribute.AttributeDetails.Where(x => CultureId == 0 || x.CultureId == CultureId)
                                                                                  .Select(c => c.Name).FirstOrDefault(),
                                                                                     Value = x.AttributeValueDetails.Where(x => CultureId == 0 || x.CultureId == CultureId)
                                                                                     .Select(c => c.Value).FirstOrDefault()
                                                                                 }).ToList() : new List<ItemDetailAttribute>(),
               }).ToList();



                cartVm.SubTotal = _currencyService.DcimalRound(cartVm.Items.Sum(x => x.Quantity * x.ProductPrice));

                cartVm.ProductFees = new List<ProductFeesVM>();

                int index = 0;

                if (CartTypes.Plan == CartType)
                {

                    await ProcessPlanCartItem(cartVm, cart);
                }
                else
                {

                    await ProcessCustomeCartItem(cartVm, cart, SiteId, IsLocalCountry, userCountry, Currency);

                }
                return cartVm;
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        private async Task UpdateSoecficItemPrice(UserCountryVM userCountry, Cart cart, long warehouseId, bool isLocalCountry)
        {
            try
            {
                if (cart != null)
                {
                    var hasChanges = false;
                    foreach (var item in cart.Items)
                    {
                        var currentPrice = isLocalCountry ? item.Product.PriceType == EPriceType.ProductBase
                                                          ? item.Product.Price.Value
                                                          : item.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == warehouseId).Select(x => x.Price).FirstOrDefault()

                                               : item.Product.ForeignPrice.HasValue ? item.Product.ForeignPrice.Value : 0;

                        if (item.Price != currentPrice)
                        {
                            item.Price = currentPrice;
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        await _cartItemRepository.SaveChangesAsync();
                        await _cartRepository.SaveChangesAsync();
                    }

                  
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CountrySpecificItemRemoval(UserCountryVM userCountry, Cart cart, long warehouseId)
        {
            try
            {
                var ItemIds = new List<long>();

                if (cart != null)
                {

                    var Items = cart.Items.Select(x => new
                    {
                        ItemId = x.Id,
                        ProductId = x.ProductId,
                        Countries = x.Product.ProductAvaliablity,

                        WarehouseIds=x.Product.ProductBranch.Any()? x.Product.ProductBranch.Select(x=>x.WarehouseId).ToList() :new List<long>()


                    }).ToList();

                    foreach (var item in Items)
                    {

                        if (item.Countries.Count() == 0 || item.Countries.Any(x => x.Permission == EProductCountryPermission.Allowed && x.CountryId == userCountry.Id) )
                        {

                            continue;

                        }
                        if (item.WarehouseIds.Count()==0 || item.WarehouseIds.Any(x => x==warehouseId))
                        {

                            continue;

                        }
                        ItemIds.Add(item.ItemId);
                    }

                    if (ItemIds.Count() > 0)
                    {
                        var RemoveItemsExcluded = cart.Items.Where(x => ItemIds.Any(i => i == x.Id)).ToList();
                        _cartItemRepository.RemoveRange(RemoveItemsExcluded);
                        await _cartItemRepository.SaveChangesAsync();
                        foreach (var item in RemoveItemsExcluded)
                        {
                            cart.Items.Remove(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task IsAvaliableProduct(long productId, UserCountryVM userCountry, string siteCountryId)
        {
            var WarehouseId = await workContext.GetCurrentWarehouseId();

            var product = await _productRepository.Query().AsNoTracking()
                                                  .Where(x => x.Id == productId)
                                                  .Include(x => x.ProductBranch)
                                                  .Include(x => x.ProductPriceWarehouses)
                                                  .Include(x => x.ProductAvaliablity)
                                                  .FirstOrDefaultAsync(); ;

            if (product != null)
            {
                var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

                if (!(AllowdProductBaseedOnPrice && (product.ProductAvaliablity.Count() == 0  || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed))))
                {

                    throw new Exception("Unavalible in your country");
                }
                if (WarehouseId != 0 && product.ProductBranch.Any()
                    && !product.ProductBranch.Any(x => x.WarehouseId == WarehouseId))
                {

                    throw new Exception("Unavalible in your Branch");

                }
            }
        }


        public async Task CheckAvaliableStock(int quantity, long productId)
        {
            try
            {


                var SerialsSettings = await _SiteSettings.GetSiteSettingsByModule("Serials", await workContext.GetCurrentSiteIdAsync());
                // var ReserveSerialsAfterPayment = SerialsSettings.Where(x => x.Id == "ReserveSerialsAfterPayment").FirstOrDefault();
                var GetSerialsFromOriginalProduct = SerialsSettings.Where(x => x.Id == "GetSerialsFromOriginalProduct").FirstOrDefault();
                long TargetProductId = productId;

                if (GetSerialsFromOriginalProduct != null &&
                   (bool.Parse(GetSerialsFromOriginalProduct.Value)))
                {

                    var OriginalProduct = await _productService.GetOriginalProduct(productId);
                    TargetProductId = OriginalProduct.Id;

                }

                var TargetProduct = await _productService.GetProductById(TargetProductId);

                var CultureId = workContext.GetCurrentCultureId();
                if (TargetProduct.IsProductSerial)
                {
                    var AvailableSerailsCount = _productserailRepository.Query()
                        .Include(x => x.Product)
                        .ThenInclude(x => x.ProductDetails)
                         .Where(x => x.ProductId == TargetProductId && x.Status == EProductSerialStatus.Available)
                         .Count();

                    if (quantity > AvailableSerailsCount)
                    {
                        throw new Exception("There is no enough serials for the selected product: " + TargetProductId.ToString());

                    }
                }
                else
                {

                    Product product = await _productRepository.Query()
                        .AsNoTracking()
                        .Include(x => x.ProductDetails)
                        .Include(x => x.Stock)
                        .Include(x => x.StockAmounts)
                        .ThenInclude(x => x.WareHouse)
                        .Where(p => p.Id == TargetProductId)
                        .FirstOrDefaultAsync();
                    if (product.StockTrackingIsEnabled)
                    {
                        if (product.StockAmounts != null && product.StockAmounts.Count() > 0)
                        {
                            if (!product.StockAmounts.Any(x => x.Quantity >= quantity))
                            {

                                throw new Exception("Product: " + product.ProductDetails == null ? "" : product.ProductDetails.Where(x => x.CultureId == CultureId).Select(c => c.Name).FirstOrDefault() + " Is Out Of Stock");

                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<CouponValidationResult> ApplyCoupon(long cartId, string couponCode)
        {
            var cart = await _cartRepository.Query().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == cartId);
            var cartVm = await GetActiveCartDetails(cart.CustomerId, cart.CartType, cart.SiteId);


            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = cart.Items.Select(x => new CartItemForCoupon { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            };
            if (!string.IsNullOrEmpty(couponCode))
            {
                couponCode = couponCode.Trim();
            }
            var couponValidationResult = await _couponService.Validate(cart.CustomerId, couponCode, cartInfoForCoupon);
            if (couponValidationResult.Succeeded)
            {
              
                cart.CouponCode = couponCode;
                cart.CouponRuleName = couponValidationResult.CouponRuleName;
                await _cartRepository.SaveChangesAsync();
            }

            return couponValidationResult;
        }
        public async Task<RedeemPointsValidationDto> ApplyLoyaltyPoints(CartTypes CartType)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();
                var customerId = await workContext.GetCurrentUserId();
                var cartDetails = await GetActiveCartDetails(customerId, CartType, siteId);

                var redeemPoints = await _loyaltyWalletService.RedeemPoints(customerId, siteId, null, cartDetails.Id, cartDetails.OrderTotal);

                if (redeemPoints.Succeeded)
                {
                    var cart = await _cartRepository.Query()
                                              .Where(x => x.Id == cartDetails.Id && x.IsActive)
                                              .FirstOrDefaultAsync();

                    cart.RedeemAmount = cartDetails.RedeemDiscount > 0 ? cart.RedeemAmount + redeemPoints.RedeemDiscount : redeemPoints.RedeemDiscount;
                    await _cartRepository.SaveChangesAsync();


                }

                return redeemPoints;
            }
            catch (Exception e)
            {

                throw e;
            }

        }
        public async Task<bool> RemovePoints(Cart cart, long userId, long siteId)
        {
            var result = await _loyaltyWalletService.RefundPoints(userId, cart.RedeemAmount, siteId, null, cart.Id);
            if (result.Succeeded)
            {
                cart.RedeemAmount = 0;
                await _cartRepository.SaveChangesAsync();
                return true;

            }
            return false;

        }
        public async Task RemoveCoupon(Cart cart)
        {
            cart.CouponCode = null;
            cart.CouponRuleName = null;
            await _cartRepository.SaveChangesAsync();

        }
        public async Task MigrateCart(long fromUserId, long toUserId, long SiteId)
        {

            try
            {
                foreach (var cartType in (CartTypes[])Enum.GetValues(typeof(CartTypes)))
                {

                    var cartFrom = await GetActiveCart(fromUserId, cartType, SiteId, null, null);

                    if (cartFrom != null && cartFrom.Items.Any() && cartType != CartTypes.Plan)
                    {
                        var cartTo = await GetActiveCart(toUserId, cartType, SiteId,null,null);
                        if (cartTo == null)
                        {
                            cartTo = new Cart
                            {
                                CustomerId = toUserId,
                                CreatedById = toUserId,
                                IsProductPriceIncludeTax = cartFrom.IsProductPriceIncludeTax,
                                IsActive = true,
                                CartType = cartType,
                                SiteId = SiteId,
                                ReferenceId=ToJson( cartFrom.ReferenceId)
                            };

                            _cartRepository.Add(cartTo);
                        }

                        foreach (var fromItem in cartFrom.Items)
                        {
                            var toItem = cartTo.Items.FirstOrDefault(x => x.ProductId == fromItem.ProductId);
                            if (toItem == null)
                            {
                                toItem = new CartItem
                                {
                                    Cart = cartTo,
                                    ProductId = fromItem.ProductId,
                                    Quantity = fromItem.Quantity,
                                    CreatedOn = DateTimeOffset.Now,
                                    AdditionalDetails = fromItem.AdditionalDetails ?? null

                                };
                                cartTo.Items.Add(toItem);
                            }
                            else
                            {
                                toItem.Quantity = toItem.Quantity + fromItem.Quantity;
                            }
                        }
                        await DeActiveCart(cartFrom.Id);
                        await _cartRepository.SaveChangesAsync();
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public async Task UnlockCart(Cart cart)
        {
            if (cart == null)
            {
                throw new ArgumentNullException(nameof(cart));
            }

            if (cart.LockedOnCheckout)
            {
                cart.LockedOnCheckout = false;
                await _cartRepository.SaveChangesAsync();
            }
        }
        public async Task DeActiveCart(long cartId)
        {
            try
            {
                var cart = await _cartRepository
             .Query()
             .Where(x => x.Id == cartId).FirstOrDefaultAsync();
                if (cart != null)
                {
                    cart.IsActive = false;
                    _cartRepository.Update(cart);
                    await _cartRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task DeActiveAllCarts(long UserId, long CreatedBy)
        {

            var cart = await _cartRepository
              .Query()
              .Where(x => x.CustomerId == UserId && x.CreatedById == CreatedBy && x.IsActive).ToListAsync();
            foreach (var item in cart)
            {
                item.IsActive = false;
            }
            // Persist all deactivations in one round-trip instead of one SaveChanges per cart.
            // Entities are all tracked on the same context; net result is identical.
            await _cartRepository.SaveChangesAsync();

        }
        public async Task ReActiveCart(long cartId)
        {
            var cart = await _cartRepository
              .Query()
              .Where(x => x.Id == cartId).FirstOrDefaultAsync();
            cart.IsActive = true;
            await _cartRepository.SaveChangesAsync();
        }

        public async Task updateAdditionalInfo(
       UpdateCartInfoVM cartInfoVM,
       long customerId,
       long siteId,
            long? cartId=null)
        {
            var cart = await GetActiveCart(customerId, cartInfoVM.CartType, siteId,cartId)
                ?? throw new InvalidOperationException("No active cart found");
            cart.ReferenceId = ToJson(cartInfoVM.ReferenceId);


            CartItem cartItem;

            if (cartInfoVM.ProductId.HasValue && cartInfoVM.ProductId > 0)
            {
                cartItem = cart.Items
                    .FirstOrDefault(x => x.ProductId == cartInfoVM.ProductId.Value)
                    ?? throw new InvalidOperationException("Product not found in cart");

                cartItem.Quantity = cartInfoVM.Quantity;
            }
            else
            {
                cartItem = cart.Items.FirstOrDefault()
                    ?? throw new InvalidOperationException("No items found in the cart");
            }

            cartItem.AdditionalDetails = cartInfoVM.AdditionalInfo;

  //          cart.Items = [cartItem];
  //          _cartRepository.Update(cart);
          

		////	_cartItemRepository.Update(cartItem);

		//	await _cartRepository.SaveChangesAsync();



			cartItem.AdditionalDetails = cartInfoVM.AdditionalInfo;
			_cartRepository.Update(cart);
			_cartItemRepository.Update(cartItem);
			await _cartRepository.SaveChangesAsync();
			await _cartItemRepository.SaveChangesAsync();
		}


        public async Task CalculateShippingRate(Cart cart)
        {
            try
            {
                var ShippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);

                var shippingAddress = await userAddressRepository
                                                              .Query()
                                                              .Include(x => x.Address)
                                                              .ThenInclude(x => x.StateOrProvince)
                                                              .Where(x => x.Id == ShippingData.ShippingAddressId)
                                                              .Select(x => x.Address).FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(shippingAddress.Latitude) && string.IsNullOrEmpty(shippingAddress.Longitude))
                {
                    await CheckForRelatedWareHouse(cart, shippingAddress);
                }
                else
                {
                    var Result = await topDeliveryServices.EstimateDeliveryPriceAsync(cart, shippingAddress);
                    cart.ShippingAmount =Convert.ToDecimal( Result.deliveryPrice);
                //    await ApplyFreeShippingPromotion(cart);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        public async Task<bool> CalculateRateAramix(long id)
        {
            try
            {
                var calculateRate = await aramexShippingService.CalculateRate(id);
                var cart = await _cartRepository
                    .Query()
                    .Where(x => x.Id == id)
                    .FirstOrDefaultAsync();

                if (calculateRate.HasErrors)
                {
                    throw new Exception(string.Join(", ", calculateRate.Notifications));
                }

                if (cart != null)
                {
                    cart.ShippingAmount = Convert.ToDecimal(calculateRate.TotalAmount.Value);
                 //   await ApplyFreeShippingPromotion(cart);
                }

                await _cartRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

public async Task<bool> RemoveShippingAddress(long cartId)
        {
            try
            {


                var cart = await _cartRepository.Query().Where(x => x.Id == cartId).FirstOrDefaultAsync();
                if (cart == null)
                {
                    throw new Exception("Cart Not Found");

                }

                cart.ShippingData = null;
                cart.ShippingAmount = 0;

                await _cartRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task ProcessCustomeCartItem(CartVm cartVm, Cart cart, long SiteId, bool IsLocalCountry, UserCountryVM userCountry, CurrencyByCultureVM Currency)
        {
            try
            {
                Address shippingAddress = null;
                if (cart.ShippingData != null)
                {
                    var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);
                    shippingAddress = await userAddressRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.Id == shippingData.ShippingAddressId)
                        .Select(x => x.Address)
                        .FirstOrDefaultAsync();
                }

                foreach (CartItemVm item in cartVm.Items)
                {
                    var CalculatedItemPrice = productPricingService.CalculatedProductPrice(cart.Items.Where(x => x.ProductId == item.ProductId).Select(x => x.Product).FirstOrDefault(), userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry);

                    item.ProductPriceWithDiscount = CalculatedItemPrice.Price;
                    var PromoDiscount = _currencyService.DcimalRound(await _IDiscountService.CalculateDiscount(SiteId, (item.ProductPrice * item.Quantity), item.ProductId, cart.CreatedById));
                    var PromoPerProductDiscount = _currencyService.DcimalRound(await _IDiscountService.CalculateDiscount(SiteId, (item.ProductPrice), item.ProductId, cart.CreatedById));

                    cartVm.Discount += (PromoDiscount);


                    if (item.TaxClassId.HasValue)
                    {
                        if (shippingAddress != null)
                        {
                            var taxPercent = await _taxService.GetTaxPercent(item.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId);

                            item.TaxAmount = (item.Quantity * (item.ProductPrice - PromoPerProductDiscount)) * (taxPercent / 100);

                            item.TaxPercentage = ((taxPercent) / 100).ToString("0.##%");

                        }

                    }

                    foreach (ProductFeesVM fee in item.ProductAdditionalFees)
                    {
                        cartVm.ProductFees.Add(fee);

                        if (fee.FeesType == DiscountTypes.Percentage)
                        {

                            cartVm.TotalFees += (fee.FeesValue / 100) * item.Total;
                            fee.FeesValue = (fee.FeesValue / 100) * item.Total;
                            fee.FeesValueString = _currencyService.FormatByUserCurrency(fee.FeesValue);
                        }
                        else if (fee.FeesType == DiscountTypes.FixedAmount)
                        {
                            cartVm.TotalFees += fee.FeesValue * item.Quantity;
                            fee.FeesValue = fee.FeesValue * item.Quantity;
                            fee.FeesValueString = _currencyService.FormatByUserCurrency(fee.FeesValue);
                        }
                    }

                }


                var GlobalDiscount = _currencyService.DcimalRound(await _IDiscountService.CalculateGlobalDiscount(SiteId, cartVm.SubTotal));

                cartVm.Discount += GlobalDiscount;

                cartVm.TaxAmount = _currencyService.DcimalRound(cartVm.Items.Sum(x => x.TaxAmount));


                var cartInfoForCoupon = new CartInfoForCoupon
                {
                    Items = cartVm.Items.Select(x => new CartItemForCoupon { ProductId = x.ProductId, ProductName = x.ProductName, Quantity = x.Quantity, Price = x.ProductPrice }).ToList()
                };
                var couponValidationResult = await _couponService.Validate(cart.CreatedById, cartVm.CouponCode, cartInfoForCoupon);


                if (!string.IsNullOrWhiteSpace(cartVm.CouponCode) || couponValidationResult.IsAutoCoupon)
                {
                    if (couponValidationResult.Succeeded)
                    {
                        cartVm.CouponDiscount = couponValidationResult.DiscountAmount;
                    }
                    else
                    {

                        cartVm.CouponValidationErrorMessage = couponValidationResult.ErrorMessage;
                    }
                }
            }


            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task ProcessPlanCartItem(CartVm cartVm, Cart cart)
        {
            try
            {
                var cultureId = workContext.GetCurrentCultureId();
                var WarehouseId = await workContext.GetCurrentWarehouseId();
                if (cart.PlansVersionId.HasValue)
                {
                    if (cart.PlansVersion.DiscountId.HasValue)
                    {
                        var Discount = _currencyService.DcimalRound(await _IDiscountService.CalculatePlanDiscount(cart.PlansVersion.DiscountId.Value, cartVm.SubTotal));

                        cartVm.Discount += Discount;

                    }
                    if (cart.PlansVersion.TaxClassId.HasValue)
                    {
                        if (cart.ShippingData != null)
                        {
                            var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);

                            var shippingAddress = await userAddressRepository.Query()
                                .AsNoTracking()
                                .Where(x => x.Id == shippingData.ShippingAddressId)
                                .Select(x => x.Address)
                                .FirstOrDefaultAsync();

                            if (shippingAddress != null)
                            {
                                var taxPercent = await _taxService.GetTaxPercent(cart.PlansVersion.TaxClassId.Value, shippingAddress.CountryId, shippingAddress.StateOrProvinceId);

                                cartVm.TaxAmount = (cartVm.SubTotal - cartVm.Discount) * (taxPercent / 100);

                                cartVm.TaxPercentage = ((taxPercent) / 100).ToString("0.##%");

                            }

                        }

                    }

                    cartVm.Plan = cart.PlansVersion == null ? new PlansVersionOutput()
                                                            : new PlansVersionOutput
                                                            {
                                                                DiscountId = cart.PlansVersion.DiscountId.HasValue ? cart.PlansVersion.DiscountId.Value : null,
                                                                DiscountValue = cart.PlansVersion.DiscountId.HasValue ? cart.PlansVersion.Discount.DiscountValue : 0,
                                                                RefundPeriod = cart.PlansVersion.RefundPeriod,
                                                                CartRuleId = cart.PlansVersion.CartRuleId,
                                                                PlanName = cart.PlansVersion.Plan.PlanDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                                                RepeatAmount = cart.PlansVersion.RepeatAmount,
                                                                RepeatPeriod = cart.PlansVersion.RepeatPeriod,
                                                                TaxClassId = cart.PlansVersion.TaxClassId.HasValue ? cart.PlansVersion.TaxClassId.Value : null,
                                                                PlanProducts = cart.PlansVersion.PlanProductVersions.Select(x => new PlanProductVersionOutputs
                                                                {

                                                                    Status = x.Status,
                                                                    Price = x.Product.PriceType == EPriceType.ProductBase
                                                                    ? x.Product.Price.Value
                                                          : x.Product.ProductPriceWarehouses.Where(x => x.WarehouseId == WarehouseId).Select(x => x.Price).FirstOrDefault(),
                                                                    ProductId = x.ProductId,
                                                                    ProductName = x.Product
                                                               .ProductDetails
                                                                .Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                                                    Quantity = x.Quantity,


                                                                }).ToList()

                                                            };
                    if (!string.IsNullOrWhiteSpace(cartVm.CouponCode))
                    {
                        var couponValidationResult = await _couponService.ValidatePlan(cart.CreatedById, cartVm.CouponCode, cartVm.SubTotal, cartVm.Plan.CartRuleId.HasValue ? null : cartVm.Plan.CartRuleId.Value);

                        if (couponValidationResult.Succeeded)
                        {
                            cartVm.CouponDiscount = couponValidationResult.DiscountAmount;
                        }
                        else
                        {
                            cartVm.CouponValidationErrorMessage = couponValidationResult.ErrorMessage;
                        }
                    }


                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task CheckForRelatedWareHouse(Cart cart, Address shippingAddress)
        {
            try
            {

                var shippingRate = await GetShippingRateForAddress(shippingAddress);
                if (shippingRate == null)
                    throw new Exception("This area does not have any Stock");

                cart.ShippingAmount = shippingRate.ShippingPrice;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<CartVm> CheckInventoryItems(CartTypes cartTypes)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();
                var userId = await workContext.GetCurrentUserId();
                var CultureId = workContext.GetCurrentCultureId();
                var WarehouseId = await workContext.GetCurrentWarehouseId();

                var cart = await GetActiveCartDetails(userId, cartTypes, siteId);

                var ResultShippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);

                var shippingAddress = await userAddressRepository
                                                          .Query()
                                                          .Include(x => x.Address)
                                                          .ThenInclude(x => x.StateOrProvince)
                                                          .Where(x => x.Id == ResultShippingData.ShippingAddressId)
                                                          .Select(x => x.Address).FirstOrDefaultAsync();
                foreach (var item in cart.Items)
                {
                    try
                    {
                        var cartItem = await _cartItemRepository.Query().Include(x => x.Product).Where(x => x.Id == item.Id).FirstOrDefaultAsync();
                        await WarehouseInventoryStatus(cartItem, shippingAddress, siteId, cart.ShippingData, item.ProductName, WarehouseId);
                        item.InStock = true;
                    }
                    catch
                    {
                        item.InStock = false;
                    }
                }

                return cart;
            }
            catch
            {

                throw;
            }
        }
        public async Task NotifyUserAfterPeriod(string BGTemplate, int cartNotificationLeadTimeInMinutes)
        {
            try
            {

                var carts = await _cartRepository.Query()
                                               .Where(x => x.IsActive)
                                               .Include(x => x.Customer)
                                               .ThenInclude(x => x.UserSites)
                                               .ThenInclude(x => x.Culture)
                                               .Where(x => x.CreatedOn.AddMinutes(cartNotificationLeadTimeInMinutes) < DateTimeOffset.Now.ToUniversalTime())
                                               .ToListAsync();

                var siteId = carts.Select(x => x.SiteId).FirstOrDefault();

                var settings = await _SiteSettings.GetSiteSettingsByModule(BGTemplate, siteId);

                var NotificationType = EskaCommerce.Notifications.Areas.Notifications.ViewModels.NotificationType.PushNotification;

                if (carts == null || carts.Count() == 0)
                {
                    return;
                }

                foreach (var cart in carts)
                {
                    var Culture = cart.CreatedBy.UserSites
                                                 .Where(x => x.SiteId == siteId)
                                                 .Where(x => x.Culture != null).Select(x => x.Culture).FirstOrDefault();
                    var (pushTemplateId, smsTemplateId, notificationType) = GetSettingsValues(settings, Culture != null ? Culture.CultureId : "en");

                    NotificationObject notificationObject = new NotificationObject
                    {
                        MessageBody = string.Empty,
                        MessageTitle = "Cart Notification",
                        SendVia = SendBy.MobileNumber,
                        SendValue = new List<string> { cart.CreatedBy.PhoneNumber },
                        NotificationType = NotificationType.SMS,

                        EmailOptions = new EmailSenderVM
                        {
                            Emails = new List<string>(),
                            TemplateId = smsTemplateId,
                        },
                        EmailTemplateId = smsTemplateId,
                    };
                    try
                    {

                        if (!string.IsNullOrEmpty(cart.CreatedBy.PhoneNumber))
                        {
                            if (notificationType == NotificationType.SMS || notificationType == NotificationType.BothSMSEmail)
                            {
                                await testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId);
                            }

                        }
                        if (!string.IsNullOrEmpty(cart.CreatedBy.DeviceToken))
                        {
                            if (notificationType == NotificationType.PushNotification)
                            {
                                var notification = await PushNotificationHelper(new List<User> { cart.CreatedBy }, pushTemplateId);
                                await testNotifier.SendMessageAsync(NotificationType.PushNotification, null, notificationObject, siteId);
                            }
                        }

                        cart.IsNotified = true;
                    }
                    catch (Exception ex)
                    {

                    }
                }

                await _cartRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private (string, string, NotificationType) GetSettingsValues(List<SiteSettingsVM> settings, string Culture)
        {
            if (settings.Count > 0)
            {
                string PushNotificationTemplateId = Culture.Contains("en") ? GetStringSettingValue(settings, "PushNotificationCartNotify") : GetStringSettingValue(settings, "PushNotificationCartNotifyArabic");
                string smsTemplateId = Culture.Contains("en") ? GetStringSettingValue(settings, "SMSCartNotify") : GetStringSettingValue(settings, "SMSCartNotifyArabic");


                NotificationType notificationType = settings?.FirstOrDefault(x => x.Id == "NotificationTypeCart")?.Value != null ?
                        (NotificationType)Enum.Parse(typeof(NotificationType), settings.First(x => x.Id == "NotificationTypeCart").Value, true) :
                        NotificationType.PushNotification;

                return (PushNotificationTemplateId, smsTemplateId, notificationType);
            }
            else
            {
                return (string.Empty, string.Empty, NotificationType.PushNotification);


            }
        }


        private string GetStringSettingValue(List<SiteSettingsVM> settings, string settingId)
        {
            var setting = settings.FirstOrDefault(x => x.Id == settingId);
            return setting != null ? setting.Value : "";
        }
        private async Task<NotificationObject> PushNotificationHelper(List<User> users, string pushTemplateId)
        {
            var emailTemplate = await emailTemplateService.GetEmailTemplateById(pushTemplateId);


            NotificationObject notificationObjPush = new NotificationObject
            {
                SendVia = SendBy.Username,
                MessageBody = $"{emailTemplate.EmailBody}",
                MessageTitle = $"{emailTemplate.Subject}",
                SendValue = users.DistinctBy(x => x.DeviceToken).Select(x => x.DeviceToken).ToList(),
                NotificationType = NotificationType.PushNotification,
                UserId = users.Select(x => x.Id).ToArray(),
                PushNotificationOptions = new PushNotification
                {
                    Topics = new List<string>(),
                    DesitinationType = DesitinationType.Personal
                }
            };

            return notificationObjPush;
        }

        #region Helpers Stock And Warehouse
        public async Task<long> WarehouseInventoryStatus(CartItem cartItem, Address? shippingAddress, long siteId, string ShippingData, string productName, long SelectedwarehouseId)
        {

            var availableStocks = await GetAvailableStocksForProduct(cartItem.ProductId);
            var WareHouse = await warehouseRepository.Query().AsNoTrackingWithIdentityResolution().ToListAsync();
            if (cartItem.Product.IsProductSerial)
            {
                var mainwarehouse = await warehouseRepository.Query().Where(x => x.IsMain && x.SiteId == siteId).FirstOrDefaultAsync();
                if (mainwarehouse == null) throw new Exception("Main Warehouse are not defiend");

                return mainwarehouse.Id;
            }
            if (SelectedwarehouseId == 0 && WareHouse.Count() > 1)
            {
                if (availableStocks.Count == 0)
                    throw new Exception($"No stock available for {productName}");

                var country = await userSiteService.GetSiteCountry(siteId);

                if (IsSameCountryAsShippingAddress(country, shippingAddress))
                {

                    return await CheckForRelatedWareHouse(siteId, cartItem, availableStocks, shippingAddress, cartItem.ProductId, ShippingData);
                }
                else
                {
                    return await CheckMainWareHouse(GetMainStock(availableStocks), cartItem, cartItem.ProductId);
                }
            }
            else if (SelectedwarehouseId != 0)
            {

                return await CheckSelectedWarehouse(cartItem.Quantity, cartItem.ProductId, SelectedwarehouseId);
            }
            else
            {
                return await CheckMainWareHouse(GetMainStock(availableStocks), cartItem, cartItem.ProductId);

            }
        }
        private async Task<long> CheckForRelatedWareHouse(long SiteId, CartItem cartItem, List<Stock> availableStocks, Address shippingAddress, long productId, string ShippingData)
        {
            if (ShippingData == null)
            {
                throw new Exception("ShippingData Is Empty");
            }

            var ResultShippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(ShippingData);

            if (await IsEnableShipment(SiteId))
            {

                if (ResultShippingData != null && ResultShippingData.IsPickuped.HasValue && ResultShippingData.IsPickuped.Value)
                {
                    if (cartItem.Product.StockTrackingIsEnabled)
                    {
                        var availableQuantityAtWarehouse = await GetAvailableQuantityAtWarehouse(productId, ResultShippingData.WarehouseId.Value);

                        if (availableQuantityAtWarehouse < cartItem.Quantity)
                        {
                            throw new Exception($"There is no stock available for the product");
                        }
                    }

                    return ResultShippingData.WarehouseId.Value;
                }
                else
                {

                    if (shippingAddress == null)
                    {
                        shippingAddress = await userAddressRepository
                                                                 .Query()
                                                                 .Include(x => x.Address)
                                                                 .ThenInclude(x => x.StateOrProvince)
                                                                 .Where(x => x.Id == ResultShippingData.ShippingAddressId)
                                                                 .Select(x => x.Address).FirstOrDefaultAsync();
                    }

                    var shippingRate = await GetShippingRateForAddress(shippingAddress);
                    if (shippingRate == null)
                        throw new Exception("This area for Rate does not have any Stock");
                    if (cartItem.Product.StockTrackingIsEnabled)
                    {
                        var availableQuantityAtWarehouse = await GetAvailableQuantityAtWarehouse(productId, shippingRate.WarehouseId);

                        if (availableQuantityAtWarehouse < cartItem.Quantity)
                            throw new Exception("Not enough stock");


                    }
                    return shippingRate.WarehouseId;

                }
            }
            else
            {
                if (shippingAddress == null)
                {
                    shippingAddress = await userAddressRepository.Query().Where(x => x.Id == ResultShippingData.ShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();

                }
                var warehouseForShipping = MatchingShippingAddress(availableStocks, shippingAddress);

                if (warehouseForShipping == null)
                {
                    throw new Exception("This area does not have any Stock");
                }
                if (cartItem.Product.StockTrackingIsEnabled)
                {
                    var availableQuantityAtWarehouse = await GetAvailableQuantityAtWarehouse(productId, warehouseForShipping.WarehouseId);

                    if (availableQuantityAtWarehouse < cartItem.Quantity)
                        throw new Exception("Not enough stock");
                }
                return warehouseForShipping.WarehouseId;
            }
        }
        private async Task<bool> IsEnableShipment(long siteId)
        {
            var enableShipment = await siteSettingRepository.Query()
                .Where(x => x.Id == "EnableShippment" && x.SiteId == siteId)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();
            return enableShipment != null && Convert.ToBoolean(enableShipment);
        }
        private async Task<long> CheckMainWareHouse(Stock? mainStock, CartItem cartItem, long productId)
        {
            if (mainStock != null)
            {
                var availableQuantityAtWarehouse = await GetAvailableQuantityAtWarehouse(productId, mainStock.WarehouseId);

                if (availableQuantityAtWarehouse > cartItem.Quantity)
                    return mainStock.WarehouseId;
                else
                    throw new Exception("There is no stock available for the product");
            }
            else
            {
                throw new Exception("Main Warehouse and Stocke are not defiend.");
            }
        }
        private async Task<long> CheckSelectedWarehouse(int Quantity, long productId, long warehouseId)
        {

            var availableQuantityAtWarehouse = await GetAvailableQuantityAtWarehouse(productId, warehouseId);

            if (availableQuantityAtWarehouse > Quantity)
                return warehouseId;
            else
                throw new Exception("There is no stock available for the product");

        }

        // Helper Functions

        private async Task<List<Stock>> GetAvailableStocksForProduct(long productId)
        {
            try
            {

                return await stockRepository.Query()
                                              .Where(stock => stock.ProductId == productId)
                                              .Include(x => x.Warehouse)
                                              .ThenInclude(x => x.Address)
                                              .AsNoTracking()
                                              .ToListAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private bool IsSameCountryAsShippingAddress(string countryId, Address shippingAddress)
        {
            try
            {
                if (shippingAddress != null)
                {
                    return countryId == shippingAddress.CountryId;
                }
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private Stock GetMainStock(List<Stock> stocks)
        {
            try
            {

                return stocks.FirstOrDefault(stock => stock.Warehouse.IsMain);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private Stock MatchingShippingAddress(List<Stock> StockwarehouseAddress, Address shippingAddress)
        {
            try
            {
                var results = StockwarehouseAddress.Where(warehouseAddress =>
                               warehouseAddress.Warehouse.Address.CountryId == shippingAddress.CountryId).ToList();

                if (results.Count == 0)
                {
                    throw new Exception("This country does have any stock");
                }

                var state = StockwarehouseAddress.Where(warehouseAddress => warehouseAddress.Warehouse.Address.StateOrProvinceId == shippingAddress.StateOrProvinceId).ToList();

                if (state != null && state.Count() > 0)
                {
                    var district = StockwarehouseAddress.Where(warehouseAddress =>

                       warehouseAddress.Warehouse.Address.DistrictId == shippingAddress.DistrictId).FirstOrDefault();
                    if (district == null)
                    {
                        var stateResult = state.Where(x => !x.Warehouse.Address.DistrictId.HasValue).FirstOrDefault();
                        if (stateResult == null)
                        {
                            return state.FirstOrDefault();
                        }
                        return stateResult;
                    }
                }

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<PriceAndDestination> GetShippingRateForAddress(Address shippingAddress)
        {
            try
            {
                var results = await priceAndDestinationRepository.Query().AsNoTracking()
                    .Where(rate => rate.CountryId == shippingAddress.CountryId && rate.Status != EStatus.Deleted)
                    .ToListAsync();

                if (results.Count == 0)
                {
                    throw new Exception("This country does have any Rate");
                }

                var state = results.Where(rate => rate.StateOrProvinceId == shippingAddress.StateOrProvinceId).ToList();
                if (state != null && state.Count() > 0)
                {
                    var district = results.FirstOrDefault(rate => rate.DistrictId == shippingAddress.DistrictId);
                    return district ?? throw new Exception($"Area not This Area not Have any Related Branch for this District ");
                }
                else
                {
                    throw new Exception($"This Area not Have any Related Branch for This StateOrProvince");

                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while getting shipping rate.", ex);
            }
        }
        private async Task<int> GetAvailableQuantityAtWarehouse(long productId, long warehouseId)
        {
            try
            {
                var availableProductQuantity = await stockproductRepository.Query()
                                                                          .AsNoTracking()
                                                                          .Where(x => x.ProductId == productId && x.WareHouseId == warehouseId)
                                                                          .FirstOrDefaultAsync();
                if (availableProductQuantity == null)
                {

                    return 0;
                }
                return availableProductQuantity.Quantity;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


      

            public static string ToJson(string referenceId)
            {
                const string ClaimPrefix = "JO";

                if (referenceId == null) return null;

                bool isClaimId = referenceId.StartsWith(ClaimPrefix, StringComparison.OrdinalIgnoreCase);
            var obj = new ReferencedModel();

			try {
				 obj = JsonConvert.DeserializeObject<ReferencedModel>(referenceId);
			}
			catch {

                obj = null;
            
            }

                var model = new ReferencedModel
                {
                    RetriveQuotationId = obj!=null ? obj.RetriveQuotationId:" ",
                    JOClaimId = obj != null ?  obj.JOClaimId : " ",
                    JoClaimStatus= obj != null ?  obj.JoClaimStatus : " "
                };

                return System.Text.Json.JsonSerializer.Serialize(model);
            }

            public static string FromJson(string referenceIdJson)
            {
                if (referenceIdJson == null) return null;

                var model = System.Text.Json.JsonSerializer.Deserialize<ReferencedModel>(referenceIdJson);
                return !string.IsNullOrEmpty(model.JOClaimId) ? model.JOClaimId : model.RetriveQuotationId;
            }
        }

        #endregion

    }



