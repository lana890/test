using EskaCMS.Core.Services;
using EskaCMS.Engine;
using EskaCMS.Engine.PagePreviewCode.BGS;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Helper;
using EskaCommerce.Payments.Services.Interface;
using EskaCommerce.UserCatalog.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.RecurringPayments.BGS
{
    public class PayTabsRecuringBackgroundServices : BaseBackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(3000));
        private readonly IConfiguration _configuration;
        public readonly ILogger<PayTabsRecuringBackgroundServices> _logger;

        public PayTabsRecuringBackgroundServices(IServiceProvider serviceProvider
            , ILogger<PayTabsRecuringBackgroundServices> logger
            , IConfiguration configuration)
            : base(nameof(PayTabsRecuringBackgroundServices), logger, serviceProvider, configuration)
        {
            _logger = logger;

            this.serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public override async Task<bool> Execute()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                ISubscriptionServices SubscriptionServices = scope.ServiceProvider.GetRequiredService<ISubscriptionServices>();
                IPaymentServices PaymentServices = scope.ServiceProvider.GetRequiredService<IPaymentServices>();
                IPaymentMepsService payTabsServices = scope.ServiceProvider.GetRequiredService<IPaymentMepsService>();

                var UsersRecuring = await SubscriptionServices.GetUserPlanRecuring();
                foreach (var UserRecuring in UsersRecuring)
                {
                    var Result = await payTabsServices.PaytabsRecuring(UserRecuring.OrderId, UserRecuring.AgreementId, UserRecuring.Tran_ref);


                    await SubscriptionServices.UpdateUserPlanStatus(UserRecuring.OrderId, Result.token, Result.tran_ref);

                    await PaymentServices.UpdatePayment(UserRecuring.Order, Result.tran_ref, PaymentProviderHelper.MepsPayTabsProviderId, "Successful Creating Recuring Payment", Entities.Entity.PaymentStatus.Recurring);

                }
            }
            catch (Exception ex)
            {
                using var scope = serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("PayTabs Recuring BackgroundService", "", ex.Message, "", null);

            }

            return true;
        }
    }
}
