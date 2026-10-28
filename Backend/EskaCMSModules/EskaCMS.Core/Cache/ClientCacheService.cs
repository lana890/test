using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Cache
{
    public class ClientCacheService : IClientCacheService
    {
        // Shared HttpClient (avoids socket exhaustion); SSR flush is occasional + best-effort.
        private static readonly System.Net.Http.HttpClient ssrHttpClient =
            new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly IConfiguration configuration;
        private readonly ISiteHostNameServices siteHostNameServices;
        private readonly ISlugServices slugServices;
        private readonly IRepository<Page> pageRepository;
        private readonly IWorkContext workContext;
 

        public ClientCacheService(IConfiguration configuration,
                                  ISiteHostNameServices siteHostNameServices ,
                                  ISlugServices slugServices,
                                  IRepository<Page> pageRepository

)
        {
            this.configuration = configuration;
            this.siteHostNameServices = siteHostNameServices;
            this.slugServices = slugServices;
            this.pageRepository = pageRepository;
            this.workContext = workContext;
        }

        public async Task RemoveAllCacheFile()
        {
            try
            {
                var Baseurl = configuration.GetValue<string>("CacheConfig:BaseUrl");

                if (System.IO.Directory.Exists(Baseurl))
                {
                    string[] subDirectories = System.IO.Directory.GetDirectories(Baseurl);

                    foreach (string subDirectory in subDirectories)
                    {
                        System.IO.Directory.Delete(subDirectory, true);
                    }

                }
                await RemoveIndexCacheFile();
               
                
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task FlushSsrCache()
        {
            try
            {
                var urls = configuration.GetSection("CacheConfig:SsrFlushUrls").Get<string[]>();
                var secret = configuration.GetValue<string>("CacheConfig:SsrFlushSecret");

                // Not configured -> nothing to do (endpoint is also disabled on the SSR side).
                if (urls == null || urls.Length == 0 || string.IsNullOrEmpty(secret))
                    return;

                foreach (var baseUrl in urls)
                {
                    if (string.IsNullOrWhiteSpace(baseUrl)) continue;

                    try
                    {
                        var endpoint = baseUrl.TrimEnd('/') + "/cache/flush";
                        using (var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, endpoint))
                        {
                            request.Headers.Add("x-cache-flush-secret", secret);
                            request.Content = new System.Net.Http.StringContent("{}", Encoding.UTF8, "application/json");
                            await ssrHttpClient.SendAsync(request);
                        }
                    }
                    catch
                    {
                        // Best-effort: one SSR node being unreachable must not fail ClearCache.
                    }
                }
            }
            catch
            {
                // Never let SSR flush break the primary cache clear.
            }
        }

        private async Task RemoveIndexCacheFile()
        {
            try
            {
                var Fileurl = configuration.GetValue<string>("CacheConfig:IndexUrl");


                if (System.IO.File.Exists(Fileurl))
                {
                    System.IO.File.Delete(Fileurl);
                }
            }
            catch (Exception ex) 
            {

                throw;
            }
        }

        public async Task RemoveCacheFile(List<SlugCacheVM> SlugCache, long SiteId)
        {
            try
            {
                var Baseurl = configuration.GetValue<string>("CacheConfig:BaseUrl");
                foreach (var Data in SlugCache)
                {
                    string Prefix = await siteHostNameServices.GetSiteHostNamePrefix(SiteId, Data.CultureId);

                    var fullPath = Baseurl + (string.IsNullOrEmpty(Prefix) ? Data.Slug : Prefix + "/" + Data.Slug);

                    if (System.IO.Directory.Exists(fullPath))
                    {
                        System.IO.Directory.Delete(fullPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }


        }

        public async Task RemoveCacheFile(string Slug, long siteId, long cultureId)
        {
            try
            {
                var Baseurl = configuration.GetValue<string>("CacheConfig:BaseUrl");
               
                    string Prefix = await siteHostNameServices.GetSiteHostNamePrefix(siteId, cultureId);

                    var fullPath = Baseurl + (string.IsNullOrEmpty(Prefix) ? Slug : Prefix + "/" + Slug);

                    if (System.IO.Directory.Exists(fullPath))
                    {
                        System.IO.Directory.Delete(fullPath, true);
                    }
                
            }
            catch (Exception ex)
            {
                return;
            }


        }

        public async Task<bool> GetCacheStatusBySlug(CachingCheckerViewModel model)
        {
            try
            {
                var SlugInfo = await slugServices.GetSlugDataBySlug(model.Slug);
               
                if (SlugInfo!=null && SlugInfo.Entity == EEntity.Page)
                {
                    var IsCacheable = await pageRepository.Query()
                                                         .Where(x => x.SlugInfoId == SlugInfo.Id)
                                                         .AsNoTracking()
                                                         .Select(x => x.IsCacheable)
                                                         .FirstOrDefaultAsync();
                    return IsCacheable;

                }

                return true;

            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
