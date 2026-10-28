using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Search.Areas.Search.ViewModels
{
    public class AddUserQueryVM
    {
        
        public string Api { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string PageName { get; set; }
        [Required]
        public string Payload { get; set; }
        public bool? IsShared { get; set; }
    }
}
