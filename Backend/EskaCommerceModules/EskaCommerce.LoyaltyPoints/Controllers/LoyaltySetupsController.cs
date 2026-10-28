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
    [Route("api/LoyaltySetups/[action]")]
    [ApiController]
    [Authorize]
    public class LoyaltySetupsController : Controller
    {
        private readonly ILoyaltySetupService _loyaltySetupService;
        public LoyaltySetupsController(ILoyaltySetupService loyaltySetupService)
        {
            _loyaltySetupService = loyaltySetupService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoyaltySetupDto model)
        {
            try
            {
                var result =  await _loyaltySetupService.CreateLoyaltySetup(model);
                if (result)
                    return Ok("Loyalty Setup has been added");

                return BadRequest("Failed to create loyalty setup");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost("{loyltySetupId}")]
        public async Task<IActionResult> Update([FromBody] LoyaltySetupDto model ,[FromRoute]long loyltySetupId)
        {
            try
            {
                var result = await _loyaltySetupService.UpdateLoyaltySetup(model, loyltySetupId);
                if (result)
                    return Ok("Loyalty Setup has been updated");

                return BadRequest("Failed to update loyalty setup");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet("{loyltySetupId}")]
        public async Task<IActionResult> Details( [FromRoute]long loyltySetupId)
        {
            try
            {
                var result = await _loyaltySetupService.LoyaltySetupDetails(loyltySetupId);
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpDelete("{loyltySetupId}")]
        public async Task<IActionResult> Delete([FromRoute] long loyltySetupId)
        {
            try
            {
                var result = await _loyaltySetupService.DeleteLoyaltySetup(loyltySetupId);
                if (result)
                    return Ok("Loyalty Setup has been deleted");

                return BadRequest("Failed to delete loyalty setup");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost("{loyltySetupId}")]
        public async Task<IActionResult> DeactivateLoyaltySetup([FromRoute] long loyltySetupId)
        {
            try
            {
                var result = await _loyaltySetupService.DeactivateLoyaltySetup(loyltySetupId);
                if (result)
                    return Ok("Loyalty Setup has been deactivated");

                return BadRequest("Failed to deactivate loyalty setup");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost("{loyltySetupId}")]
        public async Task<IActionResult> ActivateLoyaltySetup([FromRoute] long loyltySetupId)
        {
            try
            {
                var result = await _loyaltySetupService.ActivateLoyaltySetup(loyltySetupId);
                if (result)
                    return Ok("Loyalty Setup has been activated");

                return BadRequest("Failed to active loyalty setup");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetAllLoyaltySetup([FromBody] SmartTableParam param)
        {
            try
            {
                var result = await _loyaltySetupService.GetAllLoyaltySetups(param);
               
                    return Ok(result);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
    }
}
