using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.WorkFlow.Controllers
{
    [Route("api/WorkFlows")]
    [ApiController]
    [Authorize]
    public class WorkFlowsController : Controller
    {
        private readonly IWorkFlowService _workFlowService;
        private readonly IWorkFlowItemService _workFlowIemService;
        // private readonly IWorkContext _workContext;
        public WorkFlowsController(IWorkFlowService workFlowService,
                                    /* IWorkContext workContext, */
                                    IWorkFlowItemService workFlowIemService)
        {
                _workFlowService = workFlowService;
                // _workContext = workContext;
                _workFlowIemService = workFlowIemService;
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] CreateWorkFlowDto model)
        {
            try
            {
                var result = await _workFlowService.CreateWorkFlow(model);
                if (result)
                    return Ok("WorkFlow has been added");

                return BadRequest("Failed to create WorkFlow");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("Update")]
        public async Task<IActionResult> Update([FromBody] EditWorkFlowDto model)
        {
            try
            {
                var result = await _workFlowService.EditWorkFlow(model);
                if (result)
                    return Ok("WorkFlow has been updated");

                return BadRequest("Failed to update WorkFlow");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("Details/{WorkFlowId}")]

        public async Task<IActionResult> Details([FromRoute] long WorkFlowId)
        {
            try
            {
                var result = await _workFlowService.GetWorkFlowDetails(WorkFlowId);
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAllWorkFlows")]
        public async Task<IActionResult> GetAllWorkFlows([FromBody] SmartTableParam param)
        {
            try
            {
                var result = await _workFlowService.GetAllWorkFlows(param);

                return Ok(result);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("Delete/{WorkFlowId}")]
        public async Task<IActionResult> Delete([FromRoute] long WorkFlowId)
        {
            try
            {
                var result = await _workFlowService.DeleteWorkFlow(WorkFlowId);
                if (result)
                {
                    return Ok("WorkFlow has been deleted");

                }

                return BadRequest("Failed to delete WorkFlow");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
        
        [HttpPost]
        [Route("ActiveWorkFlow/{WorkFlowId}")]
        public async Task<IActionResult> ActiveWorkFlow([FromRoute] long WorkFlowId)
        {
            try
            {
                var result = await _workFlowService.ActiveWorkFlow(WorkFlowId);
                if (result)
                    return Ok("WorkFlow  has been acivated");

                return BadRequest("Failed to acivate WorkFlow");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
        
        [HttpPost]
        [Route("DeActivateWorkFlow/{WorkFlowId}")]
        public async Task<IActionResult> DeActivateWorkFlow([FromRoute] long WorkFlowId)
        {
            try
            {
                var result = await _workFlowService.DeActivateWorkFlow(WorkFlowId);
                if (result)
                    return Ok("WorkFlow has been deactivated");

                return BadRequest("Failed to deactivate WorkFlow");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("CreateWorkflowItem")]
        public async Task<IActionResult> CreateWorkflowItem([FromBody] CreateWorkFlowItemDto model)
        {
            try
            {
                var result = await _workFlowIemService.CreateWorkFlowItem(model);
               
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [Route("GetWorkFlowItemDetails/{WorkFlowItemId}")]
        public async Task<IActionResult> GetWorkFlowItemDetails([FromRoute] long WorkFlowItemId)
        {
            try
            {
                var result = await _workFlowIemService.WorkFlowItemDetails(WorkFlowItemId);
              
                  
                return Ok(result);

               
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
        
        [HttpPost]
        [Route("GetAllWorkFlowItem/{WorkFlowId}")]
        public async Task<IActionResult> GetAllWorkFlowItem([FromBody]SmartTableParam param, [FromRoute]long WorkFlowId)
        {
            try
            {
                var result = await _workFlowIemService.GetAllWorkItem(param,WorkFlowId);
              
                  
                return Ok(result);

               
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAllWorkFlowItemPerUser/{WorkFlowId}")]
        public async Task<IActionResult> GetAllWorkFlowItemPerUser([FromBody] SmartTableParam param, [FromRoute] long WorkFlowId)
        {
            try
            {
                var result = await _workFlowIemService.GetAllWorkItemPerUser(param, WorkFlowId);


                return Ok(result);


            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAllWorkFlowItemForWeb")]
        public async Task<IActionResult> GetAllWorkFlowItemForWeb([FromBody] SmartTableParam param)
        {
            try
            {
                var result = await _workFlowIemService.GetAllWorkItemForWeb(param);


                return Ok(result);


            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("ActionOnWorkflowItem")]
        public async Task<IActionResult> ActionOnWorkflowItem([FromBody] ActionOnItemDto model)
        {
            try
            {
                var result = await _workFlowIemService.ActionOnWorkFlowItem(model);


                return Ok(result);


            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetLastWorkFlowItem")]
        public async Task<IActionResult> GetLastWorkFlowItem()
        {
            try
            {
                var result = await _workFlowIemService.GetLastWorkFlowItem();


                return Ok(result);


            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpGet("WorkFLowType")]
        public IActionResult WorkFLowType()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EWorkFlowType)))
            {
              
                    result.Add(new
                    {
                        Value = (int)item,
                        Label = ((EWorkFlowType)item).GetDisplayName()
                    });

                

            }

            return Ok(result);

        }
        
        [HttpGet("WorkFLowItemStatus")]
        public IActionResult WorkFLowItemStatus()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EFlowStatus)))
            {
              
                    result.Add(new
                    {
                        Value = (int)item,
                        Label = ((EFlowStatus)item).GetDisplayName()
                    });

                

            }

            return Ok(result);

        }
    }
}
