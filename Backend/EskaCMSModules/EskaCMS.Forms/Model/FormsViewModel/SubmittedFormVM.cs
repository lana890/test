using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class SubmittedFormVM
    {
        public long Id { get; set; }
        public string SumbittedValue { get; set; }
        public string Code { get; set; }
        public string Subject { get; set; }
        public long? FormId { get; set; }
        public long? WorkFlowId { get; set; }
        public long? FolderId { get; set; }

        public List<long> AttachmentIds { get; set; }

        public IFormFileCollection Attachments { get; set; }
    }
}
