using System.Collections.Generic;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class EskaeProductWidgetComponentVm
    {
        public long Id { get; set; }

        public string WidgetName { get; set; }

        public EskaeProductWidgetSetting Setting { get; set; }

        public IList<ProductThumbnail> Products { get; set; } = new List<ProductThumbnail>();
    }
}
