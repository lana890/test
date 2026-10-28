using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserPlanss.Controllers
{
    [Route("api/UserPlans/[action]")]
    [ApiController]
    [Authorize]
    public class UserPlansController : ControllerBase
    {
        private readonly IUserPlansService _planServices;
        private readonly ISubscriptionServices subscriptionServices;

        public UserPlansController(IUserPlansService planServices, ISubscriptionServices subscriptionServices)
        {
            _planServices = planServices;
            this.subscriptionServices = subscriptionServices;
        }

        [HttpPost]
        public async Task<IActionResult> Create(long planId,long OrderId,string AgreementId)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                await _planServices.Create(planId,siteId, OrderId, AgreementId, EUserPlansStatus.WaitingPayment, EUserPlansStatus.Subscribed, false);
                return Ok("UserPlans has been added successfully");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult Get(int id)
        {
            try
            {
                return Ok(_planServices.Get(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPlanLatestVersion(long planId)
        {
            try
            {
                return Ok(await _planServices.GetPlanLatestVersion(planId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] UserPlansUpdate category)
        {
            try
            {
                await _planServices.Update(category);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody]SmartTableParam input)
        {
            try
            {
                return Ok(await _planServices.GetAll(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetUserPlans()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetUserPlans(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetUserProfileServices()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetUserProfileServices(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Produces(typeof(IEnumerable<UserInvoicesOutputVM>))]
        public async Task<ActionResult<IEnumerable<UserInvoicesOutputVM>>> GetUserInvoices()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetUserInvoices(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UserPlanCancelation(UserPlansCancellation model)
        {
            try
            {

                string DecryptUserPlanId = EncryptDecryptHelper.Decrypt(model.UserPlanId);
                               
                var UserPlan = await _planServices.GetUserPlanByUserPlanId(Convert.ToInt64(DecryptUserPlanId));

                await subscriptionServices.CnacelUserPlan(UserPlan, false);

                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


    }
}
