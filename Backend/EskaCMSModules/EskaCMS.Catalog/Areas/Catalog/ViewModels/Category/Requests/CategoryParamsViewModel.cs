using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests
{
    public class CategoryParamsViewModel
    {
        public string SearchText { get; set; }
        public ECategoryTypes eCategoryTypes { get; set; }

        public PaginationViewModel Pagination { get; set; }
    }
}
