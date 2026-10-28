using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
namespace EskaCMS.Core.Controllers
{//Categories

    [Route("api/ApiInvoker")]
    [ApiController]
    [Authorize]
    public class ApiInvokerController : ControllerBase
    {
        private readonly IApiMapper _ApiMapperService;
        private readonly IWorkContext _workContext;

        HttpRequestMessage request = new HttpRequestMessage();
        public ApiInvokerController(IApiMapper ApiMapperService, IWorkContext workContext)
        {
            _ApiMapperService = ApiMapperService;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("TestInvokerAPI")]
        public async Task<IActionResult> TestInvokerAPI([FromBody] ApiMapperViewModel model)
        {
            try
            {
                var listParams = new List<string>();
                var token = Request.Headers["token"];


                var result = await _ApiMapperService.TestInvokerAPI(model, model.obj, await _workContext.GetCurrentUserId(), token);

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("StaticInvoke")]
        public async Task<IActionResult> Invoke([FromBody] StaticInvokeVM model)
        {
            try
            {

                var result = await _ApiMapperService.StaticInvoke(model);
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("OldInvoke/{ApiId}")]
        public async Task<IActionResult> OldInvoke(int ApiId, [FromBody] JObject obj)
        {
            try
            {
                // only needed if JWT Token and saved on local storage
                var token = Request.Headers["token"].ToString();
                //
                var userId = await _workContext.GetCurrentUserId();

                var result = await _ApiMapperService.Invoke(ApiId, obj, userId, token);
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("InvokeWithFiles/{ApiId}")]
        public async Task<IActionResult> InvokeWithFiles([FromRoute] long ApiId)
        {
            try
            {
                var formFiles = Request.Form;
                var invokeObj = new JObject();
                var userId = await _workContext.GetCurrentUserId();
                if (!string.IsNullOrEmpty(Request.Form["obj"]))
                    invokeObj = JObject.Parse(Request.Form["obj"]);

                foreach (var key in formFiles.Keys)
                {
                    if (string.Equals(key, "obj", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    invokeObj[key] = formFiles[key].ToString();
                }
                 
                var token = Request.Headers["token"].ToString();
                
                var result = await _ApiMapperService.HandleWebAPIRequests(ApiId, invokeObj, userId, token, formFiles);
                // ✅ FILE CASE
                if (result is InvokerFileResult fileDto)
                {

                    return File(
                        fileDto.FileBytes,
                        fileDto.ContentType ?? "application/octet-stream",
                        fileDto.Name
                    );
                }
                return Ok(result);

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    var exArray = ex.Message.Split("-Invoker-");
                    if (exArray.Length > 1 && exArray[0] == ((int)HttpStatusCode.Unauthorized).ToString())
                    {
                        return new StatusCodeResult(452);
                    }
                    else
                    {

                        return BadRequest(exArray);
                    }
                }

                return BadRequest(ex.Message != null ? ex.Message : "Bad Request!");
            }
        }

        [HttpPost]
        [Route("Invoke/{ApiId}")]
        public async Task<IActionResult> Invoke([FromRoute] long ApiId, [FromBody] dynamic invokeObj)
        {
            try
            {
               
                // only needed if JWT Token and saved on local storage
                var token = Request.Headers["token"].ToString();
                if (string.IsNullOrEmpty(token) && invokeObj["token"] != null)
                {
                    token = invokeObj["token"].ToString();
                }
                //
                var userId = await _workContext.GetCurrentUserId();

                var result = await _ApiMapperService.HandleWebAPIRequests(ApiId, invokeObj, userId, token);
                // ✅ FILE CASE
                if (result is InvokerFileResult fileDto)
                {

                    return File(
                        fileDto.FileBytes,
                        fileDto.ContentType ?? "application/octet-stream",
                        fileDto.Name
                    );
                }
                return Ok(result);

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    var exArray = ex.Message.Split("-Invoker-");
                    if (exArray.Length > 1 && exArray[0] == ((int)HttpStatusCode.Unauthorized).ToString())
                    {
                        return new ObjectResult("Unauthorized Invoker Token") { StatusCode = 452 };
                    }
                    else
                    {

                        return BadRequest(exArray[0]);
                    }
                }

                return BadRequest(ex.Message != null ? ex.Message : "Bad Request!");
            }
        }


        [HttpPost]
        [Route("addEditAPIMapper")]
        public async Task<IActionResult> addEditAPIMapper(ApiMapperViewModel AddAPIOBJ)
        {
            try
            {
                long result = 0;
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                AddAPIOBJ.SiteId = SiteId;
                if (AddAPIOBJ.Id != 0)
                {
                    //  result = await _ApiMapperService.EditApiMapper(AddAPIOBJ, SiteId, UserId);
                }
                else
                {
                    // result = await _ApiMapperService.AddApiMapper(AddAPIOBJ, SiteId, UserId);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }




		[HttpPost]
        [Route("GetApiMapperTransactionHistory")]
        public async Task<IActionResult> GetApiMapperTransactionHistory([FromBody] SearchApiMapperTransactionHistory model)
        {
            try
            {
                return Ok(await _ApiMapperService.GetApiMapperTransactionHistory(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetApiMapperBySiteId")]
        public async Task<IActionResult> GetApiMapperBySiteId()
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                //var UserId = int.Parse(Request.Headers["UserId"]);

                List<ApiMapperViewModel> MapperList = await _ApiMapperService.GetApiMapperSiteId(SiteId);

                return Ok(MapperList);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetApiMapperById/{id}")]
        public async Task<IActionResult> GetApiMapperById(long id)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserId = await _workContext.GetCurrentUserId();

                ApiMapperViewModel MaperObj = await _ApiMapperService.GetApiMapperById(id);

                return Ok(MaperObj);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetMapperParamsByMapperId/{mapperId}/{direction}")]
        public async Task<IActionResult> GetMapperParamsByMapperId(long mapperId, ParameterDirection direction)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();

                dynamic MapperParmasList = await _ApiMapperService.GetMapperParamsByMapperId(mapperId, SiteId, UserId, direction);

                return Ok(MapperParmasList);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpDelete]
        [Route("DeleteParams/{paramName}")]
        public async Task<IActionResult> DeleteParams(string name)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                var result = await _ApiMapperService.DeletMapperParams(name, SiteId, UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }




		[HttpDelete]
		[Route("DeleteParamsChild/{id}")]
		public async Task<IActionResult> DeleteParamsChild(long id)
		{
			try
			{
				var SiteId = int.Parse(Request.Headers["siteId"].ToString());
				var UserId = await _workContext.GetCurrentUserId();
				var result = await _ApiMapperService.DeletMapperParamsChild(id, SiteId, UserId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex);
			}
		}



		[HttpPost]
        [Route("AddDataSource")]
        public async Task<IActionResult> AddDataSource(DatasourceVM datasourceVM)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _ApiMapperService.AddApiMapper(datasourceVM, SiteId, UserId));
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("EditDataSource")]
        public async Task<IActionResult> EditDataSource(DatasourceVM datasourceVM)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _ApiMapperService.EditApiMapper(datasourceVM, SiteId, UserId));

            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// GetAuthentication from User Options API
        /// </summary>
        /// 

        [HttpGet]
        [Route("GetAuthentication/{propertyName}/{refreshProperty}")]
        public async Task<IActionResult> GetAuthentication(string propertyName, string refreshProperty)
        {
            try
            {
                return Ok(await _ApiMapperService.GetAuthenticationUserOptions(propertyName, refreshProperty));

            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        ////////Controller  for luay API //////Start

        [HttpGet]
        [Route("GetDataSourceListBySiteId")] 
        public async Task<IActionResult> GetDataSourceListBySiteId()
        {
            try
            {
                long SiteId = long.Parse(Request.Headers["siteId"].ToString());

				List<ApiMapperDetailsViewModel> result = await _ApiMapperService.GetDataSourceListBySiteId(SiteId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);

            }
        }

        [HttpGet]
        [Route("GetDatasourceById/{id}")]
        public async Task<IActionResult> GetDatasourceById(long Id)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                ApiMapperDetailsViewModel result = await _ApiMapperService.GetDatasourceById(Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("GetDatasourceByName")]
        public async Task<IActionResult> GetDatasourceByName(string name, [FromBody] JObject obj)
        {
            try
            {
                var token = Request.Headers["token"].ToString();
                int userId = Convert.ToInt32(await _workContext.GetCurrentUserId());

                dynamic result = await _ApiMapperService.GetDatasourceByName(name, obj, userId, token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteDataSource/{Id}")]
        public IActionResult DeleteDataSource(long Id)
        {
            try
            {
                var result = _ApiMapperService.DeleteDataSource(Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetInputValues/{id}")]
        public async Task<IActionResult> GetInputValues(long Id)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["siteId"].ToString());
                var result = await _ApiMapperService.GetInputValues(Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("LinkChildParamToParent")]
        public async Task<IActionResult> LinkChildParamToParent([FromBody] LinkChildParamToParentRequest req)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var result=await _ApiMapperService.LinkChildParamToParent(req, UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Shows what a path reads out of a parent API response, to pick and check an
        /// APIMapperParamChild.ResponsePath before the child link is saved: it returns the value that path
        /// resolves to, read exactly the way the invoker reads it, plus every path the response offers.
        /// </summary>
        [HttpPost]
        [Route("PreviewResponsePath")]
        public async Task<IActionResult> PreviewResponsePath([FromBody] ResponsePathPreviewRequest req)
        {
            try
            {
                var token = Request.Headers["token"];
                var UserId = await _workContext.GetCurrentUserId();
                var result = await _ApiMapperService.PreviewResponsePath(req, UserId, token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        ////////Controller  for luay API //////End

    }
}
