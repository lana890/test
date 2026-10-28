using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Area("Core")]
    //[Authorize(Roles = "admin")]
    [Route("api/appsettings")]
    public class AppSettingApiController : Controller
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IConfigurationRoot _configurationRoot;

        public AppSettingApiController(IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, IConfiguration configuration)
        {
            _appSettingRepository = appSettingRepository;
            _configurationRoot = (IConfigurationRoot)configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await _appSettingRepository.Query().AsNoTracking().Where(x => x.IsVisibleInCommonSettingPage).ToListAsync();
            return Json(settings);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] IList<SiteSettings> model)
        {
            if (ModelState.IsValid)
            {
                var settings = await _appSettingRepository.Query().Where(x => x.IsVisibleInCommonSettingPage).ToListAsync();
                foreach (var item in settings)
                {
                    var vm = model.FirstOrDefault(x => x.Id == item.Id);
                    if (vm != null)
                    {
                        item.Value = vm.Value;
                    }
                }

                await _appSettingRepository.SaveChangesAsync();
                _configurationRoot.Reload();

                return Accepted();
            }

            return BadRequest(ModelState);
        }
    }
}
