using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Forms.Enums.SubmittedFormStatus;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class SubmittedFormByCodeVM
    {
        public string Code { get; set; } 
       public Dictionary<string, string> UserOptions { get; set; }

        public ESubmittedFormStatus? Status { get; set; } = ESubmittedFormStatus.InProgress;

        public int? PageSize { get; set; } = 10;
        public int? PageIndex { get; set; } = 0;

    }
}
