using EskaCms.CoreShared.ViewModels;
using EskaCMS.Core.BusinessModels;
using EskaCMS.CoreShared.ViewModels;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCms.CoreShared
{
    public static class QueryableExtensions
    {
        public static IQueryable<User> FilterUsers(this IQueryable<User> query, UserListSearchVM search, List<long> userIds, long siteId)
        {
            if (search == null)
                return query;

            query = query.Where(user =>
                user.UserSites.Any(us => us.SiteId == siteId) &&
                (string.IsNullOrEmpty(search.Name) ||
                    user.FullName.ToUpper().Contains(search.Name) ||
                    user.NormalizedUserName.Contains(search.Name)) &&
                (!search.GroupId.HasValue || search.GroupId == 0 ||
                    user.CustomerGroups.Any(r => r.CustomerGroupId == search.GroupId)) &&
                (search.GroupIds == null ||
                    user.CustomerGroups.Any(r => search.GroupIds.Contains(r.CustomerGroupId))) &&
                (search.RoleId == null || !search.RoleId.Any() ||
                    user.UserRoles.Any(r => search.RoleId.Contains(r.RoleId))) &&
                (search.Status == null || !search.Status.Any() ||
                    search.Status.Contains(user.Status)) &&
                (search.UserOptions == null || !search.UserOptions.Any() ||
                    userIds.Contains(user.Id)) &&
                (!search.Birthday.HasValue ||
                    (user.DateOfBirth.HasValue &&
                     user.DateOfBirth.Value.Month == search.Birthday.Value.Month &&
                     user.DateOfBirth.Value.Day == search.Birthday.Value.Day)) &&
                (!search.CreatedAfter.HasValue || user.CreationDate > search.CreatedAfter.Value) &&
                (!search.CreatedBefore.HasValue || user.CreationDate < search.CreatedBefore.Value));

            return query;
        }

        public static IQueryable<DepartmentEmployeeViewModel> FilterEmployees(
       IQueryable<DepartmentEmployeeViewModel> employees,
       ESearchType searchType,
       FilterUserVM model)
        {
            DateTime today = DateTime.Today;
            DateTime toDate = today.AddDays(model.Duration.Value);

            switch (searchType)
            {
                case ESearchType.NewEmployees:
                    DateTime fromDate = today.AddDays(-model.Duration.Value);
                    return employees.Where(e =>
                        e.StartDate.HasValue &&
                        e.StartDate.Value >= fromDate &&
                        e.StartDate.Value <= today
                    );

                case ESearchType.AnniversaryEmployees:
                    return employees
                        .Where(e => e.StartDate.HasValue)
                        .AsEnumerable()
                        .Where(e =>
                        {
                            var anniversary = e.StartDate.Value.AddYears(1);
                            return anniversary >= today && anniversary <= toDate;
                        })
                        .AsQueryable();

                case ESearchType.BirthDate:
                    return employees
                        .Where(e => e.DateOfBirth.HasValue)
                        .AsEnumerable()
                        .Where(e =>
                        {
                            var dob = e.DateOfBirth.Value;
                            var nextBirthday = new DateTime(today.Year, dob.Month, dob.Day);
                            if (nextBirthday < today)
                                nextBirthday = nextBirthday.AddYears(1);

                            var daysUntilBirthday = (nextBirthday - today).TotalDays;
                            return daysUntilBirthday <= model.Duration;
                        })
                        .AsQueryable();

                default:
                    return employees;
            }
        }

    }

}
