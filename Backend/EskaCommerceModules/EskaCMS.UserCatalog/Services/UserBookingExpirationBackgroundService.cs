using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services
{


    public class UserBookingExpirationBackgroundService : BackgroundService
    {
        //  private readonly IRepository<UserBooking> userBookingRepository;

        private readonly long SystemUserId = 2;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        //private readonly IBackgroundSettingsService _BackgroundSettingsService;
        public UserBookingExpirationBackgroundService(IServiceProvider serviceProvider
            , ILogger<UserBookingExpirationBackgroundService> logger
           // IBackgroundSettingsService BackgroundSettingsService*/
           //
           )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            //  _BackgroundSettingsService = BackgroundSettingsService;
        }


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            using (var scope = _serviceProvider.CreateScope())
            {
                var _BackgroundSettingsService = scope.ServiceProvider.GetRequiredService<IBackgroundSettingsService>();
                _logger.LogInformation("ExpiredBookingsCancellationBackgroundService : Get Settings");
                var ServiceSettings = await _BackgroundSettingsService.Get(this.GetType().Name);
                if (ServiceSettings != null)
                {
                    if (ServiceSettings.IsActive)
                    {
                        _logger.LogInformation("UserBookingExpirationBackgroundService is starting.");
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("UserBookingExpirationBackgroundService is working.");
                            await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
                            var UserBookingRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserBooking>>();
                            var UserBookingService = scope.ServiceProvider.GetRequiredService<IUserBookingService>();
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                            await FetchExpiredUserBookings(UserBookingRepository, UserBookingService, mediator, stoppingToken);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No active settings for : UserBookingExpirationBackgroundService");
                    }
                }
                else
                {
                    _logger.LogInformation("No active settings for : UserBookingExpirationBackgroundService");
                }

            }
        }

        //public void RegisterUserBookingExpirationJob()
        //{
        //    RecurringJob.AddOrUpdate(() => FetchExpiredUserBookings(), Cron.Minutely);
        //}
        public async Task FetchExpiredUserBookings(IRepository<UserBooking> UserBookingRepository, IUserBookingService UserBookingService, IMediator mediator, CancellationToken stoppingToken)
        {
            var bufferTime = DateTimeOffset.Now.TimeOfDay.Subtract(TimeSpan.FromMinutes(5));
            var expiredUserBooking = UserBookingRepository.Query()
                .Where(x =>
               x.Status != UserBookingStatus.Expired &&
               x.Status != UserBookingStatus.OnTheWay &&
               x.Status != UserBookingStatus.ServiceStarted &&
               x.Status != UserBookingStatus.ServiceCompleted &&
               (x.ReservationDate.Date <= DateTimeOffset.Now.Date || (x.ReservationDate.Date <= DateTimeOffset.Now.Date && x.ReservationTime <= bufferTime))
               )
                .ToList();
            foreach (var userBooking in expiredUserBooking)
            {
                UserBookingStatus oldStatus = userBooking.Status;
                userBooking.Status = UserBookingStatus.Expired;
                UserBookingRepository.SaveChanges();
            }
        }

    }

}
