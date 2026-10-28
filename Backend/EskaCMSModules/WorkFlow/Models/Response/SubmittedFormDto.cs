using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class SubmittedFormDto
    {
        public long Id { get; set; }
        public string SubmittedValue { get; set; }
        public string Subject { get; set; }
        public List<SubmittedFormMediaVM> Attachments { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string CreatedBy { get; set; }
     
    }
}
