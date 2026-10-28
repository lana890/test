using System.Collections;
using System.Collections.Generic;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemOptionCombinationVm
    {
        public long OptionId { get; set; }

        public string OptionName { get; set; }

        public long? ValueId { get; set; }
        public List<ProductValuesOptionsViewModel>? Values { get; set; } = new List<ProductValuesOptionsViewModel>();

        public int SortIndex { get; set; }
    }
    public class ProductValuesOptionsViewModel
    {
        public long ValueId { get; set; }
        public string ValueName { get; set; }
        public long? CultureId { get; set; }

    }
  

    }


