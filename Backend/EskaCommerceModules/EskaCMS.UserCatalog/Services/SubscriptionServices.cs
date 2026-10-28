using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.TelrPayment;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Notifications.Notifiers;

using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Services
{
    public class SubscriptionServices : ISubscriptionServices
    {

        private readonly IWorkContext workContext;
        private readonly IRepository<Payment> paymentRepository;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository;
        private readonly IRepository<UserPlans> userPlansRepository;
        private readonly IUserPlansService userPlansService;
        private readonly IRepository<TransactionHistory> transHistoryRepository;
        private readonly IdcmsLogService dcmsLogService;
        private readonly ISiteSettingsService settingservice;

        private readonly IRepository<UserPlansHistory> userplanHistoryRepository;


        public SubscriptionServices(
          IWorkContext workContext,
          IRepository<Payment> paymentRepository,
          IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
          IRepository<UserPlans> UserPlansRepository,
          IUserPlansService UserPlansService,
          IRepository<TransactionHistory> transHistoryRepository,
          IdcmsLogService dcmsLogService,
          ISiteSettingsService settingservice,
            IRepository<UserPlansHistory> userplanHistoryRepository
   )
        {
 
            this.workContext = workContext;
            this.paymentRepository = paymentRepository;
            this.paymentProviderRepository = paymentProviderRepository;
            userPlansRepository = UserPlansRepository;
            userPlansService = UserPlansService;
            this.transHistoryRepository = transHistoryRepository;
            this.dcmsLogService = dcmsLogService;
            this.settingservice = settingservice;

            this.userplanHistoryRepository = userplanHistoryRepository;

        }

        public async Task HandleRegularPlans(Cart cart, long siteId, long orderId, PlansVersionsOutput planVersionDetails, Order orderCreationResult)
        {
            try
            {
                var CultureId = workContext.GetCurrentCultureId();

                if (cart.PlansVersionId.HasValue)
                {
                    planVersionDetails = await userPlansService.GetLatestVersion(cart.PlansVersionId.Value);
                    string planName = planVersionDetails.Plan.Name;
                    var zeroDecimalOrderAmount = orderCreationResult.OrderTotal;
                    if (planVersionDetails.Plan.RecurringType == RecurringTypes.Recurring)

                    {
                        if (PlanTypes.Trial == planVersionDetails.Plan.PlanType)
                        {
                            planVersionDetails = await userPlansService.GetPlanLatestVersion(cart.PlansVersion.Plan.NextPlanId.Value);

                            await HandleTrialPlan(cart, siteId, orderId, planVersionDetails);

                        }
                        else
                        {
                            await HandleRegularPlan(cart, siteId, orderId, planVersionDetails);
                        }
                    }
                    else
                    {
                        await HandleOneTimeService(planVersionDetails, siteId, orderId);
                       
                    }

                }
                else
                {
                    throw new Exception("Plan is empty");
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public bool IsApplicableCartType(CartTypes cartType)
        {
            return (cartType == CartTypes.Plan || cartType == CartTypes.Custom);
        }

        public string CalculateRepeatStart(PlanTypes planTypes, PlanEnums.RepeatPeriod repeatPeriod, int repeatInterval)
        {
            string repeatStart = string.Empty;

            if (repeatPeriod == PlanEnums.RepeatPeriod.M)
            {
                repeatStart = planTypes == PlanTypes.Trial ? DateTime.Now.AddMonths(1).ToString("ddMMyyyy") : DateTime.Now.AddMonths(repeatInterval).ToString("ddMMyyyy");
            }
            else if (repeatPeriod == PlanEnums.RepeatPeriod.W)
            {
                repeatStart = planTypes == PlanTypes.Trial ? DateTime.Now.AddDays(7).ToString("ddMMyyyy") : DateTime.Now.AddDays(repeatInterval * 7).ToString("ddMMyyyy");
            }

            return repeatStart;
        }

        public async Task UpdateUserPlanStatus(long orderId,string token,string Trans_ref)
        {
            try
            {
                var userPlans = await userPlansService.GetUserPlanByOrderId(orderId);
                if (userPlans != null && userPlans.Count > 0)
                {
                    foreach (var plan in userPlans)
                    {
                        if (!string.IsNullOrEmpty(token))
                        { 
                            plan.AgreementId = token;
                        }
                        plan.Tran_ref = Trans_ref;

                        await CheckAndUpdateUserPlan(plan);
                    }
                }
            }
            catch (Exception exc)
            {
                await HandleException(exc, orderId, "UpdateUserPlanStatus");
            }
        }


        //public long GetSubscribedNumber(long UserPlanId)
        //{
        //    var UserPlansHistory = userPlansService.GetUserPlanHistoryByUserPlanId(UserPlanId).Result;
        //    if (UserPlansHistory != null)
        //        return UserPlansHistory.Where(x => x.UserPlanId == UserPlanId && x.CurrentStatus == EUserPlansStatus.Subscribed).Count();
        //    else
        //        return 0;

        //}


        //public async Task<long> GetLatestTransactionId(long OrderId)
        //{
        //    try
        //    {
        //        // var PaymentId=   await _transHistoryRepository.Query().Where(x => x.OrderId == userPlansOutput.Order.Id).Select(x => x.PaymentId.ToString()).FirstOrDefaultAsync();

        //        var Payment = await paymentRepository.Query().Where(x => x.OrderId == OrderId).Select(x => x.GatewayTransactionId).FirstOrDefaultAsync();
        //        if (Payment != null)
        //            return Convert.ToInt64(Payment);
        //        else
        //            return 0;
        //    }
        //    catch (Exception exc)
        //    {

        //        throw exc;
        //    }
        //}


        //public async Task CreatePlanHistory(long UserplanId, EUserPlansStatus? oldStatus, EUserPlansStatus newStatus, string AdditionInfo)
        //{
        //    UserPlansHistory userplanHis = new UserPlansHistory
        //    {
        //        TransDate = DateTimeOffset.Now,
        //        AdditionalInfo = AdditionInfo,
        //        OldStatus = oldStatus,
        //        CurrentStatus = newStatus,
        //        UserPlanId = UserplanId
        //    };
        //    userplanHistoryRepository.Add(userplanHis);
        //    await userplanHistoryRepository.SaveChangesAsync();
        //}

        public async Task CnacelUserPlan(UserPlansOutput userPlan, bool? isRefundSuccess =false)
        {
            UserPlansUpdate objuserPlansUpdate = new UserPlansUpdate();
            objuserPlansUpdate.CurrentStatus = EUserPlansStatus.Canceled;
            objuserPlansUpdate.NextStatus = EUserPlansStatus.Expired;

            if (isRefundSuccess.HasValue && isRefundSuccess.Value)
            {
                objuserPlansUpdate.CurrentStatus = EUserPlansStatus.Refunded;
            }

            objuserPlansUpdate.IsSubscribed = false;
            objuserPlansUpdate.Id = userPlan.Id;

            await userPlansService.Update(objuserPlansUpdate);
        }

        #region Helpers Plans
        private async Task HandleRegularPlan(Cart cart, long siteId, long orderId, PlansVersionsOutput planVersionDetails)
        {
            // Current Waiting Payment || Next subscribed
            planVersionDetails = await userPlansService.GetPlanLatestVersion(cart.PlansVersion.PlanId
                );
            await userPlansService.Create(planVersionDetails.PlanId, siteId, orderId, "", EUserPlansStatus.WaitingPayment, EUserPlansStatus.Subscribed);

        }

        private async Task HandleTrialPlan(Cart cart, long siteId, long orderId, PlansVersionsOutput planVersionDetails)
        {


            if (cart.PlansVersion.Plan.NextPlanId.HasValue)
            {
                var trial = await userPlansService.Create(cart.PlansVersion.PlanId, siteId, orderId, "", EUserPlansStatus.WaitingPayment, EUserPlansStatus.Expired);

                // Create Next real plan
                await userPlansService.Create(Convert.ToInt64(cart.PlansVersion.Plan.NextPlanId), siteId, orderId, "", EUserPlansStatus.WaitingPayment, EUserPlansStatus.Scheduled, false, trial);
            }

        }

        private async Task HandleOneTimeService(PlansVersionsOutput planVersionDetails, long siteId, long orderId)
        {
            try
            {
            
                await userPlansService.Create(planVersionDetails.PlanId, siteId, orderId, "", EUserPlansStatus.WaitingPayment, EUserPlansStatus.Expired, true);

            }
            catch (Exception EX)
            {

                throw EX;
            }
        

        }

        #endregion


        #region UpdateUserPlan

        private async Task CheckAndUpdateUserPlan(UserPlansOutput userPlan)
        {
            try
            {
                UserPlansUpdate objUserPlansUpdate = new UserPlansUpdate();

                if (userPlan.CurrentStatus == EUserPlansStatus.WaitingPayment)
                {
                    objUserPlansUpdate.AgreementId = userPlan.AgreementId;
                    objUserPlansUpdate.Tran_Ref = userPlan.Tran_ref;
                    await HandleWaitingPaymentStatus(userPlan, objUserPlansUpdate);
                }
                else if (userPlan.CurrentStatus == EUserPlansStatus.Subscribed)
                {
                    objUserPlansUpdate.AgreementId = userPlan.AgreementId;
                    objUserPlansUpdate.Tran_Ref = userPlan.Tran_ref;
                    await HandleSubscribedStatus(userPlan, objUserPlansUpdate);
                }
                else if (userPlan.CurrentStatus == EUserPlansStatus.Scheduled && userPlan.NextStatus == EUserPlansStatus.Subscribed)
                {
                    objUserPlansUpdate.AgreementId = userPlan.AgreementId;
                    objUserPlansUpdate.Tran_Ref = userPlan.Tran_ref;
                    await HandleScheduledStatus(userPlan, objUserPlansUpdate);
                }
                //else
                //{
                //    await HandleNoMatchingStatus(userPlan);
                //}
            }
            catch (Exception exc)
            {
                await HandleException(exc, userPlan.Id, "CheckAndUpdateUserPlan");
            }
        }

        private async Task HandleWaitingPaymentStatus(UserPlansOutput userPlan, UserPlansUpdate objUserPlansUpdate)
        {
            if (userPlan.NextStatus == EUserPlansStatus.Subscribed)
            {
                objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Subscribed;
                objUserPlansUpdate.NextStatus = EUserPlansStatus.Recurring;
                objUserPlansUpdate.IsSubscribed = true;
            }
            else if (userPlan.NextStatus == EUserPlansStatus.Expired)
            {
                if (userPlan.PlanType == PlanTypes.Trial)
                {
                    objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Subscribed;
                    objUserPlansUpdate.NextStatus = EUserPlansStatus.Expired;
                    objUserPlansUpdate.IsSubscribed = true;
                }
                else
                {
                    objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Expired;
                    objUserPlansUpdate.NextStatus = EUserPlansStatus.Inactive;
                    objUserPlansUpdate.IsSubscribed = false;
                }
            }
            else if (userPlan.NextStatus == EUserPlansStatus.Scheduled)
            {
                objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Scheduled;
                objUserPlansUpdate.NextStatus = EUserPlansStatus.Subscribed;
                objUserPlansUpdate.IsSubscribed = true;
            }

            objUserPlansUpdate.Id = userPlan.Id;
            await userPlansService.Update(objUserPlansUpdate);
            await LogInfo(userPlan, "No matching with statuses");
        }

        private async Task HandleSubscribedStatus(UserPlansOutput userPlan, UserPlansUpdate objUserPlansUpdate)
        {
            if (userPlan.NextStatus == EUserPlansStatus.Recurring)
            {
                objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Subscribed;
                objUserPlansUpdate.NextStatus = EUserPlansStatus.Recurring;
                objUserPlansUpdate.IsSubscribed = true;
            }
            else if (userPlan.NextStatus == EUserPlansStatus.Canceled)
            {
                objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Canceled;
                objUserPlansUpdate.NextStatus = EUserPlansStatus.Inactive;
                objUserPlansUpdate.IsSubscribed = false;
            }
            else if (userPlan.NextStatus == EUserPlansStatus.Expired)
            {
                if (userPlan.PlanType == PlanTypes.Trial)
                {
                    objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Subscribed;
                    objUserPlansUpdate.NextStatus = EUserPlansStatus.Expired;
                    objUserPlansUpdate.IsSubscribed = true;
                }
                else
                {
                    objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Expired;
                    objUserPlansUpdate.NextStatus = EUserPlansStatus.Inactive;
                    objUserPlansUpdate.IsSubscribed = false;
                }
            }

            objUserPlansUpdate.Id = userPlan.Id;
            await userPlansService.Update(objUserPlansUpdate);
            await LogInfo(userPlan, "No matching with statuses");
        }

        private async Task HandleScheduledStatus(UserPlansOutput userPlan, UserPlansUpdate objUserPlansUpdate)
        {
            objUserPlansUpdate.CurrentStatus = EUserPlansStatus.Subscribed;
            objUserPlansUpdate.NextStatus = EUserPlansStatus.Recurring;
            objUserPlansUpdate.IsSubscribed = true;
            objUserPlansUpdate.Id = userPlan.Id;
            await userPlansService.Update(objUserPlansUpdate);
            await LogInfo(userPlan, "No matching with statuses");
        }

        private async Task HandleNoMatchingStatus(UserPlansOutput userPlan)
        {
            var settings = await settingservice.GetSiteSettingById("PaymentHold", 1);
            var emailSetting = JsonConvert.DeserializeObject<EmailViewModel>(settings.Value);
            List<string> adminEmails = new List<string>();
            adminEmails.Add(emailSetting.Value);

            // await _emailSender.SendEmailAsync(adminEmails, "Update Plan to Subscribe failure", "Failed to update plan to subscribed, UserPlanId = " + userPlan.Id + " PlanName : " + userPlan.PlanName, 1);
            await LogInfo(userPlan, "No matching with statuses");
        }

        private async Task LogInfo(UserPlansOutput userPlan, string details)
        {
            await dcmsLogService.AddInfo(userPlan.UserId, "FromPlansServices", "CheckAndUpdateUserPlan", userPlan.Id, userPlan.PlanName, "UserPlan.CurrentStatus : " + userPlan.CurrentStatus + " / " + "UserPlan.NextStatus : " + userPlan.NextStatus, null, details, "", null);
        }

        private async Task HandleException(Exception exc, long id, string methodName)
        {
            await dcmsLogService.AddError("FromPlansServices", exc.Message, methodName + "Id : = " + id.ToString(), methodName, null);
            throw exc;
        }

        public async Task<List<UserPlans>> GetUserPlanRecuring()
        {
            try
            {
                var UserPlans = await userPlansRepository.Query().Include(x => x.Order)
                                                                 .Where(x => x.NextStatus == EUserPlansStatus.Recurring || x.NextStatus == EUserPlansStatus.Subscribed)
                                                                 .Where(x => x.NextStatusDate.Date == DateTimeOffset.Now.Date)
                                                                 .ToListAsync();  

                return UserPlans;   
            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }

        #endregion
    }
}
