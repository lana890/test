using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class CategoryListVm
    {
    
        public long Id { get; set; }

        public CategoryDetailsListViewModel? CategoryDetails { get; set; }

    }
    public class CategoryDetailsListViewModel
    {

        public string Name { get; set; }
        public string? CultureCode { get; set; }
        public long? CultureId { get; set; }


    }
}
