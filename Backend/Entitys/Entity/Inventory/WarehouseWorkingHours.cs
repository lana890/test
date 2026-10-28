using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Inventory
{
    public class WarehouseWorkingHours :EntityBase
    {
        public long WarehouseId { get; set; }
        public WarehouseWorkingDay DayOfWeek { get; set; } 
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public long SiteId { get; set; }
        public Warehouse Warehouse { get; set; }
        public Site Site { get; set; }

    }
}
