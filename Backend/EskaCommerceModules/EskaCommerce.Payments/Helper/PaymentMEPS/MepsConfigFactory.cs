using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Models.PaymentMEPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Helper.PaymentMEPS
{
    public class MepsConfigFactory
    {
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> PaymentProviderRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingRepository;
        private static GatewayApiConfig GatewayApiConfig;
        public MepsConfigFactory(IWorkContext workContext, IRepositoryWithTypedId<PaymentProvider,string> paymentProviderRepository)
        {
            _workContext = workContext;
            PaymentProviderRepository= paymentProviderRepository;
      }

        public GatewayApiConfig GetMepsConfig()
        {
            if(GatewayApiConfig == null)
            {
                var MepsConfigItem = new MepsConfigrationItems();

                MepsConfigItem.FillConfigItems(PaymentProviderRepository);

                var GetwaApi = new GatewayApiConfig(MepsConfigItem.MerchantId, MepsConfigItem.ApiPassword, MepsConfigItem.BaseUrl, MepsConfigItem.Currencey, MepsConfigItem.Version, MepsConfigItem.Enviroment, MepsConfigItem.ServerKey, MepsConfigItem.AllowedCurrencies);

                GatewayApiConfig = GetwaApi;

                return GetwaApi;

            }
            else
            {
                return GatewayApiConfig;
            }

        }
    }
}
