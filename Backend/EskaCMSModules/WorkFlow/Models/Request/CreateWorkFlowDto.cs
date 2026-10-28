using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Models.Request
{
    public class CreateWorkFlowDto
    {
        public List<WorkFlowCutlureDetailsDto> WorkFlowDetails { get; set; }
        public EWorkFlowType Type { get; set; }
    }

    public class WorkFlowCutlureDetailsDto
    {
        public string Name { get; set; }
        public long CultureId { get; set; }
        public string? CutlureCode { get; set; }
    }
}
