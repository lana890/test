using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemWidgetSetting
    {
        public int NumberOfItems { get; set; }

        public long? CategoryId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemWidgetOrderBy OrderBy { get; set; }

        public bool FeaturedOnly { get; set; }
    }
}
