using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ErrorLogVM
    {
        public string Module { get; set; }

        public string Scope { get; set; }
        public string exception { get; set; }
        public string Message { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
