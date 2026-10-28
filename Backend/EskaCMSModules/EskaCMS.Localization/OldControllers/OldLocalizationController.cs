//using System.Reflection;
//using System.Threading.Tasks;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Core.Extensions;
//using EskaCMS.Infrastructure.Data;
//using EskaCMS.Localization.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Localization;

//namespace EskaCMS.Localization.Controllers
//{
//   // [ApiExplorerSettings(IgnoreApi = false)]
//    [Route("api/Weblocalization")]
//    public class OldLocalizationController : Controller
//    {
//        private readonly IRepositoryWithTypedId<User, long> _userRepository;
//        private readonly IWorkContext _workContext;
//        // private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
//        private readonly IStringLocalizer _sharedLocalizer;
//        public OldLocalizationController(IRepositoryWithTypedId<User,
//            long> userRepository,
//            IWorkContext workContext,
//            IStringLocalizerFactory factory)
//        {

//            var type = typeof(SharedResource);
//            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);

//            _userRepository = userRepository;
//            _workContext = workContext;
//            // _sharedLocalizer = sharedLocalizer;
//            _sharedLocalizer = factory.Create("SharedResource", assemblyName.Name);
//        }

//        [HttpPost("SetUserCulture")]
//        public async Task<IActionResult> SetUserCulture(long CultureId)
//        {
//            var currentUser = await _workContext.GetCurrentUser();

//            currentUser.DefaultCultureId = CultureId;
//            _userRepository.SaveChanges();

//            return Ok();
//        }

//        [HttpPost("SetUserCurrencyCulture")]
//        public async Task<IActionResult> SetUserCurrencyCulture(long CurrencyCultureId)
//        {
//            var currentUser = await _workContext.GetCurrentUser();

//            currentUser.CurrencyCultureId = CurrencyCultureId;
//            _userRepository.SaveChanges();

//            return Ok();
//        }

//        [HttpGet]
//        [Route("GetLocalizedString/{Name}")]
//        public async Task<IActionResult> GetLocalizedString(string Name)
//        {
//            var loclizedR = _sharedLocalizer[Name];
//            return  Ok(loclizedR);
//        }
//    }
//}
