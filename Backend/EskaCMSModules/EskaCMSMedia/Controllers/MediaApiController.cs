
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using EskaCMS.Core;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services;
using EskaCMS.Media.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Media.Enums;
using EskaCMS.Core.Runtime;
using Microsoft.AspNetCore.Http;
using System.IO;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Media.Controllers
{
    /// <summary>
    /// Api controller for alias management.
    /// </summary>

    [Route("api/media")]
    // [Authorize]
    [ApiController]
    public class MediaApiController : Controller
    {
        private readonly IMediaService _service;

        private readonly IWorkContext _workContext;

        public MediaApiController(IMediaService service, IWorkContext workContext)
        {
            _service = service;

            _workContext = workContext;
        }

        /// <summary>
        /// Gets single media
        /// </summary>
        /// <returns>The list model</returns>
        [Route("get-media/{id}")]
        [HttpGet]
        public async Task<IActionResult> Get(long id)
        {
            var media = await _service.GetById(id);
            if (media == null)
            {
                return NotFound();
            }

            return Ok(media);
        }


        [Route("GetMediaPysicalFile/{URL}")]
        [HttpGet]
        public async Task<IActionResult> GetMediaPysicalFile(string URL)
        {
            try
            {
                URL = HttpUtility.UrlDecode(URL);
                var media = await _service.GetMediaPysicalFile(URL);


                return File(media.FileBytes, media.ContentType, media.Name);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }



        }
        /// <summary>
        /// Gets single media
        /// </summary>
        /// <returns>The list model</returns>
        [Route("get-folder/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetFolderById(long id)
        {
            long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

            var folder = await _service.GetFolderByIdAsync(siteId, id);
            if (folder == null)
            {
                return NotFound();
            }

            return Ok(folder);
        }

        /// <summary>
        /// Gets the image url for the specified dimensions.
        /// </summary>
        /// <param name="id">The unqie id</param>
        /// <param name="width">The optional width</param>
        /// <param name="height">The optional height</param>
        /// <returns>The public url</returns>
        [Route("url/{id}/{width?}/{height?}")]
        [HttpGet]
        public async Task<IActionResult> GetUrl(long id, int? width = null, int? height = null)
        {
            if (!width.HasValue)
            {
                var media = await _service.GetByIdAsync(id);

                if (media != null)
                {
                    return Redirect(media.PublicUrl);
                }
                return NotFound();
            }
            else
            {
                return Redirect(await _service.EnsureVersionAsync(id, width.Value, height));
            }
        }

        /// <summary>
        /// Gets the list model.
        /// </summary>
        /// <returns>The list model</returns>
        [Route("list")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> List([FromBody] MediaRequestViewModel model)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var Result = await _service.GetList(SiteId, model.folderId, model.filter, model.width, model.height,
                  model.PaginationFolder.PageSize,
                  model.PaginationFolder.PageNumber,
                  model.PaginationMedia.PageSize,
                  model.PaginationMedia.PageNumber,
                  model.SearchText,
                  model.orderFilter,
                  model.status,model.ContentType);
                return Ok(Result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Saves the meta information for the given media asset.
        /// </summary>
        /// <param name="model">The media model</param>
        [Route("meta/save")]
        [HttpPost]
        public async Task<IActionResult> SaveMeta(MediaListModel.MediaItem model)
        {
            model.SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

            if (await _service.SaveMeta(model))
            {
                return Ok("The meta information was succesfully updated");
            }
            else
            {
                return Ok("The meta information was succesfully updated");
            }
        }

        [Route("meta/{cultureId}/{mediaId}")]
        [HttpGet]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GetMediaDetailsbyCultureId(long cultureId, long mediaId)
        {
            try
            {
                long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

               
                var result = await _service.GetMeta(mediaId, siteId, cultureId);
                if (result != null)
                {
                    return Ok(result);
                }

                    
                return Ok("There is no meta for this image ");
                
                
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
            
        }

        private IActionResult Ok(EskaCMS.Entities.Entity.Core.Media media)
        {
            throw new NotImplementedException();
        }

        [Route("GetFolderDetailsbyCultureId/{cultureId}/{FolderId}")]
        [HttpGet]
        public async Task<IActionResult> GetFolderDetailsbyCultureId(long CultureId, long FolderId)
        {
            long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

            if (FolderId != 0)
            {
                var result = await _service.GetFolderDetailsByCultureId(CultureId, FolderId, siteId);
                if (result != null)
                    return Ok(result);
                else
                    return Ok(new MediaFoldersDetails());
            }
            else
            {
                return BadRequest("Folder name should be sent");
            }
        }

        [Route("folder/save")]
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> SaveFolder(RequestMediaFolderVM model)
        {
            try
            {
                var CreatedById = await _workContext.GetCurrentUserId();
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                await _service.SaveFolder(model);

                var result = await _service.GetList(SiteId, model.ParentId);

                result.Status = new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = String.Format("The folder {0} was saved", model.MediaFoldersDetails.Select(x=>x.Title).FirstOrDefault())
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("scan")]
        public async Task<IActionResult> ScanFolder([FromQuery] string path)
        {
            try
            {
                int processedCount = await _service.ScanAndSaveFoldersAsync(path);
                return Ok(new
                {
                    Success = true,
                    Message = $"Successfully processed {processedCount} images",
                    ProcessedCount = processedCount
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { Success = false, ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, ex.Message });
            }
        }
        [Route("folder/delete")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFolder([FromBody] DeleteFolderVM model)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var folderId = model.Action == MediaActionsEnum.Delete ? await _service.DeleteFoldersWithMedia(model.FolderIds) : await _service.SoftFoldersWithMedia(model.FolderIds);

                var result = await _service.GetList(SiteId, folderId);

                result.Status = new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = "The folder was successfully deleted"
                };

                return Ok(result);
            }
            catch (ValidationException e)
            {
                return BadRequest(e.Message);
            }
        }


        [Route("folder/RestoreFolderWithContent")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RestoreFolderWithContent([FromBody]RestoreFolders folders)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var result = await _service.RetrieveFolderWithMedia(folders);


                return Ok("The folder was successfully retrieved with all folders and media inside it");
            }
            catch (ValidationException e)
            {
                return BadRequest(e.Message);
            }
        }






        /// <summary>
        /// Adds a new media upload.
        /// </summary>
        /// <param name="model">The upload model</param>
        [Route("upload")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] MediaUploadModel model)
        {

            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId(); ;

            // Allow for dropzone uploads
            if (!model.Uploads.Any())
            {
                model.Uploads = HttpContext.Request.Form.Files;
            }

            try
            {
                if (model.SiteId == 0)
                    model.SiteId = siteId;

                if (model.CreatedBy == 0)
                    model.CreatedBy = userId;


                var uploaded = await _service.SaveMedia(model);

                // if (uploaded != null)
                return Ok(new { uploaded });
                #region Comments
            
                #endregion

            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);
                if (e.Message != null && e.Message.Contains("Duplicate File Name") )
                {
                    return Conflict("This Record Cannot Be Duplicated Please Try To Change The Name Before Upload");
                }
                return BadRequest(e.Message);
            }
        }

        [Route("move/{folderId?}")]
        [HttpPost]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> Move([FromBody] IEnumerable<long> items, long? folderId, [FromQuery] bool Force = false)
        {
            try
            {
                var moved = 0;
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

                foreach (long id in items)
                {

                    var media = await _service.GetByIdAsync(id);

                    if (media != null)
                    {


                        await _service.MoveAsync(media, folderId, null, null, Force);
                        moved++;

                        continue;
                    }


                }


                if (moved > 0)
                {
                    return Ok($"Media file{(moved > 1 ? "s" : "")} was successfully moved.");
                }
                else
                {
                    return BadRequest("The media file was not found.");
                }
            }
            catch (Exception e)
            {
                if (e.Message != null && e.Message.Contains("Duplicate File Name"))
                {
                    return Conflict("This record cannot be duplicated Please Change The Name Before Move");
                }
                return BadRequest(e.Message);
            }
        }

        [Route("delete")]
        [HttpPost]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] DeleteMediaVM model)
        {
            try
            {

                var result = model.Action == MediaActionsEnum.Delete ? await _service.DeleteMultibleMedia(model.MediaIds) : await _service.SoftDeleteMedia(model.MediaIds);


                return Ok(new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = $"The media files was successfully deleted"
                });
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }


        [Route("UploadSystemMedia")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadSystemMedia([FromForm] IFormFile file)
        {
            try
            {

                string uploaded = await _service.UploadSystemMedia(file);

                return Ok(uploaded);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Route("RestoreMedia")]
        [HttpPost]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> RestoreMedia([FromBody] RestoreMediaVM model)
        {
            try
            {

                var result = await _service.RetrieveMultibleMedia(model);

                return Ok($"The media files was successfully UnArchived");
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.Message.Contains("foreign key constraint fails"))
                {
                    return BadRequest("File is already being used and cannot be deleted");
                }
                return BadRequest(e.Message);
            }
        }

        [Route("setAsFolderThumbnail")]
        [HttpPut]
        public async Task<IActionResult> setAsFolderThumbnail(MediaFolderVM media)
        {
            try
            {
                StatusMessage statusMessage = new StatusMessage();
                var defultThumbnail = await _service.setThumbnailFolder(media);
                if (defultThumbnail == true)
                {
                    statusMessage.Body = "updated successfuy";
                    statusMessage.Type = StatusMessage.Success;
                }

                return Ok(statusMessage);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }

        [Route("GetFileExtension")]
        [HttpPost]
        public async Task<IActionResult> GetFileExtension([FromForm] CreateFileVM model)
        {
            try
            {
                var result = await _service.GetFileExtensionFromBase64(model);

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }

        [Route("GetFolderBredCrumb")]
        [HttpPost]
        public async Task<IActionResult> GetFolderBredCrumb([FromBody] long folderId)
        {
            try
            {
                var result = await _service.GetFolderBreadCrumb(FolderId: folderId, null);

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }

        [Route("MoveFolders")]
        [HttpPost]
        public async Task<IActionResult> MoveFolders([FromBody] TransferFolderVm model)
        {
            try
            {
                var result = await _service.MoveFolders(model);

                if (result)
                {

                    return Ok("The folder has been transferd");
                }

                return BadRequest("Faild to transfer folder");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }

        [HttpGet("MediaTypes")]
        public IActionResult MediaTypes()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(MediaType)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = item.ToString()
                });
            }

            return Ok(result);

        }

        [HttpGet("ExtensionFile")]
        public IActionResult ExtensionFile()
        {

            var AllTypes = MediaManager.Documents.Concat(MediaManager.Images).Concat(MediaManager.Videos).Concat(MediaManager.Audio);
            var result = AllTypes.Where(x => !string.IsNullOrWhiteSpace(x?.Extension)).Select(x => new
            {
                Label = x.Extension.TrimStart('.'),
                Value = x.ContentType
            }).ToList();

            return Ok(result);

        }

        [HttpPost]
        [Route("AssignFolderPermissions")]

        public async Task<IActionResult> AssignFolderPermissions([FromBody] RequestFolderRoleViewModel model)
        {
            try
            {
                await _service.AssignFolderPermissions(model);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("AssignMediaPermissions")]

        public async Task<IActionResult> AssignMediaPermissions([FromBody] RequestMediaRoleViewModel model)
        {
            try
            {
                await _service.AssignMediaPermissions(model);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("AssignedMediaRoles/{Id}")]

        public async Task<IActionResult> AssignedMediaRoles([FromRoute] long Id)
        {
            try
            {
                
                return Ok(await _service.AssignedMediaRoles(Id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("AssignedFolderRoles/{Id}")]

        public async Task<IActionResult> AssignedFolderRoles([FromRoute] long Id)
        {
            try
            {

                return Ok(await _service.AssignedFolderRoles(Id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
