
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    public class CurrentLoggedInUsers : EntityBaseWithTypedId<string>
    {
        public CurrentLoggedInUsers()
        {
            Id = Guid.NewGuid().ToString("N");
        }
        public virtual User User { get; set; }
        public long UserId { get; set; }
        public DateTimeOffset LoginDate { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }
        public bool IsLookedOut { get; set; }
        public virtual SitesHostNames Hostname { get; set; }
        public long? HostnameId { get; set; }
        public virtual Organization Organization { get; set; }
        public long? OrganizationId { get; set; }
        public string ApiToken { get; set; }

        [StringLength(450)]
        public string RefreshTokenHash { get; set; }
        public string ClientIP { get; set; }
        public EUsersTypes UserType { get; set; }
        public LoginType LoginType { get; set; }

        public ETypeLogin TypeLogin    { get; set; }

        public bool KillSession { get; set; }


    }
}
