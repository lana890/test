using EskaCMS.Core.BusinessModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class GetUserByUserOptionVm
    {
        public long UserId { get; set; }

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Image { get; set; }

        public string UserName { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
        public long RoleId { get; set; }
        public string RoleName { get; set; }
        public IList<UserOptionsVM> UserOptions { get; set; }

    }
}
