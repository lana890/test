#nullable enable
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Elastic.Clients.Elasticsearch.MachineLearning;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Payments.Services.PayFort.Interfaces;
using EskaCommerce.Payments.Models.PayFort;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Services.PayFort
{
    public class PayFortConfigurationServices : IPayFortConfigurationServices
    {
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<SitesCultures> _siteCultureRepository;
        private readonly IConfiguration _config;

        private static readonly Random _random = new Random();
        private const int _merchantRefLength = 20;

        #region Constructor
        public PayFortConfigurationServices(
            ICurrencyService currencyService,
            IWorkContext workContext,
            IRepository<SitesCultures> siteCultureRepository,
            IConfiguration config)
        {
            _currencyService = currencyService;
            _workContext = workContext;
            _siteCultureRepository = siteCultureRepository;
            _config = config;
        }
        #endregion

        #region Main Request Builder
        public async Task<object> Request(
            Cart cart,
            ConfigurePayFort settings,
            Order order,
            EPlatformSource? device,
            string? CP_URL,
            string? sdk_token,
            int type,
            long? CartId)
        {
            try
            {
                var request = new PayFortRequest();
                var vm = new PayFortRequestViewModel();
                var encryptedOrderId = EncryptDecryptHelper.Encrypt(order.Id.ToString());

                var cultureId = _workContext.GetCurrentCultureId();
                var culture = await _siteCultureRepository.Query().FirstOrDefaultAsync(x => x.Id == cultureId);

                var additionalDetails = cart.Items
                    .Where(x => x.AdditionalDetails != null)
                    .Select(x => x.AdditionalDetails)
                    .FirstOrDefault();
                dynamic? extraObj;
                try
                {
                    extraObj=additionalDetails != null ? JsonConvert.DeserializeObject<dynamic>(additionalDetails) : null;

                }
                catch (Exception)
                {
                    extraObj=null;
                }

                #region Fill Base Fields

                vm.AccessCode = settings.AccessCode;
                vm.Command = settings.Command;
                vm.Currency = order.CurrencyCode;
                vm.Merchant_Identifier = settings.MerchantIdentifier;
                vm.Merchant_Reference = GenerateMerchantReference(order.Id, cart);
                vm.Url = settings.URl;
                vm.CustomerEmail = GetCustomerEmail(cart, order);
                vm.CustomerName = GetCustomerName(cart, order); 

                vm.Language = culture?.CultureId?.Split("-").FirstOrDefault() ?? "en";
                // vm.ReturnUrl = CP_URL ?? settings.ReturnUrl;
                var baseUrl = vm.Language.Contains("en") ? settings.BaseUrlForEnglish : settings.BaseUrlForArabic;
                vm.ReturnUrl = !string.IsNullOrWhiteSpace(CP_URL) ? settings.ReturnUrl+$"?baseUrl={CP_URL}&OrderId={order.Id}" : settings.ReturnUrl+$"?baseUrl={baseUrl}&OrderId={order.Id}";

                vm.order_description = order.OrderItems
                    .SelectMany(x => x.Product.ProductDetails)
                    .Where(x => x.CultureId == cultureId)
                    .Select(x => x.Name)
                    .FirstOrDefault() ?? "Unknown";

                vm.Amount = CalculateAmount(order, settings, vm.Currency);
                vm.merchant_extra = order.SiteId.ToString();
                vm.merchant_extra1 = order.Id.ToString();
          //      vm.customer_ip = "192.168.2.20";
                vm.merchant_extra2 = $"{type}culture{cultureId}";
                vm.merchant_extra4 = !string.IsNullOrEmpty(CartId.ToString()) ? CartId.ToString() :"";

				vm.phone_number = order.ShippingAddress?.Phone ?? cart.Customer?.PhoneNumber;
                vm.notification_url = settings.NotificationUrl;

                if (extraObj?["stepId"] != null)
                {
                    vm.merchant_extra3 = extraObj["stepId"];
                    vm.merchant_extra4 = extraObj["userPropertiesId"];
                }
                #endregion

                #region Web / Mobile Switch
                if (device == EPlatformSource.Web)
                {
                    vm.RequestPhrase = settings.RequestPharse;
                    vm.Signature =  GenerateSha256Signature(vm, vm.RequestPhrase);
                    request.Request = ConvertRequestToString(vm);
                    request.Url = settings.URl;

                    return request;
                }

                if (device == EPlatformSource.Mobile)
                {
                    vm.sdk_token = sdk_token;
                    vm.OrderId = encryptedOrderId;
                    return vm;
                }
                #endregion

                return request;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion



        #region Helpers - Merchant / Customer
        private string GetCustomerEmail(Cart cart, Order order)
        {
            return order.ShippingAddress?.Email
                ?? cart.Customer?.Email
                ?? "Guest@Guest.com";
        }

        private string GetCustomerName(Cart cart, Order order)
        {
            return order.ShippingAddress?.ContactName
                ?? cart.Customer?.FullName
                ?? "Guest";
        }

        private string GenerateMerchantReference(long orderId, Cart cart)
        {
            return $"{Guid.NewGuid():N}";


        }
        #endregion

        #region Helpers - Amount / Currency
        private string CalculateAmount(Order order, ConfigurePayFort settings, string currency)
        {
            string rate = settings.Rate;

            switch (currency?.ToUpper())
            {
                case "JOD": rate = settings.RateJordan; break;
                case "UAE": rate = settings.RateUAE; break;
            }

            var total = Convert.ToDecimal(order.OrderTotal) * Convert.ToDecimal(rate);
            return Convert.ToInt64(total).ToString();
        }
        #endregion

        #region Signature
        public string GenerateSha256Signature(PayFortRequestViewModel model, string phrase)
        {
            var dict = ConvertAsDictionary(model);

            var builder = new StringBuilder();
            builder.Append(phrase);

            foreach (var kv in dict.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    builder.Append($"{kv.Key}={kv.Value}");
            }

            builder.Append(phrase);

            return ComputeSha256(builder.ToString());
        }
        public string GenerateSha256Signature(IDictionary<string, string> parameters, string phrase)
        {
            var builder = new StringBuilder();
            builder.Append(phrase);

            foreach (var kv in parameters.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    builder.Append($"{kv.Key}={kv.Value}");
            }

            builder.Append(phrase);

            return ComputeSha256(builder.ToString());
        }


        private string ComputeSha256(string value)
        {

            var bytes = Encoding.UTF8.GetBytes(value);
            var hashstring = SHA256.Create();
            var hash = hashstring.ComputeHash(bytes);

            var stringbuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                var hex = b.ToString("x2");
                stringbuilder.Append(hex);
            }
            return stringbuilder.ToString();
        }
        #endregion

        #region Random
        public static string RandomeMerchantReferenceId(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)])
                .ToArray());
        }
        #endregion

        #region Convert Request
        private Dictionary<string, string> ConvertAsDictionary<T>(T model)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            // نجيب كل properties من T
            var props = typeof(T).GetProperties()
                .Where(p => p.GetValue(model) != null);

            foreach (var p in props)
            {
                var value = p.GetValue(model)?.ToString();

                // تحويل الاسم من PascalCase إلى snake_case

                var key = ConvertToSnakeCase(p.Name);

                // PayFort uses lowercase keys
                key = key.ToLower();

                if (key == "signature")
                    continue;
                if (key == "request_phrase")
                    continue;
                if (key == "notification_url")
                    continue;
                if (key == "url")
                    continue;
                dict[key] = value;
            }

            return dict;
        }

        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var builder = new StringBuilder();
            bool previousWasUnderscore = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // لو كان underscore موجود أصلاً
                if (c == '_')
                {
                    builder.Append('_');
                    previousWasUnderscore = true;
                    continue;
                }

                // لو حرف كبير ويجب إضافة underscore
                if (char.IsUpper(c) && i > 0 && !previousWasUnderscore)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLower(c));
                previousWasUnderscore = false;
            }

            return builder.ToString();
        }

        public string ConvertRequestToString(PayFortRequestViewModel request)
        {
            try
            {
                var dict = ConvertAsDictionary(request);
                dict.Add("signature", request.Signature);

                var builder = new StringBuilder();
                builder.Append("{");

                foreach (var kv in dict.OrderBy(x => x.Key))
                {
                    builder.Append($"\"{kv.Key}\":\"{kv.Value}\",");
                }

                if (builder[^1] == ',')
                    builder.Remove(builder.Length - 1, 1);

                builder.Append("}");

                return builder.ToString();
            }
            catch
            {
                return "{}";
            }
        }
        #endregion

        #region Verify
        public bool VerifyResponseSigneture(IFormCollection form, string phrase)
        {
            var signature = form["signature"].ToString();

            var data = form
                .Where(x => x.Key != "signature")
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.ToString(),
                    StringComparer.Ordinal
                );

            var raw = new StringBuilder();
            raw.Append(phrase);

            foreach (var kv in data.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                raw.Append($"{kv.Key}={kv.Value}");
            }

            raw.Append(phrase);

            var computed = ComputeSha256(raw.ToString());

            return computed.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }


        #endregion
    }
}
