using EskaCommerce.TelrPayment.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.TelrPayment.Services.Interfaces
{
   public interface ITelrPayment
    {
     
        Task RegisterTelrPayment(Dictionary<string, string> dic, long SiteId);
        Task<TelrResponseVM> TelrPlanRequest(long SiteId, CartTypes CartType);

        Task<TransactionsVm> GetTransactionsSearch(long OrderId, long SiteId);
        //Task<bool> PaymentRefund(long UserPlanId, long SiteId);
        Task PaymentCancelation(long UserPlanId, long SiteId);
    }
}
