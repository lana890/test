using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EskaCommerce.Infrastructure.Data;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Module.Core.Models;
using EskaCommerce.Module.Core.Services;
using EskaCMS.Infrastructure.Data;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Authorize(Roles = "admin")]
    [Route("api/Eskae-product-widgets")]
    public class EskaeProductWidgetApiController : Controller
    {
        private readonly IRepository<WidgetInstance> _widgetInstanceRepository;

        public EskaeProductWidgetApiController(IRepository<WidgetInstance> widgetInstanceRepository/* , IMediaService mediaService */)
        {
            _widgetInstanceRepository = widgetInstanceRepository;
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var widgetInstance = _widgetInstanceRepository.Query().FirstOrDefault(x => x.Id == id);
            var model = new EskaeProductWidgetForm
            {
                Id = widgetInstance.Id,
                Name = widgetInstance.Name,
                WidgetZoneId = widgetInstance.WidgetZoneId,
                PublishStart = widgetInstance.PublishStart,
                PublishEnd = widgetInstance.PublishEnd,
                DisplayOrder = widgetInstance.DisplayOrder,
                Setting = JsonConvert.DeserializeObject<EskaeProductWidgetSetting>(widgetInstance.Data)
            };

            if (model.Setting == null)
            {
                model.Setting = new EskaeProductWidgetSetting();
            }

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] EskaeProductWidgetForm model)
        {
            if (ModelState.IsValid)
            {
                var widgetInstance = new WidgetInstance
                {
                    Name = model.Name,
                    WidgetId = "EskaeProductWidget",
                    WidgetZoneId = model.WidgetZoneId,
                    PublishStart = model.PublishStart,
                    PublishEnd = model.PublishEnd,
                    DisplayOrder = model.DisplayOrder,
                    Data = JsonConvert.SerializeObject(model.Setting)
                };

                _widgetInstanceRepository.Add(widgetInstance);
                await _widgetInstanceRepository.SaveChangesAsync();
                return CreatedAtAction(nameof(Get), new { id = widgetInstance.Id }, null);
            }

            return BadRequest(ModelState);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] EskaeProductWidgetForm model)
        {
            if (ModelState.IsValid)
            {
                var widgetInstance = _widgetInstanceRepository.Query().FirstOrDefault(x => x.Id == id);
                widgetInstance.Name = model.Name;
                widgetInstance.WidgetZoneId = model.WidgetZoneId;
                widgetInstance.PublishStart = model.PublishStart;
                widgetInstance.PublishEnd = model.PublishEnd;
                widgetInstance.DisplayOrder = model.DisplayOrder;
                widgetInstance.Data = JsonConvert.SerializeObject(model.Setting);

                await _widgetInstanceRepository.SaveChangesAsync();
                return Accepted();
            }

            return BadRequest(ModelState);
        }
    }
}
