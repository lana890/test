using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Controllers
{
    [Route("api/LoyaltyWallet/[action]")]
    [ApiController]
    [Authorize]
    public class LoyaltyWalletController : Controller
    {
        private readonly ILoyaltyWalletService _loyaltyWalletService;
        private readonly IWorkContext _workContext;
        public LoyaltyWalletController(ILoyaltyWalletService loyaltyWalletService, IWorkContext workContext)
        {
            _loyaltyWalletService = loyaltyWalletService;
            _workContext= workContext;
        }

       



        //for admin and web
        [HttpGet("{userId}")]
        [HttpGet]
        public async Task<IActionResult> UserWallet(long? userId)
        {
            try
            {
                var Id = userId.HasValue ? userId : await _workContext.GetCurrentUserId();
                var result = await _loyaltyWalletService.GetLoyaltyWallet(Id.Value);
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost("{userId}")]
        [HttpPost]
        public async Task<IActionResult> GetLoyaltyTransactions([FromRoute]long? userId,[FromBody] SmartTableParam model)
        {
            try
            {
                var Id = userId.HasValue ? userId : await _workContext.GetCurrentUserId();
                var result = await _loyaltyWalletService.GetLoyaltyTransactions(Id.Value,model);
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpPost("{userId}")]
        public async Task<IActionResult> DepositUserWallet([FromRoute] long userId, [FromBody] AddPointsDto model)
        {
            try
            {
               
                var result = await _loyaltyWalletService.DepositUserWalletByAdmin(userId, model);
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpPost("{userId}")]
        public async Task<IActionResult> SuspendUserWallet([FromRoute] long userId)
        {
            try
            {
                var result = await _loyaltyWalletService.SuspendLoyaltyWallet(userId);
                if(result)
                return Ok("User wallet has been suspended");

                return BadRequest(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> ActiveUserWallet([FromRoute] long userId)
        {
            try
            {
                var result = await _loyaltyWalletService.ActiveLoyaltyWallet(userId);
                if (result)
                    return Ok("User wallet has been Activated");

                return BadRequest(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


    }
}
