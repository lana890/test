using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Inventory.Areas.Inventory.ViewModels
{
    public class StockVm
    {
        public long ProductId { get; set; }

        public int AdjustedQuantity { get; set; }

        public string Note { get; set; }
    }


    public class GetProductsStockViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int AvaliableQuantity { get; set; }

        public List<GetWarehouseStock>? WarehouseStocks { get; set; }

    }
    public class AddEditProductStockViewModel
    {
        public long ProductId { get; set; }

        public List<AddEditWarehouseStockViewModel> Warehouses { get; set; }

    }
    public class StockStatusViewModel
    {
        public long StockId { get; set; }

        public EStatus Status { get; set; }
        public long warehouseId { get; set; }
    }
    public class GetWarehouseStock
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int Quantity { get; set; }
        public int AvaliableQuantity { get; set; }
    }

    public class AddEditWarehouseStockViewModel
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public int AdjustedQuantity { get; set; }
        public string Note { get; set; }
    }
    public class ObjWarehouseStockVM
    {
        public long WarehouseId { get; set; }
        public int AdjustedQuantity { get; set; }
        public string Note { get; set; }
        public long ProductId { get; set; }
    }
}
