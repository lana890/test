using EskaCMS.BlogArchive.Models;
using EskaCMS.Pages.Models;
using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
   public class SiteMapDetailsVM
   {
        public IList<PageSiteMapResultVM> Pages { get; set; }
        public List<PostsViewModel> Posts { get; set; }


        public List<SiteHostNamesWithCultureVM> HostNames { get; set; }



    }
}
