using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EskaCommerce.Aramex.Models.Requests.Shipping
{
    [XmlRoot(ElementName = "PickupCancelationRequest", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]

    public class PickupCancelationRequestDto
    {
        public ClientInfo ClientInfo { get; set; } = new ClientInfo();
        public Transaction Transaction { get; set; } = new Transaction();
        public string PickupGUID { get; set; } = "";
        public string Comments { get; set; } = "";
    }

}
