using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductAttribueVM
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The Attribute Group field is required.")]
        public long GroupId { get; set; }
        public string GroupName { get;set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public List<ProductAttribueVM> Cultures { get; set; }
        public bool IsVisible { get; set; }
        public long? ParentId { get; set; }
        public EStatus Status { get; set; }
    }
}
