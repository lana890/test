using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping
{
    [XmlRoot(ElementName = "LabelPrintingRequest", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]

    public class LabelPrintingRequestDto
    {
        public ClientInfo ClientInfo { get; set; }=new ClientInfo();
        public Transaction Transaction { get; set; }=new Transaction();
        public string ShipmentNumber { get; set; } = "";
        public string ProductGroup { get; set; } = "";
        public string OriginEntity { get; set; } = "";
        public LabelInfo LabelInfo { get; set; } = new LabelInfo();
    }
}
