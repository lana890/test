using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EskaCommerce.Aramex.Models.Responses.RateCalculate;

namespace EskaCommerce.Aramex.Models.Responses.Pickup
{
    public class PickupeCancellationResponse
    {

        public Transaction Transaction { get; set; }

        public List<NotificationsErrors> Notifications { get; set; } = new List<NotificationsErrors>();

        public bool HasErrors { get; set; }

        public string Message { get; set; }

    }
}
