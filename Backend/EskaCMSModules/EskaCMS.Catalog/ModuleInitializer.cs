using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
//using EskaCommerce.Module.Catalog.Events;
using EskaCommerce.Catalog.Data;
using EskaCommerce.Catalog.Services.CatalogService;
using EskaCommerce.Catalog.Services.CatalogService.BrandServices;
using EskaCommerce.Catalog.Services.CatalogService.BrandServices.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea.TemplateInterfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea.Interfaces;
using EskaCommerce.UserCatalog.Services;
using EskaCommerce.UserCatalog.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCommerce.Catalog
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        { 
            // Confirmed unused: IProductTemplateProductAttributeRepository has no consumer anywhere in the
            // solution (only its interface declaration, its implementation, and this registration).
            // services.AddTransient<IProductTemplateProductAttributeRepository, ProductTemplateProductAttributeRepository>();
         //   services.AddTransient<ItemTemplateProductAttributeRepository, ItemTemplateProductAttributeRepository>();
          //  services.AddTransient<INotificationHandler<ReviewSummaryChanged>, ReviewSummaryChangedHandler>();
            services.AddTransient<IBrandService, BrandService>();
            services.AddTransient<ICategoryService, CategoryService>();
            services.AddTransient<IProductPricingService, ProductPricingService>();
            services.AddTransient<IProductServices, ProductServices>();
            services.AddTransient<IProductSerialService, ProductSerialService>();
            services.AddTransient<IBiddingTrackerService, BiddingTrackerService>();
            services.AddTransient<IProductFeesService, ProductFeesService>();
            services.AddTransient<IProductWebServices, ProductWebServices>();
            services.AddTransient<IVariationServices, VariationServices>();
            services.AddTransient<IProductAttributeGroup, ProductAttributeGroupServices>();
            services.AddTransient<IProductApiAttribute, ProductAttributApiServices>();
            //services.AddTransient<IProductAttributeGroup, ProductAttributeGroupServices>();
            services.AddTransient<IProductTemplateServices, ProductTemplateServices>();
            services.AddTransient<IProductOptionsServices, ProductOptionsServices>();
            services.AddTransient<IProductLinkServices, ProductLinksServices>();
            services.AddTransient<ICategoryWebServices, CategoryWebServices>();
            services.AddTransient<IUserWishList, UserWishListServices>();
            services.AddTransient<IProductBranchServices, ProductBranchServices>();
            services.AddTransient<IPlanService, PlanService>();
            services.AddTransient<IUserBookingService, UserBookingService>();
            services.AddTransient<IUserBookingApiService, UserBookingApiService>();
            services.AddTransient<IPlanProductVersionService, PlanProductVersionService>();
            services.AddTransient<IPlansVersionsService, PlansVersionsService>();
            services.AddTransient<IUserPlansService, UserPlansService>();
            services.AddTransient<IUserPlansApiService, UserPlansApiService>();
            services.AddTransient<IProviderTimeSlotsService, ProviderTimeSlotsService>();
            services.AddTransient<ISubscriptionServices, SubscriptionServices>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
