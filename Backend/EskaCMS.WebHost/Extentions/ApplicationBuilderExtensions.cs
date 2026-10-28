using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.IO;
using Microsoft.Extensions.Hosting;
using EskaCMS.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Localization;
using EskaCMS.Middlewares.Middlewares;
using EskaCMS.RateLimit.Middleware;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.APIs.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseCustomizedIdentity(this IApplicationBuilder app)
        {

            app.UseAuthentication();
            app.UseMiddleware<SecurityValidationMiddleware>();
            app.UseAuthorization();


            app.Use(
                async (context, next) =>
                {
                    var hasBearerToken = context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorization)
                        && authorization.Any(value => value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

                    var hasCookies = context.Request.Cookies.Count > 0;

                    if (!hasBearerToken && !hasCookies)
                    {
                        await next();
                        return;
                    }

                    var principal = new ClaimsPrincipal();

                    if (hasCookies)
                    {
                        var cookiesAuthResult = await context.AuthenticateAsync("Identity.Application");
                        if (cookiesAuthResult?.Principal != null)
                        {
                            principal.AddIdentities(cookiesAuthResult.Principal.Identities);
                        }
                    }

                    if (hasBearerToken)
                    {
                        var bearerAuthResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                        if (bearerAuthResult?.Principal != null)
                        {
                            principal.AddIdentities(bearerAuthResult.Principal.Identities);
                        }
                    }

                    if (principal.Identities.Any())
                    {
                        context.User = principal;
                    }

               //     context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                    //context.Response.Headers.Add("Content-Security-Policy", "default-src 'self';");
                 //   context.Response.Headers.Add("Content-Security-Policy", "object-src 'self'; script-src 'self'; default-src 'self';  style-src 'self' 'sha256-4Su6mBWzEIFnH4pAGMOuaeBrstwJN4Z3pq/s1Kn4/KQ=' ;");

                    await next();
                });

            return app;
        }

        //public static IApplicationBuilder UseHangFiredashboard(this IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IServiceProvider serviceProvider, IWebHostEnvironment env)
        //{
        //    app.UseHangfireDashboard();
        //    backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));
        //    using (var scope = app.ApplicationServices.CreateScope())
        //    {
        //        var mappers = serviceProvider.GetService<IHangFireJobsService>();
        //        mappers.RegisterUserBookingExpirationJob();              
        //    }
        //    return app;
        //}FFF

        public static IApplicationBuilder UseCustomizedClientRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientRateLimitMiddleware>();
        }
        public static IApplicationBuilder UseCustomizedIPRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpRateLimitMiddleware>();
        }
        public static IApplicationBuilder UseCustomizedStaticFiles(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            NoCache = true,
                            NoStore = true,
                            MaxAge = TimeSpan.FromDays(-1)
                        };
                    }
                });
            }
            else
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromDays(60)
                        };
                    }
                });
            }

            return app;
        }
        public static IApplicationBuilder UseCustomizedRequestLocalization(this IApplicationBuilder app)
        {
            string defaultCultureUI = GlobalConfiguration.DefaultCulture;
            string Cultures = string.Empty;

            try
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var cultureRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<Culture, string>>();
                    //  GlobalConfiguration.Cultures = cultureRepository.Query().ToList();
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    defaultCultureUI = config.GetValue<string>("Global.DefaultCultureUI");


                    var supportedCultures = cultureRepository.Query().Select(c => c.Id).ToArray();
                    app.UseRequestLocalization(options =>
                    options
                        .AddSupportedCultures(supportedCultures)
                        .AddSupportedUICultures(supportedCultures)
                        .SetDefaultCulture("en-US")
                    //.RequestCultureProviders.Insert(0, new EfRequestCultureProvider())
                    );
                }
            }
            catch
            {
                app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures("en-US")
                    .AddSupportedUICultures("en-US")
                    .SetDefaultCulture("en-US")
                );
            }
            return app;
        }

    }
}
