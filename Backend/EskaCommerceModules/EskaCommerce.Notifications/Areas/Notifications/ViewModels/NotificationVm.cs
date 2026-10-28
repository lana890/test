using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Notifications.Models;

namespace EskaCommerce.Notifications.Areas.Notifications.ViewModels
{
    public class NotificationVm
    {
        public string Severity { get; set; }

        public string Message { get; set; }

        public long[] UserId { get; set; }

        public List<SelectListItem> OnlineUsers { get; set; }

        public List<SelectListItem> Severities { get; set; }

        public NotificationVm()
        {
            Severities = EnumHelper.ToDictionary(typeof(EskaCMS.Entities.Entity.NotificationSeverity)).Select(s => new SelectListItem(s.Key.ToString(), s.Value)).ToList();
        }
    }
}
