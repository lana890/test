using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Shared
{
    public class EntityTags : EntityBase
    {
        public EEntity EntityType { get; set; }
        public long EntityId { get; set; }
        public long TagsId { get; set; }
        public Tags Tags { get; set; }

    }
}
