using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class GoogleLoginViewModel
    {
        public string credential { get; set; }
        [Required]
        public long CustomerGroupId { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}
