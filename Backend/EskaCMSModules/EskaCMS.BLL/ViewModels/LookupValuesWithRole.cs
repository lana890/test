using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.General.ViewModels
{
    public class LookupValuesWithRole
    {
        public long LookupValueId { get; set; }
        public string LookupValueName { get; set;}
        public List<Role> AssignedRoles { get; set; }
        public List<Role> UnAssignedRoles { get; set; }

    }
}
