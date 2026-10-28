using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Zoho.Models
{
    public class AccessTokenVM
    {
        public string BaseURL { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string redirect_uri { get; set; }
        public string grant_type { get; set; }

    }
    public class AccessTokenResponseVM
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string api_domain { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }

    }
}
