using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductLinkViewModel
    {
        public long Id { get; set; }

        public long LinkedProductId { get; set; }

        public EStatus Status { get; set; }
        public string Name { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }

    }

    public class ProductLinkDetailsViewModel
    {
        public long LinkedProductId  { get; set; }

        public EStatus Status { get; set; }
        public string Name { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }


    }

    public class ProductlikCultureDetailsViewModel
    {

        public string Name { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }

    }
}
