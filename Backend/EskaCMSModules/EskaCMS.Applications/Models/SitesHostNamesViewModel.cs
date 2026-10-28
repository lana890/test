using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{
    public class SitesHostNamesViewModel
    {
        public long? Id { get; set; }
        public long SiteId { get; set; }

        public string HostName { get; set; }

        public bool IsActive { get; set; }
        public string Prefix { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CultureId { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset ModificationDate { get; set; }
        public string CultureName { get; set; }

        public string AddtionalMetaTags { get; set; }

    }




    public class SitesHostNamesViewModelWithStatus : SitesHostNamesViewModel
    {
        public EStatus Status { get; set; }

    }

}
