using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Shipments.Services.ShippingPricesService.Interfaces;
using EskaCommerce.Shippments.ShippingPrices.Services;

namespace EskaCommerce.ShippingPrices.Services
{
    public class ShippingPriceService : IShippingPriceService
    {
        private HttpContext _httpContext;
        private readonly IRepositoryWithTypedId<ShippingProvider, string> _shippingProviderRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IRepository<ProductAttributeValue> _productAttributeRepository;

        public ShippingPriceService(IHttpContextAccessor contextAccessor, IRepositoryWithTypedId<ShippingProvider, string> shippingProviderRepository,
            ICurrencyService currencyService, IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IRepository<ProductAttributeValue> productAttributeRepository)
        {
            _httpContext = contextAccessor.HttpContext;
            _shippingProviderRepository = shippingProviderRepository;
            _currencyService = currencyService;
            _appSettingRepository = appSettingRepository;
            _productAttributeRepository = productAttributeRepository;
        }

        public async Task<IList<ShippingPrice>> GetApplicableShippingPrices(GetShippingPriceRequest request)
        {
            var applicableShippingPrices = new List<ShippingPrice>();
            var providers = await _shippingProviderRepository.Query().ToListAsync();
            var shippingRateServices = _httpContext.RequestServices.GetServices<IShippingPriceServiceProvider>();

            foreach (var provider in providers)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                if (!provider.ToAllShippingEnabledCountries)
                {
                    if (!provider.OnlyCountryIds.Contains(request.ShippingAddress.CountryId))
                    {
                        continue;
                    }
                }

                if (!provider.ToAllShippingEnabledStatesOrProvinces)
                {
                    if (request.ShippingAddress.StateOrProvinceId != null)
                    {
                        if (!provider.OnlyStateOrProvinceIds.Contains((long)request.ShippingAddress.StateOrProvinceId))
                        {
                            continue;
                        }
                    }
                }
                var priceServiceType = Type.GetType(provider.ShippingPriceServiceTypeName);
                var priceService = shippingRateServices.Where(x => x.GetType() == priceServiceType).FirstOrDefault();
                var response = await priceService.GetShippingPrices(request, provider);
                applicableShippingPrices.AddRange(response.ApplicablePrices);
            }

            return applicableShippingPrices;
        }
        public async Task<IList<ShippingPrice>> GetApplicableShippingPrices(GetShippingPriceRequest request, Cart cart)
        {
            var applicableShippingPrices = new List<ShippingPrice>();
            var providers = await _shippingProviderRepository.Query().ToListAsync();
            var shippingRateServices = _httpContext.RequestServices.GetServices<IShippingPriceServiceProvider>();

            foreach (var provider in providers)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                if (!provider.ToAllShippingEnabledCountries)
                {
                    if (!provider.OnlyCountryIds.Contains(request.ShippingAddress.CountryId))
                    {
                        continue;
                    }
                }

                if (!provider.ToAllShippingEnabledStatesOrProvinces)
                {
                    if (request.ShippingAddress.StateOrProvinceId != null)
                    {
                        if (!provider.OnlyStateOrProvinceIds.Contains((long)request.ShippingAddress.StateOrProvinceId))
                        {
                            continue;
                        }
                    }
                }

                var priceServiceType = Type.GetType(provider.ShippingPriceServiceTypeName);
                var priceService = shippingRateServices.Where(x => x.GetType() == priceServiceType).FirstOrDefault();
                var response = await priceService.GetShippingPrices(request, provider, cart);

                applicableShippingPrices.AddRange(response.ApplicablePrices);

            }

            return applicableShippingPrices;
        }

    }

}
