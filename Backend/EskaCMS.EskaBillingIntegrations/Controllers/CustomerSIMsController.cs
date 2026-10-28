using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.EskaBilling.Services;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Integrations.EskaBilling.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
namespace EskaCMS.Core.Controllers
{//Categories

    [Route("api/CustomerSIMs")]
    [ApiController]
    [Authorize]
    public class CustomerSIMsController : ControllerBase
    {


        private readonly IIOTService _iotService;
        public CustomerSIMsController(IIOTService _iotService)
        {

            _iotService = _iotService;
        }


     



        //[HttpPost]
        //[Route("AddCustomerSims/{CustomerId}")]
        //public async Task<IActionResult> AddCustomerSims([FromForm] IFormFile CustomerSims, [FromRoute] long CustomerId)
        //{
        //    try
        //    {
        //        return Ok(await _iotService.AddCustomerSim(CustomerSims, CustomerId));
        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc.Message);
        //    }
        //}

        [HttpGet]
        [Route("GetCustomerSims")]
        public async Task<IActionResult> GetCustomerSims(SmartTableParam param)
        {
            try
            {
                //return Ok(_iotService.GetCustomerSims(param));
                return Ok();
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}