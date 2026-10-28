using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts.ECommerce
{
    public static class StockQuentityView
    {
        //public static string stockquentityview = @"
        //    Create VIEW productquantityviews As 

        //    SELECT Prod.Id As Id,(stock.Quantity-  (orditem.Quantity)) As StockQuentity

        //    FROM   orders_order As ord

        //    inner Join orders_orderitem As orditem On orditem.OrderId=ord.Id 

        //    inner Join catalog_product As Prod On Prod.Id In (orditem.ProductId) and Prod.IsProductSerial

        //    inner Join inventory_stock As stock On stock.ProductId in (Prod.Id)

        //    where ord.OrderStatus=30 or ord.OrderStatus=20

        //    group by Prod.Id;";

        public static string stockquentityview = @"Create VIEW productquantityviews As select `product`.`Id` AS `Id`,ifnull((`stock`.`Quantity` - (select count(`ps`.`Id`) from (`catalog_productserial` `ps` join `orders_order` `o` on((`o`.`Id` = `ps`.`OrderId`))) where (((`o`.`OrderStatus` = 30) or (`o`.`OrderStatus` = 20) or (`o`.`OrderStatus` = 31)) and (`ps`.`ProductId` = `product`.`Id`)))),0) AS `StockQuentity` from (`catalog_product` `product` left join `inventory_stock` `stock` on(((`product`.`StockTrackingIsEnabled` = 1) and (`stock`.`ProductId` = `product`.`Id`)))) where (`product`.`Status` = 2);";
    }
}
