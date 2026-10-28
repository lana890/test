using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Route("api/user-booking")]
    public class UserBookingApiController : Controller
    {
        private readonly IUserBookingApiService _userBookingApiService;
        private readonly IRepository<UserBooking> _userBookingRepository;

        public UserBookingApiController(
            IRepository<UserBooking> userBookingRepository,
            IUserBookingApiService userBookingApiService
            )
        {
            //_userBookingServices = userBookingServices;
            _userBookingRepository = userBookingRepository;
            _userBookingApiService = userBookingApiService;
        }

        [HttpPost("user-booking-by-userId/{userId}")]
        public IActionResult GetBookingByUserId(long? userId, [FromBody] SearchTableParam<UserBookingListSearchVM> param)
        {
            try
            {
                var userBookings = _userBookingApiService.GetFilteredUserBookings(userId, param);
                return Ok(userBookings);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetUserBookingById/{id}")]
        public IActionResult GetBookingById(long id)
        {
            try
            {
                var userBooking = _userBookingApiService.GetUserBookingById(id);
                return Ok(userBooking);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("user-booking-status")]
        public IActionResult UserBookingStatus()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(UserBookingStatus)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = item.ToString()
                });
            }

            return Ok(enumVals);

        }
        [HttpGet("user-booking-history-by-id/{bookingId}")]
        public async Task<IActionResult> GetUserBookingHistoryById(long bookingId)
        {
            try
            {
                var bookingHistory = await _userBookingApiService.GetUserBookingHistoryById(bookingId);
                return Ok(bookingHistory);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("export-by-userId/{userId}")]
        public async Task<ActionResult<UserBookingExportVm>> ExportByUserId(long userId, [FromBody] SmartTableParam param)
        {

            var query = _userBookingRepository.Query()
                            .Include(x => x.UserPlan)
                            .Include(x => x.Provider)
                            .Include(x => x.ProviderTimeSlot)
                            .Include(x => x.UserAddress)
                            .ThenInclude(x => x.Address)
                            .Where(x => x.UserId == userId);

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.ReservationDate != null)
                {
                    if (search.ReservationDate.before != null)
                    {
                        DateTimeOffset before = search.ReservationDate.before;
                        query = query.Where(x => x.ReservationDate <= before);
                    }

                    if (search.ReservationDate.after != null)
                    {
                        DateTimeOffset after = search.ReservationDate.after;
                        query = query.Where(x => x.ReservationDate >= after);
                    }
                }
                if (search.ProviderName != null)
                {
                    string providerName = search.ProviderName;
                    query = query.Where(x => x.Provider.FullName.Contains(providerName));
                }
                if (search.ServiceName != null)
                {
                    string serviceName = search.ServiceName;
                    query = query.Where(x => x.UserPlan.PlanVersions.Plan.PlanDetails.Any(x=>x.Name.Contains(serviceName)));
                }
                if (search.Status != null)
                {
                    var status = (UserBookingStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

            }
            var userPlans = await query
                .Select(c => new UserBookingExportVm
                {
                    BookingId = c.Id,
                    ServiceName = c.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                    Status = c.Status,
                    ProviderName = c.Provider.FullName,
                    ReservationDate = c.ReservationDate,
                    FromTime = c.ProviderTimeSlot.FromTime,
                    ToTime = c.ProviderTimeSlot.ToTime,
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(userPlans);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "user-booking-export.csv");
        }
        private static string ConvertTimaSpanToString(TimeSpan timeSpan)
        {
            DateTime dateTime = DateTime.Today.Add(timeSpan);
            string displayTime = dateTime.ToString("hh:mm tt");
            return displayTime;
        }
    }
}
