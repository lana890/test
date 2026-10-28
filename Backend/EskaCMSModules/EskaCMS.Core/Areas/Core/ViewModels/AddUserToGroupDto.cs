using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class AddUserToGroupDto
    {
        [Required]
        public long CustomerGroupId { get; set; }
        [Required]
        public List<long> UserIds { get; set; }
    }
}
