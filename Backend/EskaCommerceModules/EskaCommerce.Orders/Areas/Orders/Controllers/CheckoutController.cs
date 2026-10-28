using EskaCMS.Addresses.Models;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Route("api/checkout")]
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IRepository<WarehouseWorkingHours> warehouseworkinghoursrepository;
        private readonly IRepository<Warehouse> warehouserepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        // private readonly IShippingPriceService _shippingPriceService;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Cart> _cartRepository;
        // private readonly UserManager<User> _userManager;

        public CheckoutController(
            IRepository<StateOrProvince> stateOrProvinceRepository,
            IRepositoryWithTypedId<Country, string> countryRepository,
            IRepository<UserAddress> userAddressRepository,
            /* IShippingPriceService shippingPriceService, */
            IOrderService orderService,
            IRepository<WarehouseWorkingHours> warehouseworkinghoursrepository,
            IRepository<Warehouse> warehouserepository,
            IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,

        ICartService cartService,
            IWorkContext workContext,
            IRepository<Order> orderRepository,
            IRepository<Cart> cartRepository/* ,
            UserManager<User> userManager */)
        {
            _stateOrProvinceRepository = stateOrProvinceRepository;
            _countryRepository = countryRepository;
            _userAddressRepository = userAddressRepository;
            // _shippingPriceService = shippingPriceService;
            _orderService = orderService;
            this.warehouseworkinghoursrepository=warehouseworkinghoursrepository;
            this.warehouserepository = warehouserepository;
            this.siteSettingsRepository = siteSettingsRepository;
            _cartService = cartService;
            _workContext = workContext;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            // _userManager = userManager;
        }

        [HttpGet("shipping-details")]
        public async Task<IActionResult> ShippingDetails()  
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null || currentUser.Id <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                //var cart = await _cartService.GetActiveCartDetails(currentUser.Id);
                //if (cart == null || !cart.Items.Any())
                //{/
                //    return Ok();
                //}

                var model = new DeliveryInformationVm();

                PopulateShippingForm(model, currentUser);

                return Ok(model);
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }

        }



        [HttpPost("shipping-order")]
        public async Task<IActionResult> Shipping([FromBody] DeliveryInformationVm model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Address data invalid");
                }

                long SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null || currentUser.Id <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                if (!(model.IsPickuped.HasValue && model.IsPickuped.Value && model.WarehouseId.HasValue))
                {
                    if (model.ShippingAddressId != 0 ||
                    model.BillingAddressId != 0)
                    {
                        if (model.UseShippingAddressAsBillingAddress)
                        {
                            model.BillingAddressId = model.ShippingAddressId;
                        }
                        // return Ok(model);
                    }
                    else
                    {
                        await fillAddress(model, SiteId, currentUser.Id);
                    }
                }
                var cart = await _cartService.GetActiveCart(currentUser.Id, model.CartTypes, SiteId,model.CartId);
                if (cart == null)
                    throw new ApplicationException($"Cart of user {currentUser.Id} cannot be found");


                await ValidateWorkingHouseWarehouse();

                cart.ShippingData = JsonConvert.SerializeObject(model);

                await _cartRepository.SaveChangesAsync();

                //}
                await CalculateShippment(cart, model);


                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task ValidateWorkingHouseWarehouse()
        {
            try
            {
                var warehouseId = await _workContext.GetCurrentWarehouseId(); // ✅ لازم await

                if (warehouseId == 0)
                    return;

                var today = (WarehouseWorkingDay)(((int)DateTime.Now.DayOfWeek + 1));

                var schedule = await warehouseworkinghoursrepository.Query().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && (int)x.DayOfWeek == (int)today);

                if (schedule == null)
                    return;
                ValidateWorkingJourney(schedule.DayOfWeek, schedule.OpenTime, schedule.CloseTime, schedule.IsClosed);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void ValidateWorkingJourney(WarehouseWorkingDay dayOfWeek, TimeSpan? openTime, TimeSpan? closeTime, bool isClosed)
        {
            var today = (WarehouseWorkingDay)(((int)DateTime.Now.DayOfWeek + 1));

            if (dayOfWeek != today)
                throw new Exception($"Warehouse is not available today ({today}).");

            if (isClosed)
                throw new Exception($"Warehouse is closed today ({today}).");

            if (!openTime.HasValue || !closeTime.HasValue)
                throw new Exception("Working hours are not defined for this day.");

            var now = DateTime.Now.TimeOfDay;

            if (now < openTime.Value || now > closeTime.Value)
                throw new Exception($"Operation not allowed now. Working hours today are from {openTime} to {closeTime}.");
        }

        private void PopulateShippingForm(DeliveryInformationVm model, User currentUser)
        {
            var cultureId = _workContext.GetCurrentCultureId();
            model.ExistingShippingAddresses = _userAddressRepository
                .Query()
                .Where(x => (x.AddressType == EskaCMS.Entities.Enum.AddressType.Shipping) && (x.UserId == currentUser.Id))
                .Select(x => new ShippingAddressVm
                {
                    UserAddressId = x.Id,
                    ContactName = x.Address.ContactName,
                    Phone = x.Address.Phone,
                    Email = x.Address.Email,
                    AddressLine1 = x.Address.AddressLine1,
                    CityName = x.Address.City,
                    ZipCode = x.Address.ZipCode,
                    DistrictName = x.Address.District.DistrictDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    StateOrProvinceName = x.Address.StateOrProvince.StateOrProvinceDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    StateOrProvinceId = x.Address.StateOrProvince.Id,
                    CountryName = x.Address.Country.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    CountryId = x.Address.Country.Id,
                    IsCityEnabled = x.Address.Country.IsCityEnabled,
                    IsZipCodeEnabled = x.Address.Country.IsZipCodeEnabled,
                    IsDistrictEnabled = x.Address.Country.IsDistrictEnabled


                }).ToList();

            model.ShippingAddressId = currentUser.DefaultShippingAddressId ?? 0;

            model.UseShippingAddressAsBillingAddress = true;

            model.NewAddressForm.ShipableContries = _countryRepository.Query().Include(x => x.CountryDetails)
                .Where(x => x.IsShippingEnabled)
                .Select(x => new SelectListItem
                {
                    Text = x.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    Value = x.Id.ToString()
                })
                .OrderBy(x => x.Text)
                .ToList();

            if (model.NewAddressForm.ShipableContries.Count == 1)
            {
                var onlyShipableCountryId = model.NewAddressForm.ShipableContries.First().Value;

                model.NewAddressForm.StateOrProvinces = _stateOrProvinceRepository
                .Query().Include(x => x.StateOrProvinceDetails)
                .Where(x => x.CountryId == onlyShipableCountryId)
                .Select(x => new SelectListItem
                {
                    Text = x.StateOrProvinceDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    Value = x.Id.ToString()
                })
                .OrderBy(x => x.Text)
                .ToList();
            }
        }
        private async Task fillAddress(DeliveryInformationVm model, long siteId, long UserId)
        {
            try
            {
                Address billingAddress = new Address();
                Address shippingAddress = new Address();
                long ShippingAddressId = 0;
                if (model.ShippingAddressId == 0)
                {

                    var address = new Address
                    {
                        AddressLine1 = model.NewAddressForm.AddressLine1,
                        AddressLine2 = model.NewAddressForm.AddressLine2,
                        ContactName = model.NewAddressForm.ContactName,
                        CountryId = model.NewAddressForm.CountryId,
                        StateOrProvinceId = model.NewAddressForm.StateOrProvinceId,
                        DistrictId = model.NewAddressForm.DistrictId,
                        City = model.NewAddressForm.City,
                        Longitude = model.NewAddressForm.Longitude,
                        Latitude = model.NewAddressForm.Latitude,
                        ZipCode = model.NewAddressForm.ZipCode,
                        Phone = model.NewAddressForm.Phone,
                        Email = model.NewAddressForm.Email,
                        SiteId = siteId

                    };
                    var userAddress = new UserAddress
                    {
                        Address = address,
                        AddressType = AddressType.Shipping,
                        UserId =UserId
                    };
                    _userAddressRepository.Add(userAddress);

                    await _userAddressRepository.SaveChangesAsync();
                    ShippingAddressId = userAddress.Id;
                    model.ShippingAddressId = userAddress.Id;

                    shippingAddress = address;
                }
                if (model.UseShippingAddressAsBillingAddress)
                {
                    billingAddress = shippingAddress;
                    model.BillingAddressId = ShippingAddressId;
                }
                else if (model.BillingAddressId == 0)
                {
                    var address = new Address
                    {
                        AddressLine1 = model.NewBillingAddressForm.AddressLine1,
                        AddressLine2 = model.NewBillingAddressForm.AddressLine2,
                        ContactName = model.NewBillingAddressForm.ContactName,
                        CountryId = model.NewBillingAddressForm.CountryId,
                        StateOrProvinceId = model.NewBillingAddressForm.StateOrProvinceId,
                        DistrictId = model.NewBillingAddressForm.DistrictId,
                        City = model.NewBillingAddressForm.City,
                        Longitude = model.NewBillingAddressForm.Longitude,
                        Latitude = model.NewBillingAddressForm.Latitude,
                        ZipCode = model.NewBillingAddressForm.ZipCode,
                        Phone = model.NewBillingAddressForm.Phone,
                        Email = model.NewBillingAddressForm.Email,


                    };

                    var userAddress = new UserAddress
                    {
                        Address = address,
                        AddressType = AddressType.Billing,
                        UserId =UserId
                    };

                    _userAddressRepository.Add(userAddress);

                    await _userAddressRepository.SaveChangesAsync();

                    model.BillingAddressId = userAddress.Id;

                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CalculateShippment(Cart cart, DeliveryInformationVm model)
        {
            var EnableSettings = await siteSettingsRepository.Query().Where(x => x.Id == "EnableShippment" && x.SiteId==cart.SiteId).Select(x => x.Value).FirstOrDefaultAsync();

            if (EnableSettings == null)
            {
                return;

            }

            var EnableShippment = Convert.ToBoolean(EnableSettings);

            if (EnableShippment)
            {

                if (!string.IsNullOrEmpty(cart.ShippingData))
                {
                    try
                    {
                        DeliveryOptionEnum deliveryOption = DeliveryOptionEnum.None;

                        if ((model.IsPickuped.HasValue && model.IsPickuped.Value && model.WarehouseId.HasValue))
                        {
                            deliveryOption = DeliveryOptionEnum.Pickup;
                        }
                        else
                        {   

                            deliveryOption = await _cartService.CheckDeliveryAddress(cart.ShippingData);
                        }

                        if (deliveryOption == DeliveryOptionEnum.Aramex)
                        {
                            cart.ShippingDelivery = DeliveryOptionEnum.Aramex;

                            await _cartRepository.SaveChangesAsync();

                            await _cartService.CalculateRateAramix(cart.Id);
                        }
                        else if (deliveryOption == DeliveryOptionEnum.Domestic|| deliveryOption == DeliveryOptionEnum.TopDelivery)
                        {
                            cart.ShippingDelivery = deliveryOption;

                            await _cartService.CalculateShippingRate(cart);

                            await _cartRepository.SaveChangesAsync();
                        }
                        else if (deliveryOption == DeliveryOptionEnum.Pickup)
                        {
                            cart.ShippingDelivery = DeliveryOptionEnum.Pickup;
                            await _cartRepository.SaveChangesAsync();
                        }

                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }
                }
                else
                {
                    cart.ShippingDelivery = DeliveryOptionEnum.None;
                }
                await _cartRepository.SaveChangesAsync();

            }

        }

        [HttpPost("update-tax-and-shipping-prices")]
        public async Task<IActionResult> UpdateTaxAndShippingPrices([FromBody] TaxAndShippingPriceRequestVm model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Tax and shipping request is required.");
                }

                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                var currentUserId = await _workContext.GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                var cart = await _cartService.GetActiveCart(currentUserId, model.CartType, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                var orderTaxAndShippingPrice = await _orderService.UpdateTaxAndShippingPrices(cart.Id, model, SiteId);

                return Ok(orderTaxAndShippingPrice);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }



        }


        [HttpGet("GetShipapleCountries")]
        public IActionResult GetShipapleCountries()
        {
            return Ok(_countryRepository.Query()
                .Include(x => x.CountryDetails)
               .Where(x => x.IsShippingEnabled)
               .Select(x => new SelectListItem
               {
                   Text = x.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                   Value = x.Id.ToString()
               })
               .OrderBy(x => x.Text)
               .ToList());
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel(CartTypes CartType)
        {
            var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
            var currentUserId = await _workContext.GetCurrentUserId();
            var cart = await _cartService.GetActiveCart(currentUserId, CartType, SiteId);
            if (cart != null && cart.LockedOnCheckout)
            {
                cart.LockedOnCheckout = false;
                await _cartRepository.SaveChangesAsync();
            }

            return Ok();
        }



        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderViewModel Model)
        {
            try
            {
                if (Model == null)
                {
                    return BadRequest("Order request is required.");
                }

                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                var currentUserId = await _workContext.GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    return Unauthorized("Current user is not valid. Please sign in again.");
                }

                var cart = await _cartService.GetActiveCartDetails(currentUserId, CartTypes.Shop, SiteId);
                if (cart == null)
                {
                    return NotFound();
                }

                var orderCreateResult = await _orderService.CreateOrder(cart.Id, Model.PaymentMethod, Model.PaymentFees, SiteId, Model.OrderStatus);

                if (!orderCreateResult.Success)
                {
                    //  await _orderService.DeActiveCart(cart.Id);
                    return BadRequest(orderCreateResult.Error);
                }
                //await _orderService.DeActiveCart(cart.Id);
                return Ok(orderCreateResult.Value.Id);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);

            }
        }

        [HttpPost("ChangeOrderStatus")]
        public async Task<IActionResult> ChangeOrderStatus([FromBody] OrderViewModel Model)
        {
            try
            {


                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                await _orderService.OrderChangeStatus(Model.OrderId, Model.OrderStatus, Model.OrderNote, SiteId, true);
                await _orderService.ReserveSerials(Model.OrderId);


                return Ok(true);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);

            }
        }

        [HttpGet("GetOrderSummery/{CartType}")]

        public async Task<IActionResult> GetOrderSummery([FromRoute] CartTypes CartType)
        {
            try
            {

                var UserId = await _workContext.GetCurrentUserId();

                return Ok(_orderService.GetOrderSummery(UserId, CartType));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPut("MappingOrderReferance/{OrderId}/{OrderReferanceId}")]

        public async Task<IActionResult> MappingOrderReferance([FromRoute] long OrderId, [FromRoute] string OrderReferanceId)
        {
            try
            {
                var Query = await _orderRepository.Query().Where(x => x.Id == OrderId).FirstOrDefaultAsync();

                Query.OrderReferanceId = OrderReferanceId;

                await _orderRepository.SaveChangesAsync();

                return Ok();

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }



    }

}
