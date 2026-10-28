using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class OptionViewModel
    {
        public long? Id { get; set; }
        public long? SiteId { get; set; }

        public IList<OptionDetailsViewModel>  optionDetails { get; set; }
    }


    public class OptionDetailsViewModel
    {

        public string Name { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public long? Id { get; set; }

    }


   
    



}
