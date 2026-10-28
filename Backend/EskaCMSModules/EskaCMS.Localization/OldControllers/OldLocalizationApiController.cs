//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using EskaCMS.Entities.Entity;
//using Localization;
//using EskaCMS.Core.Extensions;
//using EskaCMS.Infrastructure.Data;
//using EskaCMS.Localization.ViewModel;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Localization;


//namespace SimplCommerce.Module.Localization.Areas.Localization.Controllers
//{
//    [Area("Localization")]
//    [Route("api/localizationOld")]
//    public class OldLocalizationApiController : Controller
//    {
//        private readonly IStringLocalizer _localizer;
//        private readonly IRepository<ResourceLocalization> _resourceRepository;
//        private readonly IRepository<SitesCultures> _sitescultureRepository;
//        private readonly IWorkContext _workContext;

//        public OldLocalizationApiController(IStringLocalizerFactory stringLocalizerFactory,
//            IRepository<ResourceLocalization> resourceRepository, IRepository<SitesCultures> sitescultureRepository, IWorkContext workContext)
//        {
//            _localizer = stringLocalizerFactory.Create(null);
//            _resourceRepository = resourceRepository;
//            _sitescultureRepository = sitescultureRepository;
//            _workContext = workContext;
//        }

//        [HttpGet("get-translation")]
//        public IActionResult GetTranslation()
//        {
//            var strings = _localizer.GetAllStrings().ToDictionary(x => x.Name, x => x.Value);
//            return Json(strings);
//        }

//        [HttpGet("get-cultures")]
//        public async Task<IActionResult> GetCultures()
//        {
//            var cultures = await _sitescultureRepository.Query().Where(x => x.SiteId == _workContext.GetCurrentSiteId()).ToListAsync();
//            return Ok(cultures);
//        }

//        [HttpGet("get-resources")]
//        public async Task<IActionResult> GetResources()
//        {
//           // var CultureString = _sitescultureRepository.Query().Where(x => x.Id == CultureId).Select(x => x.CultureId).FirstOrDefault();
//            var resources = await _resourceRepository.Query()
//                .Where(x => x.SiteCultureId == _workContext.GetCurrentCultureId())
//                .Select(x => new ResourceItemVm
//                {
//                    Key = x.Key,
//                    Value = x.Value,
//                    SiteCultureId = x.SiteCultureId,
//                    IsTranslated = true
//                })
//                .ToListAsync();

//            //if (CultureString != GlobalConfiguration.DefaultCulture)
//            //{
//                var standardResources = await _resourceRepository.Query()
//                .Where(x => x.SiteCultureId == _workContext.GetCurrentCultureId() /*GlobalConfiguration.DefaultCulture*/)
//                .ToListAsync();

//                foreach (var item in standardResources)
//                {
//                    if (resources.All(x => x.Key != item.Key))
//                    {
//                        resources.Add(new ResourceItemVm { Key = item.Key, SiteCultureId = _workContext.GetCurrentCultureId(), Value = item.Key, IsTranslated = false });
//                    }
//                }
//           // }

//            return Json(resources.OrderBy(x => x.IsTranslated).ThenBy(x => x.Key));
//        }

//        [HttpPost("update-resources")]
//       // [Authorize(Roles = "admin")]
//        public async Task<IActionResult> UpdateResource( [FromBody] IList<ResourceItemVm> model)
//        {
//            var resources = await _resourceRepository.Query().Where(x => x.SiteCultureId == _workContext.GetCurrentCultureId()).ToListAsync();

//            foreach (var resourceItemForm in model)
//            {
//                var resource = resources.FirstOrDefault(x => x.Key == resourceItemForm.Key);
//                if (resource != null)
//                {
//                    resource.Value = resourceItemForm.Value;
//                }
//                else if (resourceItemForm.Key != resourceItemForm.Value)
//                {
//                    _resourceRepository.Add(new ResourceLocalization
//                    {
//                        SiteCultureId = _workContext.GetCurrentCultureId(),
//                        Key = resourceItemForm.Key,
//                        Value = resourceItemForm.Value
//                    });
//                }
//            }

//            _resourceRepository.SaveChanges();
//            return Accepted();
//        }
//    }
//}
