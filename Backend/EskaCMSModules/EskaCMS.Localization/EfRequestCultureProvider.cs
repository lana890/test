using System;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Localization
{
    public class EfRequestCultureProvider : RequestCultureProvider
    {
        private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IWorkContext _workContext;
      


        public override async Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            var workContext = httpContext.RequestServices.GetRequiredService<IWorkContext>();           
            //var user = await workContext.GetCurrentUser();
            var culture = await workContext.GetCurrentUserCultureCode();

            if (culture == 0)
            {
                return await Task.FromResult((ProviderCultureResult)null);
            }

            var providerResultCulture = new ProviderCultureResult("en-US");

            return await Task.FromResult(providerResultCulture);
        }
    }
}