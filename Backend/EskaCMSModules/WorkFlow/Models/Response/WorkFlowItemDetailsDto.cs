using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class WorkFlowItemDetailsDto
    {
        public long Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        
        public BasicStageInfoDto CurrentStage { get; set; }

        public string CreatedBy { get; set; }

        public EFlowStatus Status { get; set; }

        public string Code { get; set; }

        public SubmittedFormDto SubmittedForm { get; set; }
        public AttachmentDto Attachment { get; set; }

        public List<ItemHistoryDto> ItemHistories { get; set; }
    }
}
