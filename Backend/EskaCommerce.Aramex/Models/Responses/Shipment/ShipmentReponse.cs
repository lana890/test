using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCommerce.Aramex.Models.Responses.RateCalculate;

namespace EskaCommerce.Aramex.Models.Responses.Shipment
{

    public class AramexResponseResponse
    {
        public TransactionResponse? Transaction { get; set; }
        public IList<NotificationsErrors> Notifications { get; set; } = new List<NotificationsErrors>();
        public bool? HasErrors { get; set; }
        public List<ShipmentResponse>? Shipments { get; set; }
    }

    public class TransactionResponse
    {
        public string? Reference1 { get; set; }
        public string? Reference2 { get; set; }
        public string? Reference3 { get; set; }
        public string? Reference4 { get; set; }
        public string? Reference5 { get; set; }
    }

    public class ShipmentResponse
    {
        public string? ID { get; set; }
        public string? Reference1 { get; set; }
        public string? Reference2 { get; set; }
        public string? Reference3 { get; set; }
        public string? ForeignHAWB { get; set; }
        public bool? HasErrors { get; set; }
        public List<object>? Notifications { get; set; }
        public ShipmentLabelResponse? ShipmentLabel { get; set; }
        public ShipmentDetailsResponse? ShipmentDetails { get; set; }
        public List<ShipmentAttachmentResponse>? ShipmentAttachments { get; set; }
    }

    public class ShipmentLabelResponse
    {
        public string? LabelURL { get; set; }
        public List<object>? LabelFileContents { get; set; }
    }

    public class ShipmentDetailsResponse
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public WeightResponse? ChargeableWeight { get; set; }
        public string? DescriptionOfGoods { get; set; }
        public string? GoodsOriginCountry { get; set; }
        public int? NumberOfPieces { get; set; }
        public string? ProductGroup { get; set; }
        public string? ProductType { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentOptions { get; set; }
        public MoneyResponse? CustomsValueAmount { get; set; }
        public MoneyResponse? CashOnDeliveryAmount { get; set; }
        public MoneyResponse? InsuranceAmount { get; set; }
        public MoneyResponse? CashAdditionalAmount { get; set; }
        public MoneyResponse? CollectAmount { get; set; }
        public string? Services { get; set; }
        public string? OriginCity { get; set; }
        public string? DestinationCity { get; set; }
    }

    public class WeightResponse
    {
        public string? Unit { get; set; }
        public double? Value { get; set; }
    }

    public class MoneyResponse
    {
        public string? CurrencyCode { get; set; }
        public double? Value { get; set; }
    }

    public class ShipmentAttachmentResponse
    {
        public string? Name { get; set; }
        public int? Type { get; set; }
        public string? Url { get; set; }
    }


}
