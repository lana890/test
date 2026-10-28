using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace EskaCommerce.DomesticShipment.SimsCreation.Dtos
{
    using Newtonsoft.Json;
    using System;

    public class OrderResponseViewModel
    {
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("data")]
        public OrderData Data { get; set; }
    }

    public class OrderData
    {
        [JsonProperty("service_providersId")]
        public int ServiceProvidersId { get; set; }

        [JsonProperty("location_id")]
        public int LocationId { get; set; }

        [JsonProperty("client_name")]
        public string ClientName { get; set; }

        [JsonProperty("client_number")]
        public string ClientNumber { get; set; }

        [JsonProperty("client_address")]
        public string ClientAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("area")]
        public string Area { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("building_no")]
        public string BuildingNo { get; set; }

        [JsonProperty("dates")]
        public DateTime Dates { get; set; }

        [JsonProperty("days")]
        public object Days { get; set; }

        [JsonProperty("timeId")]
        public int TimeId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("payment")]
        public string Payment { get; set; }

        [JsonProperty("delivery_fees")]
        public decimal DeliveryFees { get; set; }

        [JsonProperty("est_time")]
        public object EstTime { get; set; }

        [JsonProperty("times")]
        public object Times { get; set; }

        [JsonProperty("tag")]
        public object Tag { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("line_items")]
        public object LineItems { get; set; }

        [JsonProperty("u_id")]
        public int UId { get; set; }

        [JsonProperty("tenant_id")]
        public int TenantId { get; set; }

        [JsonProperty("orders_no")]
        public string OrdersNo { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }
    }

    public class Location
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tax_rate")]
        public string TaxRate { get; set; }

        [JsonProperty("tax_free")]
        public int TaxFree { get; set; }

        [JsonProperty("description")]
        public object Description { get; set; }

        [JsonProperty("active")]
        public int Active { get; set; }

        [JsonProperty("tenant_id")]
        public int TenantId { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("deleted_at")]
        public object DeletedAt { get; set; }

        [JsonProperty("u_id")]
        public int UId { get; set; }

        [JsonProperty("country_id")]
        public int CountryId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }
    }

    public class DeleteOrderViewModel
    {
        public string message { get; set; }

    }

}
