using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Threading.Tasks;
namespace EskaCMS.Core.Controllers
{

    [Route("api/currency")]
    [ApiController]
    [Authorize]
    public class CurrencyRatesApiController : ControllerBase
    {
        private readonly ICurrencyRatesApiService _CurrencyRatesService;
        private readonly ICurrencyService _currencyService;



        public CurrencyRatesApiController(ICurrencyRatesApiService CurrencyRatesService, ICurrencyService currencyService)
        {
            _CurrencyRatesService = CurrencyRatesService;
            _currencyService = currencyService;
        }

        [HttpGet("get-all-currencies-and-cultures")]
        public IEnumerable GetCurrencies()
        {
            try
            {
                var currencies = _CurrencyRatesService.GetAllCurrencies();
                return currencies;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpGet("get-all-currencies")]
        public IEnumerable GetCurrenciesByCulture()
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currencies = _CurrencyRatesService.GetAllCurrencies(SiteId);
                return currencies;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        [HttpGet("get-all-currencies-By-Culture-Admin")]
        public IEnumerable GetCurrenciesByCultureAdmin(string Culture)
        {
            try
            {
                var currencies = _CurrencyRatesService.GetAllCurrencies(Culture);
                return currencies;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        [HttpGet("get-used-cultures")]
        public async Task<IActionResult> GetUsedCultures()
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _CurrencyRatesService.GeUsedCultures());
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpPost("get-currencies-grid")]
        public IActionResult GetCurrencies([FromBody] SmartSearchTableParam<CurrencyRatesSearchListVM> param)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currencies = _CurrencyRatesService.GetCurrencies(param, SiteId);
            return Ok(currencies);
        }

        [HttpGet("getCurrencyById/{id}")]
        public async Task<IActionResult> GetCurrencyById(long Id)
        {
            return Ok(await _CurrencyRatesService.GetCurrencyById(Id));
        }
        [HttpDelete("delete-currency/{id}")]
        public async Task<IActionResult> DeleteCurrency(long Id)
        {
            return Ok(await _CurrencyRatesService.Delete(Id));
        }

        [HttpPost("add-currency")]
        public async Task<IActionResult> addCurrency([FromBody] CurrencyRatesVM Currency)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _CurrencyRatesService.Create(Currency, SiteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("edit-currency")]
        public async Task<IActionResult> editCurrency([FromBody] CurrencyRatesVM Currency)
        {
            try
            {
                return Ok(await _CurrencyRatesService.Edit(Currency));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        //[HttpGet("delete-currency/{id}")]
        //public async Task<IActionResult> deleteCurrency(long Id)
        //{
        //    var defaultCurrency = _config.GetValue<string>("Global.CurrencyCulture");
        //    if (ModelState.IsValid)
        //    {
        //        var currencyObj = _currencyRepository.Query().FirstOrDefault(x => x.Id == Id);
        //        var usersaCount = await _userRepository.Users.Where(x => x.Currency == currencyObj.Culture).CountAsync();
        //        if (defaultCurrency.Trim() == currencyObj.Culture.Trim())
        //        {
        //            return BadRequest("You can't delete the default currency");
        //        }
        //        if (usersaCount > 0)
        //        {
        //            return BadRequest("You can't delete this currency");
        //        }
        //        currencyObj.IsDeleted = true;
        //        await _currencyRepository.SaveChangesAsync();

        //        return Ok();
        //    }
        //    return BadRequest(ModelState);
        //}



        [HttpPost("FormatCurrencyWithText")]
        public async Task<IActionResult> FormatCurrencyWithText([FromBody] NumberToBeFormattedVM model )
        {
            try
            {
                var number = model.Decimal_Number.HasValue ? model.Decimal_Number.Value :  Convert.ToDecimal(model.String_Number);
                var result =await _currencyService.FormatCurrencyWithText(number);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}