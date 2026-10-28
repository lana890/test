using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;


namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductOptionValue : EntityBase
    {
        public long OptionId { get; set; }

        public ProductOption Option { get; set; }
        //public string ProductId { get; set; }

        [StringLength(450)]
        public string DisplayType { get; set; }

        public int SortIndex { get; set; }


        public IList<ProductOptionValueDetails>  productOptionValueDetails { get; set; }

    }
}
