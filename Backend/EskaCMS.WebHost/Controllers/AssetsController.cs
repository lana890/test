using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace EskaCMS.WebHost.Controllers
{
    [Route("api/assets/[action]")]
    [ApiController]
    public class AssetsController : ControllerBase
    { 
        private readonly IWebHostEnvironment _env;
        public AssetsController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult GetAsset(string assetId)
        {
            var path = Path.Combine(_env.ContentRootPath, "UiBundles", assetId);
            return PhysicalFile(path, "application/javascript", assetId);
        } 
        [HttpGet]
        public IActionResult GetImage(string imageId)
        {
            var path = Path.Combine(_env.ContentRootPath, "Images", imageId);
            return PhysicalFile(path, "image/png", imageId);
        }
    }
}
