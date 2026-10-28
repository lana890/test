using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCMS.Entities.Entity.Discount
{
    public class Discount : EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Name { get; set; }

        public string Description { get; set; }
        public DiscountTypes DiscountType { get; set; }
        public decimal DiscountValue { get; set; }

        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public  EStatus Status { get; set; }
        public User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long SiteId { get; set; }
        public IList<DiscountTiers> DiscountTiers { get; set; }

        public bool IsCustomerGroup { get; set; }
        public DateTimeOffset? StartDate {  get; set; }
        public DateTimeOffset? ExpiryDate{ get; set; }

        public IList<DiscountCustomerGroup> CustomerGroups { get; set; }
        public IList<DiscountProduct> Products { get; set; }

        public IList<PlansVersions> PlansVersions { get; set; }
    }
}
