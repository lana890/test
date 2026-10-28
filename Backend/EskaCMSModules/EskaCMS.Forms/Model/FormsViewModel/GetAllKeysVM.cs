using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class GetAllKeysVM
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string FormName { get; set; }
        public string ComponentSettingsId { get; set; }
    }
}
