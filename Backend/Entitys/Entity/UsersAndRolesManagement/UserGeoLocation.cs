using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class UserGeoLocation : EntityBase
    {
        public long UserId { get; set; }

        public User User { get; set; }

        public long SiteId { get; set; }

        public Site Site { get; set; }

        public string Country { get; set; }
        public string City { get; set; }

        public string Area { get; set; }

        public string Longitude { get; set; }
        public string Latitude { get; set; }

        public string FullAddress { get; set; }
        public DateTimeOffset CreationDate { get; set; }

    }
}
