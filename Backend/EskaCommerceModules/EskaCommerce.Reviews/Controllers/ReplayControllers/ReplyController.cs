using System;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Entities.Entity;
using EskaCommerce.Reviews.Services.Interfaces;
using EskaCommerce.Reviews.Models.ReplayModels;

namespace EskaCommerce.Reviews.Areas.Reviews.Controllers
{
    [Route("api/Reply")]   
    [Authorize]
    [ApiController]
    public class ReplyController : ControllerBase
    {
        private readonly IReplyServices replyServices;

        public ReplyController(IReplyServices replyServices )
        {
            this.replyServices = replyServices;
        }

        [Authorize]
        [HttpPost("Insert")]
        public async Task<IActionResult> Insert(ReplyForm model)
        {
            try
            {
                return Ok(await replyServices.Insert(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
           
        }


        [HttpDelete("Delete/{ReplyId}")]

        public async Task<IActionResult> Delete([FromRoute] long ReplyId)
        {
            try
            {
                return Ok(await replyServices.Delete(ReplyId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
    }
}
