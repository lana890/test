using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity;
namespace EskaCommerce.Notifications.Models.NotificationModels
{
    /// <summary>
    /// Represents a published notification for a user.
    /// </summary>
    [Serializable]
    public class NotificationDetailDto
    {
        /// <summary>
        /// Id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Unique notification name.
        /// </summary>
        public string NotificationName { get; set; }

        /// <summary>
        /// Notification data.
        /// </summary>
        public NotificationData Data { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// Name of the entity type (including namespaces).
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// Entity id.
        /// </summary>
        public object EntityId { get; set; }

        /// <summary>
        /// Severity.
        /// </summary>
        public NotificationSeverity Severity { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    }
    public class GetNotifications
    {
        public bool UnRead { get; set; }
        public string content { get; set; }
        public long NotificationId { get; set; }
        public long UserId { get; set; }
        public string EncryptedUserId { get; set; }
        public string FullName { get;  set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
    public class GetNotficationResult
    {

        public List<GetNotifications> Notifications { get; set; } = new List<GetNotifications>();
        public int UnReadNotifcations { get; set; }
        public int TotalNotification { get; set; }

    }
}
