using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;


using EskaCMS.Tax.Areas.Tax.ViewModels;

using EskaCMS.Infrastructure.Helpers;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using System.Net;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Entities.Entity.Address;

namespace EskaCMS.Tax.Areas.Tax.Controllers
{
    [Area("Tax")]
    //[Authorize(Roles = "admin")]
    [Route("api/tax-rates")]
    public class TaxRateApiController : Controller
    {
        private readonly IRepository<TaxRate> _taxRateRepository;
        private readonly IRepository<StateOrProvinceDetails> _stateOrProvinceRepository;

        public TaxRateApiController(IRepository<TaxRate> taxRateRepository, IRepository<StateOrProvinceDetails> stateOrProvinceRepository)
        {
            _taxRateRepository = taxRateRepository;
            _stateOrProvinceRepository = stateOrProvinceRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var taxRates = await _taxRateRepository
                .Query()
                .Include(x=>x.StateOrProvince)
                .ThenInclude(x=>x.StateOrProvinceDetails)
                 .Include(x => x.Country)
                .ThenInclude(x => x.CountryDetails)
                .Include(x=>x.TaxClass)
                .Select(x => new
                {
                    x.Id,
                    TagClassName = x.TaxClass.Name,
                    CountryName = x.Country.CountryDetails.Select(x=>x.Name).FirstOrDefault(),
                    StateOrProvinceName = x.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),

                    x.ZipCode,
                    x.Rate
                })
                .ToListAsync();
            return Json(taxRates);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var taxRates = await _taxRateRepository
                .Query()
                 .Include(x => x.StateOrProvince)
                .ThenInclude(x => x.StateOrProvinceDetails)
      
                .Select(x => new TaxRateImport
                {
                    TaxClassId = x.TaxClassId,
                    CountryId = x.CountryId,
                    StateOrProvinceName = x.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                    ZipCode = x.ZipCode,
                    Rate = x.Rate
                })
                .ToListAsync();

            var csvString = CsvConverter.ExportCsv(taxRates);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "tax-rates.csv");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var taxRate = await _taxRateRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (taxRate == null)
            {
                return NotFound();
            }

            var model = new TaxRateForm
            {
                Id = taxRate.Id,
                TaxClassId = taxRate.TaxClassId,
                CountryId = taxRate.CountryId,
                StateOrProvince = taxRate.StateOrProvinceId,
                ZipCode = taxRate.ZipCode,
                Rate = taxRate.Rate
            };

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TaxRateForm model)
        {
            if (ModelState.IsValid)
            {
                var taxRate = await _taxRateRepository.Query().Include(x=>x.TaxClass).FirstOrDefaultAsync(x => x.TaxClassId == model.TaxClassId);
                if (_taxRateRepository.Query().Any(x =>  x.TaxClassId == model.TaxClassId && x.CountryId == model.CountryId && x.StateOrProvinceId == model.StateOrProvince))
                    return BadRequest("This Tax Rate Already Exist");

                var tagRate = new TaxRate
                {
                    TaxClassId = model.TaxClassId,
                    CountryId = model.CountryId,
                    StateOrProvinceId = model.StateOrProvince,
                    ZipCode = model.ZipCode,
                    Rate = model.Rate
                };

                _taxRateRepository.Add(tagRate);
                await _taxRateRepository.SaveChangesAsync();

                return CreatedAtAction(nameof(Get), new { id = tagRate.Id }, null);

            }
            return BadRequest(ModelState);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] TaxRateForm model)
        {
            if (ModelState.IsValid)
            {
                var taxRate = await _taxRateRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
                
                var taxclass = await _taxRateRepository.Query().Include(x => x.TaxClass).FirstOrDefaultAsync(x => x.TaxClassId == model.TaxClassId);

                if (_taxRateRepository.Query().Any(x => x.Id != id && x.TaxClassId == model.TaxClassId && x.CountryId == model.CountryId && x.StateOrProvinceId == model.StateOrProvince))
                    return BadRequest("This Tax Rate Already Exist");
                if (taxRate == null)
                {
                    return NotFound();
                }

                taxRate.TaxClassId = model.TaxClassId;
                taxRate.CountryId = model.CountryId;
                taxRate.StateOrProvinceId = model.StateOrProvince;
                taxRate.ZipCode = model.ZipCode;
                taxRate.Rate = model.Rate;
                _taxRateRepository.Update(taxRate);

                await _taxRateRepository.SaveChangesAsync();
                return Accepted();
            }

            return BadRequest(ModelState);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var taxRate = await _taxRateRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (taxRate == null)
            {
                return NotFound();
            }

            try
            {
                _taxRateRepository.Remove(taxRate);
                await _taxRateRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"The tax rate {taxRate.Id} can't not be deleted because it is referenced by other tables" });
            }

            return NoContent();
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(TaxRateImportForm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var inputStream = model.CsvFile.OpenReadStream();
            var records = CsvConverter.ReadCsvStream<TaxRateImport>(model.CsvFile.OpenReadStream(), model.IncludeHeader, model.CsvDelimiter);

            foreach (var record in records)
            {
                var stateOrProvince = _stateOrProvinceRepository
                                                              .Query()
                                                              .Where(x => x.Name == record.StateOrProvinceName).FirstOrDefault();
                var taxRate = new TaxRate
                {
                    TaxClassId = record.TaxClassId,
                    CountryId = record.CountryId,
                    StateOrProvinceId = stateOrProvince?.StateOrProvinceId,
                    ZipCode = record.ZipCode,
                    Rate = record.Rate
                };
                _taxRateRepository.Add(taxRate);
            }

            await _taxRateRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
