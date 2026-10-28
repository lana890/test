using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Models.Responses.RateCalculate
{

    public class RateCalculatorResponse
    {
        public Transaction Transaction { get; set; }
        public List<NotificationsErrors> Notifications { get; set; }
        public bool HasErrors { get; set; }
        public TotalAmount TotalAmount { get; set; }
        public RateDetails RateDetails { get; set; }
    }

    public class Transaction
    {
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public string Reference3 { get; set; }
        public string Reference4 { get; set; }
        public string Reference5 { get; set; }
    }

    public class TotalAmount
    {
        public string CurrencyCode { get; set; }
        public double Value { get; set; }
    }

    public class RateDetails
    {
        public double Amount { get; set; }
        public double OtherAmount1 { get; set; }
        public double OtherAmount2 { get; set; }
        public double OtherAmount3 { get; set; }
        public double OtherAmount4 { get; set; }
        public double OtherAmount5 { get; set; }
        public double TotalAmountBeforeTax { get; set; }
        public double TaxAmount { get; set; }
    }

    public class NotificationsErrors
    {
        public string Code { get; set; }
        public string Message { get; set; }

    }
}
