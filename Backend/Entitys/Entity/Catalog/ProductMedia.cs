
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;


namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductMedia : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long MediaId { get; set; }

        public Media Media { get; set; }

        public int DisplayOrder { get; set; }
        
        

    }
}
