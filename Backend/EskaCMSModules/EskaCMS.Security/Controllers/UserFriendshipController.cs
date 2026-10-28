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
using static EskaCMS.Entities.Enum.FriendshipEnums;
using EskaCMS.Security.Models.FriendshipModels;
using EskaCMS.Core.Areas.Core.ViewModels;

namespace EskaCMS.Security.Controllers
{
    [Route("api/UserFriendsship")]
    [Authorize]
    public class UserFriendshipController : Controller
    {
        

   
   
        // private readonly IWorkContext _workContext;
        private readonly IUserFriendshipService _userFriendshipService;
       
    


        public UserFriendshipController(
            
            /* IWorkContext workContext, */
            IUserFriendshipService userFriendshipService

           )
        {
           
            // _workContext = workContext;
            _userFriendshipService = userFriendshipService;


        }

        [HttpPost]
        [Route("MakeFriendRequest")]
        [Authorize]
        public async Task<IActionResult> MakeFriendRequest([FromBody] GetUserInfoVm model)
        {

            try
            {
                var result = await _userFriendshipService.CreateFriendrequest(model);
                if(result)
                {
                        return Ok("The request susceefully sent");

                }

                return BadRequest("Faild to send request");
            }
            catch (Exception exc)
            {
               
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAllFriendRequest")]
            
        public async Task<IActionResult> GetAllFriendRequest([FromBody] SmartTableParam model)
        {
            try
            {
                var result = await _userFriendshipService.GetAllFriendRequest(model);
                
                    return Ok(result);

                

            
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllSendingRequest")]

        public async Task<IActionResult> GetAllSendingRequest([FromBody] SmartTableParam model)
        {
            try
            {
                var result = await _userFriendshipService.GetAllSentFriendRequest(model);

                return Ok(result);




            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllFriends")]
            
        public async Task<IActionResult> GetAllFriends([FromBody] SmartTableParam model)
        {
            try
            {
                var result = await _userFriendshipService.GetAllFriends(model);
                
                    return Ok(result);

                

            
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("AcceptFriendRequest")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] GetUserInfoVm model)
        {
            try
            {

                var result = await _userFriendshipService.ActionOnFriendRequest(model.UserId, EFriendshipStatus.Accepted,model.EmailTemplateId);
                if (result)
                {
                    return Ok("The friend request has been accepted");

                }

                return BadRequest();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("RequestAndConfirmFriendship")]
        public async Task<IActionResult> RequestAndConfirmFriendship([FromBody] GetUserInfoVm model)
        {
            try
            {
                var Request = await _userFriendshipService.RequestAndConfirmFriendship(model);

                
                    return Ok("The friend request has been accepted");

             

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("CancelFriendRequest")]
        public async Task<IActionResult> CancelFriendRequest([FromBody] GetUserInfoVm model)
        {
            try
            {

                var result = await _userFriendshipService.CancelSendingRequest(model.UserId);
                if (result)
                {
                    return Ok("The friend request has been canceld");

                }

                return BadRequest("Faild to cancel friend request");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("RejectFriendRequest")]
        public async Task<IActionResult> RejectFriendRequest([FromBody] GetUserInfoVm model)
        {
            try
            {

                var result = await _userFriendshipService.ActionOnFriendRequest(model.UserId, EFriendshipStatus.Rejected, model.EmailTemplateId);
                if (result)
                {
                    return Ok("The friend request has been rejected");

                }

                return BadRequest("Faild to reject");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteFriend")]
        public async Task<IActionResult> DeleteFriend([FromBody] GetUserInfoVm model)
        {
            try
            {

                var result = await _userFriendshipService.DeleteFriend(model.UserId);
                if (result)
                {
                    return Ok("The user has ben deleted");

                }

                return BadRequest("Faild to delete friend");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetUserProfile")]
        public async Task<IActionResult> GetUserProfile([FromBody] GetUserInfoVm model)
        {
            try
            {

                var result = await _userFriendshipService.GetUserProfile(model.UserId);
                
                  
                return Ok(result);

                

                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllUsers")]

        public async Task<IActionResult> GetAllUsers([FromBody] SmartSearchTableParam<AllUsersVM> model)
        {
            try
            {
                var result = await _userFriendshipService.GetAllUsers(model);

                return Ok(result);




            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("FriendshipStatus")]
        public IActionResult FriendshipStatus()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EUserProfileStatus)))
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
