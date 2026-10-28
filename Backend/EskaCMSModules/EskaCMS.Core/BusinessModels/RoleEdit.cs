using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.BusinessModels
{
    public class RoleEdit
    {
        public Role Role { get; set; }
        public IEnumerable<User> Members { get; set; }
        public IEnumerable<User> NonMembers { get; set; }
    }
}
