using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Comments.Data;
using EskaCommerce.Comments.Models;
using EskaCMS.Entities.Enum;
using EskaCommerce.Comments.Services.Interfaces;
using EskaCommerce.Comments.Services;

namespace EskaCommerce.Comments.Areas.Comments.Controllers
{
    [Authorize]
    [Route("api/CommentsApi")]
    [ApiController]
    public class CommentApiController : ControllerBase
    {
        private readonly ICommentService _commentServices;

        public CommentApiController(ICommentService commentServices/* , IMediator mediator */)
        {
            _commentServices = commentServices;
        }

        [HttpPost("Grid")]
        public async Task<IActionResult> Grid([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await  _commentServices.Grid(param));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }
      

        [HttpPut("ChangeStatus/{Id}/{StatusId}")]
        public async Task<IActionResult> ChangeStatus([FromRoute] long Id, [FromRoute] int StatusId)
        {
            try
            {

            return Ok(await _commentServices.ChangeStatus(Id, StatusId));
          
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }
    }
}
