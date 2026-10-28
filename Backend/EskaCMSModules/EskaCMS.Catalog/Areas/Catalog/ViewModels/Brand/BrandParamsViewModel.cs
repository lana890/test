using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm
{
    public class BrandParamsViewModel
    {

      
        public string SearchText { get; set; }

        public long[]? BrandIds { get; set; }

        public pagination pagination { get; set; }
    }

    public class pagination
    {

        public int PageSize { get; set; } 
        public int PageNumber { get; set; } 

    }
}
