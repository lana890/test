
using EskaCMS.Pages.Models;
using EskaCMS.Security.Models;
using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
   public interface IAdminApi
    {
        Task<SiteMapDetailsVM> GetSiteDetailsByHostName(string HostName);
        Task<List<PageCultureDetailsVM>> GetRelatedPages();
        Task<List<PostCultureDetailsVm>> GetRelatedPosts();
        Task<SiteMapViewModel> GetSiteDetailByHostName(string HostName);
        Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedProducts();
        Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedBrands();
        Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedCategory();
    }
}
