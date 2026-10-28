using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Inventory
{
    public class WarehouseRoles : EntityBase
    {
        public Warehouse Warehouse { get; set; }
        public Role Role { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public long WarehouseId { get; set; }
        public long RoleId { get; set; }
    }
}
