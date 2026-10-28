using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EskaCommerce.Shipments.Models.AramixModels.Responses.RateCalculate;

namespace EskaCommerce.Shipments.Models.AramixModels.Responses.LabelPrinting
{

    [XmlRoot("LabelPrintingResponse", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]
    public class LabelPrintingResponseViewModel
    {
        public Transaction Transaction { get; set; }
        public IList<NotificationsErrors> Notifications { get; set; } = new List<NotificationsErrors>();
        public bool HasErrors { get; set; }
        public string ShipmentNumber { get; set; }
        public ShipmentLabel ShipmentLabel { get; set; }
    }



    public class ShipmentLabel
    {
        public string LabelURL { get; set; }
        public string LabelFileContents { get; set; }
    }

}
