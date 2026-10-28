using System.Threading;
using System.Threading.Tasks;
using MediatR;

using EskaCommerce.ProductComparison.Services;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using System;

namespace EskaCommerce.ProductComparison.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly IWorkContext _workContext;
        private readonly IComparingProductService _comparingService;

        public UserSignedInHandler(IWorkContext workContext, IComparingProductService comparingService)
        {
            _workContext = workContext;
            _comparingService = comparingService;
        }

        public async Task Handle(UserSignedIn user, CancellationToken cancellationToken)
        {
            //Added by rami to make project work 
           
            var guestUserId = await _workContext.GetCurrentUserId();
            _comparingService.MigrateComparingProduct(guestUserId, user.UserId);
        }
    }
}
