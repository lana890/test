using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.Response
{
    public class ProductSmartTableVm
    {
        public long Id { get;  set; }
        public string ProductName { get;  set; }
        public bool IsFeaturedProduct { get;  set; }
        public int DisplayOrder { get;  set; }
    }
}
