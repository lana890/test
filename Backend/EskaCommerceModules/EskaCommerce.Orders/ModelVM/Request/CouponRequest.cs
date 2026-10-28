using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class CouponRequest
    {
        public long groupId {  get; set; }
        public long couponId { get; set; }

    }
    public class LoyalityPointRequest
    {
        public decimal Points { get; set; }
        public string Note {  get; set; }
    }
}
