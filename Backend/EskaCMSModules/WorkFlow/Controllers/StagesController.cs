using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Helpers;
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
    [Route("api/WorkFlows/Stages")]
    [ApiController]
    [Authorize]
    public class StagesController : Controller
    {
        private readonly IStageServices _stageServices;
        // Confirmed unused: never assigned and never read (compiler warning CS0169).
        // private readonly IWorkContext _workContext;

        public StagesController(IStageServices stageServices)
        {
                _stageServices=stageServices;
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] CreateStageDto model)
        {
            try
            {
                var result = await _stageServices.CreateStage(model);
                if (result)
                    return Ok("Stage has been added");

                return BadRequest("Failed to create Stage");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
        
        [HttpPost]
        [Route("Update")]
        public async Task<IActionResult> Update([FromBody] EditStageDto model)
        {
            try
            {
                var result = await _stageServices.UpdateStage(model);
                if (result)
                    return Ok("Stage has been updated");

                return BadRequest("Failed to update Stage");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("UpdateStageOrdering")]
        public async Task<IActionResult> UpdateStageOrdering([FromBody] List<StageOrderDto> model)
        {
            try
            {
                var result = await _stageServices.EditMultipleOrderStage(model);
                if (result)
                    return Ok("Stage ordering has been updated");

                return BadRequest("Failed to update stage ordering");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("Delete/{StageId}")]
        public async Task<IActionResult> Delete([FromRoute] long StageId )
        {
            try
            {
                var result = await _stageServices.DeleteStage(StageId);
                if (result)
                    return Ok("Stage has been deleted");

                return BadRequest("Failed to deete Stage");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("RejectionBehavior")]
        public async Task<IActionResult> RejectionBehavior()
        {
            try
            {
                var enumValues = new List<object>();

                foreach (var item in Enum.GetValues(typeof(ERejectionBehavior)))
                {

                    enumValues.Add(new
                    {
                        id = (int)item,
                        name = ((ERejectionBehavior)item).GetDisplayName()
                    });
                }

                return Ok(enumValues);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpGet]
        [Route("StageType")]
        public async Task<IActionResult> StageType()
        {
            try
            {
                var enumValues = new List<object>();

                foreach (var item in Enum.GetValues(typeof(EStageType)))
                {

                    enumValues.Add(new
                    {
                        id = (int)item,
                        name = ((EStageType)item).GetDisplayName()
                    });
                }

                return Ok(enumValues);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        [HttpGet]
        [Route("GetApprovalTypes")]
        public async Task<IActionResult> GetApprovalTypes()
        {
            try
            {

                var model = EnumHelper.ToDictionary(typeof(EApprovalStep)).Select(x => new { Id = x.Key, Name = x.Value });
                return Json(model);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

    }
}
