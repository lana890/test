using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{
    public class UserRole : IdentityUserRole<long>
    {
        public override long UserId { get; set; }

        public User User { get; set; }

        public override long RoleId { get; set; }

        public Role Role { get; set; }
    }
}
