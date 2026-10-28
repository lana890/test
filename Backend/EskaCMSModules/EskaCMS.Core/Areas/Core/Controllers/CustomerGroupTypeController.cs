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
using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Request;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize]
    [Route("api/CustomerGroupType")]
    public class CustomerGroupTypeController : Controller
    {
        private readonly IGroupTypeService _groupTypeService;

        public CustomerGroupTypeController(IGroupTypeService groupTypeService)
        {
            _groupTypeService = groupTypeService;
        }

        
        [HttpPost("GetGroupTypes")]
        public async Task<ActionResult> GetGroupTypes([FromBody] SmartTableParam param)
        {
            try
            {
                var result = await _groupTypeService.GetAll(param);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Details/{groupTypeId}")]
        public async Task<ActionResult> Details([FromRoute] long groupTypeId)
        {
            try
            {
                var result = await _groupTypeService.Details(groupTypeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Create")]
        public async Task<ActionResult> Create([FromBody] CreateVM model)
        {
            try
            {
                var result = await _groupTypeService.Create(model);
                if (result)
                {
                    return Ok("The group type has been added");

                }

                return BadRequest("Failed to add new group type");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Update")]
        public async Task<ActionResult> Update([FromBody] UpdateVM model)
        {
            try
            {
                var result = await _groupTypeService.Update(model);
                if (result)
                {
                    return Ok("The group type has been updated");

                }

                return BadRequest("Failed to update group type");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Publish/{groupTypeId}")]
        public async Task<ActionResult> Publish([FromRoute] long groupTypeId)
        {
            try
            {
                var result = await _groupTypeService.Publish(groupTypeId);
                if (result)
                {
                    return Ok("The group type has been published");

                }

                return BadRequest("Failed to publish group type");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("UnPublish/{groupTypeId}")]
        public async Task<ActionResult> UnPublish([FromRoute] long groupTypeId)
        {
            try
            {
                var result = await _groupTypeService.UnPublish(groupTypeId);
                if (result)
                {
                    return Ok("The group type has been published");

                }

                return BadRequest("Failed to publish group type");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        
        [HttpPost("Delete/{groupTypeId}")]
        public async Task<ActionResult> Delete([FromRoute] long groupTypeId)
        {
            try
            {
                var result = await _groupTypeService.Delete(groupTypeId);
                if (result)
                {
                    return Ok("The group type has been published");

                }

                return BadRequest("Failed to publish group type");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
