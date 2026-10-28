using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Request
{
    public class CreateStageDto
    {
       
        public List<StageCultureDetailsDto>  StageDetails { get; set; }
        public EStageType Type { get; set; }
        public EApprovalStep? ApprovalStep { get; set; }
        public long WorkFlowId { get; set; }
        public long? CustomerGroupId { get; set; }      
        public long? RoleId { get; set; }
        public bool IsOverridable { get; set; }
        public ERejectionBehavior? RejectionBehavior { get; set; } = ERejectionBehavior.BackToPreviousStage;
    }

    public class StageCultureDetailsDto
    {
        public string Description { get; set; }
        public long CultureId { get; set; }
        public string? CultureCode { get; set; }
    }
}
