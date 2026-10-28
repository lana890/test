using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts.ECommerce
{
    public static class ManageStockQueries
    {
        public static string AddToProduct_StockQuantity = @"UPDATE catalog_product SET StockQuantity = StockQuantity + {0}  WHERE Id = {1};";
        public static string SubtractFromProduct_StockQuantity = @"UPDATE catalog_product SET StockQuantity = StockQuantity - {0}  WHERE Id = {1};";
        public static string AddToInventoryStock_StockQuantity = @"update inventory_stock set Quantity = Quantity + {0} where ProductId =  {1};";
        public static string SubtractFromInventoryStock_StockQuantity = @"update inventory_stock set Quantity = Quantity - {0} where ProductId =  {1};";
        public static string AddToInventoryStock_StockHistory = @"INSERT INTO inventory_stockhistory (`ProductId`, `WarehouseId`, `CreatedOn`, `CreatedById`, `AdjustedQuantity`, `Note`)  VALUES ({0}, {1}, {2}, {3}, {4}, 'upload new serials');";
        public static string MakeProductSerialAvaliable = @"update catalog_productserial set Status = 1,OrderId = null where OrderId={0};";

    }
}
