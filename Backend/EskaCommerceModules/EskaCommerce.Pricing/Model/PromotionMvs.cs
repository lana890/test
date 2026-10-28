using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Pricing.Model
{
    public class PromotionMvs
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal? QuantityOfOrders { get; set; }
        public long SiteId { get; set; }

        public PromotionsEnum EPromotion { get; set; }

        public UserState UserState { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset ModificationOn { get; set; }

        public long CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public long CartRuleId { get; set; }


    }
        public class RequestPromotionViewModel
        {
            public long? Id { get; set; }
            public string Name { get; set; }
            public int? QuantityOfOrders { get; set; }

            public PromotionsEnum EPromotion { get; set; }

            public UserState UserState { get; set; }


            public long CartRuleId { get; set; }
        }
}
