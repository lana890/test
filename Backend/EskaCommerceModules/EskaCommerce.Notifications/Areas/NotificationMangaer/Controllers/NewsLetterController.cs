using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.NotificationManager.Models;
using EskaCMS.NotificationManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Notifications.Areas.NotificationMangaer.Controllers
{
    
    [ApiController]
    [Route("api/NewsLetter/[action]")]
    [Authorize]
    public class NewsLetterController : ControllerBase
    {
        public readonly INewsLetterService _newsLetterService;
        public readonly IWorkContext _workContext;

        public NewsLetterController(INewsLetterService newsLetterService, IWorkContext workContext)
        {
            _newsLetterService = newsLetterService;
            _workContext = workContext; 
        }

        [HttpPost]
        public async Task<IActionResult> UpsertNewsLetterAsync([FromBody]RequestNewsLetterVm newsLetter)
        {
            try
            { 
                return Ok( await _newsLetterService.UpsertNewsLetterAsync(newsLetter));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
        [HttpPost]
        public async Task<IActionResult> AddSubscriber(long PageId, string ComponentSettingsId, EditUserDto User)
        {
            try
            {
                await _newsLetterService.AddSubscriber(PageId, ComponentSettingsId, User);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsLetterUsersAsync(long Id)
        {
            try
            {
               return Ok( await _newsLetterService.GetNewsLetterUsersAsync( Id));
                
            }
            catch( Exception ex )
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        public async Task<IActionResult> GetUsersForNewsLetter()
        {
            try
            {
             
                return Ok(await _newsLetterService.GetUsersForNewsLetterAsync());
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsLetterEdit(long Id)
        {
            try
            {

                return Ok(await _newsLetterService.GetNewsLetterEdit(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
        [HttpPost]
        public async Task<IActionResult> GetNewsLetterListAsync(NewsLetterSearchParamsDto Search)
       {
            try
            {

                return Ok(await _newsLetterService.GetNewsLetterListAsync(Search));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(long Id)
        {
            try 
            {
                await _newsLetterService.DeleteAsync(Id);
                return Ok();

            }
            catch( Exception ex)
            {
                return BadRequest( ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> SubscibedNotificationforUser([FromBody] SmartTableParam model)
        {
            try
            {
                return Ok(await _newsLetterService.GetSubscribedNotifications(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateNoificationLink([FromBody]NotificationLinkVM notificationLinkModel)
        {
            try
            {
             
               return Ok(await _newsLetterService.GenerateNoificationLink(notificationLinkModel));


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost("{Id}")]
        public async Task<IActionResult> MuteNotification([FromRoute] long Id)
        {
            try
            {
                var result = await _newsLetterService.MuteSubcribedNotifications(Id);
                if (result)
                {
                    return Ok("Notification successfully muted");

                }

                return BadRequest("Faild to mute this notification");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost("{Id}")]
        public async Task<IActionResult> UnMuteNotification([FromRoute] long Id)
        {
            try
            {
                var result = await _newsLetterService.UnMuteSubcribedNotifications(Id);
                if (result)
                {
                    return Ok("Notification successfully unmuted");

                }

                return BadRequest("Faild to unmuted this notification");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost("{Id}")]
        public async Task<IActionResult> ViewMobileNotification([FromRoute] long Id)
        {
            try
            {
                var result = await _newsLetterService.UpdateNewsletterViewerCount(Id);
                if (result)
                {
                    return Ok("Notification successfully updated");

                }

                return BadRequest("Faild to update this notification");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost("{Id}")]
        public async Task<IActionResult> GetNotificationStatistics([FromBody] SmartTableParam model, [FromRoute] long Id)
        {
            try
            {
                return Ok(await _newsLetterService.GetNewsLetterStatistics(Id, model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost("{StatisticId}")]
        public async Task<IActionResult> GetNotificationViewers([FromBody] SmartTableParam model, [FromRoute] long StatisticId)
        {
            try
            {
                return Ok(await _newsLetterService.GetNotificationViewers(StatisticId, model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
    }


    }
