using EskaCMS.Core.Extensions;
using EskaCMS.General.Services.Interfaces;
using EskaCMS.General.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.General.Controllers
{
    [ApiController]
    [Route("api/Lookup")]
    [Authorize]


    public class LookupController : ControllerBase
    {
        private readonly ILookupServices _lookupServices;
        // private readonly IWorkContext _workContext;

        public LookupController(ILookupServices lookupServices/* ,IWorkContext workContext */)
        {
            _lookupServices = lookupServices;
            // _workContext = workContext;
        }




        [HttpGet]
        [Route("GetLookup")]
        public async Task<IActionResult> GetLookup()
        {
            try
            {
                var result = await _lookupServices.GetLookup();

                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetLookupById/{lookupId}")]
        [Authorize]
        public async Task<IActionResult> GetLookupById(long lookupId)
        {
            try
            {

                return Ok(await _lookupServices.GetLookupById(lookupId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetLookupValuesbyParentIdAndLookupId/{LookupId}/{ParentId?}")]
        [Authorize]
        public async  Task<IActionResult> GetLookupValues([FromRoute] long LookupId, [FromRoute] long? ParentId = null)
        {
            try
            {

                return Ok(await _lookupServices.GetLookupValues(LookupId, ParentId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }

        }
        [HttpGet]
        [Route("GetLookupValuesbylookupId/{lookupId}/{ParentId?}")]
        [Authorize]
        public async Task<IActionResult> GetLookupValuesbylookupId(long lookupId,long? ParentId=null)
        {
            try
            {

                return Ok(await _lookupServices.GetLookupValuesbylookupId(lookupId,ParentId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }

        }

        [HttpGet]
        [Route("GetLookupByLookupIdAndName/{lookupId}/{Name}")]
        [Authorize]
        public async Task<IActionResult> GetLookupByLookupIdAndName([FromRoute]long lookupId, [FromRoute] string Name)
        {
            try
            {

                return Ok(await _lookupServices.GetLookupByLookupIdAndName(lookupId, Name));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }

        }

        [HttpGet]
        [Route("GetLookupValues")]
        [Authorize]
        public async Task<IActionResult> GetLookupValues()
        {
            try
            {
                return Ok(await _lookupServices.GetLookupValues());
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }

        }
    
        [HttpPost]
        [Route("LookupValueSearch/{LookupId}")]
        [Authorize]
        public async Task<IActionResult> GetLookupValueList(long LookupId, LookupSearchVM lookupSearchVM)
        {
            try
            {
                var result = await _lookupServices.GetLookupValueList(LookupId, lookupSearchVM);


                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }

        }
    }
}
