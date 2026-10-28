using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Customattributes;

namespace EskaCMS.Entities.Enum
{
    public enum DeliveryOptionEnum
    {
        [DeliveryDescription("None")]
        None = 0,

        [DeliveryDescription("Delivery")]
        Domestic = 1,

        [DeliveryDescription("Delivery")]
        Aramex = 2,

        [DeliveryDescription("Delivery")]
        Dhl = 3,

        [DeliveryDescription("Pickup")]
        Pickup = 4,

        [DeliveryDescription("Delivery")]
        TopDelivery = 5
    }

}
