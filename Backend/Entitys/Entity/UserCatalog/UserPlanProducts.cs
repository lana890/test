using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class UserPlanProducts : EntityBase
    {

        public long UserPlanId { get; set; }
        public UserPlans UserPlan { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }

    }
}
