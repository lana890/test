using System;
using System.Collections.Generic;

namespace EskaCommerce.Notifications.Jobs
{
    /// <summary>
    /// Arguments for <see cref="NotificationDistributionJob"/>.
    /// </summary>
    [Serializable]
    public class NotificationDistributionJobArgs
    {
        /// <summary>
        /// Notification Id.
        /// </summary>
        public List<long> NotificationId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDistributionJobArgs"/> class.
        /// </summary>
        public NotificationDistributionJobArgs(List<long> notificationId)
        {
            NotificationId = notificationId;
        }
    }
}
