using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Models.TransactionHistory;
using EskaCommerce.Payments.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Services
{
    public class PaymentServices : IPaymentServices
    {
        private readonly IRepository<PaymentTransactionHistory> paymentTransactionHistoryRepo;
        private readonly IRepository<Payment> paymentRepository;
		public PaymentServices(IRepository<PaymentTransactionHistory> PaymentTransactionHistoryRepo,IRepository<Payment> paymentRepository)
        {
            paymentTransactionHistoryRepo = PaymentTransactionHistoryRepo;
            this.paymentRepository = paymentRepository;
        }

        public async Task<bool> AddTransactionHistory(TransactionHistoryDto transactionHistoryDto)
        {
            try
            {

                var TransactionHistory = await paymentTransactionHistoryRepo.Query()
                    .Where(x=>x.OrderId==transactionHistoryDto.OrderId)
                    .OrderByDescending(x=>x.Id).FirstOrDefaultAsync();

                var HistoryObject = new PaymentTransactionHistory()
                {
                    Date = transactionHistoryDto.Date,
                    CartId = transactionHistoryDto.CartId,
                    Channel = transactionHistoryDto.Channel,
                    NewStatus = transactionHistoryDto.NewStatus,
                    OldStatus = transactionHistoryDto.OldStatus,
                    Info = transactionHistoryDto.Info,
                    ParentId = transactionHistoryDto.ParentId,
                    PaymentId = transactionHistoryDto.PaymentId,
                    PaymentMethod = transactionHistoryDto.PaymentMethod,
                    OrderId = transactionHistoryDto.OrderId,
                    UserId = transactionHistoryDto.UserId
                };


                if (TransactionHistory != null)
                {
                    HistoryObject.OldStatus = TransactionHistory.OldStatus;
                    HistoryObject.ParentId = TransactionHistory.Id;
                }

                paymentTransactionHistoryRepo.Add(HistoryObject);
                

                await paymentTransactionHistoryRepo.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw new Exception( ex.Message);
            }
        }
        public async Task CreatePayment(Order order,string GatewayTransactionId,string PaymentMethod,string Exception, PaymentStatus NewPaymentStatus, string AgreementId=null, decimal? amount = null)
        {
            try
            {
                    var payment = new Payment()
                    {
                        OrderId = Convert.ToInt64(order.Id),
                        Amount = amount.HasValue ? amount.Value : order.OrderTotal,
                        PaymentMethod = PaymentMethod,
                        CreatedOn = DateTimeOffset.UtcNow,
                        GatewayTransactionId = GatewayTransactionId,
                        Status = NewPaymentStatus,
                        FailureMessage = Exception,
                        AgreementId = AgreementId
                    };

                    paymentRepository.Add(payment);

				try
				{

					await paymentRepository.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException ex)
				{
					foreach (var entry in ex.Entries)
					{
						Console.WriteLine("🔥 Concurrency error on entity: "
										  + entry.Entity.GetType().Name);

						var primaryKey = entry.Metadata.FindPrimaryKey()
							.Properties
							.Select(p => entry.Property(p.Name).CurrentValue)
							.FirstOrDefault();

						Console.WriteLine("Key: " + primaryKey);
					}

					throw;
				}


				await paymentRepository.SaveChangesAsync();

                await AddTransactionHistory(new TransactionHistoryDto
                {

                    Date = payment.CreatedOn,
                    Channel = order.Channel,
                    CartId = order.CartId,
                    NewStatus = NewPaymentStatus,
                    OrderId = order.Id,
                    Info = Exception,
                    PaymentId = payment.Id,
                    PaymentMethod = PaymentMethod,
                    UserId=order.CustomerId,
                    ParentId=null,
                    OldStatus=null
                });

            }
            catch (Exception EX)
            {

                throw;
            }
        }

		

		public async Task UpdatePayment(Order order, string GatewayTransactionId, string PaymentMethod, string Exception, PaymentStatus NewPaymentStatus,string AgreementId=null)
        {
            try
            {
                var payment = await paymentRepository.Query().Where(x => x.OrderId == order.Id).FirstOrDefaultAsync();
                payment.GatewayTransactionId = GatewayTransactionId;
                payment.Status = NewPaymentStatus;
                payment.FailureMessage = Exception;
                if (!string.IsNullOrEmpty(payment.AgreementId))
                { payment.AgreementId = AgreementId; } 
                await paymentRepository.SaveChangesAsync();
                await AddTransactionHistory(new TransactionHistoryDto
                {

                    Date = payment.CreatedOn,
                    Channel = order.Channel,
                    CartId = order.CartId,
                    NewStatus = NewPaymentStatus,
                    OrderId = order.Id,
                    Info = Exception,
                    PaymentId = payment.Id,
                    PaymentMethod = PaymentMethod,
                    UserId = order.CustomerId,
                    ParentId = null,
                    OldStatus = null
                });
            }
            catch (Exception EX)
            {

                throw;
            }
        }
        public async Task<Payment> GetPaymentByOrderId(long orderId)
        {
            try
            {
                return await paymentRepository.Query().Where(x=>x.OrderId==orderId).FirstOrDefaultAsync();
            }
            catch (Exception EX)
            {

                throw;
            }
        }

     

    }
}
