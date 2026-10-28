
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class ProductsCategoryViewModel
    {
        public long CategoryId { get; set; }

        public long? ParentCategorId { get; set; }

        public string Categoryimage { get;  set; }

        public IList<CategoryDetailsAdminViewModel> CategoryDetails { get; set; }

    }
}
