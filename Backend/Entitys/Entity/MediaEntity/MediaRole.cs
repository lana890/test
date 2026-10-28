using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.MediaEntity
{
    public class MediaRole : EntityBase
    {
        public long RoleId { get; set; }
        public Role Role { get; set; }
        public long MediaId { get; set; }
        public Media Media { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public User CreatedBy { get; set; }
    }
}
