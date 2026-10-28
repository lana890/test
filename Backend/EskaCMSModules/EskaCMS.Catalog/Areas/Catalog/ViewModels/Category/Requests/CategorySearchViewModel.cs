using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests
{
    public class CategorySearchViewModel
    {
        public string Name { get; set; }
        public long? CultureId { get; set; }
        public long? CategoryTypedId { get; set; }
        public PaginationRequestViewModel Pagination { get; set; }
    }

    public class PaginationRequestViewModel
    {

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

    public class CategorySearchSiteViewModel
    {
        public string Name { get; set; }
        public long? CultureId { get; set; }
        public long[] CategoryTypedId { get; set; }
        public PaginationViewModel Pagination { get; set; }
    }

    public class CategoryFiltrationViewModel
    {
        public bool? IsFeatured { get; set; }
        public bool? IsPromotional { get; set; }


        public PaginationViewModel Pagination { get; set; }
    }
}
