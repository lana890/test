using EskaCMS.BlogArchive.Models;
using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/BolgArchive")]
    [ApiController]
    [Authorize]
    public class BolgArchiveController : ControllerBase
    {

        private readonly IBlogArchive _BlogArchiveRepository;
        // private readonly IRepository<Posts> _PostRepository;
        private readonly IWorkContext _workContext;
        public BolgArchiveController(IBlogArchive BlogArchiveRepository, /* IRepository<Posts> PostRepository, */ IWorkContext workContext)
        {
            _BlogArchiveRepository = BlogArchiveRepository;
            // _PostRepository = PostRepository;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("GetPosts")]
        public async Task<IActionResult> GetPosts([FromBody] PostsSearchVM search)
         {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                search.SiteId = SiteId;
                search.CultureId = search.CultureId.HasValue ? search.CultureId.Value :  CultureId;
                return Ok(await _BlogArchiveRepository.GetPosts(search));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("SetLoginPage")]
        public async Task<IActionResult> SetLoginPage([FromBody] SetPostLoginPageVM model)
        {

            try
            {

                var result = await _BlogArchiveRepository.SetLoginPage(model);
                if (result)
                    return Ok();
                else
                    return BadRequest();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("GetPostsForAdmin")]
        public async Task<IActionResult> GetPostsForAdmin([FromBody] PostsSearchVM search)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId(); 
                search.SiteId = SiteId;
                search.SearchValue = search.SearchValue.ToUpper();
                return Ok(await _BlogArchiveRepository.GetPostsForAdmin(search));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetPostById/{PostId}")]
        public async Task<IActionResult> GetPostById(long PostId)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostById(PostId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetPostByParentId/{ParentId}")]
        public async Task<IActionResult> GetPostByParentId(long ParentId)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostByParentId(ParentId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetPostByCultures/{parentId}/{cultureId}")]
       public async Task<IActionResult> GetPostByCultures(long parentId,long cultureId)
       {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostByCultures(parentId, cultureId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }


       }
        [HttpGet]
        [Route("GetPostBySlug/{Slug}")]
        public async Task<IActionResult> GetPostBySlug(string Slug)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostBySlug(Slug));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("AddNewPost")]
        public async Task<IActionResult> AddNewPost(PostsViewModel Post)
        {
            try
            {
                Post.CreatedById = await _workContext.GetCurrentUserId();
                Post.SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _BlogArchiveRepository.AddPost(Post));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("DuplicatePost/{PostId}/{ParentId}")]
        public async Task<IActionResult> DuplicatePost(long PostId,long ParentId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _BlogArchiveRepository.DuplicatePost(PostId,ParentId,UserId, SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpGet]
        [Route("UpdateStatusPost/{PostId}/{Status}")]
        public async Task<IActionResult> UpdateStatusPosts(long PostId, EStatus Status)
        {
            try
            {
                var ModifiedById =await _workContext.GetCurrentUserId();
                return Ok(await _BlogArchiveRepository.UpdatePostStatus(PostId, ModifiedById, Status));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("UpdateStatusPosts/{ParentPostId}/{Status}")]
        public async Task<IActionResult> UpdateRelativePostsStatus(long ParentPostId, EStatus Status)
        {
            try
            {
                var ModifiedById = await _workContext.GetCurrentUserId();
                return Ok(await _BlogArchiveRepository.UpdateRelativePostsStatus(ParentPostId, ModifiedById, Status));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetPostsByCategoryId/{cultureId}/{categoryId}")]
        public async Task<IActionResult> GetPostsByCategoryId([FromRoute]long cultureId, [FromRoute ]long categoryId)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostByCategoryId(categoryId, cultureId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }





        [HttpPost]
        [Route("AddEditPost")]
        public async Task<IActionResult> AddEditPost(PostsViewModel EditPostObj)
        {
            try
            {


                EditPostObj.ModifiedById = await _workContext.GetCurrentUserId();
                EditPostObj.SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                EditPostObj.ModificationDate = DateTimeOffset.Now;
                //var thumbObject = Request.Form.Files;
                return Ok(await _BlogArchiveRepository.AddEditPost(EditPostObj));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }




        //bulk update and delete
        [HttpPost]
        [Route("UpdatePostsTemplate/{PostsIds}/{TemplateId}")]
        public async Task<IActionResult> UpdatePostsTemplate(string PostsIds, int TemplateId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _BlogArchiveRepository.UpdatePostsTemplate(PostsIds, TemplateId, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("DeletePosts/{PostsIds}")]
        public async Task<IActionResult> DeletePosts(string PostsIds)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _BlogArchiveRepository.UpdateStatusPosts(PostsIds, UserId,  EStatus.Deleted));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("PublishPosts/{PostsIds}")]
        public async Task<IActionResult> PublishPosts(string PostsIds)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok(await _BlogArchiveRepository.UpdateStatusPosts(PostsIds, UserId, EStatus.Published));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPut]
        [Route("ChangeParentByCultureId/{CultureId}")]
        public async Task<IActionResult> ChangeParentByCultureId([FromRoute] long CultureId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _BlogArchiveRepository.ChangeParentByCultureId(SiteId, CultureId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        //////end bulk edit and delete
        //[HttpGet]
        //[Route("GetAllPostsTags")]
        //public async Task<IActionResult> GetAllPostsTags()
        //{
        //    try
        //    {
        //        var SiteId = _workContext.GetCurrentSiteId();

        //        return Ok(await _BlogArchiveRepository.GetAllPostsTags(SiteId));
        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc.Message);
        //    }
        //}

        [HttpGet]
        [Route("GetPostsByTagId/{Id}")]
        public async Task<IActionResult> GetPostByTagId([FromRoute] long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _BlogArchiveRepository.GetPostsByTagId(SiteId, Id));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetPostAuthorsByPostId/{Id}")]
        public async Task<IActionResult> GetPostAuthorsByPostId([FromRoute] long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _BlogArchiveRepository.GetPostAuthorsByPostId(SiteId, Id));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        //[HttpPost]
        //[Route("DuplicatePost/{PostId}")]
        //public async Task<IActionResult> DuplicatePost(long ParentPostId)
        //{
        //    try
        //    {
        //        var UserId = await _workContext.GetCurrentUserId();
        //        var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        return Ok(await _BlogArchiveRepository.DuplicatePost(ParentPostId, UserId, SiteId));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}


        [HttpGet]
        [Route("GetPostAssignedRoles/{id}")]
        public async Task<IActionResult> GetPostAssignedRoles(long id)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"]);
                var result = await _BlogArchiveRepository.GetPostAssignedRoles(id, SiteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("SavePostAssginedRoles")]
        public async Task<IActionResult> SavePostAssginedRoles(RolesViewModel vm)
        {
            try
            {
                vm.UserId  = await _workContext.GetCurrentUserId();
                var result = await _BlogArchiveRepository.SavePostAssginedRoles(vm);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpPost]
        [Route("UpsertPost")]
        public async Task<IActionResult> UpsertPost(List<PostsViewModel> Posts)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _BlogArchiveRepository.UpsertPostFirstStep(Posts, UserId,SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



		[HttpDelete]
		[Route("DeleteCategoryDetails")]
		public async Task<IActionResult> DeleteCategoryDetails([FromQuery] long Id)
		{
			try
			{
				var UserId = await _workContext.GetCurrentUserId();
				var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
				await _BlogArchiveRepository.DeleteCategoryDetails(Id);
                return Ok();
			}
			catch (Exception exc)
			{
				return BadRequest(exc.Message);
			}
		}

		[HttpPost]
        [Route("UpsertPostForWeb")]
        public async Task<IActionResult> UpsertPostForWeb([FromBody]List<AddPostForWebVM> Posts)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _BlogArchiveRepository.UpsertPostForWeb(Posts, UserId, SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("CompleteUpsertPost")]
        public async Task<IActionResult> CompleteUpsertPost(List<CompleteUpsertPostVM> Posts)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.CompleteUpsertPost(Posts));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetPostByYearAndMonth")]
        public async Task<IActionResult> GetPostByYearAndMonth([FromBody] FilterForMonthsViewModel model )
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetPostByYearAndMonth(model));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }        
        }


        [HttpGet]
        [Route("CreatedByInfo/{PostId}")]
        public async Task<IActionResult> CreatedByInfo([FromRoute]long PostId)
        {
            try
            {
                return Ok(await _BlogArchiveRepository.GetCreatedByInfo(PostId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}