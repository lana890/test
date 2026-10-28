using EskaCMS.WorkFlow.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class StageDetailsDto
    {
        public long Id { get; set; }

        public List<StageCultureDetailsDto> StageDetails { get; set; }
        public int Order { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public EStageType Type { get; set; }
        
        public long? CustomerGroupId { get; set; }
       
        public long? RoleId { get; set; }

        public string Description { get; set; }
        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
        public ERejectionBehavior RejectionBehavior { get; set; }
        public bool IsOverridable { get; set; }
        public EApprovalStep? ApprovalStep { get;  set; }
    }
}
