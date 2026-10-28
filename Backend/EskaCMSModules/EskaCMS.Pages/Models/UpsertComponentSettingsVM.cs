using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class UpsertComponentSettingsVM
    {
        public bool LazyUi { get; set; }
        public string  ComponentName { get; set; }

        public string Settings { get; set; }
        public string ComponentSettingsId { get; set; }
        public string? Alias { get; set; }
        public long PageVersionId { get; set; }
       

    }
}
