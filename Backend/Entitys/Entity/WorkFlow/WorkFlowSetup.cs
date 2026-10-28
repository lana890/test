using EskaCMS.Infrastructure.Models;

using EskaCMS.Entities.Entity.Core;

using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class WorkFlowSetup : EntityBase
    {

       // public string Name { get; set; }
        public EStatus Status { get; set; }
        public EWorkFlowType Type { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long CreatedById { get; set;}
        public User CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }
        public List<Stage> Stages { get; set; }
        public List<WorkFlowDetails> WorkFlowDetails { get; set; }
    }
}
