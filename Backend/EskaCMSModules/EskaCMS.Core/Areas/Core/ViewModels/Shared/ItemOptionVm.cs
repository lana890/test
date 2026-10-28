using System.Collections.Generic;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemOptionVm
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string DisplayType { get; set; }

        public IList<ItemOptionValueVm> Values { get; set; } = new List<ItemOptionValueVm>();
    }
}
