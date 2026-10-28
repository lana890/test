using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class SlugManagementVm
    {
        public long Id { get; set; }
        public long? CultureId { get; set; }
        public long? SiteId { get; set; }
        public EEntity Entity { get; set; }
        public string Slug { get; set; }

    }
}
