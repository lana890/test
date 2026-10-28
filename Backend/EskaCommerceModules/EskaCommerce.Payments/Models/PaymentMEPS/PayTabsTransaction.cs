using System;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class PayTabsTransaction
    {
        public PayTabsTransaction()
        {
            customer_details = new CustomerDetails();
           // payment_result = new Payment_Result();
           // payment_info = new Payment_Info();
        }
        //public int merchant_id { get; set; }
        public int profile_id { get; set; }

        public string tran_ref { get; set; }
        public string tran_type { get; set; }

        public string cart_id { get; set; }
        public float cart_amount { get; set; }
        public string cart_currency { get; set; }
        public string cart_description { get; set; }
        public bool hide_shipping { get; set; }
        public string tran_class { get; set; }
        public string tran_currency { get; set; }
        public float tran_total { get; set; }
        public string paypage_lang { get; set; } = "en";

        public int tokenise { get; set; }
        public dynamic customer_details { get; set; }

      //  public Payment_Result payment_result { get; set; }

      //  public Payment_Info payment_info { get; set; }

        public string callback { get; set; }
        public string @return { get; set; }

    }


    public class PaymentTransactionRecuring
    {
        public int profile_id { get; set; }

        public string tran_ref { get; set; }
        public string tran_type { get; set; }
        public string cart_id { get; set; }
        public string cart_currency { get; set; }
        public float cart_amount { get; set; }
        public string tran_class { get; set; }

        public string cart_description { get; set; }
        public string token { get; set; }




    }


}
