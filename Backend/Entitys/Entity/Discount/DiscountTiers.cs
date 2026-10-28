using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Discount
{
   public class DiscountTiers : EntityBase
    {
        public Discount Discount { get; set; }

        public long DiscountId { get; set; }
        public decimal FromValue { get; set; }
        public decimal ToValue { get; set; }

        public DiscountTiersTypes DiscountTierType { get; set; }
        public decimal DiscountValue { get; set; }
        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }


        public User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
