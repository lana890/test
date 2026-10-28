using EskaCMS.Security.Models.FriendshipModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interface
{
    public interface INotificationServices
    {
        Task<bool> SendMessage(NotificationDto notification);
    }
}
