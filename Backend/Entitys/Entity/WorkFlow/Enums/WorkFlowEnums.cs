using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.WorkFlow.Enums
{
    public class WorkFlowEnums
    {
        public enum EWorkFlowType
        {
            Pages = 1,
            Posts = 2,
            Templates = 3,
            Forms = 4,
            Documents = 5,
        }
        
        public enum EStageType
        {
            [Description("Role Based")]
            RoleBased = 1,
            [Description("Customer Group")]
            CustomerGroup = 2,
            Hierarchical = 3,
        }

        public enum EActionType
        {
            Approve = 1,
            Reject = 2,
        }
        public enum EFlowStatus
        {
            Approved = 1,
            Rejected = 2,
            [Description("In Progress")]
            InProgress = 3,
           

        }

        public enum ERejectionBehavior
        {
            [Description("Direct Reject")]
            DirectReject = 1,
            [Description("Back To Previous Stage")]
            BackToPreviousStage =2

        }
        public enum EApprovalStep
        {
            FirstStep = 1,
            AllSteps = 2
        }
    }
}
