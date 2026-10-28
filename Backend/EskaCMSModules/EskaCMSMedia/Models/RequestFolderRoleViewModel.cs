using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Models
{
    public class RequestFolderRoleViewModel
    {
        public List<long> FolderIds { get; set; }=new List<long>();
        public List<long> RoleIds { get; set; }=new List<long>();
    }

    public class RequestAllMediaRoleViewModel
    {
        public List<long> MediaIds { get; set; } = new List<long>();
        public List<long> RoleIds { get; set; } = new List<long>();
    }

    public class RequestMediaRoleViewModel
    {
        public RequestAllMediaRoleViewModel?AddMediaRole { get; set; }

        public long? MediaId { get; set; }
        public List<long>? RoleIds { get; set; } = new List<long>();
    }

    public class MediaAssignedRolesVM
    {

        public List<Role> AssingedRoles { get; set; }
        public List<Role> UnassingedRoles { get; set; }
    }
}
