using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemAttributeGroupFormVm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public long CultureId { get; set; }
        public List<CultureVM> CultureVM { get; set; }
        public long ParentId { get; set; }
    }
    public class ItemAttributeGroupVM
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public long ParentId { get; set; }
        public EStatus Status { get; set; }
    }
}
