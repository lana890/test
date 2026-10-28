
using EskaCMS.Entities.Entity;
using EskaCMS.Security.DynamicAuthorization.Models;
using EskaCMS.Security.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.Security.DynamicAuthorization.Core
{
    public interface IControllerDiscovery
    {
       Task< IEnumerable<ControllerInfoVM>> GetControllers();
    }
}