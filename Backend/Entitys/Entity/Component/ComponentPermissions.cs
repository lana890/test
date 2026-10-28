using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Elements;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Component
{
    public class ComponentPermissions :EntityBase
    {
        public string ComponentSettingsId { get; set; }
        public long PermissionId { get; set; }
        public  EskaCMS.Entities.Entity.Core.Option Permission { get; set; }
        public  DateTimeOffset? CreationDate { get; set; }
        public  long CreatedById { get; set; }
        public  User CreatedBy { get; set; }
        public  long RoleId { get; set; }
        public  Role Role { get; set; }
    }
}
