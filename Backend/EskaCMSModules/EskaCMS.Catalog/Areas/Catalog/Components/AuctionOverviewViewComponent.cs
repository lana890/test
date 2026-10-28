
using EskaCMS.Catalog.Models;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Module.Catalog.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class AuctionOverviewViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IWorkContext _workContext;
        public AuctionOverviewViewComponent(IRepository<Product> productRepository, IWorkContext workContext)
        {
            _productRepository = productRepository;
            _workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            var user =  _workContext.GetCurrentUser();
            var productAuctions = _productRepository.Query()
                .Where(f => f.CreatedById == user.Result.Id && f.Type == ProductTypes.Auction)
                .ToList();
            var AuctionOverview = new ProductOverviewVM
            {
                Auctions = Convert.ToInt32(productAuctions.Count.ToString()),
                Bids = 10,
                TotalBids = 2500,
                TotalPurchase = 6000
            };

            return View(this.GetViewPath(), AuctionOverview);
        }
    }
}
