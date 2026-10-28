using EskaCMS.Entities.Entity.cart;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Pricing.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCommerce.Pricing.Model.PromotionMvs;

namespace EskaCommerce.Pricing.Services.Interfaces
{
    public interface IServicePromotions
    {
        Task<List<CartRuleMV>> GetAllCartRule();
        Task<bool> CreatePromotion(RequestPromotionViewModel promotion);
        Task<bool> UpdatePromotion(RequestPromotionViewModel promotion);
        Task<bool> DeletePromotion(long promId);
        Task<Promotions> GetPromotoion(long promId);
        Task<PaginationResult<PromotionMvs>> GetAllPromotion(int PageSize, int PageNumber);

    }
}
