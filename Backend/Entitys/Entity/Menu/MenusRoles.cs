using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Menus
{
    public class MenusRoles : EntityBase
    {
        public long MenuId { get; set; }
        public long RoleId { get; set; }
        public Role Role { get; set; }
        public Menu Menu { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
