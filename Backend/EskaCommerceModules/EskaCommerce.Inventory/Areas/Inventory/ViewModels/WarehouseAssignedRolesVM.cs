using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Inventory.Areas.Inventory.ViewModels
{
    public class WarehouseAssignedRolesVM
    {
        
            public List<Role> AssingedRoles { get; set; }
            public List<Role> UnassingedRoles { get; set; }
          

       
    }
}
