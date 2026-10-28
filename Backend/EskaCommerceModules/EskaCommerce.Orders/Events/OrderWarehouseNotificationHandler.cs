using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Events
{
    public class OrderWarehouseNotificationHandler : INotificationHandler<OrderWarehouseNotification>
    {

        private readonly ITestNotifier _testNotifier;
        private readonly IRepository<WarehouseRoles> _warehouseRolesRepository;
        private readonly IRepository<User> _userRepository;

        public OrderWarehouseNotificationHandler(ITestNotifier testNotifier,
                                                IRepository<WarehouseRoles> warehouseRolesRepository, IRepository<User> userRepository)
        {
            _warehouseRolesRepository = warehouseRolesRepository;
            _testNotifier = testNotifier;
            _userRepository = userRepository;
        }

        public async Task Handle(OrderWarehouseNotification notification, CancellationToken cancellationToken)
        {

            var userRolesShouldNotify = await _warehouseRolesRepository.Query()
                                                                       .Where(x => x.WarehouseId == notification.Order.WarehouseId)
                                                                       .Select(x => x.RoleId)
                                                                       .ToListAsync();
            if (userRolesShouldNotify.Any())
            {
                var userWithTargetRoles = await _userRepository.Query()
                                                         .Where(x => !string.IsNullOrEmpty(x.DeviceToken) &&
                                                                    x.UserSites.Any(x => x.SiteId == notification.Order.SiteId) &&
                                                                    x.UserRoles.Any(y => userRolesShouldNotify.Contains(y.RoleId)))
                                                         .ToListAsync();


                if (userWithTargetRoles.Any())
                {
                    var notificationObject = await PushNotificationHelper(userWithTargetRoles, notification);

                    await _testNotifier.SendMessageAsync(NotificationType.PushNotification, null, notificationObject, notification.Order.SiteId);
                }

            }




        }


        private async Task<NotificationObject> PushNotificationHelper(List<User> users, OrderWarehouseNotification notification)
        {

            NotificationObject notificationObjPush = new NotificationObject
            {
                SendVia = SendBy.Username,
                MessageBody = $"{notification.Body}",
                MessageTitle = $"{notification.Title}",
                SendValue = users.DistinctBy(x => x.DeviceToken).Select(x => x.DeviceToken).ToList(),
                NotificationType = NotificationType.PushNotification,
                UserId = users.Select(x => x.Id).ToArray(),
                PushNotificationOptions = new PushNotification
                {
                    Topics = new List<string>(),
                    DesitinationType = DesitinationType.Personal,

                }
            };

            return notificationObjPush;
        }



    }
}
