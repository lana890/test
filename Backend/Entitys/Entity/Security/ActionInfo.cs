using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Security
{
    public class ActionInfo : EntityBaseWithTypedId<string>
    {
        public ActionInfo()
        {

        }
      
        //public override string Id => $"{ControllerId}:{Name}";

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string ControllerId { get; set; }
        public virtual ControllerInfo Controller { get; set; }
    }
}