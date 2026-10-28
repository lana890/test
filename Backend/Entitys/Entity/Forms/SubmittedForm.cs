using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.WorkFlow;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.Entities.Entity.Forms
{
    public class SubmittedForm : EntityBase
    {
        public string SubmittedValue { get; set; }
        public string Subject { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Code { get; set; }
        public  long  CreatedById { get; set; }
        public  User  CreatedBy { get; set; }
        public long?  FormId { get; set; }
        public Forms Form { get; set; }
        
    
        public WorkFlowItem WorkFlowItem { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }

        public List<SubmittedFormMedia> Medias { get; set; }
    }
}
