using EskaCMS.WorkFlow.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class WorkFlowItemList
    {
        public long Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public BasicStageInfoDto CurrentStage { get; set; }

        public string CreatedBy { get; set; }

        public EFlowStatus Status { get; set; }
        public EWorkFlowType Type { get; set; }

        public SubmittedFormDto SubmittedForm { get; set; }
        public AttachmentDto Media { get; set; }

        public string Code { get; set; }
        public string WorkFlowName { get; set; }
        public List<WorkFlowCutlureDetailsDto> WorkFlowDetails { get; set; }
    }
}
