using System;
using System.Linq;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Search.Areas.Search.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Areas.Core.ViewModels;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;
using EskaCMS.Search.Services.Interfaces;
using EskaCMS.Media.Services.Interfaces;

namespace EskaCommerce.Module.Search.Areas.Search.Controllers
{
    [Area("Search")]
    [Route("api/Search")]
    [Authorize]
    public class SearchController : Controller
    {

        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }


        [HttpPost]
        [Route("full-search")]

        public async Task<IActionResult> FullSearch([FromBody] SearchFilterInputVM input)
        {
            try
            {
                return Ok(await _searchService.FullSearch(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Route("ElasticFullSearch")]

        public async Task<IActionResult> ElasticFullSearch([FromBody] ElasticSearchInputVM input)
        {
            try
            {
                return Ok(await _searchService.ElasticFullSearch(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpPost]
        [Route("SearchInProducts")]

        public async Task<IActionResult> SearchInProducts([FromBody] ProductElasticSearchInput input)
        {
            try
            {
                return Ok(await _searchService.ProductSearch(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


    }

}


