using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services;
using EskaCommerce.UserCatalog.Services.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCMS.Engine.ExpiredBookingsCancellation
{
    public class ExpiredBookingsCancellationBackgroundService : BaseBackgroundService
    {
        private readonly long SystemUserId = 2;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ExpiredBookingsCancellationBackgroundService(IServiceProvider serviceProvider
            , ILogger<ContentPublishBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(ExpiredBookingsCancellationBackgroundService), logger, serviceProvider, configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

        }

        public override async Task<bool> Execute()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(120));
                using var scope = _serviceProvider.CreateScope();
                var userBookingRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserBooking>>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await CancelExpiredBookings(userBookingRepository, mediator);

            }
            catch (Exception ex)
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("Expired Bookings Cancellation BackgroundService", "", ex.Message, "", null);

            }
            return true;
        }


        private async Task CancelExpiredBookings(IRepository<UserBooking> userBookingRepository, IMediator mediator)
        {
            try
            {
                var durationToCancel = DateTimeOffset.Now.AddMinutes(-5);
                var expiredUserBooking = await userBookingRepository.Query().Where(x =>
                    x.Status != UserBookingStatus.Expired &&
                    x.ReservationDate.Date == DateTime.Now.Date
                    ).ToListAsync();
                foreach (var userBooking in expiredUserBooking)
                {
                    UserBookingStatus oldStatus = userBooking.Status;
                    userBooking.Status = UserBookingStatus.Expired;
                    var userBookingChanged = new UserBookingChanged
                    {
                        UserBookingId = userBooking.Id,
                        OldStatus = oldStatus,
                        NewStatus = UserBookingStatus.Expired,
                        UserId = SystemUserId,
                        UserBooking = userBooking,
                        Note = "System canceled"
                    };
                    await mediator.Publish(userBookingChanged);
                    await userBookingRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

    }


}
