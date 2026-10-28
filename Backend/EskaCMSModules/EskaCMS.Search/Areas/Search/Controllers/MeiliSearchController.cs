using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCMS.Search.Model;
using EskaCMS.Search.Services.Interfaces;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Search.Areas.Search.Controllers
{
    [Area("MeiliSearch")]
    //[Authorize(Roles = "admin")]
    [Route("api/MeiliSearch")]
    [Authorize]

    public class MeilSearchController : ControllerBase
    {

        private readonly IMeiliSearchService _searchService;

        public MeilSearchController(IMeiliSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost]
        [Route("full-search")]

        public async Task<IActionResult> FullSearch([FromBody] MeiliSearchInputVM input)
        {
            try
            {
                return Ok(await _searchService.MeiliFullSearch(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
