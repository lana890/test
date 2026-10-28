using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Menus.Models
{
    public class MenuRoleGetViewModel
    {
        public long Id { get; set; }
        public long MenuId { get; set; }
        public long RoleId { get; set; }
        public long UserId { get; set; }
        public string MenuName { get; set; }
        public string RoleName { get; set; }
        public string Username { get; set; }
    } 
    
    public class MenuRolesViewModel
    {
         public long Id { get; set; }
        public List<Role> assingedRoles  { get; set; }
        public List<Role> unassingedRoles  { get; set; }
        public long UserId { get; set; }
     
    }
}
