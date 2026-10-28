using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;

using EskaCommerce.Module.Tax.Areas.Tax.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Tax;

namespace EskaCMS.Tax.Areas.Tax.Controllers
{
    [Area("Tax")]

    [Route("api/tax-classes")]
    public class TaxClassApiController : Controller
    {
        private readonly IRepository<TaxClass> _taxClassRepository;
        private readonly int _defaultTaxClassId;

        public TaxClassApiController(IRepository<TaxClass> taxClassRepository, IConfiguration config)
        {
            _taxClassRepository = taxClassRepository;
            _defaultTaxClassId = config.GetValue<int>("Tax.DefaultTaxClassId");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var taxClasses = await _taxClassRepository
                .Query().Where(x => x.SiteId == SiteId)
                .Select(x => new { Id = x.Id, Name = x.Name })
                .ToListAsync();
            return Json(taxClasses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var taxClass = await _taxClassRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (taxClass == null)
            {
                return NotFound();
            }

            var model = new TaxClassForm
            {
                Id = taxClass.Id,
                Name = taxClass.Name,
            };

            return Json(model);
        }

        [HttpGet("default")]
        public async Task<IActionResult> Default()
        {
            var defaultTaxClass = await _taxClassRepository.Query()
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .FirstOrDefaultAsync(x => x.Id == _defaultTaxClassId);

            return Ok(defaultTaxClass);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TaxClassForm model)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            if (ModelState.IsValid)
            {
                var tagClass = new TaxClass
                {
                    Name = model.Name,
                    SiteId = SiteId
                };

                _taxClassRepository.Add(tagClass);
                await _taxClassRepository.SaveChangesAsync();

                return CreatedAtAction(nameof(Get), new { id = tagClass.Id }, null);
            }
            return BadRequest(ModelState);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] TaxClassForm model)
        {
            if (ModelState.IsValid)
            {
                var taxClass = await _taxClassRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
                if (taxClass == null)
                {
                    return NotFound();
                }

                taxClass.Name = model.Name;
                await _taxClassRepository.SaveChangesAsync();
                return Accepted();
            }

            return BadRequest(ModelState);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var taxClass = await _taxClassRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (taxClass == null)
            {
                return NotFound();
            }

            try
            {
                _taxClassRepository.Remove(taxClass);
                await _taxClassRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"The tax class {taxClass.Name} can't not be deleted because it is referenced by other tables" });
            }

            return NoContent();
        }
    }
}
