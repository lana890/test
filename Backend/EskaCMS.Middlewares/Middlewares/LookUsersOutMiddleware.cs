using EskaCMS.Filters.Helpers;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using EskaCMS.Middlewares.ViewModels;

using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace EskaCMS.Middlewares.Middlewares
{
    public class LookUsersOutMiddleware
    {

        private readonly RequestDelegate Next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMemoryCache _memoryCache;
        private static readonly TimeSpan SessionCheckCacheDuration = TimeSpan.FromSeconds(10);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionCheckLocks = new();

        public LookUsersOutMiddleware(RequestDelegate requestDelegate,
                                  IServiceScopeFactory serviceScopeFactory,
                                  IMemoryCache memoryCache)
        {
            this.Next = requestDelegate;
            _serviceScopeFactory = serviceScopeFactory;
            _memoryCache = memoryCache;
        }
        public async Task Invoke(HttpContext context)
        {
            var apiUrl = context.Request.Path.Value ?? string.Empty;
            if (IsAnonymousRequest(context))
            {
                await this.Next(context);
                return;
            }

            using(var scope =  _serviceScopeFactory.CreateScope())
            {
                var loggedInUsersService = scope.ServiceProvider.GetRequiredService<ILoggedInUserService>();
                var workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                var userId = await workContext.GetCurrentUserId();

                if (userId <= 0)
                {
                    await this.Next(context);
                    return;
                }

                var siteId = await workContext.GetCurrentSiteIdAsync();
                var clientIp = workContext.GetIPAddress();

                if (await IsCurrentUserLockedAsync(loggedInUsersService, userId, siteId))// && !apiUrl.EndsWith("/login")
                {
                    context.Response.StatusCode = apiUrl.Contains("token/refresh", StringComparison.OrdinalIgnoreCase)  ? 500 :401;
                    await context.Response.WriteAsync("You are looked , please try again later");
                    return;
                }
                if (await IsCurrentUserKilledAsync(loggedInUsersService, clientIp, userId))
                {
                    if (apiUrl.Contains("token/refresh", StringComparison.OrdinalIgnoreCase) || apiUrl.Contains("login", StringComparison.OrdinalIgnoreCase) )
                    {
                        await loggedInUsersService.RemoveloggedInUser(userId, clientIp);
                        _memoryCache.Remove(GetKilledCacheKey(clientIp, userId));
                        context.Response.StatusCode = 500;
                    }
                    else { 
                        context.Response.StatusCode = 401;

                    }

                    await context.Response.WriteAsync("You are Session End   , please try again later Or Log Out");
                    return;
                }


                await this.Next(context);
            }
        }

        private async Task<bool> IsCurrentUserLockedAsync(ILoggedInUserService loggedInUsersService, long userId, long siteId)
        {
            var cacheKey = $"look-users-out:locked:{siteId}:{userId}";
            return await GetCachedSessionFlagAsync(
                cacheKey,
                () => loggedInUsersService.CheckIfLookedOutUser(userId, siteId));
        }

        private async Task<bool> IsCurrentUserKilledAsync(ILoggedInUserService loggedInUsersService, string clientIp, long userId)
        {
            var cacheKey = GetKilledCacheKey(clientIp, userId);
            return await GetCachedSessionFlagAsync(
                cacheKey,
                () => loggedInUsersService.CheckUserIsKilled(clientIp, userId));
        }

        private async Task<bool> GetCachedSessionFlagAsync(string cacheKey, Func<Task<bool>> valueFactory)
        {
            if (_memoryCache.TryGetValue(cacheKey, out bool cachedValue))
            {
                return cachedValue;
            }

            var sessionCheckLock = SessionCheckLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await sessionCheckLock.WaitAsync();
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out cachedValue))
                {
                    return cachedValue;
                }

                cachedValue = await valueFactory();
                _memoryCache.Set(cacheKey, cachedValue, SessionCheckCacheDuration);
                return cachedValue;
            }
            finally
            {
                sessionCheckLock.Release();
                SessionCheckLocks.TryRemove(cacheKey, out _);
            }
        }

        private static string GetKilledCacheKey(string clientIp, long userId)
        {
            return $"look-users-out:killed:{clientIp}:{userId}";
        }
         
        private static bool IsAnonymousRequest(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            if (!StringValues.IsNullOrEmpty(context.Request.Headers["Authorization"]))
            {
                return false;
            }

            return !context.Request.Cookies.ContainsKey("UserToken")
                && !context.Request.Cookies.ContainsKey("GuestToken");
        }



    }
}
