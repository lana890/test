using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using static BusinessModels.BusinessModel;
using EskaCMS.Core.Data;

using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Core.Cache;

namespace EskaCMS.Components.Controllers
{

    [ApiController]
    [Route("api/Cashing")]
    [Authorize]
    public class CashingController : ControllerBase
    {
      
        private readonly ICacheService _cacheService;
        // private readonly IWorkContext _workContext;
        private readonly IClientCacheService clientCacheService;

        public CashingController(ICacheService cacheService, 
                                 /* IWorkContext workContext, */IClientCacheService clientCacheService)
        {
            _cacheService = cacheService;
            // _workContext = workContext;
            this.clientCacheService = clientCacheService;
        }


        [HttpPost]
        [Route("ClearCache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                await _cacheService.RemoveAll();

                await clientCacheService.RemoveAllCacheFile();

                // Also purge the SSR (Angular/iisnode) in-memory caches so updated
                // site/hostname/slug settings reflect immediately instead of after TTL.
                await clientCacheService.FlushSsrCache();

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetCacheStatusBySlug")]
        public async Task<IActionResult> GetCacheStatusBySlug([FromBody] CachingCheckerViewModel model)
        {
            try
            {
              

                return Ok(await clientCacheService.GetCacheStatusBySlug(model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


    }
}