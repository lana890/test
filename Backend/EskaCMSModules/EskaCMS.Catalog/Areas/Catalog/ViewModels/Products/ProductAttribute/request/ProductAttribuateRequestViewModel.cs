using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductAttribuateRequestViewModel
    {
        public long? Id { get; set; }
        public long GroupId { get; set; }
        public bool IsVisible { get; set; }
        public EStatus Status { get; set; }

        public IList<ProductAttributeDetailsViewModel> productAttributeDetailsViewModels { get; set; }

    }
    public class ProductAttribuateResponseViewModel
    {
        public long? Id { get; set; }
        public long GroupId { get; set; }
        public bool IsVisible { get; set; }
        public EStatus Status { get; set; }

        public string Name { get; set; }
        public long? CultureId { get; set; }
    }
    public class ProductAttributeDetailsViewModel
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public long CultureId { get; set; }
    }
    public class GetGroupsFlat
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public long CultureId { get; set; }
    }
}
