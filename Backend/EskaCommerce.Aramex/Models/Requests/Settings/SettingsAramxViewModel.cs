using EskaCommerce.Aramex.Models.Requests.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Models.Requests.Settings
{
    public class SettingsAramxViewModel
    {
        public ClientInfo ClientInfo { get; set; }
        public string BaseUrlShipping { get; set; }
        public string BaseUrlTracking { get; set; }
        public string BaseUrlCalculateRate { get; set; }
        public string BaseUrlLocation { get; set; }

        public SettingShipment DomisticSettings { get; set; }
        public SettingShipment ExpressSettings { get; set; }
        public string ReportID { get; set; }

    }

}
