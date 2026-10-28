using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class ItemHistoryDto
    {
       
        public BasicStageInfoDto Stage { get; set; }
        public string Note { get; set; }
        public EActionType ActionType { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string CreatedBy { get; set; }
      
    }
}
