
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.General
{
    public class Lookup : EntityBase
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DefultValue { get; set; }
        public bool IsEditable { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Estatus { get; set; }
        public bool EnableDataSecurity { get; set; }

        public long? SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }
    
        public List<LookupValues> lookupvalues { get; set; }


    }
}