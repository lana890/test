using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class VariationRequestViewModel
    {
        public long ProductId { get; set; }
        public List<VariationsViewModel> Variations { get; set; }
     
    }
}
