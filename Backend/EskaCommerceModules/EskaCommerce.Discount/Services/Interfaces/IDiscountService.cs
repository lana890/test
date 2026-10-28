using EskaCommerce.Discount.Models;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Discount.Services
{
   public interface IDiscountService
    {
        Task<IList<DiscountVM>> GetDiscountsBySiteId(long SiteId);
        Task<DiscountVM> GetDiscount(long Id, long SiteId);
        Task<bool> Delete(long id);
        Task<decimal> CalculateDiscount(long siteId, decimal tiersAmount, long productId ,long UserId);
        Task<decimal> CalculateGlobalDiscount(long siteId, decimal tiersAmount);
        Task<decimal> CalculatePlanDiscount(long DiscountId, decimal tiersAmount);
        Task<bool> UpsertDiscount(RequestDiscountViewModel model);
    }
}
