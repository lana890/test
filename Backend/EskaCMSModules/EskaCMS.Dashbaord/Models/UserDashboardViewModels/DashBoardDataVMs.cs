using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserDashboard.ViewModels
{
    public class GetDashBoardDataVM
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }

        public long SiteId { get; set; }
        public long NumberOfNewUsers { get; set; }
        public long NumberOfReturningUsers { get; set; }


    }

    public class PeriodVM
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public long? UserId { get; set; }
        public string BrowserType { get; set; }
        public long? RoleId { get; set; }


    }

    public class UsersByVisitVM 
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public long? UserId { get; set; }
        public string FromSlug { get; set; }
        public string ToSlug { get; set; }
        public string SingleSlug { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string SearchText{ get; set; }
        public SortType sortType { get; set; }
        public long SearchByUserRole{ get; set; }
        public string searchByBrowserName { get; set; }


    }



    public class UsersByVisitResultVM
    {
        public long NavigationId { get; set; }
        public DateTimeOffset? NavigationDate { get; set; }
        public long? UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
       
        public TimeSpan PeriodTime { get; set; }
        public string BrowserName { get; set; }
        public List<long> UserRoleIds { get; set; }
        public List<string> UserRoleName { get; set; }
        public string ClientIp { get; set; }


    }

    public class UserRolesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }


    public class NavigationsByUserAndVisitVM
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public long UserId { get; set; }
        public string FromSlug { get; set; }
        public string ToSlug { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }


    }



    public class NavigationsByUserAndVisitResultVM
    {
        public long NavigationId { get; set; }
        public DateTimeOffset? NavigationDate { get; set; }

    }


    public class NavigationVM
    {
        public string Slug { get; set; }
        public string PreviousSlug { get; set; }

    }


    public class UserVisitVM
    {
        public long Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long NavigationsCount { get; set; }

    }


    public class FlowStatisticsVM
    {
        public string Slug { get; set; }
        public string PreviousSlug { get; set; }
        public int count { get; set; }

    }
    public class UserNavigationVM
    {
        public string Slug { get; set;}
        public string PreviousSlug { get; set; }
        public string SingleSlug { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public long UserId { get; set; }


    }

    public class UserNavigationResultVM
    {
       
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string FromSlug { get; set; }
        public string ToSlug { get; set; }
        public TimeSpan Period { get; set; }
        public string ClientIp { get; set; }
        public string BrowserName { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public DateTimeOffset FromDate { get; set; }
        public DateTimeOffset ToDate { get; set; }



    }


}
