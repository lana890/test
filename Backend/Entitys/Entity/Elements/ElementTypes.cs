
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Elements
{
    public class ElementTypes : EntityBaseWithTypedId<string>
    {

        public ElementTypes(string id)
        {
            Id = id;
        }

        public string Desciption { get; set; }
        
        public DateTimeOffset CreationDate { get; set; }

        public long? SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }


    }
}
