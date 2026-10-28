using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAdditionalFees : EntityBase
    {
        [StringLength(450)]
        public string Name { get; set; }

        public DiscountTypes FeeType { get; set; } 
        
        public decimal FeeValue { get; set; }
        public Product Product { get; set; }
        public long ProductId { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public User ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public string Note { get; set; }
        public bool Refundable { get; set; }
        public EStatus Status { get; set; }
    }
}
