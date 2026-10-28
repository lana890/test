using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using EskaCommerce.Infrastructure.Data;
using EskaCommerce.Infrastructure.Web;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Module.Catalog.Models;
using EskaCommerce.Module.Catalog.Services;
using EskaCommerce.Module.Core.Areas.Core.ViewModels;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Module.Core.Models;
using Microsoft.Extensions.Configuration;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class ProductWidgetViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductPricingService _productPricingService;
        private readonly bool EnableApprovals;
        private readonly bool IsPriceVisible;
        private readonly bool IsRatingVisible;

        public ProductWidgetViewComponent(IRepository<Product> productRepository, IMediaService mediaService, IProductPricingService productPricingService, IConfiguration config)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _productPricingService = productPricingService;
            EnableApprovals = config.GetValue<bool>("Administration.EnableApprovals");
            IsPriceVisible = config.GetValue<bool>("Catalog.IsPriceVisible");
            IsRatingVisible = config.GetValue<bool>("Catalog.IsRatingVisible");
        }

        public IViewComponentResult Invoke(WidgetInstanceViewModel widgetInstance)
        {
            var model = new ProductWidgetComponentVm
            {
                Id = widgetInstance.Id,
                WidgetName = widgetInstance.Name,
                Setting = JsonConvert.DeserializeObject<ItemWidgetSetting>(widgetInstance.Data)
            };

            var query = _productRepository.Query()
              .Where(x => x.IsPublished && x.IsVisibleIndividually && x.Type==ProductTypes.Product);

            if (EnableApprovals) query.Where(x => x.Status == RequestStatus.Approved);

            if (model.Setting.CategoryId.HasValue && model.Setting.CategoryId.Value > 0)
            {
                query = query.Where(x => x.Categories.Any(c => c.CategoryId == model.Setting.CategoryId.Value));
            }

            if (model.Setting.FeaturedOnly)
            {
                query = query.Where(x => x.IsFeatured);
            }

            model.Products = query
              .Include(x => x.ThumbnailImage)
              .OrderByDescending(x => x.CreatedOn)
              .Take(model.Setting.NumberOfItems)
              .Select(x => ProductThumbnail.FromProduct(x)).ToList();

          
            foreach (var product in model.Products)
            {
                product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
                product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
                product.IsPriceVisible = IsPriceVisible;
                product.IsRatingVisible = IsRatingVisible;
            }

            return View(this.GetViewPath(), model);
        }
    }
}
