using System;
using System.Collections.Generic;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemListItem
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public bool HasOptions { get; set; }

        public bool IsVisibleIndividually { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public bool IsPublished { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int? StockQuantity { get; set; }
         public List<ItemListItem> Variations { get; set; } = new List<ItemListItem>();

    }
}
