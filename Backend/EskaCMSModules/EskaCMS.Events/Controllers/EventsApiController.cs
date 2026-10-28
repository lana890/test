using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using EskaCMS.Events.Services.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Controllers
{
    [Route("api/Events")]
    [ApiController]
    [Authorize]
    public class EventsApiController : ControllerBase
    {
        private readonly IEventsService _eventsService;
        // private readonly IWorkContext _workContext;
        public EventsApiController(IEventsService eventsService/* , IWorkContext workContext */)
        {
            _eventsService = eventsService;
            // _workContext = workContext;
        }

        #region Categories APIs


        [HttpGet]
        [Route("GetEventCategories")]
        public async Task<IActionResult> GetEventCategories()
        {
            try
            {
                return Ok(await _eventsService.GetCategoriesByTypeId());
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPut]
        //[Authorize(Policy = Permission.Admin)]
        [Route("ChangeCategoryName/{CatId}")]
        public async Task<IActionResult> ChangeCategoryName(long CatId, [FromBody] CategoryViewModel categoryVM)
        {
            try
            {
                return Ok(await _eventsService.ChangeCategoryName(CatId, categoryVM));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("AddEventCategory")]
        public async Task<IActionResult> AddEventCategory(long? catId, [FromBody] CategoryViewModel category)
        {
            try
            {
                return Ok(await _eventsService.AddCategory(catId.HasValue ? catId.Value : 0, category));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPut]
        [Route("UpdateEventCategory/{catId}")]
        public async Task<IActionResult> UpdateEventCategory(long catId, [FromBody] CategoryViewModel category)
        {
            try
            {
                return Ok(await _eventsService.AddCategory(catId, category));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteEventCategory/{catId}")]
        public async Task<IActionResult> DeleteEventCategory(long catId)
        {
            try
            {
                return Ok(await _eventsService.DeleteCategory(catId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        #endregion

        #region Events APIs
        [HttpGet]
        [Route("GetEvents")]

        public async Task<IActionResult> GetEvents([FromQuery]string Search="", [FromQuery]string Description="", [FromQuery]DateTimeOffset? FromDate=null, [FromQuery]DateTimeOffset? ToDate = null, [FromQuery] DateTimeOffset? FromTime = null, [FromQuery] DateTimeOffset? ToTime = null, [FromQuery]long? LocationId = null, [FromQuery]long? CultureId = null, [FromQuery]long? CategoryId = null, [FromQuery]EStatus? Status = null, [FromQuery]long? ParentId = null)
        {
            try
            {
                List<EventVM> eventVmList = new List<EventVM>();
                eventVmList = await _eventsService.GetAllEvents(Search, Description, FromDate, ToDate, FromTime, ToTime, LocationId, CultureId, CategoryId, Status, ParentId);
                return Ok(eventVmList);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetEventById/{EventId}")]

        public async Task<IActionResult> GetEventById(long EventId)
        {
            try
            {
                var eventResult = await _eventsService.GetEventById(EventId);
                if (eventResult != null)
                {
                    return Ok(eventResult);
                }
                else
                {
                    return NotFound("Event Not Found");
                }
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("AddEvent")]
        public async Task<IActionResult> AddEvent([FromBody] List<EventVM> EventVMList)
        {
            try
            {
                return Ok(await _eventsService.AddEvent(EventVMList));
            }
            catch(InvalidOperationException exc)
            {
                return BadRequest("Room is reserved by another event");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPut]
        [Route("UpdateEvent/{Id}")]
        public async Task<IActionResult> UpdateEvent(long Id, [FromBody] List<EventVM> eventVMList)
        {
            try
            {
                return Ok(await _eventsService.EditEvent(eventVMList, Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPut]
        [Route("ToggleStatus/{Id}")]
        public async Task<IActionResult> TogleStatus(long Id, [FromBody] EventVM eventVM)
        {
            try
            {
                return Ok(await _eventsService.ToggleStatus(Id, eventVM.EventStatus));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("CreateCalendarEvent")]
        public async Task<IActionResult> CreateCalendarEventAsync(
            CalendarNotificationModel calendarNotificationModel)
        {
            //string calRecord;
            //calRecord = "BEGIN:VCalendar" + Environment.NewLine;
            //calRecord += "METHOD:" + "REQUEST" + Environment.NewLine;
            //calRecord += "BEGIN:VEVENT" + Environment.NewLine;
            //calRecord += "DTSTART:" + calendarNotificationModel.StartDate.Date;

            //calRecord += "T" + "000000" + "Z" + Environment.NewLine;

            ////calEvent.hour += calEvent.duration;
            //calRecord += "DTEND:" + calendarNotificationModel.EndDate.Date;

            //calRecord += "T" + "000000" + "Z" + Environment.NewLine;

            //calRecord += "LOCATION:" + calendarNotificationModel.Location + Environment.NewLine;
            //calRecord += "SUMMARY:" + "test summary" + Environment.NewLine;

            //// Calculate unique ID based on current DateTime and its MD5 hash
            //string strHash = string.Empty;
            //foreach (byte b in (new System.Security.Cryptography.MD5CryptoServiceProvider()).ComputeHash(System.Text.Encoding.Default.GetBytes(DateTime.Now.ToString())))
            //{
            //    strHash += b.ToString("X2");
            //}
            //calRecord += "UID:" + strHash + Environment.NewLine;
            //calRecord += "END:VEVENT" + Environment.NewLine;
            //calRecord += "END:VCalendar";

            //  calRecord;



            // return Ok();
            //   return File(Encoding.ASCII.GetBytes(calRecord.ToString()), "text/calendar", "calendar.ics");


            var attendees = calendarNotificationModel.Attendees.Select(x => new Ical.Net.DataTypes.Attendee()
            {
                CommonName = x.AttendeeName,
                ParticipationStatus = "REQ-PARTICIPANT",
                Rsvp = true,
                Value = new Uri($"mailto:{x.AttendeeEmail}")
            }).ToList();


            var calendar = new Calendar();

            var icalEvent = new CalendarEvent
            {
                Class = "PUBLIC",
                Summary = calendarNotificationModel.Title,
                Description = calendarNotificationModel.Description,
                Created = new CalDateTime(DateTime.Now),
                // 15th of march 2021 12 o'clock.
                Start = new CalDateTime(calendarNotificationModel.StartDate),
                // Ends 3 hours later.
                End = new CalDateTime(calendarNotificationModel.EndDate.AddDays(1)),
                Location = calendarNotificationModel.Location,
                Sequence = 0,
                Uid = Guid.NewGuid().ToString(),
                //Attendees = attendees
            };

            calendar.Events.Add(icalEvent);

            var iCalSerializer = new CalendarSerializer();
            string result = iCalSerializer.SerializeToString(calendar);

            return File(Encoding.Unicode.GetBytes(result), "text/calendar", "calendar.ics");
        }


    
        #endregion

    }

//    public struct VCalendar
//    {
//        public int year, month, day, hour, minute, second, duration;
//        public string summary, location, method;
//    }

//    static string CreateCalendarRecord(VCalendar calEvent)
//    {
//        string calRecord;
//        calRecord = "BEGIN:VCalendar" + Environment.NewLine;
//        calRecord += "METHOD:" + "REQUEST" + Environment.NewLine;
//        calRecord += "BEGIN:VEVENT" + Environment.NewLine;
//        calRecord += "DTSTART:" + calEvent.year.ToString("0000") + calEvent.month.ToString("00") + calEvent.day.ToString("00");

//        calRecord += "T" + calEvent.hour.ToString("00") + calEvent.minute.ToString("00") + calEvent.second.ToString("00") + "Z" + Environment.NewLine;

//        calEvent.hour += calEvent.duration;
//        calRecord += "DTEND:" + calEvent.year.ToString("0000") + calEvent.month.ToString("00") + calEvent.day.ToString("00");

//        calRecord += "T" + calEvent.hour.ToString("00") + calEvent.minute.ToString("00") + calEvent.second.ToString("00") + "Z" + Environment.NewLine;

//        calRecord += "LOCATION:" + calEvent.location + Environment.NewLine;
//        calRecord += "SUMMARY:" + calEvent.summary + Environment.NewLine;

//        // Calculate unique ID based on current DateTime and its MD5 hash
//        string strHash = string.Empty;
//        foreach (byte b in (new System.Security.Cryptography.MD5CryptoServiceProvider()).ComputeHash(System.Text.Encoding.Default.GetBytes(DateTime.Now.ToString())))
//        {
//            strHash += b.ToString("X2");
//        }
//        calRecord += "UID:" + strHash + Environment.NewLine;
//        calRecord += "END:VEVENT" + Environment.NewLine;
//        calRecord += "END:VCalendar";
//        return calRecord;
//    }

//    // Create calendar event
//    VCalendar calEvent = new VCalendar();
//    calEvent.summary = "Meeting to discuss the new project";
//calEvent.location = "NY Office";
//calEvent.year = 2008;
//calEvent.month = 12;
//calEvent.day = 12;
//calEvent.hour = 12;
//calEvent.minute = 20;
//calEvent.second = 59;
//calEvent.duration = 1;
//calEvent.method = "REQUEST";

//Smtp.LicenseKey = "trial or permanent license key";
//Smtp mailer = new Smtp();

//    mailer.Message.From.Email = "jdoe@domain.com";
//mailer.Message.To.Add("bill@domain.com");
//mailer.Message.Subject = "Meeting Request";

//// Create calendar body
//mailer.Message.BodyParts.Add("text/calendar; method=\"REQUEST\"").Text = CreateCalendarRecord(calEvent);

//    // Connect to SMTP server and send meeting request
//    mailer.SmtpServers.Add("mail.domain.com", "jdoe", "secret");
//mailer.Send();
}
