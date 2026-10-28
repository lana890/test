using EskaCMS.Core.Models.GeoLocation;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface IGeoLocationServices
    {
        Task AddGeoLocation();
        Task<PaginationResult<UserAddressInfoResponse>> GetUserGeoLocation(UserGeoLocationSearchRequest model);
        Task<List<GeoLocationAreaResponse>> GetUsersArea(GeoLocationSearchRequest model);
        Task<List<GeoLocationCityResponse>> GetUsersCities(GeoLocationSearchRequest model);
        Task<List<GeoLocationCountryResponse>> GetUsersCountries(int PageSize, int PageNumber);
    }
}
