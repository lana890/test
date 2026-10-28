using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Security
{
    public class RoleAccess : EntityBase
    {
        public long RoleId { get; set; }
        public virtual Role Role { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public List<ControllerInfo> Controllers { get; set; }
    }
}