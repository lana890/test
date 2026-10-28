using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EskaCommerce.Comments.Data;
using EskaCommerce.Comments.Models;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Comments;
using EskaCommerce.Comments.Services.Interfaces;
using EskaCommerce.Comments.Models.Request;
using System;
using EskaCommerce.Comments.Models.Response;

namespace EskaCommerce.Comments.Areas.Comments.Controllers
{
    [Route("api/comments")]
    [Authorize]
    [ApiController]
    public class CommentController : ControllerBase
    {

        private readonly ICommentService _commentServices;

        public CommentController(ICommentService commentServices)
        {
            _commentServices = commentServices;
        }

        #region Comment Products

        [HttpPost("Get")]

        public async Task<IActionResult> Get([FromBody]GetRequestViewModel model)
        {
            return Ok(await _commentServices.GetComments(model));
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CommentRequestViewModel model)
        {
            try
            {

                return Ok(await _commentServices.Create(model));
            }

            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }



        }

        #endregion

        #region Comment Posts

        [HttpPost("GetCommentPosts")]

        public async Task<IActionResult> GetCommentPosts([FromBody] CommentRequestVM model)
        {
            return Ok(await _commentServices.GetCommentPosts(model));
        }

        [HttpPost("CreateCommentPosts")]
        public async Task<IActionResult> CreateCommentPosts([FromBody] CommentPostsRequestViewModel model)
        {
            try
            {

                return Ok(await _commentServices.CreatePosts(model));
            }

            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }



        }
        #endregion
    }
}
