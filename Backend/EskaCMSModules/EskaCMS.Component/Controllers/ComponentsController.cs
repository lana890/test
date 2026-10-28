using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using static BusinessModels.BusinessModel;
using EskaCMS.Core.Data;

using EskaCMS.Components.Models;
using EskaCMS.Components.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Extensions;

namespace EskaCMS.Components.Controllers
{

    [ApiController]
    [Route("api/Components")]
    [Authorize]
    public class ComponentsController : ControllerBase
    {
      
        private readonly IComponent _componentServices;
        private readonly IWorkContext _workContext;

        public ComponentsController(IComponent ComponentsService , IWorkContext workContext)
        {
            _componentServices = ComponentsService;
            _workContext = workContext;
        }


        [HttpPost]
        [Route("SaveComponent")]
        public async Task<IActionResult> SaveComponent(ComponentViewModel comp)
        {
            try
            {
               
                comp.UserId = await _workContext.GetCurrentUserId();
                comp.SiteId = long.Parse(Request.Headers["SiteId"].ToString());
               // comp.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());

                return Ok(await _componentServices.SaveComponent(comp));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("DeleteComponent/{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var result = await _componentServices.DeleteComponent(UserId, id);

                return Ok(result.Id);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpGet]
        [Route("GetAllComponents")]
       // [Authorize]
        public async Task<IActionResult> GetAllComponents()
        {
            try
            {
                var siteId = long.Parse(Request.Headers["SiteId"].ToString());

                return Ok(await _componentServices.GetAllComponents(siteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetComponentByID/{ID}")]
        public async Task<IActionResult> GetComponentByID(long ID)
        {
            try
            {
                return Ok(await _componentServices.GetComponentByID(ID));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        
        //[HttpPost]
        //[Route("SaveComponentCategory/{siteId}")]
        //public async Task<IActionResult> SaveComponentCategory(long siteId,ComponentViewModel cat)
        //{
        //    try
        //    {
        //        var UserId = int.Parse(Request.Headers["UserId"]);

        //        return Ok(await _componentServices.SaveComponentCategory(siteId,cat.Name, cat.Id, UserId));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}


        [HttpPost]
        [Route("SaveComponentSettings")]
        public async Task<IActionResult> SaveComponentSettings(ComponentViewModel cat)
        {
            try
            {
               

                return Ok(await _componentServices.SaveComponentSettings(cat.Settings, cat.Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        
        [HttpPost]
        [Route("ShareComponent")]
        public async Task<IActionResult> ShareComponent(ComponentViewModel cat)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());
                cat.SiteId = SiteId;
                cat.UserId = UserId;
                return Ok(await _componentServices.ShareComponentAsync(cat));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

    }
}