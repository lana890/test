using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCMS.Entities.Entity.EskaCoreReports
{
    public class Reports : EntityBase
    {
        public long MenuId { get; set; }
        public virtual Menu Menu { get; set; }
        public long PostId { get; set; }
        public virtual Posts Post { get; set; }
        public EStatus Status { get; set; }


        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public List<ReportHistory> ReportHistories { get; set; }


    }
}
