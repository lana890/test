using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IUserBookingService
    {
        Task<long> Create(UserBookingCreate userBooking, long siteId);
        Task<UserBookingListOutputVM> Get(long id);
        Task<UserBooking> GetByUserPlanId(long userPlanId);
        Task<IList<UserBookingListOutputVM>> GetBookingByUserId(long userId);
        Task<UserBookingDetailsVM> GetDetails(long id);
        Task Update(UserBookingUpdate userBooking);
        Task Delete(long userBookingId);
        Task<IList<UserBookingListOutputVM>> GetAll(UserBookingListInput input);
        IList<UserTimeSlotOutput> GetAvailableProviderSlots(long ProviderId, ProviderSlotInputVM data);
        Task<IActionResult> CreateUserTimeSlot(UserTimeSlotCreate timeSlotInput, long siteId);
        Task<IList<UserBookingListOutputVM>> GetPastServices(long siteId);
        Task<UpComingServicesResponseVM<UserBookingListOutputVM>> GetUpcomingServices(long siteId);
        Task<UpComingServicesResponseVM<BarberBookingListOutputVM>> GetBarberUpcomingServices();
        Task UpdateUserConnectionId(LocationStremingVM stremingVM);
        Task<IActionResult> StartProviderTrip(ProviderTripCreateVM input);
    }

}
