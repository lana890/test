using Newtonsoft.Json;

namespace EskaCMS.Security.Models
{
    public class FaceBookInfoUserResult
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }

    }
}
