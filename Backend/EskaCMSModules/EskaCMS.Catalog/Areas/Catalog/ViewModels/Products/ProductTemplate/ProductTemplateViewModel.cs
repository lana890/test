using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductTemplateViewModel
    {
        public long? Id { get; set; }
        public EStatus Status { get; set; }
        public List<long> AttributesItemId { get; set; } = new List<long>();

        public IList<ProductTemplateDetailsViewModel> productTemplateDetails { get; set; }
    }

    public class ProductTemplateDetailsViewModel
    {
        public long? Id { get; set; }

        public long CultureId { get; set; }
        public string Name { get; set; }



    }
}
