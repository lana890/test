using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Discount
{
    public class DiscountProduct: EntityBase
    {
        public long DiscountId { get; set; }

        public Discount Discount { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

    }
}
