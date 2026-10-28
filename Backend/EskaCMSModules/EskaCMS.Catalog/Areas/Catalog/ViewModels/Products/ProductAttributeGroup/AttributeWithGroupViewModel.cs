using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class AttributeWithGroupViewModel
    {
        public long? Id { get; set; }
        public EStatus Status { get; set; }
        public string Name { get; set; }
        public long CultureId { get; set; }
        public IList<ProductAttraibuteDetailsGroup> productAttribute { get; set; }
    }

    public class ProductAttraibuteDetailsGroup
    {
        public long? Id { get; set; }
        public long GroupId { get; set; }
        public bool IsVisible { get; set; }
        public long SiteId { get; set; }

        public EStatus Status { get; set; }

        public string DefaultGroupName { get; set; }
        public string Name { get;set; }
        public long CultureId { get;set; }

       
    }
}
