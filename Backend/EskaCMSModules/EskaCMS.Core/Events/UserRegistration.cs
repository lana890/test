using MediatR;

namespace EskaCMS.Core.Events
{
    public class UserRegistration : INotification
    {
        public long UserId { get; set; }
    }
}
