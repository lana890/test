using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemCloneFormVm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Slug { get; set; }
    }
}
