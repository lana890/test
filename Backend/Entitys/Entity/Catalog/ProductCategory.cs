
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;



namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductCategory : EntityBase
    {
     //   public bool IsFeaturedProduct { get; set; }

        public int DisplayOrder { get; set; }

        public long CategoryId { get; set; }

        public long ProductId { get; set; }

        public Category Category { get; set; }

        public Product Product { get; set; }
    }
}
