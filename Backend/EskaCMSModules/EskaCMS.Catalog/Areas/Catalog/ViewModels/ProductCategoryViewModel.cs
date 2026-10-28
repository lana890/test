using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductCategoryViewModel
    {
        public long ProductId { get; set; }
        public long[] CategoriesId { get; set; }
    }
}
