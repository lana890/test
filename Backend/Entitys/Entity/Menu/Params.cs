using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Menus
{
    public class Params:EntityBase
    {
        public Params()
        {
            CreationDate = DateTimeOffset.Now;
        }

        public string Key { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long MenuId { get; set; }
        public virtual Menu Menu { get; set; }
    }
}
