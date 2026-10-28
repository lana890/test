using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.cart;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Pricing.Model;
using EskaCommerce.Pricing.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCommerce.Pricing.Model.PromotionMvs;

namespace EskaCommerce.Pricing.Services
{
    public class ServicePromotion : IServicePromotions
    {
        public IWorkContext _workContext;
        public IRepository<CartRule> _CartRule;
        public IRepository<Promotions> _Promotions;

        public ServicePromotion(
            IWorkContext workContext,
            IRepository<CartRule> CartRule,
            IRepository<Promotions> Promotions
            )
        {
            _CartRule = CartRule;
            _workContext = workContext;
            _Promotions = Promotions;
        }
        public async Task<List<CartRuleMV>> GetAllCartRule()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                List<CartRuleMV> results = new List<CartRuleMV>();
                var auto_cart_rules = await _CartRule.Query()
                    .AsNoTracking()
                    .Where(x => x.IsAutoDiscount == true &&x.SiteId== SiteId && x.Status !=EStatus.Deleted)
                    .ToListAsync();

                results = auto_cart_rules.Select(x => new CartRuleMV
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<bool> CreatePromotion(RequestPromotionViewModel promotion)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                var exists = await _Promotions.Query().CountAsync(x => x.EPromotion == promotion.EPromotion && x.SiteId == SiteId) > 0;
                if (exists)
                {
                    throw new InvalidOperationException("This promotion type already exists for this site");
                }
                var prm = new Promotions
                {
                    Name = promotion.Name,
                    CreatedOn = DateTimeOffset.Now,
                    EPromotion = promotion.EPromotion,
                    QuantityOfOrders = promotion.QuantityOfOrders//??null,
                    ,
                    SiteId = SiteId,
                    UserState = promotion.UserState,
                    CreatedById = userId,
                    CartRuleId=promotion.CartRuleId,
                    ModifiedById=userId

                };
                _Promotions.Add(prm);

                await _Promotions.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<bool> UpdatePromotion(RequestPromotionViewModel promotion)
        {
            try
            {
                var SiteId= _workContext.GetCurrentSiteId();
                var existsedProm = await _Promotions.Query()
                    .Where(x => x.Id == promotion.Id)
                    .SingleOrDefaultAsync();
                var exists = await _Promotions.Query().CountAsync(x => x.EPromotion == promotion.EPromotion && x.SiteId == SiteId&&x.Id!=promotion.Id) > 0;
                if (exists)
                {
                    throw new InvalidOperationException("This promotion type already exists for this site");
                }
                var userId = await _workContext.GetCurrentUserId();
                existsedProm.Name = promotion.Name;
            
                existsedProm.QuantityOfOrders = promotion.EPromotion == PromotionsEnum.NumberOfOrders?promotion.QuantityOfOrders:null;
                existsedProm.CartRuleId = promotion.CartRuleId;
                existsedProm.EPromotion = promotion.EPromotion;
                existsedProm.UserState = promotion.UserState;
                existsedProm.ModificationOn = DateTimeOffset.Now;
                existsedProm.ModifiedById = userId;

                _Promotions.Update(existsedProm);
                await _Promotions.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> DeletePromotion(long promId)
        {
            try
            {
                var prom = _Promotions.Query().Where(x => x.Id == promId).SingleOrDefault();
                if (prom == null)
                {
                    return false;
                }
                else
                {
                    _Promotions.Delete(prom);
                    await _Promotions.SaveChangesAsync();
                    return true;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<Promotions> GetPromotoion(long promId)
        {
            try
            {
                var prom = await _Promotions.Query()
                                           .Include(x => x.CreatedBy)
                                           .Include(x => x.ModifiedBy)
                                           .Where(x => x.Id == promId).FirstOrDefaultAsync();
                if (prom == null)
                {
                    return null;
                }
                else
                {

                    return prom;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PaginationResult< PromotionMvs>> GetAllPromotion(int PageSize, int PageNumber)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var prom = await _Promotions.Query()
                    .Where(x => x.SiteId==siteId)
                                           .Include(x => x.CreatedBy)
                                           .Include(x => x.ModifiedBy).Select(x => new PromotionMvs
                                           {
                                               CartRuleId=x.CartRuleId,
                                               CreatedById=x.CreatedById,
                                               EPromotion=x.EPromotion,
                                               Name=x.Name,
                                               QuantityOfOrders=x.QuantityOfOrders,
                                               UserState=x.UserState,
                                               Id=x.Id

                                           })
                                           .PaginateAsync(PageSize, PageNumber);

                return prom;

            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
