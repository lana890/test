using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace EskaCommerce.Aramex.Models.Requests.Shipping
{
    [XmlRoot(ElementName = "ShipmentCreationRequest", Namespace = "http://ws.aramex.net/ShippingAPI/v1/")]
    public class ShipmentCreationRequest
    {
        public ClientInfo ClientInfo { get; set; } = new ClientInfo();
        public Transaction Transaction { get; set; } = new Transaction();

        public List<Shipment> Shipments { get; set; } = new List<Shipment>();
        public LabelInfo LabelInfo { get; set; } = new LabelInfo();
    }

    public class ClientInfo
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Version { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountPin { get; set; } = "";
        public string AccountEntity { get; set; } = "";
        public string AccountCountryCode { get; set; } = "";
        public string Source { get; set; } = "";
    }

    public class Transaction
    {
        public string Reference1 { get; set; } = "";
        public string Reference2 { get; set; } = "";
        public string Reference3 { get; set; } = "";
        public string Reference4 { get; set; } = "";
        public string Reference5 { get; set; } = "";
    }

    public class Shipments
    {
        public Shipment Shipment { get; set; } = new Shipment();
    }

    public class Shipment
    {
        public string Reference1 { get; set; } = "";
        public string Reference2 { get; set; } = "";
        public string Reference3 { get; set; } = "";
        public Shipper Shipper { get; set; } = new Shipper();
        public Consignee Consignee { get; set; } = new Consignee();
        public ThirdParty ThirdParty { get; set; } = new ThirdParty();
        public DateTime ShippingDateTime { get; set; } = new DateTime();
        public DateTime DueDate { get; set; }
        public string Comments { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string OperationsInstructions { get; set; } = "";
        public string AccountingInstrcutions { get; set; } = "";
        public Details Details { get; set; } = new Details();
        public List<object> Attachments { get; set; } = new List<object>();
        public string ForeignHAWB { get; set; } = "";
        public int TransportType { get; set; }
        public string PickupGUID { get; set; } = "";
        public string Number { get; set; } = "";
    }

    public class Shipper
    {
        public string Reference1 { get; set; } = "";
        public string Reference2 { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public Party PartyAddress { get; set; } = new Party();
        public Contact Contact { get; set; } = new Contact();
    }
    public class Party
    {
        public string Line1 { get; set; } = "";
        public string Line2 { get; set; } = "";
        public string Line3 { get; set; } = "";
        public string City { get; set; } = "";
        public string StateOrProvinceCode { get; set; } = "";
        public string PostCode { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
        public class PartyAddress
    {
        public string Line1 { get; set; } = "";
        public string Line2 { get; set; } = "";
        public string Line3 { get; set; } = "";
        public string City { get; set; } = "";
        public string StateOrProvinceCode { get; set; } = "";
        public string PostCode { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string BuildingNumber { get; set; } = "";
        public string BuildingName { get; set; } = "";
        public string Floor { get; set; } = "";
        public string Apartment { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class Contact
    {
        public string Department { get; set; } = "";
        public string PersonName { get; set; } = "";
        public string Title { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string PhoneNumber1 { get; set; } = "";
        public string PhoneNumber1Ext { get; set; } = "";
        public string PhoneNumber2 { get; set; } = "";
        public string PhoneNumber2Ext { get; set; } = "";
        public string FaxNumber { get; set; } = "";
        public string CellPhone { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string Type { get; set; } = "";
    }

    public class Consignee
    {
        public string Reference1 { get; set; } = "";
        public string Reference2 { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public PartyAddress PartyAddress { get; set; } = new PartyAddress();
        public Contact Contact { get; set; } = new Contact();
    }

    public class ThirdParty
    {
        public string Reference1 { get; set; } = "";
        public string Reference2 { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public Party PartyAddress { get; set; } = new Party();
        public Contact Contact { get; set; } = new Contact();
    }

    public class Details
    {
        public Dimensions Dimensions { get; set; } = new Dimensions();
        public ActualWeight ActualWeight { get; set; } = new ActualWeight();
        public ChargeableWeight ChargeableWeight { get; set; } = new ChargeableWeight();
        public string DescriptionOfGoods { get; set; } = "";
        public string GoodsOriginCountry { get; set; } = "";
        public int NumberOfPieces { get; set; }
        public string ProductGroup { get; set; } = "";
        public string ProductType { get; set; } = "";
        public string PaymentType { get; set; } = "";
        public string PaymentOptions { get; set; } = "";
        public CustomsValueAmount CustomsValueAmount { get; set; } = new CustomsValueAmount();
        public CashOnDeliveryAmount CashOnDeliveryAmount { get; set; } = new CashOnDeliveryAmount();
        public string CashAdditionalAmountDescription { get; set; } = "";
        public string Services { get; set; }="";
        public List<object> Items { get; set; } = new List<object>();
    }

    public class Dimensions
    {
        public double Length { get; set; } = 0;
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;
        public string Unit { get; set; } = "CM";
    }

    public class ActualWeight
    {
        public string Unit { get; set; } = "";
        public double Value { get; set; }
    }

    public class ChargeableWeight
    {
        public string Unit { get; set; } = "";
        public double Value { get; set; }
    }

    public class CustomsValueAmount
    {
        public string CurrencyCode { get; set; } = "";
        public double Value { get; set; }
    }

    public class CashOnDeliveryAmount
    {
        public string CurrencyCode { get; set; } = "";
        public double Value { get; set; }
    }

    public class LabelInfo
    {
        public int ReportID { get; set; }
        public string ReportType { get; set; } = "";
    }
}
