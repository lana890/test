using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class CashSaleVM
    {
        public CashSaleAmountVM cashSale { get; set; }

        public List<PaymentVM> payments { get; set; } = new List<PaymentVM>();
        public List<BillingProductVM> products { get; set; } = new List<BillingProductVM>();
    }

    public class CashSaleAmountVM
    {
        public decimal totalAmount { set; get; }
        public Dictionary<string, string> additionalInfo { set; get; }
    }

    public class PaymentVM
    {
        public int paymentMethod { set; get; } = 3;

        public decimal amount { set; get; }
    }

    public class BillingProductVM
    {
        public string productCode { set; get; }
        public int quantity { set; get; }
        public string taxCode { set; get; }
        public decimal taxAmount { set; get; }
        public decimal discountAmount { set; get; }
        public decimal amount { set; get; }
    }

}
