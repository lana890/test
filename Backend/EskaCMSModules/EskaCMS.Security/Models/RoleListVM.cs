using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class RoleListVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public ERoleTypes Type { get; set; }
        public int UsersCount { get; set; }
        public int PermissionsCount { get; set; }
    }
}
