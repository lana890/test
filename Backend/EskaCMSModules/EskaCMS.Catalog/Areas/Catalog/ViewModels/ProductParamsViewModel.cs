
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductParamswithCategoryIdVM
    {

        public long? CategoryId { get; set; }
        public bool? IsFeatuerItem { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ProductSearchSort Sort { get; set; }
    }

    public class ProductParamsViewModel
    {

        public long[]? CategoryId { get; set; }
        public bool? IsFeatuerItem { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ProductSearchSort Sort { get; set; }
    }
    public class ProductParamswithIdVM
    {
        public List<long> ProductIds { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ProductSearchSort Sort { get; set; }
    }
    public class ProductParamsModel
    {
        public long[]? CategoryId { get; set; }
        public bool? IsFeatuerItem { get; set; }
        public List<long> ProductIds { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ProductSearchSort Sort { get; set; }
    }

    public class ProductSearchSort
    {
        public int[] SortField { get; set; }

        public ESortType SortType { get; set; }
}

    public class ProductParamsVM
    {
       
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        
    }
    public class ProductOffersVM
    {

        public long[]? CategoryId { get; set; }
        public bool? IsFeatuerItem { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ProductSearchSort Sort { get; set; }
    }

}

