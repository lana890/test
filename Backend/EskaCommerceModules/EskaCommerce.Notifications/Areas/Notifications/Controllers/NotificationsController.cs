using EskaCMS.Core.Extensions;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCommerce.Module.Notifications.Areas.Notifications.Controllers
{
    [Area("Notifications")]
    [Authorize]
    [Route("api/Notification")]

    public class NotificationsController : Controller
    {
        private readonly ITestNotifier _testNotifier;
        // private readonly IRepositoryWithTypedId<UserNotification, Guid> _userNotificationRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IWorkContext _workContext;

        public NotificationsController(
           ITestNotifier testNorifier,
           /* IRepositoryWithTypedId<UserNotification, Guid> userNotificationRepository, */
           INotificationRepository notificationRepository,
           IWorkContext workContext
)
        {
           
            _testNotifier = testNorifier;
            // _userNotificationRepository = userNotificationRepository;
            _notificationRepository = notificationRepository;
            _workContext = workContext;
        }

        //#region Etc
        //[HttpPost]
        //public async Task<ActionResult> Notification([FromBody] NotificationVm inputDto)
        //{
        //    if (inputDto.Message.IsNullOrEmpty())
        //    {
        //        inputDto.Message = "This is a test notification, created at " + DateTime.Now;
        //    }

        //    await _testNotifier.SendMessageAsync(
        //        inputDto.UserId,
        //        inputDto.Message,
        //        NotificationType.WebNotification,
        //        NotificationMessageType.WebNotification,
        //        new NotificationObject(),
        //        inputDto.Severity.ToPascalCase().ToEnum<NotificationSeverity>()
        //        );

        //    return Ok();
        //}
        //#endregion

        [HttpPost]
        [Route("Send")]
        //[EnableCors("ApiCorsPolicy")]

        public async Task<IActionResult> Send([FromBody] NotificationObject data)
        {
            try
            {
                long siteId = Convert.ToInt64(Request.Headers["SiteId"]);
             
                await _testNotifier.SendMessageAsync(
                data.NotificationType,
                null,
                data,
                siteId
                );
                return Ok("Message has been sent");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("SendWithFiles")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendWithFiles()
        {
            try
            {
                var jsonData = Request.Form["data"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jsonData))
                    return BadRequest("data is required.");

                var result = JsonConvert.DeserializeObject<NotificationObject>(jsonData);

                if (result == null)
                    return BadRequest("Invalid data.");

                var files = Request.Form.Files;
                long siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                result.Files ??= new List<FileMemoryViewModel>();

                foreach (var file in files)
                {
                    if (file.Length <= 0)
                        continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    result.Files.Add(new FileMemoryViewModel
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileBytes = ms.ToArray()
                    });
                }
                await _testNotifier.SendMessageAsync(
                    result.NotificationType,
                    null,
                    result,
                    siteId


                );

                return Ok("Message has been sent");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost("quick-search")]
        public async Task< IActionResult> GetNotifications([FromBody] SmartTableParam param)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var data = await _notificationRepository.GetSmartSearchNotifications(param, SiteId);


                return Ok(data);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
           
        }

        [HttpPost("GetLastNotifications")]
        public async Task<IActionResult> GetLastNotifications()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var data =  await _notificationRepository.GetLastNotifications(1, 0, 16, EskaCommerce.Notifications.Areas.Notifications.ViewModels.NotificationType.SignalR);


                return Ok(data);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }

        [HttpPost("GetNotificationsForWeb")]
        public async Task< IActionResult> GetNotificationsForWeb([FromBody] SmartTableParam param)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var data = await _notificationRepository.GetNotificationsForWeb(param, SiteId, userId);


                return Ok(data);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }
           
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            IQueryable<QuickSearchVM> notificationAllData = _notificationRepository.GetNotificationById(id);
            if (notificationAllData == null)
            {
                return NotFound();
            }

            return Json(notificationAllData);
        }
        
        [HttpGet("UnreadUserNotificationCount")]
        [HttpGet("UnreadUserNotificationCount/{type}")]
        public async Task<IActionResult> UnreadUserNotificationCount([FromRoute] NotificationType? type)
        {
            try
            {
                var userId =await  _workContext.GetCurrentUserId();
                var notificationType = type.HasValue ? type.ToString() : null;
                var result = await _notificationRepository.GetUserNotificationCountAsync(userId, notificationType, UserNotificationState.Unread);

                return Ok(result);

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
            
            
        }

        [HttpPost("DeleteNotification/{id}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] Guid id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                await _notificationRepository.DeleteUserNotificationAsync(id);


                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }
      
        [HttpGet("NotificationType")]
        public IActionResult NotificationType()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(NotificationType)))
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
