using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Menus
{
    public class MenuGroup : EntityBase
    {
        public string Name { get; set; }


        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; } 
        public DateTimeOffset? ModificationDate { get; set; }

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long? CategoryId { get; set; }
        public virtual Category Category { get; set; }


        public IList<Menu> Menus { get; set; }

    }
}
