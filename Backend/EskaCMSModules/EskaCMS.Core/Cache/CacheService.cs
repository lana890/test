using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace EskaCMS.Core.Cache
{
    public class CacheService:ICacheService
    {
        ObjectCache _memoryCache = MemoryCache.Default;
        // Thread-safe key registry. This service is a Singleton hit concurrently by every
        // request; the previous plain List<string> was mutated without synchronization,
        // which under load could throw or silently drop keys (causing missed RemoveByPrefix
        // invalidations). ConcurrentDictionary preserves the exact same set of tracked keys
        // and invalidation semantics, only made concurrency-safe.
        private readonly ConcurrentDictionary<string, byte> _cachedKeys = new ConcurrentDictionary<string, byte>();

        public T GetData<T>(string key)
        {
            try
            {
                key = key.ToLower();
                T item = (T)_memoryCache.Get(key);
                return item;
            }
            catch (Exception e)
            {
                throw;
            }
        }
        public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
        {
            bool res = true;
            try
            {
                if (!string.IsNullOrEmpty(key) && value is not null)
                {
                    key= key.ToLower();
                    _cachedKeys.TryAdd(key, 0);
                    _memoryCache.Set(key, value, expirationTime);
                }
            }
            catch (Exception e)
            {
                throw;
            }
            return res;
        }
        public object RemoveData(string key)
        {
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    key = key.ToLower();
                    _cachedKeys.TryRemove(key, out _);
                    return _memoryCache.Remove(key);
                }
            }
            catch (Exception e)
            {
                throw;
            }
            return false;
        }


        public void RemoveByPrefix(List<string> Prefixes)
        {
            try
            {
                foreach (string prefix in Prefixes)
                {
                   var Prefix = prefix.ToLower();
                    List<string> removedItems = _cachedKeys.Keys.Where(x => x.ToLower().StartsWith(Prefix)).ToList();
                    foreach (string key in removedItems)
                    {
                        this.RemoveData(key);

                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
        public void RemoveByPrefix(string prefix)
        {
            try
            {
               
                    var Prefix = prefix.ToLower();
                    List<string> removedItems = _cachedKeys.Keys.Where(x => x.ToLower().StartsWith(Prefix)).ToList();
                    foreach (string key in removedItems)
                    {
                        this.RemoveData(key);

                    }
                
            }
            catch (Exception e)
            {
                throw;
            }
        }
        public async Task RemoveAll()
        {
            try
            {
                while(!_cachedKeys.IsEmpty)
                {
                    this.RemoveData(_cachedKeys.Keys.FirstOrDefault());

                }


            }
            catch (Exception e)
            {
                throw;
            }
        }

    }
}
