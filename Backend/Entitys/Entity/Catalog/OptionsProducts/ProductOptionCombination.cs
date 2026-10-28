using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;



namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductOptionCombination : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }


        public long? ParentProductId { get; set; }

        public Product ParentProduct { get; set; }

        public long OptionId { get; set; }

        public ProductOption Option { get; set; }

        public long ValueId { get; set; }
        public ProductOptionValue Value { get; set; }
        public int SortIndex { get; set; }
       
    }
}
