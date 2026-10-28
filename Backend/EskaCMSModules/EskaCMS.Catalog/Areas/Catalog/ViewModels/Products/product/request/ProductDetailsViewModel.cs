using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductDetailsViewModel
    {
        public long? Id { get; set; }
        public string NormalizedName { get; set; }// c
        public string ShortDescription { get; set; }// c

        public string Description { get; set; }// c

        public string Specification { get; set; } // ?

        public string Name { get; set; }


        public long? ProductTemplateId { get; set; }


        public string Name2 { get; set; }

        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }




        public string Slug { get; set; }



        public long CultureId { get; set; }


        public long ProductId { get; set; }
        public string AdditonalMetaTags { get; set; }
    }

    public class ProductFiltrationViewModel
    {
        public bool? IsFeatured { get; set; }
        public bool? IsPromotional { get; set; }


        public PaginationViewModel Pagination { get; set; }
    }
}
