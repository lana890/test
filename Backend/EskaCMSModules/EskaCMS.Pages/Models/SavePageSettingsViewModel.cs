using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Pages.Models
{
    public class SavePageSettingsViewModel
    {
        public long PageId { get; set; }
        public string Settings { get; set; }
        public string Alias { get; set; }
        public bool IsDraft { get; set; }
        public long UserId { get; set; }
        public List<string> PageComponents { get; set; }
    }
}
