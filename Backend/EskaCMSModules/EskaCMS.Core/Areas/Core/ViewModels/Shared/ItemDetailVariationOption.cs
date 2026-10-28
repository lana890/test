using System.Collections.Generic;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemDetailVariationOption
    {
        public long OptionId { get; set; }

        public string OptionName { get; set; }

        public long ValueId { get; set; }
        public string Value { get; set; }
        public int SortIndex { get; set; }
    }

    public class ItemDetailVariationOptionAdmin
    {
        public long OptionId { get; set; }

        public List<string> OptionName { get; set; }

        public long ValueId { get; set; }
        public List<string> Value { get; set; }
    }
}
