#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentPaypalExpress
{
    public class PaypalEnums
    {

        public enum ShippingPreference
        {
            GET_FROM_FILE = 1,
            NO_SHIPPING = 2,
            SET_PROVIDED_ADDRESS = 3,
        }
        
        public enum UserAction
        {
            CONTINUE = 1,
            PAY_NOW = 2,
        }
    }
}
