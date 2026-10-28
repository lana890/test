using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class Stage : EntityBase
    {

        public int Order { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public EStageType Type { get; set; }
        public EApprovalStep? ApprovalStep { get; set; }
        public EStatus Status { get; set; }
        public ERejectionBehavior RejectionBehavior { get; set; }

        public bool IsOverridable { get; set; }
        public long WorkFlowId { get; set; }
        public WorkFlowSetup WorkFlow { get; set;}
        public long? CustomerGroupId { get; set; }
        public CustomerGroup CustomerGroup { get; set; }
        public long? RoleId { get; set; }
        public Role Role { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }    
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }
        public List<WorkFlowItem> WorkFlowItems  { get; set; }
        public List<StageDetails> StageDetails { get; set; }
    }
}
