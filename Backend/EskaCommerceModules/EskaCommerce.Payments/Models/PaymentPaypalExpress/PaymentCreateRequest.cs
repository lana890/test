#nullable enable
namespace EskaCommerce.Payments.Models.PaymentPaypalExpress
{
    public class PaymentCreateRequest
    {
        public string intent { get; set; }

        public PurchesUnits[] purchase_units { get; set; }
        public PaymentSource payment_source { get; set; }

       
    }

  

 

    public class PurchesUnits
    {
        public Amount amount { get; set; }
        //public Item[] items { get; set; }
        public string custom_id { get; set; }
    }

    public class Amount : AmountDetails
    {
       

        //public Details breakdown { get; set; }
    }

    public class Details
    {
        public string subtotal { get; set; }
        public string tax { get; set; }
        public string shipping { get; set; }
        public string handling_fee { get; set; }
        public string shipping_discount { get; set; }
        public string insurance { get; set; }
    }

    public class Payment_Options
    {
        public string allowed_payment_method { get; set; }
    }

    public class Item_List
    {
        public Item[] items { get; set; }
        public Shipping_Address shipping_address { get; set; }
    }

    public class Shipping_Address
    {
        public string recipient_name { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string city { get; set; }
        public string country_code { get; set; }
        public string postal_code { get; set; }
        public string phone { get; set; }
        public string state { get; set; }
    }

    public class Item
    {
      
        public string name { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
        public string sku { get; set; }
        public string category { get; set; }
        public AmountDetails unit_amount { get; set; }
       
    }

    public class AmountDetails
    {
        public string value { get; set; }
        public string currency_code { get; set; }
    }

    public class PaymentSource
    {
        public Paypal paypal { get; set; }
    }

    public class Paypal
    {
        public ExperienceContext experience_context { get; set; }
    }

    public class ExperienceContext
    {
        public string return_url { get; set; }
        public string cancel_url { get; set; }
        public string shipping_preference { get; set; }
        public string user_action { get; set; }

    }
}
