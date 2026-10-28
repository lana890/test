using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Request
{
    public class CreateWorkFlowItemDto
    {

        public long WorkFlowId { get; set; }
        public long? UserId { get; set; }

        public long? SubmittedFormId { get; set; }
        
        public long? PageVersionId { get; set; }
    
        public long? TemplateVersionId { get; set; }
        public long? MediaId { get; set; }
       
    }
}
