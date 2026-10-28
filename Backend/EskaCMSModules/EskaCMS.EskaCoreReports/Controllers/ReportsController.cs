using EskaCMS.Core.Extensions;
using EskaCMS.EskaCoreReports.Services;
using EskaCMS.EskaCoreReports.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.EskaCoreReports.Controllers
{
    [ApiController]
    [Route("api/Reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ICoreReports _coreReportsService;
        private readonly IWorkContext _workContext;
        public ReportsController(ICoreReports coreReportsService, IWorkContext workContext)
        {
            _coreReportsService = coreReportsService;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("UpsertReport")]
        public async Task<IActionResult> UpsertReport([FromBody] EskaCoreReportVM eskaCoreReportVM)
        {
            try
            {
                return Ok(await _coreReportsService.UpsertReport(eskaCoreReportVM));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetReportBreadCrumb")]
        public async Task<IActionResult> GetReportBreadCrumb(SlugVM model)
        {
            try
            {
                return Ok(await _coreReportsService.GetReportBreadCrumb(model.Slug));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("ChangeReportStatus/{Id}/{Status}")]
        public async Task<IActionResult> ChangeReportStatus([FromRoute] long Id, [FromRoute] EStatus Status)
        {
            try
            {
                return Ok(await _coreReportsService.ChangeReportStatus(Id, Status));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetReportById/{Id}")]
        public async Task<IActionResult> GetReportById([FromRoute] long Id)
        {
            try
            {
                return Ok(await _coreReportsService.GetReportById(Id));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetReports")]
        public async Task<IActionResult> GetReports([FromBody] SmartTableParam param)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok(await _coreReportsService.GetReports(param,SiteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetReportHistory/{Id}")]
        public IActionResult GetReportHistory([FromRoute] long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok(_coreReportsService.GetReportHistory(Id));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetReportsByMenuId")]
        public async Task<IActionResult> GetReportsByMenuId(ReportsByMenuIdSearchVM Model )
        {
            try
            {
                return Ok(await _coreReportsService.GetReportsByMenuId(Model));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetReportsCount")]
        public async Task<IActionResult> GetReportsCount()
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _coreReportsService.GetReportsCount(SiteId));

            }
            catch(Exception exc)
            {
                return BadRequest(exc.Message);
            
            }
        }

        [HttpPost]
        [Route("GetReportsByPostAuthor")]
        public async Task<IActionResult> GetReportsByPostAuthor([FromBody] SmartTableParam Param)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var siteId = _workContext.GetCurrentSiteId();
                return Ok(await _coreReportsService.GetReportsByAuthors(Param,siteId, userId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
