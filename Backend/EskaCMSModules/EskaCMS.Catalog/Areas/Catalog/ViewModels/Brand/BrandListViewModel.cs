using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm
{
    public class BrandListViewModel
    {

        public long Id { get; set; }
        public IList<BrandListDetailsViewModel> brandDetails { get; set; }
       
    }

    public class BrandListDetailsViewModel
    {
        public long Id { get; set; }
        public long BrandId { get; set; }
        public long CultureId { get; set; }
        public string Name { get; set; }
    }
    public class BrandForProductViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }

    }
}
