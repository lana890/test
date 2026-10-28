using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Security.Models
{

    public class OptionLevelViewModel
    {
        public CategoryAdminViewModel Level { get; set; }
        public List<OptionLevelViewModel> SubLevelList { get; set; }
        public List<OptionRequestViewModel> OptionsList { get; set; }
    }

    public class AddOptionViewModel
    {
        public long SiteId { get; set; }
        public long UserId { get; set; }
        public List<OptionRequestViewModel> lstOptions { get; set; }
    }


    public class OptionRequestViewModel
    {

        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(50)]
        public string Code { get; set; }
        public long CategoryId { get; set; }
        public EDefinitionType  Type { get; set; }
    }
}
