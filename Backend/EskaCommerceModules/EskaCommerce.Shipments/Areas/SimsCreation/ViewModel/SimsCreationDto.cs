using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.SimsCreation.Dtos
{
    public class SimsCreationDto
    {
        public int service_providersId { get; set; }
        public int location_id { get; set; }
        public string client_name { get; set; }
        public string client_number { get; set; }
        public string client_address { get; set; }
        public string city { get; set; }
        public string area { get; set; }
        public string street { get; set; }
        public string building_no { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string dates { get; set; }
        public string days { get; set; }
        public int timeId { get; set; }
        public decimal amount { get; set; }
        public string payment { get; set; }
        public decimal delivery_fees { get; set; }
        public string est_time { get; set; }
        public string times { get; set; }
        public string tag { get; set; }
        public string status { get; set; }
    }

    public class SimsLoginResponseViewModel
    {
        public bool result { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public DateTime? expires_at { get; set; }
        public SimsUserViewModel user { get; set; }
    }

    public class SimsUserViewModel
    {
        public int id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public int location_id { get; set; }
    }
    public class SimsConfigurationViewModel
    {
        public string email { get; set; }
        public string password { get; set; }
        public string BaseUrl { get; set; }

    }
    public class Simsloginviewmodel {

        public string email { get; set; }
        public string password { get; set; }
    }

}
