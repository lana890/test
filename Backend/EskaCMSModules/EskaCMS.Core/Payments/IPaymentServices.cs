using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Payments;
using EskaCommerce.Payments.Models.TransactionHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Services.Interface
{
    public interface IPaymentServices
    {

        Task<bool> AddTransactionHistory(TransactionHistoryDto transactionHistoryDto);
        Task CreatePayment(Order order, string GatewayTransactionId, string PaymentMethod, string Exception, PaymentStatus NewPaymentStatus,string AgreementId = null, decimal? amount = null);
        Task<Payment> GetPaymentByOrderId(long orderId);
        Task UpdatePayment(Order order, string GatewayTransactionId, string PaymentMethod, string Exception, PaymentStatus NewPaymentStatus, string AgreementId=null);
    }
}
