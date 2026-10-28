using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class CustomerLoyalityVm
    {
        public string Data { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public bool IsError { get; set; }
        public int Counter { get; set; }
    }

    public class CustomerLoyalitydataVm
    {
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalPoints { get; set; }
        public string CardNo { get; set; }
        public string CardType { get; set; }
        public decimal CustomerDiscount { get; set; }
        public decimal Exrate { get; set; }
    }
    public class CustomerLoyalityObjectVm
    {
        public CustomerLoyalitydataVm Data { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public bool IsError { get; set; }
        public int Counter { get; set; }
    }

}
