using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Services.Interface
{
    public interface ILoyaltyWalletService
    {
        Task<bool> CreateLoyaltyWallet(long userId);
        Task<bool> CreateLoyalityWallletToBulk(List<long> userIds, AddPointsDto points);

        Task<bool> SuspendLoyaltyWallet(long userId);
        Task<bool> ActiveLoyaltyWallet(long userId);
        Task<LoyaltyWalletDetails> GetLoyaltyWallet(long userId);
        Task<PaginationResult<LoyaltyTransaction>> GetLoyaltyTransactions(long userId, SmartTableParam param);
        Task<bool> ProcessRegistrationLoyaltySetups(long userId, string code);
        Task<bool> ProcessPurchesLoyaltySetups(Order order);
        Task<RedeemPointsValidationDto> RedeemPoints(long userId, long siteId, long? orderId, long? cartId, decimal totalAmount);
        Task<RedeemPointsValidationDto> RefundPoints(long userId, decimal RedeemAmount, long siteId, long? orderId, long? cartId);
        Task<RedeemPointsValidationDto> AddRedeemToOrder(long userId, long siteId, long orderId, long cartId);
        Task<bool> DepositUserWalletByAdmin(long userId, AddPointsDto model);
        Task<bool> RefundGainedPoints(long userId, long siteId, long orderId);
    }
}
