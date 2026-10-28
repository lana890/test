using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductDetailsResponseViewModel
    {
        public long? Id { get; set; }
        public string NormalizedName { get; set; }// c
        public string ShortDescription { get; set; }// c

        public string Description { get; set; }// c

        public string Specification { get; set; } // ?

        public string Name { get; set; }


        public long? ProductTemplateId { get; set; }


        public string Name2 { get; set; }

     
        public string Slug { get; set; }

        public string CultureCode { get; set; }

        public long CultureId { get; set; }


        public long ProductId { get; set; }
        public string AdditonalMetaTags { get;  set; }
    }
}
