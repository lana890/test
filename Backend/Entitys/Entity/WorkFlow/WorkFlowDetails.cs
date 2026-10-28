using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class WorkFlowDetails : EntityBase
    {

        public string Name { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

        public long WorkFlowId { get; set; }
        public virtual WorkFlowSetup WorkFlow { get; set; }
    }
}
