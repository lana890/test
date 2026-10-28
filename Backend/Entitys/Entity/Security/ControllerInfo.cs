using EskaCMS.Infrastructure.Models;
using System.Collections.Generic;

namespace EskaCMS.Entities.Entity.Security
{
    public class ControllerInfo : EntityBaseWithTypedId<string>
    {
        public ControllerInfo()
        {
         
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string AreaName { get; set; }
        public long RoleAccessId { get; set; }

        public virtual RoleAccess RoleAccess { get; set; }
        public IEnumerable<ActionInfo> Actions { get; set; }
    }
}