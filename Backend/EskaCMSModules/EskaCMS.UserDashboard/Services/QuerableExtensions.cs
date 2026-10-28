using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EskaCMS.UserDashboard.Services
{
    public static class QuerableExtensions
    {

        public static IEnumerable<UserNavigations> FilterSlug(this IEnumerable<UserNavigations> userNavigations, UsersByVisitVM usersByVisit)
        {
            if (string.IsNullOrWhiteSpace(usersByVisit.FromSlug) && string.IsNullOrWhiteSpace(usersByVisit.ToSlug))
            {
                var singleSlug = usersByVisit.SingleSlug?.Trim();
                if (!string.IsNullOrWhiteSpace(singleSlug))
                {
                    userNavigations = userNavigations.Where(n =>
                        StringEqualsIgnoreCase(n.PreviousSlug, singleSlug) &&
                        StringEqualsIgnoreCase(n.Slug, singleSlug));
                }
            }
            else
            {
                var toSlug = usersByVisit.ToSlug?.Trim();
                var fromSlug = usersByVisit.FromSlug?.Trim();

                userNavigations = userNavigations.Where(n =>
                {
                    var matchesToSlug = string.IsNullOrWhiteSpace(toSlug) || StringEqualsIgnoreCase(n.Slug, toSlug);
                    var matchesFromSlug = StringEqualsIgnoreCase(n.PreviousSlug, fromSlug);
                    return matchesToSlug && matchesFromSlug;
                });
            }

            if (usersByVisit.FromDate.HasValue)
            {
                userNavigations = userNavigations.Where(n =>
                                     n.CreationDate.Date >= usersByVisit.FromDate.Value.Date);
            }
            if (usersByVisit.ToDate.HasValue)
            {
                userNavigations = userNavigations.Where(n =>
                               n.CreationDate.Date <= usersByVisit.ToDate.Value.Date);
            }

            return userNavigations;
        }

        private static bool StringEqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
