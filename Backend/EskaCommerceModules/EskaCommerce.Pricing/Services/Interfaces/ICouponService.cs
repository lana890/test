using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Pricing.Areas.Pricing.ViewModels;

using EskaCMS.Entities.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.ShoppingCart;

namespace EskaCommerce.Pricing.Services
{
    public interface ICouponService
    {

        Task<CouponValidationResult> Validate(long customerId, string couponCode, CartInfoForCoupon cart,bool? AfteraddOrder=false);

        void AddCouponUsage(long customerId, long orderId, CouponValidationResult couponValidationResult);

        Task<long> AddCoupon(CouponFormVM model);
        SmartTableResult<CouponFormVM> CouponList(SmartTableParam param);
        Task<CouponFormVM> GetCouponById(long id);
        Task EditCoupon(long id, CouponFormVM model);
        Task DeleteCoupon(long CouponId);
        Task<CouponValidationResult> ValidatePlan(long customerId, string couponCode,decimal SubTotal, long? CartRuleId=null);
        Task<ExcelFile> BulkAddition(long CartRuleId, List<CouponVM> Coupons);
        Task<bool> addCouponToGroup(long couponId, long GroupId);
        //Task<bool> IsFreeShippingApplicable(Cart cart);
        Task<bool> QualifiesForFreeShipping(Cart cart);


    }
}
