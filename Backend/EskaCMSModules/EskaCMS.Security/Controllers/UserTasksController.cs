using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Filters.ActionFilters;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCMS.Security.Models.TaskModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using static EskaCMS.Entities.Enum.UserTaskEnums;

namespace EskaCMS.Security.Controllers
{
    [Route("api/ToDo")]
    [Authorize]
    public class UserTasksController : Controller
    {

        private readonly IWorkContext _workContext;
        private readonly IUserTaskService _userTaskService;

        public UserTasksController(

            IWorkContext workContext,
            IUserTaskService userTaskService

           )
        {

            _workContext = workContext;
            _userTaskService = userTaskService;
        }

        [HttpPost]
        [Route("CreateToDoItem")]
        [Authorize]
        public async Task<IActionResult> CreateToDoItem([FromBody] CreateTaskVM model)
        {

            try
            {
                var result = await _userTaskService.CreateTask(model);
                if (result)
                {
                    return Ok();

                }

                return BadRequest();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }
        [HttpPost]
        [Route("DeleteToDoItem/{ItemId}")]

        public async Task<IActionResult> DeleteToDoItem([FromRoute] long ItemId)
        {
            try
            {
                var result = await _userTaskService.DeleteTask(ItemId);
                if (result)
                {
                    return Ok();

                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("UpdateToDoItem")]
        public async Task<IActionResult> UpdateToDoItem([FromBody] UpdateTaskVM model)
        {
            try
            {
                var result = await _userTaskService.UpdateTask(model);
                if (result)
                {
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("UpdateItemStatus")]
        public async Task<IActionResult> UpdateItemStatus([FromBody] UpdateTaskStatusVM model)
        {
            try
            {
                var result = await _userTaskService.UpdateTask(model);
                if (result)
                {
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("GetAllTasks/{userId}")]
        [Route("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks([FromBody] SmartTableParam model, long? userId)
        {
            try
            {
                var Id = userId.HasValue ? userId : await _workContext.GetCurrentUserId();
                var result = await _userTaskService.GetAllTasks(model, Id.Value);

                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("ItemDetails/{ItemId}")]

        public async Task<IActionResult> ItemDetails([FromRoute] long ItemId)
        {
            try
            {
                var result = await _userTaskService.TaskDetails(ItemId);

                return Ok(result);



            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("GetAllTaskCategories")]


        public async Task<IActionResult> GetAllTaskCategories()
        {
            try
            {

                var result = await _userTaskService.GetAllCategories();

                return Ok(result);



            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("ChangeTasksOrder")]


        public async Task<IActionResult> ChangeTasksOrder([FromBody] List<TasksOrderDto> tasks)
        {
            try
            {

                var result = await _userTaskService.EditMultipleTasksOrder(tasks);

                if (result)
                {
                    return Ok();

                }

                return BadRequest();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("AddCategory")]


        public async Task<IActionResult> AddCategory([FromBody] CreateTaskCategoryVM model)
        {
            try
            {

                var result = await _userTaskService.AddCategoryForTasks(model);

                if (result)
                {
                    return Ok();

                }

                return BadRequest();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateCategory")]


        public async Task<IActionResult> UpdateCategory([FromBody] TaskCategoryDto model)
        {
            try
            {

                var result = await _userTaskService.UpdateCategory(model);

                if (result)
                {
                    return Ok();

                }

                return BadRequest();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("DeleteToDoCategory/{CategoryId}")]

        public async Task<IActionResult> DeleteToDoCategory([FromRoute] long CategoryId)
        {
            try
            {
                var result = await _userTaskService.DeleteCategory(CategoryId);
                if (result)
                {
                    return Ok();

                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetProfileTasks")]
        [Authorize]
        public async Task<IActionResult> GetProfileTasks([FromBody] UserProfileTasks model)
        {

            try
            {
                var result = await _userTaskService.GetUserProfileTasks(model.SmartTable,model.UserId);
               
                return Ok(result);

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet("Priorities")]
        public IActionResult Priorities()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(ETaskPriority)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = item.ToString()
                });
            }

            return Ok(result);

        }

        [HttpGet("TaskStatus")]
        public IActionResult TaskStatus()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(ETaskStatus)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = item.ToString()
                });
            }

            return Ok(result);

        }

        [HttpGet("TaskPrivacy")]
        public IActionResult TaskPrivacy()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(ETaskPrivacy)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = item.ToString()
                });
            }

            return Ok(result);

        }

    }




}
