using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public class FormItemRole :EntityBase
    {
        public long RoleId { get; set; }
        public Role Role { get; set; }
        public long FormItemId { get; set; }
        public FormItem FormItem { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public User CreatedBy { get; set; }
    }
}
