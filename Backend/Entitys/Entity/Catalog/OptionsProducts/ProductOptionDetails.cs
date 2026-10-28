using EskaCMS.Infrastructure.Models;

using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductOptionDetails : EntityBase
    {
        public ProductOptionDetails()
        {

        }

        public ProductOptionDetails(string Name,long CultureId)
        {
            this.Name = Name;
            this.CultureId = CultureId;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public long CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }

        public long ProductOptionId { get; set; }
        public ProductOption ProductOption { get; set; }

    }
}
