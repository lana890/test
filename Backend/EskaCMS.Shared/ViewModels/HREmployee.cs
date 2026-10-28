using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.ViewModels
{
    public class HREmployee
    {
        public int? ID { get; set; }
        public int? MANAGER_ID { get; set; }
        public string NAME { get; set; }
        public string MOBILE { get; set; }
        public string USERNAME { get; set; }

    }

     
}
