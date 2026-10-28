using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EskaCommerce.Shipments.Models.AramixModels.Responses.RateCalculate;

namespace EskaCommerce.Shipments.Models.AramixModels.Responses.Pickup
{
    [XmlRoot("PickupCreationResponse", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]

    public class PickupCreationResponse
    {
        public Transaction Transaction { get; set; }
        public List<NotificationsErrors> Notifications { get; set; } = new List<NotificationsErrors>();
        public bool HasErrors { get; set; }
        public ProcessedPickup ProcessedPickup { get; set; }
    }




    public class ProcessedPickup
    {
        public string ID { get; set; }
        public string GUID { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public object ProcessedShipments { get; set; }
    }

    public class PickupResponseSaveViewModel
    {
        public string ID { get; set; }
        public string GUID { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
    }
}
