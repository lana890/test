using EskaCMS.SignalR.Hubs;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Route("api/UserBooking/[action]")]
    [ApiController]
    [Authorize]
    public class UserBookingController : ControllerBase
    {
        private readonly IUserBookingService _userBookingServices;
        private readonly IHubContext<CommonHub> _hubContext;
        public UserBookingController(
            IUserBookingService userBookingServices,
            IHubContext<CommonHub> hubContext
            )
        {
            _userBookingServices = userBookingServices;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserBookingCreate userBooking)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var userBookingId = await _userBookingServices.Create(userBooking, siteId);
                return Ok(userBookingId);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> StartProviderTrip([FromBody] ProviderTripCreateVM input)
        {
            try
            {
                await _userBookingServices.StartProviderTrip(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserTimeSlot([FromBody] UserTimeSlotCreate timeSlotInput)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                await _userBookingServices.CreateUserTimeSlot(timeSlotInput, siteId);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPastServices()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _userBookingServices.GetPastServices(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetUpcomingServices()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _userBookingServices.GetUpcomingServices(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetBarberUpcomingServices()
        {
            try
            {
                return Ok(await _userBookingServices.GetBarberUpcomingServices());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Get(long bookingId)
        {
            try
            {
                return Ok(await _userBookingServices.Get(bookingId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDetails(long bookingId)
        {
            try
            {
                return Ok(await _userBookingServices.GetDetails(bookingId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch]
        public async Task<IActionResult> Update([FromForm] UserBookingUpdate userBooking)
        {
            try
            {
                await _userBookingServices.Update(userBooking);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _userBookingServices.Delete(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] UserBookingListInput input)
        {
            try
            {
                return Ok(await _userBookingServices.GetAll(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateUserConnectionId([FromBody] LocationStremingVM input)
        {
            try
            {
                await _userBookingServices.UpdateUserConnectionId(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult GetAvailableProviderSlots([FromBody] ProviderSlotInputVM providerSlotInputVM)
        {
            try
            {
                return Ok(_userBookingServices.GetAvailableProviderSlots(providerSlotInputVM.ProviderId, providerSlotInputVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult SendLocation([FromBody] LocationStremingVM request)
        {
            if (!string.IsNullOrEmpty(request.ConnectionId))
            {
                _hubContext.Clients.Client(request.ConnectionId).SendAsync("ReceiveLocation", request);
            }
            return Ok();
        }
    }
}
