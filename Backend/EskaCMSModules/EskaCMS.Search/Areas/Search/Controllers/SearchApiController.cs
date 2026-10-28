using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Infrastructure.Data;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Services.Interfaces;
using System.Threading.Tasks;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using System;

namespace EskaCommerce.Search.Areas.Search.Controllers
{
    [Area("Search")]
    //[Authorize(Roles = "admin")]
    [Route("api/search")]
    public class SearchApiController : Controller
    {
 
        private readonly ISearchService _searchService;

        public SearchApiController(/* IRepository<Query> queryRepository, */ ISearchService searchService)
        {
           
            _searchService = searchService;
        }

        [HttpPost("MostSearchKeywords")]
        public async Task<IActionResult> MostSearchKeywords([FromBody]SmartTableParam param)
        {
            var result =await _searchService.GetMostSearchKeywords(param);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("AddUserQuery")]
        public async Task<IActionResult> AddUserQuery([FromBody] AddUserQueryVM model)
        {
            try
            {
                var result = await _searchService.AddUserQuery(model);

                return Ok(result);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
           
        }
        [Authorize]
        [HttpPost("GetUserQueries")]
        public async Task<IActionResult> GetUserQueries([FromBody] SmartSearchTableParam<GetUserQueriesVM> model)
        {
            try
            {
                var result = await _searchService.GetUserQueries(model);

                return Ok(result);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
         
        }
        [Authorize]
        [HttpPost("DeleteUserQuery/{QueryId}")]
        public async Task<IActionResult> DeleteUserQuery([FromRoute] long QueryId)
        {
            try
            {
                var result = await _searchService.DeleteUserQueries(QueryId);

                return Ok(result);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }
    }
}
