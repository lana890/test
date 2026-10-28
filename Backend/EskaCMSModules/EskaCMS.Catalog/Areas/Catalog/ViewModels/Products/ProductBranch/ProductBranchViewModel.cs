using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.ProductBranch
{
    public class ProductBranchViewModel
    {
        public long ProductId { get; set; }
        public long? WarehouseId { get; set; }
        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public int? Quantity { get; set; }
        public string Name { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
    }
    public class RequestProductBranchViewModel
    {
        public long MainProductId { get; set; }
        public List<ProductBranchViewModel>? productBranches { get; set; }

    }
    public class ProductBranchDataViewModel
    {
        public long ProductId { get; set; }
        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public int? Quantity { get; set; }
        public string Name { get; set; }
        public bool StockTrackingIsEnabled { get; set; }

    }

    public class ResultProductBranchViewModel
    {
        public long WarehouseId { get; set; }
        public EPriceType PriceType { get; set; }
        public List<ProductBranchDataViewModel>?  ProductsData { get; set; }

    }
}
