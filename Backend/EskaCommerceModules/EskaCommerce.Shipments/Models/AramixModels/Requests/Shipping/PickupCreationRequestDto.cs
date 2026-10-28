using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping
{
    [XmlRoot(ElementName = "PickupCreationRequest", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]

    public class PickupCreationRequest
    {
        public ClientInfo ClientInfo { get; set; } = new ClientInfo();
        public Transaction Transaction { get; set; } = new Transaction();
        public Pickup Pickup { get; set; } = new Pickup();
    }

    public class Pickup
    {
        public PickupAddress PickupAddress { get; set; } = new PickupAddress();
        public PickupContact PickupContact { get; set; } = new PickupContact();
        public string PickupLocation { get; set; } = string.Empty;
        public DateTimeOffset PickupDate { get; set; } 
        public DateTimeOffset ReadyTime { get; set; } 
        public DateTimeOffset LastPickupTime { get; set; }
        public DateTimeOffset ClosingTime { get; set; }
        public string Comments { get; set; } = string.Empty;
        public string Reference1 { get; set; } = string.Empty;
        public string Reference2 { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public PickupItems PickupItems{ get; set; }= new PickupItems();
        public string Status { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
    }

    public class PickupAddress
    {
        public string Line1 { get; set; } = string.Empty;
        public string Line2 { get; set; } = string.Empty;
        public string Line3 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StateOrProvinceCode { get; set; } = string.Empty;
        public string PostCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public double Longitude { get; set; } 
        public double Latitude { get; set; }
    }

    public class PickupContact
    {
        public string Department { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PhoneNumber1 { get; set; } = string.Empty;
        public string PhoneNumber1Ext { get; set; } = string.Empty;
        public string PhoneNumber2 { get; set; } = string.Empty;
        public string PhoneNumber2Ext { get; set; } = string.Empty;
        public string FaxNumber { get; set; } = string.Empty;
        public string CellPhone { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class PickupItems
    {
        public PickupItemDetail PickupItemDetail { get; set; } = new PickupItemDetail();
    }

    public class PickupItemDetail
    {
        public string ProductGroup { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int NumberOfShipments { get; set; }
        public string PackageType { get; set; } = string.Empty;
        public string Payment { get; set; } = string.Empty;
        public ShipmentWeight ShipmentWeight { get; set; } = new ShipmentWeight();
        public ShipmentVolume ShipmentVolume { get; set; } = new ShipmentVolume();
        public double NumberOfPieces { get; set; } 
        public Money CashAmount { get; set; } = new Money();
        public Money ExtraCharges { get; set; } = new Money();
        public ShipmentDimensions ShipmentDimensions { get; set; } = new ShipmentDimensions();
        public string Comments { get; set; } = string.Empty;
    }

    public class ShipmentWeight
    {
        public string Unit { get; set; } = string.Empty;
        public double Value { get; set; } 
    }

    public class ShipmentVolume
    {
        public string Unit { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class Money
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class ShipmentDimensions
    {
        public double Length { get; set; } = 0;
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;
        public string Unit { get; set; } ="";
    }


    public class RequestPickupAdmin
    {
        public DateTimeOffset PickupDate
        {
            get;
            set;
        }
        public DateTimeOffset ReadyTime
        {
            get;
            set;
           
        }
        public DateTimeOffset LastPickupTime
        {
            get;
            set;
        }

        public DateTimeOffset ClosingTime
        {
            get;
            set;
        }

        public List<long> ShipmentIds { get; set; }
    }
}