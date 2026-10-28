using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using EskaCommerce.Reviews.Models.UserInteractionModels;
using EskaCommerce.Reviews.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Controllers.UserInteractionControllers
{
    [Authorize]
    [ApiController]
    [Route("api/UserInteraction")]
    public class UserInteractionController : ControllerBase
    {
        private readonly IUserInteractionServices userInteractionServices;

        public UserInteractionController(IUserInteractionServices userInteractionServices)
        {
            this.userInteractionServices = userInteractionServices;
        }

        [HttpGet("Get/{RoleId}")]

        public async Task<IActionResult> Get(GeneralEnums.ERoleTypes? RoleId = null)
        {
            try
            {
                return Ok(await userInteractionServices.Get(RoleId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpPost("Insert")]
        public async Task<IActionResult> Insert([FromBody]UserInteractionViewModel model)
        {
            try
            {
                return Ok(await userInteractionServices.Insert(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpDelete("Remove/{Id}")]
        public async Task<IActionResult> Remove([FromRoute]long Id)
        {
            try
            {
                return Ok(await userInteractionServices.Remove(Id));


            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody]UserInteractionViewModel model)
        {
            try
            {
                return Ok(await userInteractionServices.Update(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }
    }
}
