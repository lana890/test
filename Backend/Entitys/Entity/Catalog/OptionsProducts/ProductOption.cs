using EskaCMS.Infrastructure.Models;

using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductOption : EntityBase
    {
        public ProductOption()
        {

        }

        public long? SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public IList<ProductOptionValue>  productOptionValues { get; set; }
        public IList<ProductOptionDetails>  optionDetails { get; set; }


    }
}
