using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class SiteMapViewModel
    {
        public List<PageSiteMapVM> Pages { get; set; }
        public List<PostSiteMapVM> Posts { get; set; }
        public List<EcommerceSiteMapVM> Categories { get; set; }
        public List<EcommerceSiteMapVM> Products { get; set; }
        public List<EcommerceSiteMapVM> Brands { get; set; }
        public List<SiteHostNamesWithCultureVM> HostNames { get; set; }

    }
    public class PageSiteMapVM
    {
        public string Name { get; set; }
        public string Culture { get; set; }
        public long CultureId { get; set; }
        public string PageSlug { get; set; }

        public long ParentId { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


    }

    public class PostSiteMapVM
    {
        public long? ParentId { get; set; }
        public string Culture { get; set; }

        public long CultureId { get; set; }
        public string PostSlug { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }

  

    public class EcommerceSiteMapVM
    {
        public long? ParentId { get; set; }
        public long? CultureId { get; set; }
        public string CultureCode { get; set; }
        public string Slug { get; set; }
        public string RelativeUrl { get; set; }
        public string Culture { get; set; }

        public DateTimeOffset? ModificationDate { get;  set; }
    }



    public class EcommerceDetailsSiteMapCiewModel
    {

        public long? CultureId { get; set; }
        public string CultureCode { get; set; }
        public string Slug { get; set; }
        public string RelativeUrl { get; set; }
        public string Culture { get; set; }

        public long? ParentId { get; set; }
    }

    public class EcommerceRelatedSiteMapVM
    {
        public long CultureId { get; set; }
        public string CultureCode { get; set; }

        public string Slug { get; set; }
    }



}
