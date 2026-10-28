using MediatR;

namespace EskaCMS.Core.Events
{
    public class EntityViewed : INotification
    {
        public long EntityId { get; set; }

        public string EntityTypeId { get; set; }
    }
}
