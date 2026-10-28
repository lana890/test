using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using System.Collections.Generic;



namespace EskaCMS.Entities.Entity.Tax
{
    public class TaxClass : EntityBase
    {
        public TaxClass() { }

        public TaxClass(long id)
        {
            Id = id;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public long SiteId { get; set; }
        public IList<TaxRate> TaxRate { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

    }
}
