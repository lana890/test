using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request
{
    public class ProductCategoryOrder
    {
        public long ProductId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
