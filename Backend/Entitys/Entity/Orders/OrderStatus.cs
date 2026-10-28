using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EskaCMS.Entities.Entity
{
    public enum OrderStatus
    {
        New = 1,


        [Description("On Hold")]

        OnHold = 10,

        [Description("Pending Payment")]

        PendingPayment = 20,

        [Description("Cash On Delivery")]

        CashOnDelivery = 21,

        [Description("Payment Received")]

        PaymentReceived = 30,

        [Description("Paid By Coupon")]

        PaidByCoupon = 31,

        [Description("Payment Failed")]
        PaymentFailed = 35,

        Invoiced = 40,

        Complete = 70,

        Canceled = 80,

        Refunded = 90,

        Closed = 100,

        [Description("Sadad Payment")]

        SadadPayment = 101
    }
    public enum OrderStatusAdmin
    {
        [Description("Cash On Delivery / الدفع عند الاستلام")]
        CashOnDelivery = 21,

        [Description("Payment Received / مدفوع")]
        PaymentReceived = 30,

        [Description("Paid By Coupon / تم الدفع بواسطة قسيمة شرائيه")]
        PaidByCoupon = 31,

        [Description("Payment Failed / فشل الدفع")]
        PaymentFailed = 35,

        [Description("Completed / مكتمل")]
        Complete = 70,

        [Description("Canceled / ملغى")]
        Canceled = 80,

        [Description("Refunded / تم استرداد المبلغ")]
        Refunded = 90,
    }

}
