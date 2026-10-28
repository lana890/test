using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class StageDetails : EntityBase
    {

        public long StageId { get; set; }
        public Stage Stage { get; set; }

        public string Description { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
    }
}
