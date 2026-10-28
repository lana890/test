using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.ViewModels.LoggedInUser
{
    public class LoggedInUserVM
    {
        public string Id { get; set; }
        public long UserId { get; set; }

        public string UserName { get; set; }
        public DateTimeOffset LoginDate { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; }
        public bool IsLookedOut { get; set; }
        public string ClientIP { get; set; }
        public EUsersTypes UserType { get; set; }
        public LoginType LoginType { get; set; }

        public string Source { get; set; }
        public string SiteName { get; set; }
        public List<RolesVM> Roles { get; set; }
        public bool KillSession { get; set; }

    }

    public class SessionsUserViewModel
    {
        public string  Id { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public ETypeLogin Channel { get; set; }
        public string IpAddress { get; set; }
        public DateTimeOffset LoginDate { get; set; }



    }
  public  class IPHostInfo
    {
        public string HostName { get; set; }
        public string[] Aliases { get; set; }
    }

    public class UserNameViewModel
    {
        public string UserName { get; set; }


    }
}
