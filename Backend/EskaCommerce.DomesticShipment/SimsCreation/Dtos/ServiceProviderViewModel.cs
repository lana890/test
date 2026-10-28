using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.SimsCreation.Dtos
{
    public class ServiceProviderViewModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public object assigned_user_id { get; set; }
        public int location_id { get; set; }
        public string number { get; set; }
        public string address { get; set; }
        public string payment_method { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string userName { get; set; }
        public string delivery_fees { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
