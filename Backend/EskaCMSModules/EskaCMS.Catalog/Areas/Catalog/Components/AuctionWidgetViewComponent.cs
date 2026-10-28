using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using EskaCommerce.Infrastructure.Data;
using EskaCommerce.Infrastructure.Web;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Module.Core.Areas.Core.ViewModels;
using EskaCommerce.Module.Catalog.Models;
using EskaCommerce.Module.Catalog.Services;
using EskaCommerce.Module.Core.Models;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using System.Collections.Generic;
using EskaCommerce.Module.Core.Extensions;
using Microsoft.Extensions.Configuration;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class AuctionWidgetViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _auctionRepository;
        private readonly IRepository<BiddingTracker> _biddingTrackerRepository;
        private readonly IBiddingTrackerService _biddingTrackerService;
        private readonly IMediaService _mediaService;
        private readonly IProductService _auctionService;
        private readonly IWorkContext _workContext;
        private readonly bool EnableApprovals;

        public AuctionWidgetViewComponent(IRepository<Product> auctionRepository, IMediaService mediaService
            , IProductService auctionService, IBiddingTrackerService biddingTrackerService
            , IRepository<BiddingTracker> biddingTrackerRepository, IWorkContext workContext, IConfiguration config)
        {
            _auctionRepository = auctionRepository;
            _mediaService = mediaService;
            _auctionService = auctionService;
            _biddingTrackerService = biddingTrackerService;
            _biddingTrackerRepository = biddingTrackerRepository;
            _workContext = workContext;
            EnableApprovals = config.GetValue<bool>("Administration.EnableApprovals");
        }

        public IViewComponentResult Invoke(WidgetInstanceViewModel widgetInstance)
        {
            var currentuser = _workContext.GetCurrentUser();

            var model = new ProductWidgetComponentVm
            {
                Id = widgetInstance.Id,
                WidgetName = widgetInstance.Name,
                Setting = JsonConvert.DeserializeObject<ItemWidgetSetting>(widgetInstance.Data)
            };

            var query = _auctionRepository.Query()
              .Where(x => x.IsPublished && x.Type == ProductTypes.Auction);

            if (EnableApprovals) query.Where(x => x.Status == RequestStatus.Approved);

            if (model.Setting.CategoryId.HasValue && model.Setting.CategoryId.Value > 0)
            {
                query = query.Where(x => x.Categories.Any(c => c.CategoryId == model.Setting.CategoryId.Value));
            }

            if (model.Setting.FeaturedOnly)
            {
                query = query.Where(x => x.IsFeatured);
            }

            #region "TestCode"
            //get the bidding tracker info from the service
            //var BiddingTrackerModel = _biddingTrackerService.GetAll()
            //    .Select(p => new BiddingTrackerVM
            //    {
            //        Id = p.Id,
            //        BiddingAmount = p.BiddingAmount,
            //        BuyerId = p.BuyerId,
            //        CreatedById = p.CreatedById,
            //        SellerId = p.SellerId,
            //        AuctionId = p.AuctionId,
            //        CreationDate = p.CreationDate
            //    })
            //    .ToList();

            //apply outer left join with product table
            //var NewQuery = query.Include(x => x.ThumbnailImage)
            //                    .GroupJoin(BiddingTrackerModel,
            //                          product => product.Id,
            //                          bidding => bidding.AuctionId,
            //                          (product, bidding) => new { product = product, biddingTracker = bidding })
            //                     .SelectMany(
            //                        xy => xy.biddingTracker.DefaultIfEmpty(),
            //                        (product, bidding) => new { product = product, biddingTracker = bidding }
            //                        )
            //                     .Select(s => new
            //                     {
            //                         Product = s.product,
            //                         BiddingTracker = s.biddingTracker
            //                     });

            //this for test
            //var New = (from q in query
            //           join b in BiddingTrackerModel on q.Id equals b.AuctionId into bt
            //           from bid in bt.DefaultIfEmpty()
            //           group q by bid into bidding
            //           select new { Product = bidding.ToList(), Bidding = bidding.Key }).ToList();
            #endregion

            var NewQuery = query
                             .Include(x => x.ThumbnailImage)
                             .OrderByDescending(x => x.CreatedOn)
                             .Select(p => new CustomBiddingTrackerVM
                             {
                                 Id = p.Id,
                                 Name = p.Name,
                                 Slug = p.Slug,
                                 Price = p.Price,
                                 OldPrice = p.OldPrice,
                                 SpecialPrice = p.SpecialPrice,
                                 IsCallForPricing = p.IsCallForPricing,
                                 IsAllowToOrder = p.IsAllowToOrder,
                                 StockQuantity = p.StockQuantity,
                                 SpecialPriceStart = p.SpecialPriceStart,
                                 SpecialPriceEnd = p.SpecialPriceEnd,
                                 ThumbnailImage = p.ThumbnailImage,
                                 ReviewsCount = p.ReviewsCount,
                                 RatingAverage = p.RatingAverage,
                                 Status = p.Status,
                                 CurrentBid = _biddingTrackerRepository.Query().Where(f => f.AuctionId == p.Id && f.CreatedById == currentuser.Result.Id).Sum(s =>s.BiddingAmount),
                                 SellerBid = _biddingTrackerRepository.Query().Where(f => f.AuctionId == p.Id && f.SellerId == currentuser.Result.Id).Sum(x => x.BiddingAmount),
                                 BuyerBid = _biddingTrackerRepository.Query().Where(f => f.AuctionId == p.Id && f.BuyerId == currentuser.Result.Id).Sum(x => x.BiddingAmount),
                                 EndDate = p.SpecialPriceEnd
                             });

            model.Products = NewQuery
              .Take(model.Setting.NumberOfItems)
              .Select(x => ProductThumbnail.FromProduct(x)).ToList();

            foreach (var auction in model.Products)
            {
                auction.ThumbnailUrl = _mediaService.GetThumbnailUrl(auction.ThumbnailImage);
                // product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(auction);
            }

            return View(this.GetViewPath(), model);
        }
    }
}
