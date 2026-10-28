using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{

    public class PlanProductVersionOutputs
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public EStatus Status { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

    }


    public class PlansVersionOutput
    {
        public string PlanName { get; set; }
        public int? RepeatAmount { get; set; }
        public EskaCMS.Entities.Enum.PlanEnums.RepeatPeriod RepeatPeriod { get; set; }
        public IList<PlanProductVersionOutputs> PlanProducts { get; set; }
        public decimal DiscountValue { get; set; }
        public long? DiscountId { get; set; }
        public long? CartRuleId { get; set; }
        public long? TaxClassId { get; set; }

        public int RefundPeriod { get; set; }

    }
}
