using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Entities.Entity.Reviews;
using EskaCommerce.Reviews.Services.Interfaces;
using EskaCommerce.Reviews.Models.ReviewModels;
using EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts;
using System.Collections.Generic;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace EskaCommerce.Module.Reviews.Areas.Reviews.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Review")]
    public class ReviewController : ControllerBase
    {

        private readonly IReviewServices reviewServices;
        // Confirmed unused: never assigned and never read (compiler warning CS0169).
        // private readonly IWorkContext _workContext;

        public ReviewController(IReviewServices reviewServices)
        {
            this.reviewServices = reviewServices;
        }

        [HttpPost("AddReview")]
        public async Task<IActionResult> AddReviewProduct([FromForm]List<IFormFile> files)
        {
            try
            
            {
                ReviewForm model = JsonSerializer.Deserialize<ReviewForm>(Request.Form["data"].ToString());

                return Ok(await reviewServices.AddReview(model,files));
              
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpPost("List/{pageNumber}/{pageSize}/{ProductId}")]

        public async Task<IActionResult> List([FromRoute] long ProductId, [FromRoute]int pageNumber, [FromRoute] int pageSize)
        {
            try
            {
                return Ok(await reviewServices.List( ProductId,pageSize, pageNumber));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }

        #region ReviewPosts

        [HttpPost("AddReviewPosts")]
        public async Task<IActionResult> AddReviewPosts([FromBody] ReviewPostVM model)
        {
            try
            {
                return Ok(await reviewServices.AddReviewPosts(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }
        [HttpPost("ListPosts/{pageNumber}/{pageSize}/{PostId}")]

        public async Task<IActionResult> ListPosts([FromRoute] long PostId, [FromRoute] int pageNumber, [FromRoute] int pageSize)
        {
            try
            {
                return Ok(await reviewServices.ListPosts(PostId, pageSize, pageNumber));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException.Message);
            }

        }


        [HttpDelete("Delete/{ReviewId}")]

        public async Task<IActionResult> Delete([FromRoute] long ReviewId)
        {
            try
            {
                return Ok(await reviewServices.RemoveReview(ReviewId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpGet("ReviewStatus")]

        public async Task<IActionResult> ReviewStatus()
        {
            try
            {

                var result = new List<object>();

                foreach (var item in Enum.GetValues(typeof(ReviewStatus)))
                {

                    result.Add(new
                    {
                        Value = (int)item,
                        Label = ((ReviewStatus)item).GetDisplayName()
                    });



                }

                return Ok(result);
               
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        #endregion


    }
}
