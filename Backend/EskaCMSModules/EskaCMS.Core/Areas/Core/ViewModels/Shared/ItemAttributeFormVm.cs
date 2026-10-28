using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemAttributeFormVm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The Attribute Group field is required.")]
        public long GroupId { get; set; }

        public bool IsVisible { get; set; }
        public long CultureId { get; set;}
        public long ParentId { get; set; }
    }
}
