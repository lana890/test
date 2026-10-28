using MediatR;

namespace EskaCMS.Core.Events
{
    public class EntityDeleting : INotification
    {
        public long EntityId { get; set; }
    }
}
