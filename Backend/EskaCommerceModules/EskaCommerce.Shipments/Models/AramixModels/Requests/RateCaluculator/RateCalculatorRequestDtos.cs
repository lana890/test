using EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Models.AramixModels.Requests.RateCaluculator
{
    public class RateCalculatorRequestDtos
    {
        public ClientInfo ClientInfo { get; set; } = new ClientInfo();
        public string PreferredCurrencyCode { get;set;}= "";
        public Transaction Transaction { get; set; } = new Transaction();
        public Address OriginAddress { get; set; } = new Address();
        public Address DestinationAddress { get; set; }=new Address();
        public Details ShipmentDetails { get; set; } = new Details();



    }

    public class Address
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string City { get; set; }
        public string StateOrProvinceCode { get; set; }
        public string PostCode { get; set; }
        public string CountryCode { get; set; }
    
    }
}
