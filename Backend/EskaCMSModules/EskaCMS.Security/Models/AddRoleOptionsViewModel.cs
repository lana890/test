using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class AddRoleOptionsViewModel
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
        public long UserId { get; set; }
        public long SiteId { get; set; }

        public ERoleTypes RoleType { get; set; }
        public List<ListOfRoleOptions> ListOfOptions { get; set; }
        public IEnumerable<ControllerInfoVM> SelectedControllers { get; set; }
    }
    public class ListOfRoleOptions
    {
        public long OptionId { get; set; }
        public short OptionValue { get; set; }
    }
}
