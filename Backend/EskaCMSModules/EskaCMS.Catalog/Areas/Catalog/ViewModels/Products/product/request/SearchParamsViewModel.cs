using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class SearchParamsViewModel
    {

        public string? SearchText { get; set; }
        public long[]? ProductIds { get; set; }

        public List<long>? CategoryIds { get; set; }

        public PaginationViewModel pagination { get; set; }
    }

    public class PaginationViewModel
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }


    }
}
