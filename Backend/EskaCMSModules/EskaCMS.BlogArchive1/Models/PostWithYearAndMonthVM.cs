using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.BlogArchive.Models
{
    public class PostGroupByYearVM
    {
        public int Year { get; set; }
        public List<PostGroupByMonthVM> Month { get; set; }
        public int TotalCount { get; set; }
    }



    public class PostGroupByMonthVM
    {
        public string MonthName { get; set; }
        public int MonthNumber { get; set; }
        public List<PostModel> post { get; set; }
        public int TotalCount { get; set; }
    }
    public class PostModel
    {
        public string Title { get; set; }
        public long PostId { get; set; }
    }


}
