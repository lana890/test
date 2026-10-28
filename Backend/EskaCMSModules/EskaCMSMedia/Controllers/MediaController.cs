
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;
//using EskaCMS.Core.Services;
using System;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.Media.Models;

namespace EskaCMS.Media.Controllers
{
    [ApiController]
    [Route("api/Media")]
   // [Authorize] 
    public class MediaController : Controller
    {

        private readonly IMediaService _MediaService;
     

       
        public MediaController(IMediaService MediaService)
        {
            _MediaService = MediaService;
          
        }

        /// <summary>
        /// Gets the media asset with the specified id.
        /// </summary>
        /// <param name="id">The media id</param>
        /// <returns>The media asset</returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
          
            return Json(await _MediaService.GetByIdAsync(id));
        }

        /// <summary>
        /// Gets all of the media assets located in the folder
        /// with the specified id. Not providing a folder id will
        /// return all of the media assets at root level.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("list/{folderId?}")]
        public async Task<IActionResult> GetByFolderId(long? folderId = null)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Json(await _MediaService.GetAllByFolderIdAsync(SiteId, folderId));

            }
            catch (Exception ex)
            {

                return BadRequest( ex.Message);
            }
         
        }


        [HttpGet]
        [Route("GetAllByCultureId/{CultureId}")]
        public async Task<IActionResult> GetByFolderId(long CultureId)
        {
            long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            return Json(await _MediaService.GetStructureAsync(SiteId,null, CultureId));
        }

   

        /// <summary>
        /// Gets the media folder structure.
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //[Route("structure")]
        //public async Task<IActionResult> GetStructure()
        //{
        //    long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
        //    long? folderId = null;
        //    return Json(await _MediaService.GetStructureAsync(SiteId,folderId,pare));
        //}
    }

}
