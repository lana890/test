using EskaCMS.Entities.Entity.WorkFlow;
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
    public class WorkFlowDetailsDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public EWorkFlowType Type { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public List<StageDetailsDto> Stages { get; set; }
        public List<WorkFlowCutlureDetailsDto> WorkFlowDetails { get; set; }
    }
}
