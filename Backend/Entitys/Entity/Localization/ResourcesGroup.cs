using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Localization
{
    public class ResourcesGroup:EntityBaseWithTypedId<string>
    {
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public string Component { get; set; }
        public List<ResourcesKey> ResourcesKeys { get; set; }


    }
}
