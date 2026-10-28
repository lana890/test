using System;
using System.Collections.Generic;
using System.Text;
namespace EskaCommerce.Discount.Models
{
   public class DiscountTiersVM
    {
        public EskaCMS.Entities.Entity.Discount.Discount Discount { get; set; }

        public long DiscountId { get; set; }
        public decimal FromValue { get; set; }
        public decimal ToValue { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.DiscountTiersTypes DiscountTierType { get; set; }
        public decimal DiscountValue { get; set; }
     
    }
}
