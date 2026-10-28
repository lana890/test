using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.BusinessModels
{
    public class UserSearchOption
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public ERequestStatus Status { get; set; }

        public EUsersTypes UserType { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
