#nullable enable
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Services.PayFort.Interfaces;
using EskaCommerce.Payments.Models.PayFort;
using EskaCommerce.Payments.Helper;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static MimeDetective.Definitions.Default.FileTypes;

namespace EskaCommerce.Payments.Services.PayFort
{
    public class PayFortServices : IPayFortServices
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IPaymentServices _paymentServices;
        private readonly IPayFortConfigurationServices _payFortConfigServices;
        private readonly IRepository<SitesCultures> _siteCultureRepository;
        private readonly IWorkContext _workContext;
        private readonly IWorkContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public PayFortServices(
            ICartService cartService,
            IOrderService orderService,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IPaymentServices paymentServices,
            IPayFortConfigurationServices payFortConfigurationServices,
            IRepository<SitesCultures> siteCultureRepository,
            IWorkContext workContext,
            IWorkContext context,
            IHttpClientFactory httpClientFactory)
        {
            _cartService = cartService;
            _orderService = orderService;
            _paymentProviderRepository = paymentProviderRepository;
            _paymentServices = paymentServices;
            _payFortConfigServices = payFortConfigurationServices;
            _siteCultureRepository = siteCultureRepository;
            _workContext = workContext;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        #region Shared Logic (DRY)
        private async Task<(Cart cart, Result<EskaCMS.Entities.Entity.Orders.Order> orderResult, ConfigurePayFort settings, User user)>
            CreatePayFortOrderAsync(CartTypes cartType, long siteId, string paymentMethod, string providerId,long? CartId)
        {
            var user = await _context.GetCurrentUser();

            var cart = await _cartService.GetActiveCart(user.Id, cartType, siteId, CartId);
            if (cart == null)
                throw new Exception("Cart is empty");

            var orderResult = await _orderService.CreateOrder(
                cart.Id, providerId, 0, siteId, OrderStatus.PendingPayment);

            if (!orderResult.Success)
                throw new Exception(orderResult.Error);

            var settings = await GetPayFortSettingsAsync(siteId, providerId);

            return (cart, orderResult, settings, user);
        }
        #endregion

        #region Web Payment
        public async Task<RedirectResponseViewModel> PayFortTransaction(CartTypes cartType, long siteId,  string? CP_URL,int type,long? CartId)
        {
            var (cart, orderResult, settings, user) =
                await CreatePayFortOrderAsync(cartType, siteId, PaymentProviderHelper.PayFortProviderId, PaymentProviderHelper.PayFortProviderId,CartId);

            try
            {
                await _paymentServices.CreatePayment(orderResult.Value, null, "PayFort", "", PaymentStatus.Initiated);

                dynamic req = await _payFortConfigServices.Request(
                    cart, settings, orderResult.Value, EPlatformSource.Web, CP_URL,null, type,CartId);
               

                    return new RedirectResponseViewModel
                    {
                        PayfortInfo = req.Request,
                        Url = req.Url
                    };
                
            }
            catch (Exception ex)
            {
                await _paymentServices.CreatePayment(orderResult.Value, ex.Message, "PayFort", null, PaymentStatus.Failed);
                throw;
            }
        }
        #endregion

        #region Mobile SDK
        public async Task<dynamic> GenerateSdkTokenAsync(CartTypes cartType, long siteId, string deviceId,int type,long? CartId)
        {
            try
            {

                var (cart, orderResult, settings, user) =
                    await CreatePayFortOrderAsync(cartType, siteId, PaymentProviderHelper.PayFortProviderMobileId, PaymentProviderHelper.PayFortProviderMobileId,CartId);

                var cultureId = _workContext.GetCurrentCultureId();
                var stripSettings = await GetPayFortSettingsAsync(siteId, PaymentProviderHelper.PayFortProviderMobileId);
                var culture = await _siteCultureRepository.Query().Where(x => x.Id == cultureId).FirstOrDefaultAsync();
                var parameters = new SortedDictionary<string, string>
   {
        { "service_command",stripSettings.service_command },
        { "access_code", stripSettings.AccessCode },
        { "merchant_identifier", stripSettings.MerchantIdentifier },
        { "device_id", deviceId },
        { "language", "en" }

  };
                string signature =  _payFortConfigServices.GenerateSha256Signature(parameters, stripSettings.RequestPharse);
                var body = new
                {
                    service_command = "SDK_TOKEN",
                    access_code = stripSettings.AccessCode,
                    merchant_identifier = stripSettings.MerchantIdentifier,
                    language = "en",
                    device_id = deviceId,
                    signature = signature
                };
                var json = System.Text.Json.JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var client = _httpClientFactory.CreateClient();
                using var httpClient = _httpClientFactory.CreateClient();

                var response = await httpClient.PostAsync(stripSettings.MobileSDKtokenUrl, content);
                response.EnsureSuccessStatusCode();
                var ResponseContent = await response.Content.ReadAsStringAsync();

                SdkTokenResponse sdkResponse;
                try
                {
                    sdkResponse = System.Text.Json.JsonSerializer.Deserialize<SdkTokenResponse>(ResponseContent,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (System.Text.Json.JsonException)
                {
                    // PayFort may return URL-encoded form data instead of JSON
                    var dict = ResponseContent
                        .Split('&', StringSplitOptions.RemoveEmptyEntries)
                        .Select(pair => pair.Split('=', 2))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(
                            parts => Uri.UnescapeDataString(parts[0]),
                            parts => Uri.UnescapeDataString(parts[1]),
                            StringComparer.OrdinalIgnoreCase);
                    var fallbackJson = System.Text.Json.JsonSerializer.Serialize(dict);
                    sdkResponse = System.Text.Json.JsonSerializer.Deserialize<SdkTokenResponse>(fallbackJson,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                string? sdk_token = sdkResponse?.SdkToken;

                var req = await _payFortConfigServices.Request(
                        cart, settings, orderResult.Value, EPlatformSource.Mobile,null, sdk_token,(int)PaymentType.CP,CartId);

                await _paymentServices.CreatePayment(orderResult.Value, null, "PayFortMobile", null, PaymentStatus.Initiated);

                return req;

            }
            catch (Exception ex)
            {
                throw new Exception($"PayFort SDK token failed: {ex.Message}");
            }
        
        }
          

        #endregion

        #region Helpers
        public async Task<ConfigurePayFort> GetPayFortSettingsAsync(long siteId, string providerId)
        {
            var savedSettings = await _paymentProviderRepository
                .Query().AsNoTracking()
                .Where(x => x.Id == providerId && x.SiteId == siteId)
                .FirstOrDefaultAsync();

            if (savedSettings == null)
                throw new Exception("Please Add PayFort Settings From Payment Provider");

            return JsonConvert.DeserializeObject<ConfigurePayFort>(savedSettings.AdditionalSettings);
        }
        #endregion
    }
}
