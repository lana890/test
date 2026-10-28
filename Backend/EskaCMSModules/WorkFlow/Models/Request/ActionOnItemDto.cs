using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Request
{
    public class ActionOnItemDto
    {
        public long ItemId { get; set; }
        public string Note { get; set; }
        public EActionType Action { get; set; }
    }
}
