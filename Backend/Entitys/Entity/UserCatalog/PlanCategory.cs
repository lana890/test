using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class PlanCategory : EntityBase
    {
        public long PlanId { get; set; }
        public Plan Plan { get; set; }
        public long CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
