using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Dashboard.Models.DashboardViewModel
{

    public class DashboardCountersViewModel
    {
        public long publishedPages { get; set; }
        public long unpublishedPages { get; set; }
        public long publishedBlogs { get; set; }
        public long unpublishedBlogs { get; set; }
        public int ExpiredBlogs { get; set; }
        public long sitesNumber { get; set; }

        public long menuCatesgoriesNumber { get; set; }
    }

}
