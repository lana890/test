using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.LoyaltyPoints
{
    public class PointsConversionConfig : EntityBase
    {
        public decimal PointsValue { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }

        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? ModificationDate { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
    }
}
