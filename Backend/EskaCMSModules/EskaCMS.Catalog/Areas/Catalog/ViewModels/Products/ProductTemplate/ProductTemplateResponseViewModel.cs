using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductTemplateResponseViewModel
    {
        public long Id { get; set; }
        public EStatus Status { get; set; }

        public IList<ProductTemplateDetailsResponseViewModel>  productTemplateDetails { get; set; }
    }

    public class ProductTemplateDetailsResponseViewModel
    {
        public long CultureId { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public string CultureCode { get; set; }
        public long TemplateId { get; set; }
    }
}
