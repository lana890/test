using EskaCMS.Infrastructure.Helpers;

using EskaCommerce.Notifications.Services;
using EskaCommerce.HangfireJobs.Models;

namespace EskaCommerce.Notifications.Jobs
{
    /// <summary>
    /// This background job distributes notifications to users.
    /// </summary>
    public class NotificationDistributionJob : BackgroundJob<NotificationDistributionJobArgs>
    {
        private readonly INotificationDistributer _notificationDistributer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDistributionJob"/> class.
        /// </summary>
        public NotificationDistributionJob(INotificationDistributer notificationDistributer)
        {
            _notificationDistributer = notificationDistributer;
        }

        public override void Execute(NotificationDistributionJobArgs args)
        {
            if (args.NotificationId == null || args.NotificationId.Count == 0)
                return;
            AsyncHelper.RunSync(() => _notificationDistributer.DistributeAsync(args.NotificationId));
        }
    }
}
