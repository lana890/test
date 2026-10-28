using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Controllers
{
    [Area("Core")]
    [ApiController]
    public class UserClaimsApiController : Controller
    {

        // private readonly IUsersPasswordHistoryService _UsersPasswordHistoryService;
        private HttpContext _httpContext;

        public UserClaimsApiController(HttpContext httpContext/* , IUsersPasswordHistoryService usersPasswordHistoryService */)
        {

            _httpContext = httpContext;
            // _UsersPasswordHistoryService = usersPasswordHistoryService;
        }

        [HttpPost("/api/claims/AddUpdateUserClaims")]
        public async Task<IActionResult> AddUpdateUserClaims()
        {

            var contextUser = _httpContext.User;
            ((ClaimsIdentity)contextUser.Identity).AddClaim(new Claim("ProjectReader", "true"));
            return BadRequest();

        }



    }
}
