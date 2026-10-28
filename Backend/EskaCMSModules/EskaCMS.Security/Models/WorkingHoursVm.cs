using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class WorkingHoursVm
    {
        public long? Id { get; set; }
        public long CustomerGroupId { get; set; }
        public string CustomerGroupName { get; set; }
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }
        public List<DayOfWeekViewModel> Day { get; set; }
    }

    public class DayOfWeekViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }


    public class UpsertWorkingHoursVm
    {
        public long? Id { get; set; }
        public long CustomerGroupId { get; set; }
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }
        public List<long> Day { get; set; }
    }
}
