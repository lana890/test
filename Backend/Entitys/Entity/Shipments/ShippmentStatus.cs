using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Shipments
{
    public enum ShippmentStatus
    {

        Preparing = 1,
        Ready=2,
        Pickuped = 3,
        delivered=4,
        Cancelled=5,
        Failed=6

    }
}
