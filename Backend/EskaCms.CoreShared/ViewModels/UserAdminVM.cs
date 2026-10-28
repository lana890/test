using EskaCMS.Core.Models;
using EskaCMS.Core.Models.GeoLocation;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.CoreShared.ViewModels
{


    public partial class UserDDLVM
    { 
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string FullName { get; set; }
    }

    public partial class QuickSearchVM
    {
       
        public long Id { get; set; }
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public List<string> Roles { get; set; }
        public List<string> UserGroup { get; set; }

    }
    public partial class UserGetAllVM
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }

    }
    public  class UserGetddlVM
    {
        public string SearchText { get; set; }

        public long? RoleId { get; set; }

    }
    
    public class UserSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }


    }
    public class UserGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class UserListSearchVM
    {
        public string Name { get; set; }
        public long? GroupId { get; set; }
        public List<long>? GroupIds { get; set; }
        public List<long> RoleId { get; set; }
        //public EStatus? Status { get; set; }
        public List<EStatus>? Status { get; set; }

        public DateTimeOffset? CreatedAfter { get; set; }
        public DateTimeOffset? CreatedBefore { get; set; }
        public DateTimeOffset? Birthday { get; set; }

        public Dictionary<string,string>? UserOptions { get; set; }
    }

    public class UploadUsers
    {
        public IList<long> CustomerGroupIds = new List<long>();
        public IList<long> RoleIds = new List<long>();
        public IList<long> SiteIds = new List<long>();
    
    
    }

    public class UserListAdvanceSearchVM
    {
        public DateTimeOffset? CreatedAfter { get; set; }
        public DateTimeOffset? CreatedBefore { get; set; }
        public List<long> GroupIds { get; set; }
        public List<long> RoleIds { get; set; }
        public List<long> ProductIds { get; set; }

        public string City { get; set; }
        public string Country { get; set; }
        public string Area { get; set; }
        public string SearchText { get; set; }
        public DateTimeOffset? Birthday { get; set; }
    }

    public class UserListAdvanceVM
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> RoleName { get; set; }
        public List<string> UserGroup { get; set; }

        public List<AddressGeoLocation> addressGeoLocations { get; set; }


    }
}
