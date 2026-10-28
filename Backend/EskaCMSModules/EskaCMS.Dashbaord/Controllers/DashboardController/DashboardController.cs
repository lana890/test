using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BusinessModels.BusinessModel;
using EskaCMS.Dashboard.Services.DashboardService.Interfaces;

namespace EskaCMS.Dashboard.Controllers.DashboardController
{

    [ApiController]
    [Route("api/Dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {

        private readonly IDashboard _DashboardService;
        private readonly IWorkContext _workcontrextservice;


        public DashboardController(IDashboard DashboardService, IWorkContext workcontrextservice)
        {
            _DashboardService = DashboardService;
            _workcontrextservice = workcontrextservice;
        }


        [HttpGet]
        [Authorize]
        [Route("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                //var UserId = long.Parse(Request.Headers["UserId"]);
                var UserId = await _workcontrextservice.GetCurrentUserId();
                var SiteId = _workcontrextservice.GetCurrentSiteId();
                var CulterId=_workcontrextservice.GetCurrentCultureId();

                return Ok(await _DashboardService.GetDashboardData(UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("GetAllGusets/{rangeType}")]
        public async Task<IActionResult> GetGusets(long rangeType)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _DashboardService.GetSiteGuest(SiteId, rangeType));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


    }
}