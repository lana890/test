using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Notifications
{
    /// <summary>
    /// Used to store a user notification.
    /// </summary>
    [Serializable]
    public class UserNotification : EntityBaseWithTypedId<Guid>
    {
        /// <summary>
        /// User Id.
        /// </summary>
        public  long UserId { get; set; }
        public virtual User User { get; set; }


        /// <summary>
        /// NotificationDetail Id.
        /// </summary>
        public virtual long NotificationDetailId { get; set; }

        public NotificationDetail NotificationDetail { get; set; }

        /// <summary>
        /// Current state of the user notification.
        /// </summary>
        public virtual UserNotificationState State { get; set; }

        public virtual DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Soft delete tag
        /// </summary>
        public virtual bool IsDeleted { get; set; }

        public UserNotification()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserNotification"/> class.
        /// </summary>
        /// <param name="create"></param>
        public UserNotification(Guid id)
        {
            Id = id;
            State = UserNotificationState.Unread;
        }

    }
}
