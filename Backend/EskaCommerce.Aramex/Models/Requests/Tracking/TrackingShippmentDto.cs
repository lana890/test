using EskaCommerce.Aramex.Models.Requests.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Models.Requests.Tracking
{
    public class TrackingShippmentDto
    {
        public ClientInfo ClientInfo { get; set; }
        public bool GetLastTrackingUpdateOnly { get; set; }
        public List<string> Shipments { get; set; }
        public Transaction Transaction { get; set; }
    }
}
