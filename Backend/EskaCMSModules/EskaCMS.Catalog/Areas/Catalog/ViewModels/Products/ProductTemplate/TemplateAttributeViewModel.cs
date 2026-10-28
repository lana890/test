using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class TemplateAttributeViewModel
    {
        public long? Id { get; set; }

        public IList<TemplateDetails> templateDetails { get; set; }
        public EStatus Status { get; set; }
        public IList<TemplateAttributeDetailsViewModel> templateAttributes { get; set; }
    }

    public class TemplateAttributeDetailsViewModel
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public long GroupId { get; set; }


    }

    public class TemplateDetails
    {


        public long CultureId { get; set; }
        public string Name { get; set; }
    }
}
