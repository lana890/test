
using EskaCMS.Entities.Entity.EskaCoreReports;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCMS.Entities.Entity.Core
{
    public  class Role : IdentityRole<long>
    {
        public IList<UserRole> Users { get; set; } = new List<UserRole>();

        public long?  SiteId { get; set; }

        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public bool IsSystemAdmin { get; set; }
        public ERoleTypes Type { get; set; } 

        public ICollection<Options_in_Role> RoleOptions { get; set; }

    }
}
