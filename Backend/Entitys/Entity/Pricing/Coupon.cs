using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class Coupon : EntityBase
    {
        public Coupon ()
        {
            CreatedOn = DateTimeOffset.Now;
        }

        public bool IsActive { get; set; }

        public long CartRuleId { get; set; }

        public CartRule CartRule { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Code { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

    }
}
