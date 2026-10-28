using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models.GeoLocation
{
    public class GeoLocationCountryResponse
    {
        public  string  Country { get; set; }
        public List<UsersAddressInfoResponse>  usersAddressInfos { get; set; }

    }

    public class GeoLocationCityResponse
    {
        public string City { get; set; }

        public List<UsersAddressInfoResponse> usersAddressInfos { get; set; }

    }

    public class GeoLocationAreaResponse
    {
        public string Area { get; set; }

        public List<UsersAddressInfoResponse> usersAddressInfos { get; set; }

    }
    public class UsersAddressInfoResponse
    {
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
    }

    public class GeoLocationSearchRequest
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public string Country { get; set; }
        public string City { get; set; }
        public GeoLocationEnum  TypeSearch { get; set; }

    }

    public class UserGeoLocationSearchRequest
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public long UserId { get; set; }

    }
    public class UserAddressInfoResponse
    {
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Area { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }


    public class AddressGeoLocation
    {
        public string City { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

}
