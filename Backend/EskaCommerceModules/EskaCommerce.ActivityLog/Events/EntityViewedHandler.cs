using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

using EskaCommerce.Module.ActivityLog.Models;
using EskaCMS.Core.Events;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;

namespace EskaCommerce.Module.ActivityLog.Events
{
    public class EntityViewedHandler : INotificationHandler<EntityViewed>
    {
        private readonly IRepository<EskaCMS.Entities.Entity.ActivityLog.Activity> _activityRepository;
        private readonly IWorkContext _workContext;
        private const long EntityViewedActivityTypeId = 1;

        public EntityViewedHandler(IRepository<EskaCMS.Entities.Entity.ActivityLog.Activity> activityRepository, IWorkContext workcontext)
        {
            _activityRepository = activityRepository;
            _workContext = workcontext;
        }

        public async Task Handle(EntityViewed notification, CancellationToken cancellationToken)
        {
            var userId = await _workContext.GetCurrentUserId();
            var activity = new EskaCMS.Entities.Entity.ActivityLog.Activity
            {
                ActivityTypeId = EntityViewedActivityTypeId,
                EntityId = notification.EntityId,
                EntityTypeId = notification.EntityTypeId,
                UserId = userId,
                CreatedOn = DateTimeOffset.Now
            };

            _activityRepository.Add(activity);
        }
    }
}
