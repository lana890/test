using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.TelrPayment.Models
{
    public class TelrAdviceVM
    {
        public string tran_store { get; set; }
        public string tran_type { get; set; }
        public string tran_class { get; set; }
        public string tran_test { get; set; }
        public string tran_ref { get; set; }
        public string tran_prevref { get; set; }
        public string tran_firstref { get; set; }
        public string tran_currency { get; set; }
        public string tran_amount { get; set; }
        public string tran_cartid { get; set; }
        public string tran_desc { get; set; }
        public string tran_status { get; set; }
        public string tran_authcode { get; set; }
        public string tran_authmessage { get; set; }
        public string tran_check { get; set; }
        public string bill_title { get; set; }
        public string bill_fname { get; set; }
        public string bill_sname { get; set; }
        public string bill_addr1 { get; set; }
        public string bill_addr2 { get; set; }
        public string bill_addr3 { get; set; }
        public string bill_city { get; set; }
        public string bill_region { get; set; }
        public string bill_country { get; set; }
        public string bill_zip { get; set; }
        public string bill_phone1 { get; set; }
        public string bill_email { get; set; }
        public string xtra_ { get; set; }
        public string card_code { get; set; }
        public string card_last4 { get; set; }
        public string card_check { get; set; }
        public string cart_lang { get; set; }
        public string integration_id { get; set; }
        public string bill_check { get; set; }
        public string agreement_id { get; set; }
        public string authorised_count { get; set; }
        public string retry_count { get; set; }
        public string actual_payment_date { get; set; }
        public string agreement_category { get; set; }

    }
}
