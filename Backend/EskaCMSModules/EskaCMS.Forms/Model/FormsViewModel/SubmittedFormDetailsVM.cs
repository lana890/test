using EskaCMS.WorkFlow.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Forms.Enums.SubmittedFormStatus;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class SubmittedFormDetailsVM
    {
        public long Id { get; set; }
        public string SubmittedValue { get; set; }
        public string Subject { get; set; }
        public string Code { get; set; }
        public List<FormMedia> Attachments { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public ESubmittedFormStatus Status { get; set; }
        public BasicItemDetails WorkFlowItemDetails { get; set; }





    }
}
