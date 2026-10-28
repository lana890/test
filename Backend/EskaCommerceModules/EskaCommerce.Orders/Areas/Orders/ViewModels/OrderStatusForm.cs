using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderStatusForm
    {
        public int StatusId { get; set; }

        public string Note { get; set; }

        public string OrderReferanceIdInfo { get; set; } = null;
    }
}
