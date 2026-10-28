using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductFeesVM
    {
        public string Name { get; set; }
        public DiscountTypes FeesType { get; set; }
        public decimal FeesValue { get; set; }
        public long ProductId { get; set; }
        public long Id { get; set; }
        public string Note { get; set; }
        public bool Refundable { get; set; }
        public string FeesValueString { get; set; }
    }
    public class ProductFeesModel
    {
        public long ProductId { get; set; }
        public List<ProductsFees> productFeesVMs { get; set; }
    }
    public class ProductsFees
    {
        public string Name { get; set; }
        public DiscountTypes FeesType { get; set; }
        public decimal FeesValue { get; set; }
        public string Note { get; set; }
        public bool Refundable { get; set; }
        public long Id { get; set; }

    }

}
