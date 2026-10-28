
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Elements
{
   public class ShareableElement : EntityBase
    {

        public ShareableElement()
        {
            this.CategoryId = (long)ECategory.SharedElements;
        }
        public string ElementTypeId { get; set; }
        public ElementTypes ElementType { get; set; }


        public long  ElementId { get; set; } 
        public string Settings { get; set; }
        public string IdentifierCode { get; set; }
        public string Name { get; set; }
        public string DataKey { get; set; }

        public long CategoryId { get; set; }
        public Category Category { get; set; }


        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public EStatus Status { get; set; }

        public long? CultureId { get; set; }
        public SitesCultures Culture { get; set; }

        public long? ParentId { get; set; }
        public ShareableElement Parent { get; set; }


    }
}
