using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ProductRecentlyViewed.Services.Interfaces
{
    public interface IRecentlyViewedProductServices
    {
        Task<PaginationResult<RecentlyViewUserVM>> GetRecentlyViewedProduct(RecentlyRequestViewModel model);
        Task<PaginationResult<MostViewedProducts>> GetMostViewedProduct(SmartTableParam model);
    }
}
