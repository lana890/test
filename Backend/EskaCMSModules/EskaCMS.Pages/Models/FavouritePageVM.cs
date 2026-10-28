using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class FavouritePageVM
    {
        public string Name { get; set; }
        public long PageId { get; set; }
        public long slugInfoId { get;set; }
        public string Slug { get; set; }

    }
}
