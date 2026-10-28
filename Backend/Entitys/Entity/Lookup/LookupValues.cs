using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.General
{
    public class LookupValues:EntityBase
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public long Lookup_Order { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        // Lookup Forign Key
        public long lookupId { get; set; }
        public virtual Lookup LookUp { get; set; }
        // self relation
        public long? ParentId { get; set; }

        public LookupValues Parent { get; set; }

        public IList<LookupvalueRoles> LookupvalueRoles { get; set; }




    }
}
