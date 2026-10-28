using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EskaCommerce.Infrastructure.Data;
using EskaCommerce.Infrastructure.Web;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Module.Catalog.Models;
using EskaCommerce.Module.Catalog.Services;
using EskaCommerce.Module.Core.Areas.Core.ViewModels;
using EskaCommerce.Module.Core.Services;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class EskaeProductWidgetViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductPricingService _productPricingService;

        public EskaeProductWidgetViewComponent(IRepository<Product> productRepository, IMediaService mediaService, IProductPricingService productPricingService)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _productPricingService = productPricingService;
        }

        public IViewComponentResult Invoke(WidgetInstanceViewModel widgetInstance)
        {
            var model = new EskaeProductWidgetComponentVm
            {
                Id = widgetInstance.Id,
                WidgetName = widgetInstance.Name,
                Setting = JsonConvert.DeserializeObject<EskaeProductWidgetSetting>(widgetInstance.Data)
            };

            foreach (var item in model.Setting.Products)
            {
                var product = _productRepository.Query().Where(x => x.Id == item.Id).FirstOrDefault();

                if (product != null)
                {
                    var productThumbnail = ProductThumbnail.FromProduct(product);
                    productThumbnail.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
                    productThumbnail.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
                    model.Products.Add(productThumbnail);
                }
            }

            return View(this.GetViewPath(), model);
        }
    }
}
