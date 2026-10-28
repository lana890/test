using System;
using System.Collections.Generic;

namespace EskaCMS.Entities.Entity
{
    public partial class Domains
    {
        public Domains()
        {
            DomainValues = new HashSet<DomainValues>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }

        public virtual ICollection<DomainValues> DomainValues { get; set; }


    }
}
