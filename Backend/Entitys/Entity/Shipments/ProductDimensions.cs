using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class ProductDimensions :EntityBase
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public long DimensionsId { get; set; }
        public Dimensions Dimensions { get; set; }
    }
}
