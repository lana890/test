using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Events;
using EskaCommerce.Reviews.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EskaCommerce.Reviews.Services.Interfaces;
using EskaCommerce.Reviews.Models.ReviewModels;

namespace EskaCommerce.Module.Reviews.Areas.Reviews.Controllers
{
    [Authorize]
    [Route("api/ReviewApi")]
    [ApiController]
    public class ReviewApiController : ControllerBase
    {
        private readonly IReviewServices _ReviewServices;
        // private readonly IMediator _mediator;
         
        public ReviewApiController(IReviewServices ReviewServices/* , IMediator mediator */)
        {
            _ReviewServices = ReviewServices;
            // _mediator = mediator;
        }

       

        [HttpPost("Grid")]
        public async Task<IActionResult> Grid([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await _ReviewServices.Grid(param));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }

        [HttpPut("BlockUser/{UserId}")]
        public async Task<IActionResult> BlockUser([FromRoute] long UserId)
        {
            try
            {
                return Ok(await _ReviewServices.BlockUserPermissions(UserId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }
        [HttpPut("UnBlockUser/{UserId}")]
        public async Task<IActionResult> UnBlockUser([FromRoute] long UserId)
        {
            try
            {
                return Ok(await _ReviewServices.UnBlockUserPermissions(UserId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }
        [HttpPost("GetUserBlocked")]
        public async Task<IActionResult> GetUserBlocked([FromBody] UserBlockedViewModelRequest  model)
        {
            try
            {
                return Ok(await _ReviewServices.GetUserBlocked(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }
        [HttpPost("ChangeStatus/{id}/{Status}")]
        public async Task<IActionResult> ChangeStatus([FromRoute] long id, [FromRoute] long Status)
        {
            try
            {
               return Ok(await _ReviewServices.ChangeStatus(id, Status));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }
        }
        [HttpGet("GetProductReviewer/{ProductId}")]
        public async Task<IActionResult> GetProductReviewer(long ProductId)
         {
            try
            {
               return Ok(await _ReviewServices.GetProductReviewer(ProductId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }

    }
}
