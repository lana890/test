using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using EskaCMS.Core.Entities;

namespace EskaCMS.Core.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class OrganizationApiController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
        HttpRequestMessage request = new HttpRequestMessage();
        // private readonly IWorkContext _workContext;
        public OrganizationApiController(IOrganizationService organizationService/* , IWorkContext workContext */)
        {
            _organizationService = organizationService;
            // _workContext = workContext;

        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ViewModels.OrganizationAddVM org)
        {
            try
            {

                return Ok(await _organizationService.Create(org));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] ViewModels.OrganizationUpdateVM org)
        {
            try
            {

                return Ok(await _organizationService.Update(org));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                return Ok(await _organizationService.GetById(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                return Ok(await _organizationService.Delete(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        } 
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _organizationService.GetAll());
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetList([FromBody] ViewModels.OrganizationsSearchTableParam<ViewModels.OrganizationsListSearchVM> param)
        {
            try
            {

                return Ok(await _organizationService.GetList(param));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
    }
}