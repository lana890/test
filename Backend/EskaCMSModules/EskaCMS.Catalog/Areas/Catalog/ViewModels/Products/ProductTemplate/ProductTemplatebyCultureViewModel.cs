using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductTemplatebyCultureViewModel
    {
        public long CultureId { get; set; }
        public string Name { get; set; }

        public EStatus estatus { get; set; }
        public long Id { get; set; }
        public IList<TemplateAttributeDetailsViewModel> templateAttributes { get; set; }

    }
}
