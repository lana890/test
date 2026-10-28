using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class MappingProductVM
    {

        public long AttributeId { get; set; }
        public string AttributeValue { get; set; } 
        public string ProductName { get; set; }
        public long? ProductTemplateId { get; set; }
    }
}
