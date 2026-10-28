using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public  class FormBuilderVm
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Settings { get; set; }

        public string Keys { get; set; }
        public string ComponenetSettingsId { get; set; }
        public Entities.Enum.Source Source { get; set; }

        public long PageId { get; set; }
        public string AdditionalData { get; set; }

    }

}
