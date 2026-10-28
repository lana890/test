
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class PlanProductVersion : EntityBase
    {
        public long PlanVersionId { get; set; }
        public PlansVersions PlanVersion { get; set; }
        public long ProductId { get; set; } 
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public EStatus Status { get; set; }


    }
}
