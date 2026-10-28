using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Notifications
{
    /// <summary>
    /// A notification distributed to it's related user.
    /// </summary>
    public class NotificationDetail : EntityBase
    {
        /// <summary>
        /// Unique notification name.
        /// </summary>
        [MaxLength(NotificationScheme.MaxNotificationNameLength)]
        public virtual string NotificationName { get; set; }

        /// <summary>
        /// Notification data as JSON string.
        /// </summary>
        [MaxLength(NotificationScheme.MaxDataLength)]
        public virtual string Data { get; set; }

        /// <summary>
        /// Type of the JSON serialized <see cref="Data"/>.
        /// It's AssemblyQualifiedName of the type.
        /// </summary>
        [MaxLength(NotificationScheme.MaxDataTypeNameLength)]
        public virtual string DataTypeName { get; set; }

        /// <summary>
        /// Gets/sets entity type name, if this is an entity level notification.
        /// It's FullName of the entity type.
        /// </summary>
        [MaxLength(NotificationScheme.MaxEntityTypeNameLength)]
        public virtual string EntityTypeName { get; set; }

        /// <summary>
        /// AssemblyQualifiedName of the entity type.
        /// </summary>
        [MaxLength(NotificationScheme.MaxEntityTypeAssemblyQualifiedNameLength)]
        public virtual string EntityTypeAssemblyQualifiedName { get; set; }

        /// <summary>
        /// Gets/sets primary key of the entity, if this is an entity level notification.
        /// </summary>
        [MaxLength(NotificationScheme.MaxEntityIdLength)]
        public virtual string EntityId { get; set; }

        /// <summary>
        /// Notification severity.
        /// </summary>
        public virtual NotificationSeverity Severity { get; set; }

        public virtual DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Soft delete tag
        /// </summary>
        public virtual bool IsDeleted { get; set; }

        public IList<UserNotification> UserNotifications { get; set; }

        public NotificationDetail()
        {

        }

        public long CreatedById { get; set; }

        public virtual User CreatedBy { get; set; }

        public NotificationDetail(NotificationScheme notification)
        {
            NotificationName = notification.NotificationName;
            Data = notification.Data;
            DataTypeName = notification.DataTypeName;
            EntityTypeName = notification.EntityTypeName;
            EntityTypeAssemblyQualifiedName = notification.EntityTypeAssemblyQualifiedName;
            EntityId = notification.EntityId;
            Severity = notification.Severity;
        }

        public long NotificationSchemeId { get; set; }
        public NotificationScheme NotificationScheme { get; set; }
    }
}
