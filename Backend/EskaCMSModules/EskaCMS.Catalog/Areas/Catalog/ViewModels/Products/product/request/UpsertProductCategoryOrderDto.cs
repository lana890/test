using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request
{
    public class UpsertProductCategoryOrderDto
    {
        public long CategoryId { get; set; }
        public List<ProductCategoryOrder> ProductOrders { get; set; }
    }
}
