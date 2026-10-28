using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Data;
using System.Linq;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Collections.Generic;
using EskaCMS.Core.Extensions;
using EskaCommerce.UserCatalog.Dtos;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Helpers;
using System.Text;
using EskaCommerce.UserCatalog.Services.Interfaces;
using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.UserCatalog;
using Google.Api.Gax.ResourceNames;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Authorize]
    [Route("api/barber")]
    public class BarberApiController : Controller
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;
        private readonly IRepository<ProviderTrip> _providerTripRepository;
        private readonly IRepository<UserBookingHistory> _userBookingHistoryRepository;
        private readonly IRepository<ProviderTimeSlot> _ProviderTimeSlotRepository;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
        public BarberApiController(
            IRepository<ProviderTrip> providerTripRepository,
            IRepository<ProviderTimeSlot> ProviderTimeSlotRepository,
            IRepository<UserBookingHistory> userBookingHistoryRepository,
                        IWorkContext workContext,

            IMapper mapper)
        {
            _providerTripRepository = providerTripRepository;
            _userBookingHistoryRepository = userBookingHistoryRepository;
            _ProviderTimeSlotRepository = ProviderTimeSlotRepository;
            _mapper = mapper;
            _workContext = workContext;

        }

        [HttpPost("get-trip-details")]
        public async Task<IActionResult> TripDetailsByProviderId([FromBody] SmartTableParam param)
        {
            try
            {
                var query = _providerTripRepository.Query();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.ProviderName != null)
                    {
                        string providerName = search.ProviderName;
                        query = query.Where(x => x.Provider.FullName.Contains(providerName));
                    }
                    if (search.CustomerName != null)
                    {
                        string customerName = search.CustomerName;
                        query = query.Where(x => x.Booking.User.FullName.Contains(customerName));
                    }
                    if (search.ProductName != null)
                    {
                        string productName = search.ProductName;
                        query = query.Where(x => x.Booking.Product.ProductDetails.Any(y=>y.Name.Contains(productName)));
                    }
                    if (search.ReservationDate != null)
                    {
                        if (search.ReservationDate.before != null)
                        {
                            DateTimeOffset before = search.ReservationDate.before;
                            query = query.Where(x => x.Booking.ReservationDate <= before);
                        }

                        if (search.ReservationDate.after != null)
                        {
                            DateTimeOffset after = search.ReservationDate.after;
                            query = query.Where(x => x.Booking.ReservationDate >= after);
                        }
                    }
                }

                var trip = query.ToSmartTableResult(param, x => new ProviderTripDetailsVM
                {
                    ReservationDate = x.Booking.ReservationDate,
                    FromTime = ConvertTimaSpanToString(x.Booking.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(x.Booking.ProviderTimeSlot.ToTime),
                    ProductName = x.Booking.Product.ProductDetails.Select(x=>x.Name).FirstOrDefault(),
                    CustomerAddress = x.Booking.UserAddress.Address.AddressLine1,
                    CustomerName = x.Booking.User.FullName,
                    UserId = x.Booking.User.Id,
                    ProviderId = x.ProviderId,
                    Distance = x.Distance,
                    ProviderName = x.Provider.FullName

                });

                return Json(trip);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("get-service-duration")]
        public async Task<IActionResult> ServiceDurations([FromBody] SmartTableParam param)
        {
            try
            {
                var query = _userBookingHistoryRepository.Query().Where(x => x.Status == UserBookingStatus.ServiceCompleted);
                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.ReservationDate != null)
                    {
                        if (search.ReservationDate.before != null)
                        {
                            DateTimeOffset before = search.ReservationDate.before;
                            query = query.Where(x => x.UserBooking.ReservationDate <= before);
                        }

                        if (search.ReservationDate.after != null)
                        {
                            DateTimeOffset after = search.ReservationDate.after;
                            query = query.Where(x => x.UserBooking.ReservationDate >= after);
                        }
                    }
                    if (search.ProductName != null)
                    {
                        string productName = search.ProductName;
                        query = query.Where(x => x.UserBooking.Product.ProductDetails.Any(y => y.Name.Contains(productName)));

                    }
                }
                var duration = query.ToSmartTableResult(param, x => new ServiceDurationVM
                {
                    ReservationDate = x.UserBooking.ReservationDate,
                    FromTime = ConvertTimaSpanToString(x.UserBooking.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(x.UserBooking.ProviderTimeSlot.ToTime),
                    ProductName =  x.UserBooking.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),

                    StatusDate = x.StatusDate,
                    Duration = CalculateDuration(_userBookingHistoryRepository.Query().Where(u => (u.Status == UserBookingStatus.ServiceStarted || u.Status == UserBookingStatus.ServiceCompleted) && u.UserBookingId == x.UserBookingId).ToList(), UserBookingStatus.ServiceStarted, UserBookingStatus.ServiceCompleted)
                });
                return Json(duration);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-provider-time-slots")]
        public async Task<IList<ProviderTimeSlotVM>> GetProviderTimeSlots(long providerId)
        {

            IList<ProviderTimeSlotVM> timeSlots = await _ProviderTimeSlotRepository
                .Query()
                .Where(v => v.ProviderId == providerId)
                .Include(x => x.Provider)
                .Include(x => x.CreatedBy)
                .Include(x => x.ModifiedBy)
                .Select(x => new ProviderTimeSlotVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    CreationDate = x.CreationDate,
                    CreatedBy = x.CreatedBy.FullName,
                    Day = x.Day,
                    StringFromTime = ConvertTimaSpanToString(x.FromTime),
                    StringToTime = ConvertTimaSpanToString(x.ToTime),
                    FromTime = x.FromTime,
                    ToTime = x.ToTime,
                    ModifiedBy = x.ModifiedBy.FullName,
                    ModifiedById = x.ModifiedById,
                    ProviderId = x.ProviderId,
                    ProviderName = x.Provider.FullName,
                }).ToListAsync();

            return timeSlots;
        }

        private void CheckTimeSlotsOverlap(ProviderTimeSlotCreateVM ProviderInfo)
        {

        }
        [HttpPost("create-time-slot")]
        public async Task<IActionResult> Create([FromBody] ProviderTimeSlotCreateVM data)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            ProviderTimeSlot timeSlot = _mapper.Map<ProviderTimeSlot>(data);
            timeSlot.CreatedById = _workContext.GetCurrentUserIdValue();
            //timeSlot.ModifiedById = _workContext.GetCurrentUser(SiteId).Result.Id;
            timeSlot.CreationDate = TimeNow;
            _ProviderTimeSlotRepository.Add(timeSlot);
            await _ProviderTimeSlotRepository.SaveChangesAsync();
            return Ok(timeSlot.Id);
        }
        [HttpPatch("update-time-slot")]
        public async Task<IActionResult> Update([FromBody] ProviderTimeSlotUpdateVM data)
        {
            ProviderTimeSlotVM timeSlot = GetById(data.Id);

            if (data.Day.HasValue)
            {
                timeSlot.Day = (DayOfWeek)data.Day;
            }
            if (data.FromTime.HasValue)
            {
                timeSlot.FromTime = (TimeSpan)data.FromTime;
            }
            if (data.ToTime.HasValue)
            {
                timeSlot.ToTime = (TimeSpan)data.ToTime;
            }
            await _ProviderTimeSlotRepository.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("delete-time-slot")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var TimeSlot = _ProviderTimeSlotRepository.Query()
                    .FirstOrDefault(x => x.Id == id);
                _ProviderTimeSlotRepository.Remove(TimeSlot);
                _ProviderTimeSlotRepository.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private ProviderTimeSlotVM GetById(long id)
        {
            return _ProviderTimeSlotRepository.Query()
                .Include(x => x.Provider).Select(x => new ProviderTimeSlotVM
                {
                    Id = x.Id,
                    FromTime = x.FromTime,
                    ProviderId = x.ProviderId,
                    ProviderName = x.Provider.FullName,
                    ToTime = x.ToTime,
                    StringToTime = ConvertTimaSpanToString(x.ToTime),
                    StringFromTime = ConvertTimaSpanToString(x.FromTime)

                })
                .FirstOrDefault(x => x.Id == id);
        }
        private List<ProviderTimeSlotVM> GetByProviderId(long id)
        {
            return _ProviderTimeSlotRepository.Query().Where(x => x.ProviderId == id)
                .Include(x => x.Provider).Select(x => new ProviderTimeSlotVM
                {
                    Id = x.Id,
                    FromTime = x.FromTime,
                    ProviderId = x.ProviderId,
                    ProviderName = x.Provider.FullName,
                    ToTime = x.ToTime,
                    StringToTime = ConvertTimaSpanToString(x.ToTime),
                    StringFromTime = ConvertTimaSpanToString(x.FromTime)
                })
                .ToList();
        }
        [HttpPost("export-trips")]
        public async Task<ActionResult<UserBookingExportVm>> ExportTrips([FromBody] SmartTableParam param)
        {

            var query = _providerTripRepository.Query();

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.ProviderName != null)
                {
                    string providerName = search.ProviderName;
                    query = query.Where(x => x.Provider.FullName.Contains(providerName));
                }
                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Booking.User.FullName.Contains(customerName));
                }
                if (search.ProductName != null)
                {
                    string productName = search.ProductName;
                    query = query.Where(x => x.Booking.Product.ProductDetails.Any(y => y.Name.Contains(productName)));
                }
                if (search.ReservationDate != null)
                {
                    if (search.ReservationDate.before != null)
                    {
                        DateTimeOffset before = search.ReservationDate.before;
                        query = query.Where(x => x.Booking.ReservationDate <= before);
                    }

                    if (search.ReservationDate.after != null)
                    {
                        DateTimeOffset after = search.ReservationDate.after;
                        query = query.Where(x => x.Booking.ReservationDate >= after);
                    }
                }
            }

            var trip = await query
                .Select(x => new ProviderTripDetailsVM
                {
                    ReservationDate = x.Booking.ReservationDate,
                    FromTime = ConvertTimaSpanToString(x.Booking.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(x.Booking.ProviderTimeSlot.ToTime),
                    ProductName = x.Booking.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),
                    CustomerAddress = x.Booking.UserAddress.Address.AddressLine1,
                    CustomerName = x.Booking.User.FullName,
                    Distance = x.Distance,
                    ProviderName = x.Provider.FullName
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(trip);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "trips-export.csv");
        }

        [HttpPost("export-duration-service")]
        public async Task<ActionResult<UserBookingExportVm>> ExportDurations([FromBody] SmartTableParam param)
        {

            var query = _userBookingHistoryRepository.Query().Where(x => x.Status == UserBookingStatus.ServiceCompleted);

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.ReservationDate != null)
                {
                    if (search.ReservationDate.before != null)
                    {
                        DateTimeOffset before = search.ReservationDate.before;
                        query = query.Where(x => x.UserBooking.ReservationDate <= before);
                    }

                    if (search.ReservationDate.after != null)
                    {
                        DateTimeOffset after = search.ReservationDate.after;
                        query = query.Where(x => x.UserBooking.ReservationDate >= after);
                    }
                }
                if (search.ProductName != null)
                {
                    string productName = search.ProductName;
                    query = query.Where(x => x.UserBooking.Product.ProductDetails.Any(x=>x.Name.Contains(productName)));
                }
            }

            var trip = await query
                .Select(x => new ServiceDurationVM
                {
                    ReservationDate = x.UserBooking.ReservationDate,
                    FromTime = ConvertTimaSpanToString(x.UserBooking.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(x.UserBooking.ProviderTimeSlot.ToTime),
                    ProductName = x.UserBooking.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),
                    StatusDate = x.StatusDate,
                    Duration = CalculateDuration(_userBookingHistoryRepository.Query().Where(u => (u.Status == UserBookingStatus.ServiceStarted || u.Status == UserBookingStatus.ServiceCompleted) && u.UserBookingId == x.UserBookingId).ToList(), UserBookingStatus.ServiceStarted, UserBookingStatus.ServiceCompleted)
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(trip);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "durations-export.csv");
        }


        private static string CalculateDuration(IList<UserBookingHistory> statuses, UserBookingStatus status1, UserBookingStatus status2)
        {
            var statusTwo = statuses.FirstOrDefault(x => x.Status == status2);
            var statusOne = statuses.FirstOrDefault(x => x.Status == status1);
            TimeSpan duration = statusTwo.StatusDate - statusOne.StatusDate;

            return duration.ToString(@"hh\:mm\:ss");
        }

        private static string ConvertTimaSpanToString(TimeSpan timeSpan)
        {
            DateTime dateTime = DateTime.Today.Add(timeSpan);
            string displayTime = dateTime.ToString("hh:mm tt");
            return displayTime;
        }



    }
}

