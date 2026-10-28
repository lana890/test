using System.Collections.Generic;

namespace EskaCMS.Module.Core.Areas.Core.ViewModels
{
    public class ItemLinkVm
    {
        public long Id { get; set; }
        public long LinkedProductId { get; set; }

    }
    public class RelatedProductVM
    {
        public List<ItemLinkVm> RelatedProducrList { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }             
    }
}
