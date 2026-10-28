using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Services
{
    public static class QuerableFilter
    { 

        public static IQueryable<SitesHostNames> FilterHostNames(this IQueryable<SitesHostNames> sitesHostNames,string HostName)
        {
            var  siteHostNames = sitesHostNames.Where(n =>
                          n.HostName.ToLower().Contains(HostName.ToLower()) &&!n.HostName.ToLower().Contains("www"));

            if (siteHostNames.Any())
            { sitesHostNames = siteHostNames; }
            else
            {
                sitesHostNames = sitesHostNames.Where(n =>
                             n.HostName.Contains("www"));

            }

            return sitesHostNames;
        }
    }
}
