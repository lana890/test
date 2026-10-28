
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;


namespace EskaCMS.Entities.Entity.Pages

{
    public class PagesRoles : EntityBase
    {

        public Page Page { get; set; }
        public Role Role { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public long PageId { get; set; }
        public long RoleId { get; set; }
    }
}
