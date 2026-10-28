using EskaCommerce.Infrastructure.Data;
using EskaCommerce.Infrastructure.Web;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Module.Catalog.Models;
using EskaCommerce.Module.Core.Extensions;
using EskaCommerce.Module.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class ProductOverviewViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IWorkContext _workContext;
        public ProductOverviewViewComponent(IRepository<Product> productRepository, IWorkContext workContext)
        {
            _productRepository = productRepository;
            _workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            var user = _workContext.GetCurrentUser();
            var products = _productRepository.Query()
                .Where(f => f.CreatedById == user.Result.Id && f.Type == ProductTypes.Product)
                .ToList();
            var ProductOverview = new ProductOverviewVM
            {
                Products = Convert.ToInt32(products.Count.ToString()),
                IsPublished = products.Where(f => f.IsPublished == true).Count()
            };
            return View(this.GetViewPath(), ProductOverview);
        }
    }
}
