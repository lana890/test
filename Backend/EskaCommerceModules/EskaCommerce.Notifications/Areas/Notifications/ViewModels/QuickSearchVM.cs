using EskaCommerce.Notifications.Models;
using System;
using System.Collections.Generic;
using System.Text;

using EskaCMS.Entities.Entity;

namespace EskaCommerce.Notifications.Areas.Notifications.ViewModels
{
    public class QuickSearchVM
    {
        public CreatedByUserVM CreatedBy { get; set; }

        public string Data { get; set; }
        public NotificationSeverity Severity { get; set; }
        public UserNotificationState NotificationStatus { get; set; }

        public IEnumerable<string> Topics { get; set; }

        public long NotificationDetailsId { get; set; }

        public long NotificationSchemeId { get; set; }

        public string Type { get; set; }
        public string Email { get; set; }
		public bool IsDeleted { get; set; }
		public DateTimeOffset CreatedOn { get; set; }
        public string PhoneNumber { get; set; }

        public Guid Id { get; set; }
    }
}
