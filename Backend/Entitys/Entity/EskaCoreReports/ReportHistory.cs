using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.EskaCoreReports
{
    public class ReportHistory : EntityBase
    {
        public long ReportId { get; set; }
        public  Reports Report { get; set; }
        public EStatus? OldStatus { get; set; }
        public EStatus NewStatus { get; set; }

        public long CreatedById { get; set; }
        public  User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }


    }
}
