using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class ItemHistory : EntityBase
    {

        public long? CurrentStageId { get; set; }
        public Stage CurrentStage { get; set; }
        
        public long? PreviousStageId { get; set; }
        public Stage PreviousStage { get; set; }

        public string Note { get; set; }
        public EActionType ActionType { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long WorkFlowItemId { get; set; }
        public WorkFlowItem WorkFlowItem { get; set; }
    }
}
