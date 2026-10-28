using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.Security
{
    public class SecuritySettings : EntityBase
    {
        public bool RequireDigit { get; set; }
        public int RequiredLength { get; set; }
        public bool RequireNonAlphanumeric { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public int RequiredUniqueChars { get; set; }
        public TimeSpan DefaultLockoutTimeSpan { get; set; }
        public int MaxFailedAccessAttempts { get; set; }
        public bool AllowedForNewUsers { get; set; }
        public string AllowedUserNameCharacters { get; set; }

        public bool RequireUniqueEmail { get; set; }
        public bool RequireConfirmedAccount { get; set; }
        public bool RequireConfirmedPhoneNumber { get; set; }
        public bool RequireConfirmedEmail { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }

    }
}
