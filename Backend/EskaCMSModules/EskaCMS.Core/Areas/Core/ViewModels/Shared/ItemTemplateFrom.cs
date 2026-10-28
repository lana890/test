using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemTemplateFrom
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public long ParentId { get; set; }
        public long CultureId { get; set; }
        public EStatus Status { get; set; }
        public IList<ItemAttributeVm> Attributes { get; set; } = new List<ItemAttributeVm>();
    }


    public class ProductTemplateWithCultureVM
    {
        public List<ProductTemplateWithCultures> itemTemplateFroms { get; set; }
        public long CultureId { get; set; }
    }
    public class ProductTemplateWithCultures
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public long ParentId { get; set; }
        public List<ItemAttributeVm> Attributes { get; set; } = new List<ItemAttributeVm>();
        public List<TempCultureDetails> Cultures { get; set; }
        public EStatus Status { get; set; }


    }

    public class TemplateVM
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public List<TempCultureDetails> Cultures { get; set; }
        public EStatus Status { get; set; }

        public long ParentId { get; set; }
    }
    public class TempCultureDetails
    {
        public string Name { get; set; }
        public long TemplateId { get; set; }
        public long? ParentId { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public EStatus Status { get; set; }

    }
}
