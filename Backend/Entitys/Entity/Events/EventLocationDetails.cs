using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Events
{
    public class EventLocationDetails : EntityBase
    {
        public string Name { get; set; }

        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

        public EventLocation EventLocation { get; set; }
        public long EventLocationId { get; set; }

    }
}
