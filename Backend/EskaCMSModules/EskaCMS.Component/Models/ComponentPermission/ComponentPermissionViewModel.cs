using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Models.ComponentPermission
{
    public class RequestComponentPermissionViewModel
    {
        public string ComponentSettingsId { get; set; }
        public long RoleId { get; set; }
        public List<long> PermissionIds { get; set; }
    }

    public class FilterationComponentPermissionViewModel
    {
        public long? PageId { get; set; }
        public long? TemplateId { get; set; }
        public long? SharedId { get; set; }
    }
    public class GetComponentPermissionViewModel
    {
        public string ComponentName { get; set; }
        public string ComponentSettingsId { get; set; }
        public List<GetPermissionViewModel> PermissionIds { get; set; }
    }

    public class GetPermissionViewModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public long PermissionId { get; set; }
    }
}
