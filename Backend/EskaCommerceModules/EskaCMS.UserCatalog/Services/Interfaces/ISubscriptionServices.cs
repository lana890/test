using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure;
using EskaCommerce.UserCatalog.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Services
{
    public interface ISubscriptionServices
    {
        Task HandleRegularPlans(Cart cart, long SiteId, long OrderId, PlansVersionsOutput plansVersionsOutput, Order orderCreationResult);
        bool IsApplicableCartType(CartTypes cartType);
        string CalculateRepeatStart(PlanTypes planTypes, PlanEnums.RepeatPeriod repeatPeriod, int repeatInterval);
        Task UpdateUserPlanStatus(long orderId, string token, string Trans_ref);
        
        //    long GetSubscribedNumber(long UserPlanId);
        // Task<long> GetLatestTransactionId(long OrderId);

        //  Task CreatePlanHistory(long UserplanId, EUserPlansStatus? oldStatus, EUserPlansStatus newStatus, string AdditionInfo);
        Task CnacelUserPlan(UserPlansOutput userPlan, bool? isRefundSuccess = false);

        Task<List<UserPlans>> GetUserPlanRecuring();
    }
}
