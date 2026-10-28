using EskaCommerce.Payments.Models.PaymentMEPS;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Services.PaymentMEPS.Interfaces
{

    public interface IPaymentMepsService
    {
        Task<PayTabsConfig> GetPayTabsPaymentSettings(long SiteId, string PaymentProviderId);
        Task<MepsConfig> GetMepsPaymentSettings(long SiteId);
        Task<bool> IsExternalRequestAuthorised(HttpContext context,long siteId, string signature, string PaymentProviderId);
        Task<IPNCallBack> PaytabsRecuring(long OrderId, string Token, string Tran_Refrance);
        Task<NetworkConfig> GetNetowrkPaymentSettings(long siteId);
    }
}
