using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.BusinessModels
{
    public class GetUsersWithOptionsVM
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public UserOptionsVM UserOption { get; set; }
    }

    public class GetUserOptionByPropertyVM
    {
      
        public string PropertyName { get; set; }
      
    }
}
