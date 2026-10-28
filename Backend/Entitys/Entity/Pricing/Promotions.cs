using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.cart
{
    public class Promotions:EntityBase
    {
        public long Id {  get; set; }
        public string Name { get; set; }
        public decimal? QuantityOfOrders {  get; set; }
        public long SiteId {  get; set; }

        public PromotionsEnum EPromotion { get; set; }

        public UserState UserState { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset ModificationOn{ get; set; }

        public long CreatedById { get; set; }
        public long ModifiedById { get; set; }

        //public long CartRuleId {  get; set; }

        public  long CartRuleId { get; set; }
       public Site Site { get; set; }
        public CartRule CartRule { get; set; }


        public User CreatedBy { get; set; }
        public User ModifiedBy { get; set; }



    }
}
