using EskaCMS.Entities.Entity.WorkFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models
{
    public class AuthorizedStageVM
    {
        public List<Stage> AssignedStages { get; set; }
        public List<Stage> OverridableStages { get; set; }
    }
}
