using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Cache
{
    public interface IClientCacheService 
    {
        Task<bool> GetCacheStatusBySlug(CachingCheckerViewModel model);
        Task RemoveAllCacheFile();

        /// <summary>
        /// Purges the SSR (Angular/iisnode) in-memory caches by POSTing to each
        /// configured front-end /cache/flush endpoint. Best-effort: failures are
        /// swallowed so they never break the primary cache clear.
        /// </summary>
        Task FlushSsrCache();
        Task RemoveCacheFile(List<SlugCacheVM>  slugCaches, long cultureId);

        Task RemoveCacheFile(string Slug, long siteId, long cultureId);
    }
}
