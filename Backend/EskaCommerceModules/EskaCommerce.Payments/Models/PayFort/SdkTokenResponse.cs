#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class SdkTokenResponse
    {
        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("service_command")]
        public string ServiceCommand { get; set; }

        [JsonPropertyName("sdk_token")]
        public string SdkToken { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("merchant_identifier")]
        public string MerchantIdentifier { get; set; }

        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }


}

