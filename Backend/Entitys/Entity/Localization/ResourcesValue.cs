using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Localization
{
    public class ResourcesValue:EntityBase
    {
        public long KeyId { get; set; }
        public ResourcesKey Key { get; set; }

        public string Value { get; set; }

        public long? SlugInfoId { get; set; }
        public SlugInfo SlugInfo { get; set; }
        public long  CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }

    }
}
