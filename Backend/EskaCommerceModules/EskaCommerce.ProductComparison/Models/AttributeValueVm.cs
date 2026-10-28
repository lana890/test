using System.Collections.Generic;

namespace EskaCommerce.ProductComparison.Areas.ProductComparison.ViewModels
{
    public class AttributeValueVm
    {
        public long AttributeId { get; set; }

        public string Value { get; set; }
        public List<string> Names { get; set; }
    }
}
