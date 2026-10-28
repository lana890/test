using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using System.Collections.Generic;

namespace EskaCommerce.ProductComparison.Areas.ProductComparison.ViewModels
{
    public class ComparingProductVm
    {
        public long ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }
        public string ShortDescription { get; set; }

        public string ProductImage { get; set; }

        public string Slug { get; set; }

        public CalculatedItemPrice CalculatedProductPrice { get; set; }

        public IList<AttributeValueVm> Attribute { get; set; } = new List<AttributeValueVm>();

    }
}
