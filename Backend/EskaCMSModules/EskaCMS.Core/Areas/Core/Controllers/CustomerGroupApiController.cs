using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using System.Collections.Generic;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity.Catalog;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Security.Policy;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize]
    [Route("api/customergroups")]
    public class CustomerGroupApiController : Controller
    {
        private readonly IRepository<CustomerGroup> _customerGroupRepository;
        private readonly IRepository<CustomerGroupDetails> customergroupDetailsRepository;
        private readonly IWorkContext workContext;
        private readonly ICustomerGroup _ICustomer;
        private readonly IRepository<User> userRepository;

        public CustomerGroupApiController(
            IRepository<CustomerGroup> customergroupRepository,
            IRepository<CustomerGroupDetails> customergroupDetailsRepository,
            IWorkContext workContext, ICustomerGroup ICustomer,
            IRepository<User> userRepository


            )
        {
            _customerGroupRepository = customergroupRepository;
            this.customergroupDetailsRepository = customergroupDetailsRepository;
            this.workContext = workContext;
            _ICustomer = ICustomer;
            this.userRepository = userRepository;
        }

        [HttpPost("grid")]
        public async Task<ActionResult> List([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await _ICustomer.List(param));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {

                return Ok(await _ICustomer.Get());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("SiteGroups")]
        public async Task<IActionResult> GetSiteGroups()
        {
            try
            {
                long SiteId = await workContext.GetCurrentSiteIdAsync();
                return Ok(await _ICustomer.Get(SiteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet]
        [Route("GetUserGroupContactInfo")]
        public async Task<IActionResult> GetUserGroupContactInfo()
        {
            try
            {
                long SiteId = await workContext.GetCurrentSiteIdAsync();
                return Ok(await _ICustomer.GetUserGroupContactInfo(SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("GetGroupsByTopic/{Topic}")]
        public async Task<IActionResult> GetGroupsByTopic(string Topic)
        {
            long SiteId = await workContext.GetCurrentSiteIdAsync();
            var customerGroups = await _customerGroupRepository.Query().Include(x => x.customerGroupDetails)
                .Where(x => x.FireBaseTopic.ToLower() == Topic.ToLower() && (x.SiteId == SiteId || !x.SiteId.HasValue))
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.customerGroupDetails.Select(d => d.Name).FirstOrDefault(),
                    Topic = x.FireBaseTopic,
                    Email = x.Email,
                    MaxSessions = x.MaxSessionsNumber,
                    PasswordExpiryInDays = x.PasswordExpiryInDays,
                    EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                    ImageUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,

                }).ToListAsync();

            return Json(customerGroups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var customerGroup = await _customerGroupRepository.Query().AsNoTracking()
                                                              .Include(x => x.Thumbnail)
                                                              .Include(x => x.customerGroupDetails)
                                                              .FirstOrDefaultAsync(x => x.Id == id);
            if (customerGroup == null)
            {
                return NotFound();
            }

            var model = new CustomerGroupForm
            {
                Id = customerGroup.Id,
                customerGroupDetails = customerGroup.customerGroupDetails.Select(x => new CustomerGroupDetailsForm
                {

                    Description = x.Description,
                    CultureId = x.CultureId.Value,
                    Name = x.Name,

                }).ToList(),
                Description = customerGroup.customerGroupDetails.Select(x => x.Description).FirstOrDefault(),
                Name = customerGroup.customerGroupDetails.Select(x => x.Name).FirstOrDefault(),
                IsActive = customerGroup.IsActive,
                FireBaseTopic = customerGroup.FireBaseTopic,
                Email = customerGroup.Email,
                MaxSessions = customerGroup.MaxSessionsNumber,
                PasswordExpiryInDays = customerGroup.PasswordExpiryInDays,
                EnableTwoFactorAuthentication = customerGroup.EnableTwoFactorAuthentication,
                GroupTypeId = customerGroup.GroupTypeId,
                ImageUrl = customerGroup.ThumbnailId.HasValue ? customerGroup.Thumbnail.PublicUrl : null,
                ThumbnailId = customerGroup.ThumbnailId

            };

            return Json(model);
        }

        [HttpPost("Upsert")]
        public async Task<IActionResult> Upsert([FromBody] CustomerGroupForm model)
        {
            try
            {
                if (model.Id!=0)
                {
                    return Ok(await _ICustomer.Put(model));
                }
                else {

                    return Ok(await _ICustomer.Post(model));

                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }




        }


   
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var customerGroup = await _customerGroupRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (customerGroup == null)
            {
                return NotFound();
            }

            customerGroup.IsDeleted = true;
            await _customerGroupRepository.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("GetByGroupType/{GroupTypeId}")]
        public async Task<IActionResult> GetByGroupType([FromRoute] long GroupTypeId)
        {
            try
            {
                var result = await _ICustomer.GetCustomerGroupByType(GroupTypeId);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("GetByGroupTypeAndUser/{GroupTypeId}")]
        [HttpGet("GetByGroupTypeAndUser")]
        public async Task<IActionResult> GetByGroupTypeAndUser([FromRoute] long? GroupTypeId)
        {
            try
            {
                var result = await _ICustomer.GetGroupByTypeAndUser(GroupTypeId, null);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("GetByGroupTypeAndUser/{GroupTypeId}")]
        [HttpPost("GetByGroupTypeAndUser")]
        public async Task<IActionResult> GetByGroupTypeAndUser([FromRoute] long? GroupTypeId, [FromBody] CustomerGroupFiltersVM model)
        {
            try
            {
                var result = await _ICustomer.GetGroupByTypeAndUser(GroupTypeId, model.Name);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpPost]
        [Route("GetUsersByGroupIdWithFilters/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetUsersByGroupIdWithFilters([FromRoute] long groupId, [FromBody] SmartTableParam model)
        {
            try
            {
                return Ok(await _ICustomer.GetUsersByGroupIdWithFilters(groupId, model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetUnAssignedGroups")]
        [Authorize]
        public async Task<IActionResult> GetUnAssignedGroups([FromBody] SmartTableParam model)
        {
            try
            {
                return Ok(await _ICustomer.GetUnAssignedGroups(model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost("JoinGroup/{GroupId}")]
        public async Task<IActionResult> JoinGroup([FromRoute] long GroupId)
        {
            try
            {
                var result = await _ICustomer.JoinGroup(GroupId);

                if (result)
                    return Ok("You have successfully joined the group");

                return BadRequest("Failed to join the customer group");

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }

        [HttpPost("LeaveGroup/{GroupId}")]
        public async Task<IActionResult> LeaveGroup([FromRoute] long GroupId)
        {
            try
            {
                var result = await _ICustomer.LeaveGroup(GroupId);

                if (result)
                    return Ok("You have successfully left the group");

                return BadRequest("Failed to leave the customer group");
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost("AddBulkUserToGroup")]
        public async Task<IActionResult> AddBulkUserToGroup([FromBody] AddUserToGroupDto model)
        {
            try
            {
                var result = await _ICustomer.AddBulkUserToGroup(model);

                if (result)
                    return Ok("You have successfully added users to the group");

                return BadRequest("Failed to added users");
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }


    }
}
