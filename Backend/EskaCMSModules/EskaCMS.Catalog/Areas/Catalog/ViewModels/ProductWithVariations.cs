
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductWithVariationsVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<VariationsViewModel> Variations { get; set; }

    }
}
