using MediatR;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Events
{
    public class UserNotification : INotification
    {
        public long UserId { get; set; }
        public long OrderId { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
    }
}
