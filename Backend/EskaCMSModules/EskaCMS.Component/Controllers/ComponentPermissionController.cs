using EskaCMS.Components.Models.ComponentPermission;
using EskaCMS.Components.Services.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels.ComponentKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Controllers
{
    [ApiController]
    [Route("api/ComponentPermission")]
    [Authorize]
    public class ComponentPermissionController : ControllerBase
    {
        private readonly IComponentPermissionServices componentPermissionServices;

        public ComponentPermissionController(IComponentPermissionServices  componentPermissionServices)
        {
            this.componentPermissionServices=componentPermissionServices;
        }
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] RequestComponentPermissionViewModel model)
        {
            try
            {
               await componentPermissionServices.AddComponentPermission(model);
               return Ok();

            }
            catch (Exception)
            {
                    
                throw;
            }
        }

        [HttpPost("GetAll")]
        public async Task<IActionResult> GetAll([FromBody] FilterationComponentPermissionViewModel model)
        {
            try
            {

            var result = await componentPermissionServices.GetComponentPermission(model);
            return Ok(result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
