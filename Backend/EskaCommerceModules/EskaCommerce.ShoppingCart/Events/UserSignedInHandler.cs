using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCommerce.ShoppingCart.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCommerce.ShoppingCart.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly IWorkContext _workContext;
        private readonly ICartService _cartService;

        public UserSignedInHandler(IWorkContext workContext, ICartService cartService)
        {
            _workContext = workContext;
            _cartService = cartService;
        }

        public async Task Handle(UserSignedIn user, CancellationToken cancellationToken)
        {
            
            var guestUserId = await _workContext.GetCurrentUserId();
            if (guestUserId != null)     
            await _cartService.MigrateCart(guestUserId, user.UserId,user.SiteId);
        }
    }

}
