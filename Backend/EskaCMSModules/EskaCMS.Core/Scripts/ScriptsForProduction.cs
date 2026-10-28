using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public  class ScriptsForProduction
    {
        public static string DeleteOrderHistory = @"DELETE FROM orders_orderhistory;";
        public static string DeleteOrderItems = @"DELETE FROM orders_orderitem;";
        public static string DeleteTranscationHistory = @"DELETE FROM telrpayment_transactionhistory;";
        public static string DeletePaymentReFund = @"DELETE FROM payments_paymentrefund;";
        public static string DeletePayment = @"DELETE FROM payments_payment;";
        public static string DeleteProductSerialHistory = @"DELETE FROM catalog_productserialhistory;";
        public static string DeleteProductSerial = @"DELETE FROM catalog_productserial;";
        public static string DeleteOrder = @"DELETE FROM orders_order;";
        public static string DeleteCartItem = @"DELETE FROM shoppingcart_cartitem;";
        public static string DeleteCart = @"DELETE FROM shoppingcart_cart;";
        public static string DeleteOrderAddress = @"DELETE FROM orders_orderaddress;";
        public static string DeleteUserNotification = @"DELETE FROM notifications_usernotification;";
        public static string DeleteNotificationDetails = @"DELETE FROM notifications_notificationdetail;";
        public static string DeleteNotificationSchema = @"DELETE FROM notifications_notificationscheme;";
    }
}
