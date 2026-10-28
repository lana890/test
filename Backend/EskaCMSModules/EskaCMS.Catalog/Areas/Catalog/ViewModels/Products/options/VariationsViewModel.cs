using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class VariationsViewModel
    {
        public long Id { get; set; }
        public IList<ItemOptionCombinationVm>? OptionCombinations { get; set; } =
      new List<ItemOptionCombinationVm>();

        public decimal? SpecialPrice { get; set; }
        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public decimal? SpecialForignPrice { get; set; }
        public int MaxAllowedQuantity { get; set; }
        public string ThumbnailImageUrl { get; set; }
        public string Sku { get; set; }
        public List<ProductAvaliablityRequest> ProductAvaliablity { get; set; }=new List<ProductAvaliablityRequest>();

        public string Gtin { get; set; }
        public long? ThumbnailImageId { get; set; }
        public IList<VariationsDetailsViewModel>  variationsDetails { get; set; }
        public EskaCMS.Core.Areas.Core.ViewModels.Shared.CalculatedItemPrice CalculatedItemPrice { get; set; }


    }
    public class VariationsDetailsViewModel
    {
        public long Id { get; set; }

        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public long? ProductTemplateId { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }

      



    }
}
