using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;

using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.TelrPayment.Models;
using EskaCommerce.TelrPayment.Services;
using EskaCommerce.TelrPayment.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.TelrPayment.Controllers
{
    [Area("TelrPayment")]
    //[Authorize(Roles = "admin")]
    public class TelrPaymentController : Controller
    {


        private readonly ITelrPayment _TelrPayment;
        private readonly IdcmsLogService _dcmsLogService;

        public TelrPaymentController(ITelrPayment TelrPayment, IdcmsLogService dcmsLogService)
        {
            _TelrPayment = TelrPayment;
            _dcmsLogService = dcmsLogService;
        }


        [Route("api/TelrRequest")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TelrRequest(CartTypes CartType)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _TelrPayment.TelrPlanRequest(SiteId, CartType));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [Route("api/GetTransactionsSearch")]
        [HttpGet]
        public async Task<IActionResult> GetTransactionsSearch(long OrderId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _TelrPayment.GetTransactionsSearch(OrderId, SiteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [Route("api/PaymentCancelation")]
        [HttpGet]
        public async Task<IActionResult> PaymentCancelation(long UserPlanId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                await _TelrPayment.PaymentCancelation(UserPlanId, SiteId);
                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [Route("api/RegisterTelrPayment")]
        [HttpPost]
        public async Task<IActionResult> RegisterTelrPayment()
        {
            try
            {


                var dict = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                await _TelrPayment.RegisterTelrPayment(dict, 1);
                return Ok();
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError("TelrPaymentController", exc.Message, exc.Message, null, null);
                return BadRequest(exc.Message);
            }
        }
      
    }


}
