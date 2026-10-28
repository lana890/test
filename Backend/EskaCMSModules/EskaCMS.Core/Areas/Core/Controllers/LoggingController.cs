using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.LogError;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Route("api/Logging")]
    public class LoggingController : ControllerBase
    {
        private readonly IdcmsLogService idcmsLogService;
        private readonly IWorkContext workContext;

        public LoggingController(IdcmsLogService IdcmsLogService,IWorkContext WorkContext)
        {
            idcmsLogService = IdcmsLogService;
            workContext = WorkContext;
        }


        [HttpPost]
        [Route("LogError")]
        public async Task<IActionResult> LogError([FromBody] ErrorLogVM model)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var SiteId = workContext.GetCurrentSiteId();

                await idcmsLogService.AddError(userId, model.Module, model.exception, model.Message + " ,From Frontend", model.Scope, model.AdditionalInfo, SiteId);



                return Ok();

            }
            catch (Exception ex)
            {

               return BadRequest(ex.Message);
            }


        }
        [HttpPost("GetErrorsLog")]

        public async Task<IActionResult> GetErrorsLog([FromBody] RequestErrorLogVM request )
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var Result = await idcmsLogService.GetErrorsLog(request, SiteId);

                return Ok(Result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }         
        
        }
    }
}
