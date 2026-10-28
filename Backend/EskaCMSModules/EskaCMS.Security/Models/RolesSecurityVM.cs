using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;
using System.Reflection;
using System.IO;

namespace EskaCMS.Security.Models
{
    public class RolesSecurityVM
    {
        public long Id { get; set; }
        public long RoleId { get; set; }
        public virtual Role Role { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public List<ControllerInfoVM> Controllers { get; set; }
    }
    public class ControllerInfoVM
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string AreaName { get; set; }
        public long RoleAccessId { get; set; }
        public string Path { get; set; }

        public virtual RoleAccess RoleAccess { get; set; }
        public List<ActionInfoVM> Actions { get; set; }

    }
    public class ActionInfoVM
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ControllerId { get; set; }

        public string ActionName { get; set; }
        public string FullApiPath { get; set; }
        public string HttpMethod { get; set; }

        public bool isSelected { get; set; }
        public List<MethodParams> ActionParam { get; set; }
    }
    public class MethodParams
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public List<ParamDetails> ParamDetails { get; set; }

    }
    public class ParamDetails
    {
        //public string Name { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
