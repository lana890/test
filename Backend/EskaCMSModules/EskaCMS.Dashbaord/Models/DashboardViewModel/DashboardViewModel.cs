
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Pages;

namespace EskaCMS.Dashboard.Models.DashboardViewModel
{
  
    public class DashboardViewModel
    {
        public DashboardCountersViewModel PageCounters { get; set; }
          public List<Page> lastEditedPages { get; set; }
        public List<VersionActivitesViewModel> VersionActivites { get; set; }
        public List<DailyVistsViewModel> DailyVists { get; set; }
    }

    public class DailyVistsViewModel
    {
        public DateTime date { get; set; }
        public long visites { get; set; }
     
    }

    
}
