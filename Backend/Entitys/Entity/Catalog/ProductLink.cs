

using EskaCMS.Infrastructure.Models;

using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCommerce.Infrastructure.Models;


namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductLink : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long LinkedProductId { get; set; }

        public Product LinkedProduct { get; set; }

        public ItemLinkType LinkType { get; set; }
    }
}
