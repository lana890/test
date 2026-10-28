using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{

    public class ProductSerialSearchSortVM
    {
        public ProductSerialSearchSortFieldsEnum SortField { get; set; }
        public ESortType SortType { get; set; }

    }

}
