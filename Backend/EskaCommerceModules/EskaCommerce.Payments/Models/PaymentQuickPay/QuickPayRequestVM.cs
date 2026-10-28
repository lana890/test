using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PaymentQuickPay
{
    public class QuickPayRequestVM
    { 
        public string version { get; set; }
        public string merchant_id { get; set; }
        public string agreement_id { get; set; }
        public string order_id { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string continueurl { get; set; }
        public string cancelurl { get; set; }
        public string callbackurl { get; set; }
         public string autocapture { get; set; }
        public string checksum { get; set; }
    }  
    
    
    
    public class QPpaymentVM
    {

        public string brand { get; set; }
        public string currency { get; set; }
        public string country { get; set; }
        public string created_at { get; set; }
        public string last4 { get; set; }
        public string token { get; set; }
        public long id { get; set; }
        public long exp_year { get; set; }
        public long exp_month { get; set; }
        public CartTypes CartType { get; set; }

    }

    public class QPAuthVM
    {
        public long orderId { get; set; }
        public int paymentId { get; set; }
        public string linkURL { get; set; }
        public CartTypes CartType { get; set; }
    }
}
