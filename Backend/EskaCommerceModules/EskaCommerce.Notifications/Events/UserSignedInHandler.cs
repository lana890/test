using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EskaCMS.Core.Events;
using EskaCommerce.Notifications.Notifiers;

namespace EskaCommerce.Notifications.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly ITestNotifier _testNotifier;

        public UserSignedInHandler(ITestNotifier testNotifier)
        {
            _testNotifier = testNotifier;
        }

        /// <inheritdoc />
        public async Task Handle(UserSignedIn notification, CancellationToken cancellationToken)
        {
            //Edit: Demo for how to use notification service in core module.
            // await _testNotifier.WelcomeToTheApplicationAsync(notification.UserId);
            //return Task.FromResult(0);
        }
    }
}
