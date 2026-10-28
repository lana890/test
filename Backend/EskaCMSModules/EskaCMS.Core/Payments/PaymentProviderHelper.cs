using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Helper
{
    public static  class PaymentProviderHelper
    {
        public static readonly string PayFortProviderId = "PayFort";
        public static readonly string PayFortProviderMobileId = "PayFortMobile";

        public static readonly string HyperProviderId = "Hyperpay";
        public static readonly string HyperApplePayProviderId = "HyperpayApplePay";
        public static readonly string MepsPayTabsProviderId = "MepsPayTabs";
        public static readonly string MepsProviderId = "Meps";
        public static readonly string NetworkProviderId = "Network";
        public static readonly string PayPalProviderId = "Paypal";
       
        
         
    }

    public static class PaymentHelper
    {
        public static readonly string RedeemAmount = "Redeem";
        public static readonly string RefundAmount = "Refund";
        public static readonly string CouponAmount = "Coupon";
        public static readonly string CashOnDelivery = "CashOnDelivery";
        
        



    }
}

