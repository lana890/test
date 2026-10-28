using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class MepsConfigrationItems
    {

        public string Module { get; set; } = "MepsPayment";


        public string MerchantId { get; set; } = "GATEWAY_MERCHANT_ID";
        public string ApiPassword { get; set; } = "API_PASSWORD";
        public string BaseUrl { get; set; } = "GATEWAY_BASE_URL";
        public string Currencey { get; set; } = "GATEWAY_CURRENCY";
        public string Version { get; set; } = "GATEWAY_VERSION";
        public string Enviroment { get; set; } = "ENVIRONMENT";
        public string ServerKey { get; set; } = "ServerKey";
        public string AllowedCurrencies { get; set; } = "";

        public void FillConfigItems( IRepositoryWithTypedId<PaymentProvider,string> paymentProviderrepository)
        {

            var ProviderConfigurations =  paymentProviderrepository.Query().
               FirstOrDefault(x => x.Id == PaymentProviderHelper.MepsProviderId);

            if (ProviderConfigurations != null)
            {
                var ProviderSettings = JsonConvert.DeserializeObject<MepsConfig>(ProviderConfigurations.AdditionalSettings);

                this.MerchantId = ProviderSettings.MerchantId;
                this.ApiPassword = ProviderSettings.ApiPassword;
                this.BaseUrl = ProviderSettings.BaseUrl;
                this.Currencey = ProviderSettings.Currency;
                this.Version = ProviderSettings.Version;
                this.Enviroment = ProviderSettings.Enviroment;
                this.ServerKey = ProviderSettings.ServerKey;
                this.AllowedCurrencies = ProviderSettings.AllowedCurrencies;
            }
            else
            {
                throw new Exception("There are no configurations for this payment method, please contact your administrator");
            }
        }

    }
}

