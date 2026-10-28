using EskaCommerce.Module.Core.Areas.Core.ViewModels;
using System.Collections.Generic;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class ProductWidgetComponentVm
    {
        public long Id { get; set; }

        public string WidgetName { get; set; }

        public ItemWidgetSetting Setting { get; set; }

        public IList<ProductThumbnail> Products { get; set; }
    }
}
