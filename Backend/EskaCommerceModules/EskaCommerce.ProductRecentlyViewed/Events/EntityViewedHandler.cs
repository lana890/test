using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using MediatR;
 

namespace EskaCommerce.ProductRecentlyViewed.Events
{
    public class EntityViewedHandler : INotificationHandler<EntityViewed>
    {
        private const string ProductEntityTypeId = "Product";
        private readonly IRepository<RecentlyViewedProduct> _recentlyViewedProductRepository;
        private readonly IWorkContext _workContext;

        public EntityViewedHandler(IRepository<RecentlyViewedProduct> recentlyViewedProductRepository, IWorkContext workcontext)
        {
            _recentlyViewedProductRepository = recentlyViewedProductRepository;
            _workContext = workcontext;
        }

        public async Task Handle(EntityViewed notification, CancellationToken cancellationToken)
        {
            if (notification.EntityTypeId == ProductEntityTypeId)
            {
               
                var userId = await _workContext.GetCurrentUserId();
                var recentlyViewedProduct = _recentlyViewedProductRepository.Query().FirstOrDefault(x =>
                    x.ProductId == notification.EntityId
                    && x.UserId == userId);

                if(recentlyViewedProduct == null)
                {
                    var referer = _workContext.GetTrafficSource();
                    SourceTraffic source = SourceTraffic.None;

                    
                    //referer = referer.ToLower();
                    //if (referer.Contains("google") || referer.Contains("bing"))
                    //    source = SourceTraffic.OrganicSearch;
                    //if (referer.Contains("facebook") || referer.Contains("instagram") || referer.Contains("twitter") || referer.Contains("linkedin"))
                    //    source = SourceTraffic.SocialMedia;
                    //if (referer.Contains("ads") || referer.Contains("utm_campaign") || referer.Contains("utm_source=ad"))

                    //    source = SourceTraffic.PaidSearch;
                    recentlyViewedProduct = new RecentlyViewedProduct
                    {
                        UserId = userId,
                        ProductId = notification.EntityId,
                        LatestViewedOn = DateTimeOffset.Now,
                        Source=source
                    };

                    _recentlyViewedProductRepository.Add(recentlyViewedProduct);
                }

                recentlyViewedProduct.LatestViewedOn = DateTimeOffset.Now;
                _recentlyViewedProductRepository.SaveChanges();
            }
        }
    }
}
