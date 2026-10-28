using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EskaCMS.BlogArchive.Models;
using EskaCMS.Categories.Services.Interfaces;
using EskaCMS.Core;
using EskaCMS.Core.Data;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Entities.BusinessModels;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EskaCMS.APIs.Controllers
{//Categories

    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IPostCategory _categoriesRepository;
        HttpRequestMessage request = new HttpRequestMessage();
        public CategoriesController(IPostCategory categoriesRepository)
        {
            _categoriesRepository = categoriesRepository;
           
        }
        [HttpPost]
        [Route("api/Categories/SaveCategory")]
        public async Task<IActionResult> SaveCategory(PostCategoryViewModel category)
        {
            category.CreatedById = Convert.ToInt64( Request.Headers["userId"]);
            category.SiteId =Convert.ToInt64( Request.Headers["SiteId"]);
            return Ok(await _categoriesRepository.SaveCategory(category));
        }

        [HttpGet]
        //[Authorize(Policy = Permission.Admin)]
        [Route("api/Categories/GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
           try
            {
                return Ok(await _categoriesRepository.GetAllCategories());
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
    }
}