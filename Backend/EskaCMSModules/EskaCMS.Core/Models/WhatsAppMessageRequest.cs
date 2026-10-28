using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
    public class WhatsAppMessageRequest
    {
        [JsonProperty("reciverPhoneNumber")]
        public string ReciverPhoneNumber { get; set; }

        [JsonProperty("reciverName")]
        public string ReciverName { get; set; }

        [JsonProperty("mssageContent")]
        public string MssageContent { get; set; }

        [JsonProperty("isPDF")]
        public bool IsPDF { get; set; }

        [JsonProperty("linkPDF")]
        public string LinkPDF { get; set; }
    }
    public class InfoseedConfig
    {
        public string ApiUrl { get; set; } = "https://infoseedintegrationserverprod.azurewebsites.net/api/SendCampaign/SendMessage";
        public string Tenancy { get; set; } = "AlNejmahSweets";
        public string Username { get; set; }
        public string Password { get; set; }
        public string TemplateName { get; set; } = "text";
        public string DefaultRecipientName { get; set; }
    }
}
