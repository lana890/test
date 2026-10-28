
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Forms
{
   public partial class Forms : EntityBase
    {
      
        public string Name { get; set; }
        public string Settings { get; set; }
        public string Keys { get; set; }

        public string ComponentSettingsId { get; set; }

        public Enum.Source Source { get; set; }
        public string AdditionalDate { get; set; }
        public long PageId { get; set; }
        public long SiteId { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public Page Page { get; set; }

    }
}
