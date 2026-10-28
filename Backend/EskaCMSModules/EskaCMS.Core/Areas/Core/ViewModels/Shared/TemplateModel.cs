using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Shared
{
    public class TemplateModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public EStatus Status { get; set; }
    }
}
