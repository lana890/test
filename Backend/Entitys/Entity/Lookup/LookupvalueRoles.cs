using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.General
{
    public class LookupvalueRoles:EntityBase
    {
        public long RoleId { get; set; }
        public Role Role { get; set; }
        public long LookupvalueId { get; set; }
        public LookupValues Lookupvalue { get; set; }
    }
}
