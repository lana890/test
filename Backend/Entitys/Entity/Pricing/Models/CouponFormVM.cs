using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity
{
    public  class CouponFormVM
    {
        public CouponFormVM()
        {

        }
        public CouponFormVM(long CartRuleId,string Coupon)
        {
            this.Coupon = Coupon;
            this.CartRuleId = CartRuleId;
            this.CreatedOn = DateTimeOffset.Now;
            this.IsActive = true;
        }
        public long Id { get; set; }
        public string CartRuleName { get; set; }
        public long CartRuleId { get; set; }
        public string Coupon  { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public bool IsConsumed { get; set; }
        public bool UseAsWallet { get; set; }

    }
}
