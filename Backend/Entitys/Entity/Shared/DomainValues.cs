using System;
using System.Collections.Generic;

namespace EskaCMS.Entities.Entity
{
    public partial class DomainValues
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string Value { get; set; }
        public long? DomainId { get; set; }

        public virtual Domains Domain { get; set; }
    }
}
