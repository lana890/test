using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Security
{
    public class WorkingHoursDay:EntityBase
    {
        public long  WorkingHoursId { get; set; }

        public UserWorkingHours WorkingHours { get; set; }

        public long DayId { get; set; }
        public DaysOfWeek Day { get; set; }

    }
}
