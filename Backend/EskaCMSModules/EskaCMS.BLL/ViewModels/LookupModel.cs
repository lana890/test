using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.General.ViewModels
{
    public class LookupModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DefultValue { get; set; }
        public bool IsEditable { get; set; }
        public Entities.Enum.GeneralEnums.EStatus Estatus { get; set; }
        public bool EnableDataSecurity { get; set; }
        public long  Id { get; set; }
        //public long? SiteId { get; set; }

        public long CultureId { get; set; }
    }
}
