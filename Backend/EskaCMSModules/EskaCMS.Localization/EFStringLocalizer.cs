using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Localization
{
    public class EfStringLocalizer : IStringLocalizer
    {
        private IMemoryCache _resourcesCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
        private readonly IServiceProvider _serviceProvider;
        //private readonly IWorkContext _WorkContext;
        private readonly long SiteCultureId;
        public EfStringLocalizer(IServiceProvider serviceProvider, IMemoryCache resourcesCache)
        {
            _serviceProvider = serviceProvider;
            _resourcesCache = resourcesCache;
            // _WorkContext = WorkContext;
            using (var scope = _serviceProvider.CreateScope())
            {
                var _WorkContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                SiteCultureId = _WorkContext.GetCurrentCultureId();

            }
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = GetString(name);
                return new LocalizedString(name, value ?? name, value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = GetString(name);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, format == null);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            //var culture = CultureInfo.CurrentUICulture.Name;
            var resources = LoadResources(SiteCultureId);

            return resources.Select(r => new LocalizedString(r.Key, r.Value, true));
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            return new EfStringLocalizer(_serviceProvider, _resourcesCache);
        }

        public string GetString(string name)
        {

            //var culture = CultureInfo.CurrentUICulture.Name;
                var resources = LoadResources(SiteCultureId);
                var value = resources.SingleOrDefault(r => r.Key == name)?.Value;

                if (value == null)
                {
                    AutoRegisterNewString(name, SiteCultureId);
                }

                return value;
            
        }

        private void AutoRegisterNewString(string name, long SiteCultureId)
        {
            //if (culture != "en-US") //GlobalConfiguration.DefaultCulture
            //{
            using (var scope = _serviceProvider.CreateScope())
            {
                var resourceRepository = scope.ServiceProvider.GetRequiredService<IRepository<ResourceLocalization>>();

                var res = new ResourceLocalization()
                {
                    SiteCultureId = SiteCultureId,
                    Key = name,
                    Value = name
                };

                resourceRepository.Add(res);
                resourceRepository.SaveChanges();

                var freshResourcesCache = LoadResources(SiteCultureId);
                freshResourcesCache.Add(res);
                _resourcesCache.Set(SiteCultureId, freshResourcesCache, _cacheEntryOptions);
            }
            //}
        }

        private IList<ResourceLocalization> LoadResources(long SiteCultureId)
        {
            if (!_resourcesCache.TryGetValue(SiteCultureId, out IList<ResourceLocalization> resources))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var resourceRepository = scope.ServiceProvider.GetRequiredService<IRepository<ResourceLocalization>>();
                    resources = resourceRepository.Query().Where(r => r.SiteCultureId == SiteCultureId).ToList();
                }

                _resourcesCache.Set(SiteCultureId, resources, _cacheEntryOptions);
            }

            return resources;
        }
    }
}
