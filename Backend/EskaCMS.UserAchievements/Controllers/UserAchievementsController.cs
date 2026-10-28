
using EskaCMS.Infrastructure.Helpers;

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

using EskaCMS.Infrastructure.Web.SmartTable;
using static EskaCMS.Entities.Enum.UserTaskEnums;
using static EskaCMS.Entities.Enum.FriendshipEnums;

using EskaCMS.UserAchievements.Services.Interface;
using EskaCMS.UserAchievements.Models;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;

namespace EskaCMS.Security.Controllers
{
    [Route("api/UserAchievements")]
    [Authorize]
    public class UserAchievementsController : Controller
    {
        

        // private readonly IWorkContext _workContext;
        private readonly IUserAchievementsServices _userAchievementsService;

       
        public UserAchievementsController(
             IUserAchievementsServices userAchievementsService/* ,
            IWorkContext workContext */
         
           )
        {
           
            // _workContext = workContext;
            _userAchievementsService = userAchievementsService;
        }

        [HttpPost]
        [Route("CreateAchievement")]
        [Authorize]
        public async Task<IActionResult> CreateAchievement([FromBody] CreateAchievementVM model)
        {

            try
            {
                var result = await _userAchievementsService.CreateAchievement(model);
                if(result)
                {
                        return Ok("The achievement susceefully created");

                }

                return BadRequest("Faild to create achievement");
            }
            catch (Exception exc)
            {
               
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("UpdateAchievement")]
        [Authorize]
        public async Task<IActionResult> UpdateAchievement([FromBody] AchievementUpdateVM model)
        {

            try
            {
                var result = await _userAchievementsService.UpdateAchievement(model);
                if (result)
                {
                    return Ok("The achievement susceefully updated");

                }

                return BadRequest("Faild to update achievement");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAllAchivements")]
            
        public async Task<IActionResult> GetAllAchivements([FromBody] SmartTableParam model)
        {
            try
            {
                var result = await _userAchievementsService.GetAllAchivements(model);
                
                    return Ok(result);
             
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetUserAchivements")]

        public async Task<IActionResult> GetUserAchivements([FromBody] SmartTableParam model)
        {
            try
            {

                
                
                var result = await _userAchievementsService.GetUserAchivements(model);

                return Ok(result);




            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("PublishAchivement/{Id}")]

        public async Task<IActionResult> PublishAchivement([FromRoute]long Id)
        {
            try
            {
                var result = await _userAchievementsService.ChangeAchievementStatus(Id,EStatus.Published);
                if (result)
                {
                    return Ok("The achievement susceefully published");

                }

                return BadRequest("Faild to publish achievement");


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("UnPublishAchivement/{Id}")]

        public async Task<IActionResult> UnPublishAchivement([FromRoute] long Id)
        {
            try
            {
                var result = await _userAchievementsService.ChangeAchievementStatus(Id, EStatus.Unpublished);

                if (result)
                {
                    return Ok("The achievement susceefully unpublished");

                }

                return BadRequest("Faild to unpublish achievement");




            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteAchivement/{Id}")]

        public async Task<IActionResult> DeleteAchivement([FromRoute] long Id)
        {
            try
            {
                var result = await _userAchievementsService.ChangeAchievementStatus(Id, EStatus.Deleted);

                if (result)
                {
                    return Ok("The achievement susceefully deleted");

                }

                return BadRequest("Faild to delete achievement");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }




        [HttpGet("AchievmentType")]
        public IActionResult AchievmentType()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(AchievmentType)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = ((AchievmentType)item).GetDisplayName()
                });
            }

            return Ok(result);

        }

        [HttpGet("Status")]
        public IActionResult Status()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EStatus)))
            {
                var value = (int)item;
                if (value == 1 || value == 2)
                {
                    result.Add(new
                    {
                        Value = (int)item,
                        Label = item.ToString()
                    });

                }
              
            }

            return Ok(result);

        }


    }


  

}
