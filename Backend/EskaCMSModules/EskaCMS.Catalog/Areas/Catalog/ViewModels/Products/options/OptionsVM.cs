using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductOptionViewModel
    {

        public long OptionId { get; set; }

        public string DisplayType { get; set; }
        public List<ProductoptionViewModelDetails> Values { get; set; } = new List<ProductoptionViewModelDetails>();

    }
    public class ProductoptionViewModelDetails
    {
        public long CultureId { get; set; }

        public string Value { get; set; }
    }


    public class ProductOptionWithOptionValueViewModel
    {

        public long optionId { get; set; }

        public List<ProductOptionNameViewModel>  productOptionNames { get; set; }

        public List<productOptionValueViewModel> productOptionValues { get; set; }



    }

    public class ProductOptionNameViewModel
    {
        public long CultureId { get; set; }
        public string Name { get; set; }

    }

    public class productOptionValueViewModel
    {
        public string Value { get; set; }
        public long CultureId { get; set; }

        public long ValueId { get; set; }

    }




}
