using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM
{
    public class TrafficDeviceVM
    {
        public EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource channel { get; set; }
        public double AbondmentRate { get; set; }
    }
}
