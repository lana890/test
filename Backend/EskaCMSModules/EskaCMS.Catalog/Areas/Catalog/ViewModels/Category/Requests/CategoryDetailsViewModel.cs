using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.ViewModels.Category
{
    public class CategoryDetailsViewModel
    {

        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public string Slug { get; set; }
        public long?  SlugInfoId { get; set; }

        [StringLength(450, ErrorMessage = " Meta Title Length Must Be Under 450")]
        public string MetaTitle { get; set; }
        [StringLength(450, ErrorMessage = " Meta Keywords Length Must Be Under 450")]

        public string MetaKeywords { get; set; }
        [StringLength(450, ErrorMessage = " Meta Description Length Must Be Under 450")]

        public string MetaDescription { get; set; }

        public string Description { get; set; }
        public long? CategoryTemplateId { get; set; }
        public long? CultureId { get; set; }


    }
}
