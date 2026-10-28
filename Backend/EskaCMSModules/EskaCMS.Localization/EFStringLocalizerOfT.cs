using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Localization;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Localization
{
    public class EfStringLocalizer<T> : IStringLocalizer<T>
    {
        private readonly IServiceProvider _serviceProvider;
        private IMemoryCache _resourcesCache;
        private readonly long SiteCultureId;
        public EfStringLocalizer(IServiceProvider serviceProvider, IMemoryCache resourcesCache)
        {
            _serviceProvider = serviceProvider;
            _resourcesCache = resourcesCache;
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

        private string GetString(string name)
        {
           // var culture = CultureInfo.CurrentUICulture.Name;
            var resources = LoadResources(SiteCultureId);
            var value = resources.SingleOrDefault(r => r.Key == name)?.Value;

            return value;
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
                _resourcesCache.Set(SiteCultureId, resources);
            }

            return resources;
        }
    }
}