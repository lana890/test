using EskaCMS.WorkFlow.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class BasicStageInfoDto
    {
        public long Id { get; set; }
        public string Description { get; set; }
   
        public int Order { get; set; }

        public List<StageCultureDetailsDto> StageDetails { get; set; }

    }
}
