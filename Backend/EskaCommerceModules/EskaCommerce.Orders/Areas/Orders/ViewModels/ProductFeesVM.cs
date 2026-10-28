using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class ProductFeesVM
    {
        public string Name { get; set; }
        public DiscountTypes FeesType { get; set; }
        public decimal FeesValue { get; set; }
        public long ProductId { get; set; }
        public long Id { get; set; }


    }
}
