using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.BlogArchive
{
    public class PostRoles : EntityBase
    {
        public virtual Posts Post { get; set; }
        public virtual Role Role { get; set; }
        
        public long PostId { get; set; }
        public long RoleId { get; set; }
        public virtual User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
