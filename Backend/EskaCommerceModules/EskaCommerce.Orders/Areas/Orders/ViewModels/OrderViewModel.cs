using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EskaCMS.Entities.Entity;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderViewModel
    {
        public OrderStatus OrderStatus { get; set; }
        public decimal PaymentFees { get; set; }
        public string PaymentMethod { get; set; }
        public long OrderId { get; set; }
        public string OrderNote { get; set; }

    }


    public class PayCashResult
    {
        public string OrderCode { get; set; }
        public long OrderId { get; set; }
    }
    
}
