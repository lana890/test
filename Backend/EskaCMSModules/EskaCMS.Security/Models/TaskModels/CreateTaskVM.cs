using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.UserTaskEnums;

namespace EskaCMS.Security.Models.TaskModels
{
    public class CreateTaskVM
    {

        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? ExpectedStartDate { get; set; }
        public DateTimeOffset? ExpectedEndDate { get; set; }
        public long? ParentId { get; set; }
        public long? CategoryId { get; set; }
        //public int Order { get; set; }
        public ETaskPriority? Priority { get; set; } = ETaskPriority.Low;
        public ETaskStatus? Status { get; set; } = ETaskStatus.Proposed;
        public ETaskPrivacy? Privacy { get; set; } = ETaskPrivacy.Public;
    
        //public IFormFile? Attachment { get; set; }
    }
}
