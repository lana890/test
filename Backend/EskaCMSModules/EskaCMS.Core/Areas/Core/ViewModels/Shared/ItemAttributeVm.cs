namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemAttributeVm
    {
        public long Id { get; set; }

        public long AttributeValueId { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public string GroupName { get; set; }
        public long GroupId { get; set; }
    }
}
