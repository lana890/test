using EskaCMS.Engine.OrderCancelation.BGS;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.NotifyCart.BG
{
    public class NotifyActiveCartBeforeBackgroundServices : BaseBackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(3000));
        private readonly int cartNotificationLeadTimeInMinutes;

        public NotifyActiveCartBeforeBackgroundServices(IServiceProvider serviceProvider, ILogger<NotifyActiveCartBeforeBackgroundServices> logger
            , IConfiguration configuration) : base(nameof(NotifyActiveCartBeforeBackgroundServices), logger, serviceProvider, configuration)
        {
            this.serviceProvider = serviceProvider;
            cartNotificationLeadTimeInMinutes = Convert.ToInt32(configuration.GetSection("BGSs.NotifyCart:TimeToNotifyUser").Value);

        }

        public override async Task<bool> Execute()
        {
            await Task.Delay(TimeSpan.FromSeconds(20));

            using (var scope = serviceProvider.CreateScope())
            {
                var backgroundSettingsRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<BackgroundServiceSettings, string>>();
                var NotifyCartBeforeExpirationSettings = await backgroundSettingsRepository.Query().Where(x => this.GetType().Name == x.Id).FirstOrDefaultAsync();
                try
                {
                    if (NotifyCartBeforeExpirationSettings != null)
                    {
                        if (NotifyCartBeforeExpirationSettings.IsActive)
                        {
                            var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

                            await cartService.NotifyUserAfterPeriod("NotifyCartBG", cartNotificationLeadTimeInMinutes);
                        }


                    }
                }
                catch (Exception ex)
                {
                }

                return true;
            }
        }
    }
    }
