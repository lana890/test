using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Reviews.Models;
using MediatR;
 using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using EskaCommerce.Reviews.Services.Interfaces;

namespace EskaCommerce.Module.Reviews.Areas.Reviews.Controllers
{
    [Authorize]
    [Route("api/ReplyApi")]
    [ApiController]
    public class ReplyApiController : ControllerBase
    {
        private readonly IReplyServices _ReplayServices;
        // private readonly IMediator _mediator;

        public ReplyApiController(IReplyServices ReplayServices/* , IMediator mediator */)
        {
            _ReplayServices = ReplayServices;
            // _mediator = mediator;
        }


        [HttpPost("grid")]
        public async Task<IActionResult> grid([FromBody] SmartTableParam param)
        {
            try
            {
           
              return Ok( await _ReplayServices.Grid(param));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpPost("ChangeStatus/{Id}/{Status}")]
        public async Task<IActionResult> ChangeStatus([FromRoute]long Id, [FromRoute] long Status)
        {
            try
            {
                return Ok(await _ReplayServices.ChangeStatus(Id, Status));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException.Message);
            }

        }
    }
}
