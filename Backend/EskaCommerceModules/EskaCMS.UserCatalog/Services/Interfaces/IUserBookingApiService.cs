using EskaCommerce.UserCatalog.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IUserBookingApiService
    {
        GridOutputVM<UserBookingListOutputVM> GetFilteredUserBookings(long? userId, SearchTableParam<UserBookingListSearchVM> param);
        UserBookingDetailsOutputVM GetUserBookingById(long id);
        Task<IList<UserBookingHistoryDetailsOutputVM>> GetUserBookingHistoryById(long bookingId);
    }

}
