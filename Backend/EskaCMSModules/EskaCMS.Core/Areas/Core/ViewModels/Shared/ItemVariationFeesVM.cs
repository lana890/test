using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Shared
{
    public class ItemVariationFeesVM
    {
        public string Name { get; set; }
        public string FeesValueString { get; set; }
        public DiscountTypes FeesType { get; set; }
        public decimal FeesValue { get; set; }
        public long ProductId { get; set; }
        public long Id { get; set; }
    }
}
