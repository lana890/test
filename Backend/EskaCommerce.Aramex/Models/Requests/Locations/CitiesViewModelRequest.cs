using EskaCommerce.Aramex.Models.Requests.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Models.Requests.Locations
{
    public class CitiesViewModelRequest
    {
        public ClientInfo  ClientInfo { get; set; }

        public string CountryCode { get; set; }
    }
}
