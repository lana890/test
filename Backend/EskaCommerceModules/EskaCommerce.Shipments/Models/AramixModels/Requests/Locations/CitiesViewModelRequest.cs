using EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Models.AramixModels.Requests.Locations
{
    public class CitiesViewModelRequest
    {
        public ClientInfo  ClientInfo { get; set; }

        public string CountryCode { get; set; }
    }
}
