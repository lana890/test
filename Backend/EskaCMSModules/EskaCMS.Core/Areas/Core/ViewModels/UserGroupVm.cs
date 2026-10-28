using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class UserGroupVm
    {
        public long GroupId { get; set; }
        public string EncryptedUserId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }

        public string Image { get; set; }
        public string Email { get; set; }
    }
}
