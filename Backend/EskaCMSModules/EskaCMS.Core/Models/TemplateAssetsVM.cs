using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class TemplateAssetsVM
    {
        public long Id { get; set; }
        public long AssetsId { get; set; }
        public long PageTemplateId { get; set; }

        public EStatus status  { get; set; }

        public int Order { get; set; }
    }


    public class TemplateAssetsViewModel
    {

        public long Id { get; set; }

        public string AssetsName { get; set; }

        public string TemplateName { get; set; }

        public long TemplateId { get; set; }

        public long AssetsId { get; set; }

        public long Order { get; set; }
        public EStatus Status { get; set; }


    }
}
