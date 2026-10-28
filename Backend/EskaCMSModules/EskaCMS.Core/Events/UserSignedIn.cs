using MediatR;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Events
{
    public class UserSignedIn : INotification
    {
        public long UserId { get; set; }
        public CartTypes CartType { get; set; }
        public long  SiteId { get; set; }
    }
}
