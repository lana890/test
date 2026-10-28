using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.SimsCreation.Dtos
{
 
    public class OrderUserViewModel
    {
        public int Id { get; set; }
        public int? OrdersId { get; set; }
        public int? DriverId { get; set; }
        public int ServiceProvidersId { get; set; }
        public int? ContactId { get; set; }
        public string ClientName { get; set; }
        public string ClientNumber { get; set; }
        public string ClientAddress { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string BuildingNo { get; set; }
        public DateTime? Dates { get; set; }
        public int? Days { get; set; }
        public int TimeId { get; set; }
        public decimal Amount { get; set; }
        public string Payment { get; set; }
        public decimal DeliveryFees { get; set; }
        public int? EstTime { get; set; }
        public string Times { get; set; }
        public string Tag { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int UId { get; set; }
        public int TenantId { get; set; }
        public int LocationId { get; set; }
        public List<LineItemViewModel> LineItems { get; set; }
        public string OrdersNo { get; set; }
        public double? Distance { get; set; }
        public int? OrdersCategoryId { get; set; }
        public int IsSchedule { get; set; }
    }

    public class LineItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductId { get; set; }
        public int VariationId { get; set; }
        public int Quantity { get; set; }
        public string TaxClass { get; set; }
        public decimal Subtotal { get; set; }
        public decimal SubtotalTax { get; set; }
        public decimal Total { get; set; }
        public decimal TotalTax { get; set; }
        public List<object> Taxes { get; set; } // You may replace object with an appropriate type if necessary
        public List<object> MetaData { get; set; } // You may replace object with an appropriate type if necessary
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public LineItemImageViewModel Image { get; set; }
        public string ParentName { get; set; }
    }

    public class LineItemImageViewModel
    {
        public string Id { get; set; }
        public string Src { get; set; }
    }
}
