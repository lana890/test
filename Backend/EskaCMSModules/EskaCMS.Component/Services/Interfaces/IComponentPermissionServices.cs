using EskaCMS.Components.Models.ComponentPermission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Services.Interfaces
{
    public interface IComponentPermissionServices
    {
        Task AddComponentPermission(RequestComponentPermissionViewModel model);
        Task<List<GetComponentPermissionViewModel>> GetComponentPermission(FilterationComponentPermissionViewModel model);
    }
}
