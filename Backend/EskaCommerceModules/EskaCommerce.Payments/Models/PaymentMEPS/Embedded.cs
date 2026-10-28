using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public  class EmbeddedCheckoutPayloadVM
    {
        public EmbeddedCheckoutPayloadVM(string apiOperation, string MerchantName, EmbeddedOrdreVM Order, string returnUrl, CustomizedObj customData)
        {
            this.apiOperation = apiOperation;
            this.order = Order;
            this.order.custom = customData;
            this.interaction = new InteractionVM(MerchantName,returnUrl);


        }


        public EmbeddedCheckoutPayloadVM(string MerchantName,string Orderid,double Amount,string CurrencyCode,string Description,string returnUrl, CustomizedObj customData)
        {
            this.apiOperation = "INITIATE_CHECKOUT";
            this.order = new EmbeddedOrdreVM()
            {
                amount=Amount,
                currency=CurrencyCode,
                description=Description,
                id=Orderid,
                reference= Orderid,
                custom= customData
            };

            this.interaction = new InteractionVM(MerchantName, returnUrl);

        }

        public string apiOperation { get; set; } 
        public EmbeddedOrdreVM order { get; set; }
        public InteractionVM interaction { get; set; }
    }

    public class EmbeddedOrdreVM
    {
        public string id { get; set; }
        public double amount { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public string reference { get; set; }
        public CustomizedObj custom { get; set; }
    }
    public class InteractionVM
    {
        public InteractionVM(string MerchantName , string returnUrl)
        {
            this.displayControl = new DisplayControlVM();
            this.merchant = new MerchantVM(MerchantName);
            this.operation = "PURCHASE";
            this.returnUrl = returnUrl;
        }
        public string operation { get; set; } 
        public string returnUrl { get; set; } 
        public MerchantVM merchant { get; set; }
        public DisplayControlVM displayControl { get; set; } 
    }


    public class MerchantVM
    {
        public MerchantVM(string Name)
        {
            this.name = Name;
        }
        public string name { get; set; } 
    }
    
    
    public class DisplayControlVM
    {
        public string billingAddress { get; set; } = "HIDE";
        public string customerEmail { get; set; } = "HIDE";
    }


    public class EmbeddedCheckoutResponseVM
    {
        public string checkoutMode { get; set; }
        public string merchant { get; set; }
        public string result { get; set; }
        public EmbeddedCheckoutSessionVM session { get; set; }
        public string successIndicator { get; set; }
      
    }

    public class EmbeddedCheckoutSessionVM
    {
        public string id { get; set; }
        public string updateStatus { get; set; }
        public string version { get; set; }
    }

    public class CustomizedObj
    {
        public string cultureId { get; set; }
        public string frontReturnUrl { get; set; }
    }

}


