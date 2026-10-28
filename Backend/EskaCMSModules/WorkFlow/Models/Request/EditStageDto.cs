using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Request
{
    public class EditStageDto
    {
        public long Id { get; set; }
        //public string Description { get; set; }

        public List<StageCultureDetailsDto> StageDetails { get; set; }
        public EStageType Type { get; set; }
        public long? CustomerGroupId { get; set; }
        public long? RoleId { get; set; }
        public bool IsOverridable { get; set; }
        public ERejectionBehavior RejectionBehavior { get; set; }
        public EApprovalStep? ApprovalStep { get; set; }
    }
}
