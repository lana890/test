using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class CurrencyService : ICurrencyService 
    {
        private const string CurrencyDecimalPlaceSettingId = "Global.CurrencyDecimalPlace";
        private const string CurrencyCultureSettingId = "Global.CurrencyCulture";
        private const string RoundDigitSettingId = "RoundDigit";
		private readonly ISiteCultureService _siteCultureService;
 
		private readonly IConfiguration _config;
        private readonly IRepository<CurrencyRates> _currencyRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<User> _userrepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IRepository<SitesCultures> _cultureSites;
        public string DecimalPlace;
        public int RoundDigit;
        private bool _roundSettingsLoaded;   // ensures the rounding settings are read from the DB once per request

        public CurrencyService(ISiteCultureService siteCultureService,IRepository<CurrencyRates> currencyRepository, IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, IRepository<User> userrepository, IRepository<SitesCultures> cultureSites, IWorkContext workContext)
        {
            _appSettingRepository = appSettingRepository;
            _currencyRepository = currencyRepository;
            _userrepository = userrepository;
            _cultureSites = cultureSites;
            _WorkContext = workContext;
            _siteCultureService = siteCultureService;

		}


        public string GetDefaultDecimalPlaces(long siteId)
        {
            var decimalPlace = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyDecimalPlaceSettingId && x.SiteId == siteId).FirstOrDefault()?.Value;
            return decimalPlace;
        }
        public string GetDefaultDecimalPlaces()
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var decimalPlace = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyDecimalPlaceSettingId && x.SiteId == siteId).FirstOrDefault()?.Value;
            return decimalPlace;
        }

        public string GetDefaultRoundDigit()
        {
            var RoundDigit = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == RoundDigitSettingId).FirstOrDefault()?.Value;
            return RoundDigit;
        }
        public async Task<CurrencyRates> GetCurrentUserCurrency(long SiteId = 0)
        {
            try
            {
                var UserId = await _WorkContext.GetCurrentUserId();
                SiteId = SiteId == 0 ? _WorkContext.GetCurrentSiteId() : SiteId;

                var currentUserCurrency = await _userrepository.Query()
                    .AsNoTracking()
                    .Where(x => x.Id == UserId)
                    .SelectMany(x => x.UserSites
                        .Where(u => u.SiteId == SiteId)
                        .Select(u => u.Currency))
                    .FirstOrDefaultAsync();

                return currentUserCurrency;
            }
            catch (Exception E)
            {
                return null;
            }

        }

        public async Task<CurrencyRates> GetDefaultCurrency()
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var DefaultCurrency = await _currencyRepository.Query().AsNoTracking().Where(c => c.IsDefaultRate && c.SiteId ==siteId).FirstOrDefaultAsync();
            return DefaultCurrency;
        }

        private CurrencyRates GetCurrentUserCurrencySync()
        {
            var userId = _WorkContext.GetCurrentUserIdValue();
            var siteId = _WorkContext.GetCurrentSiteId();

            return _userrepository.Query()
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .SelectMany(x => x.UserSites
                    .Where(u => u.SiteId == siteId)
                    .Select(u => u.Currency))
                .FirstOrDefault();
        }

        private CurrencyRates GetUserCurrencyRateOrDefaultSync()
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var result = GetCurrentUserCurrencySync();

            return result ?? _currencyRepository.Query()
                .AsNoTracking()
                .Where(x => (x.IsDefaultRate && x.SiteId == siteId && !x.IsDeleted))
                .FirstOrDefault();
        }

        private CurrencyRates DefaultSiteCurrencyRateSync()
        {
            var siteId = _WorkContext.GetCurrentSiteId();

            return _currencyRepository.Query()
                .AsNoTracking()
                .Where(x => x.IsDefaultRate && x.SiteId == siteId && !x.IsDeleted)
                .FirstOrDefault();
        }


        public SiteSettings GetDefaultCurrencyCulture()
        {
            var CurrencyCulture = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyCultureSettingId && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();
            return CurrencyCulture;
        }



        public string FormatCurrency(decimal value, string decimalPlace, User CurrentUser, string OrderCurrency, SiteSettings CurrencyCulture)
        {
            if (CurrentUser.UserSites.Any(x => x.CurrencyId != null)) //Check the user culture
            {
                long SiteId = _WorkContext.GetCurrentSiteId();
                CurrencyRates UserCurrency = CurrentUser.UserSites.FirstOrDefault(u => u.SiteId == SiteId)?.Currency;

                if (UserCurrency == null)
                    throw new Exception("Could not find User Currency");

                value = value * UserCurrency.Rate;

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + " " + OrderCurrency;

            }
            else
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCulture.Value.ToString());

                var CurrencyCode = new RegionInfo(newCurrencyCulture.Name).ISOCurrencySymbol;
                var FormattedValue = value.ToString("n" + decimalPlace);
                return FormattedValue + " " + CurrencyCode;

            }
        }

        // use this when you want to render price of any thing Based On User Or Default Currency
        public string FormatCurrency(decimal value)
        {
            long siteId = _WorkContext.GetCurrentSiteId();

            var decimalPlace = GetDefaultDecimalPlaces(siteId);


            //Check the user culture
            var CurrentUserCurrency = GetCurrentUserCurrencySync();

            if (CurrentUserCurrency != null)
            {
                long CultureId = _WorkContext.GetCurrentCultureId();

                string CultureCode = _cultureSites.Query().Where(x => x.Id == CultureId).Select(x => x.CultureId).FirstOrDefault();

                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);

                value = value * CurrentUserCurrency.Rate;

                var CurrencyCode = CurrencyCulture(CurrentUserCurrency.CultureCode);

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + " " + CurrencyCode;

            }
            else
            {
                var CurrencyCultures = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyCultureSettingId && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();



                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCultures.Value.ToString());

                var CurrencyCode = CurrencyCulture(newCurrencyCulture.Name);

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + " " + CurrencyCode;

            }
        }


        public string FormatCurrencyForAdmin(decimal value)
        {
            long siteId = _WorkContext.GetCurrentSiteId();

            var decimalPlace = GetDefaultDecimalPlaces(siteId);


            //Check the user culture
            var CurrentUserCurrency = GetCurrentUserCurrencySync();

            if (CurrentUserCurrency != null)
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);

                value = value * CurrentUserCurrency.Rate;


                var CurrencyCode = new RegionInfo(CurrentUserCurrency.CultureCode).ISOCurrencySymbol;

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + " " + CurrencyCode;

            }
            else
            {
                var CurrencyCulture = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyCultureSettingId && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();

                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCulture.Value.ToString());

                var CurrencyCode = new RegionInfo(newCurrencyCulture.Name).ISOCurrencySymbol;

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + " " + CurrencyCode;

            }
        }


        // use this when you have the currency code
        // and the ammount is already converted to the Currency
        // ( just for REPRESENTATIONAL Perposes )
        public string FormatCurrency(decimal value, string CurrencyCode)
        {
            long siteId = _WorkContext.GetCurrentSiteId();
            var decimalPlace = GetDefaultDecimalPlaces(siteId);
            var FormattedValue = value.ToString("n" + decimalPlace);
            return FormattedValue + " " + CurrencyCode;

        }

        public string ConvertAndFormateToCurrency(decimal value,CurrencyRates TargetCurrency,string decimalPlace)
        {
            value *= TargetCurrency.Rate;
            var FormattedValue = value.ToString("n" + decimalPlace);
            return FormattedValue + " " + GetCurrencyCode( TargetCurrency.CultureCode);

        }
        public decimal ConvertToCurrency(decimal value, CurrencyRates TargetCurrency, string decimalPlace)
        {
            value *= TargetCurrency.Rate;
            var FormattedValue = value.ToString("n" + decimalPlace);
            return decimal.Parse(FormattedValue);

        }
        public  string GetUserCurrencyCode()
        {
            //long siteId = _WorkContext.GetCurrentSiteId();

            var CurrentUserCurrency = GetCurrentUserCurrencySync();

            if (CurrentUserCurrency != null)
            {
                return CurrencyCulture(CurrentUserCurrency.CultureCode);
            }
            else
            {
                return GetDefaultCurrencyCode();
            }
        }

        public string GetUserCurrencyCodeForAdmin()
        {
            //long siteId = _WorkContext.GetCurrentSiteId();

            var CurrentUserCurrency = GetCurrentUserCurrencySync();

            if (CurrentUserCurrency != null)
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);

                var CurrencyCode = new RegionInfo(CurrentUserCurrency.CultureCode).ISOCurrencySymbol;
                return CurrencyCode;


            }
            else
            {
                return GetDefaultCurrencyCodeForAdmin();
            }
        }



        public string GetDefaultCurrencyCodeForAdmin()
        {
            var DefaultCurrency = _currencyRepository.Query().AsNoTracking().Where(c => c.IsDefaultRate).FirstOrDefault();
            if (DefaultCurrency != null)
            {
                return DefaultCurrency.Name;
            }
            else
            {
                var CurrencyCulture = GetDefaultCurrencyCulture();

                var CurrencyCode = new RegionInfo(CurrencyCulture.Value.ToString()).ISOCurrencySymbol;
                return CurrencyCode;

            }
        }


        public string GetDefaultCurrencyCode()
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var DefaultCurrency = _currencyRepository.Query().AsNoTracking().Where(c => c.IsDefaultRate && c.SiteId ==siteId ).FirstOrDefault();
            if (DefaultCurrency != null)
            {
                return CurrencyCulture(DefaultCurrency.CultureCode);
            }
            else
            {
                var CurrencyCultures = GetDefaultCurrencyCulture();
                return CurrencyCulture(CurrencyCultures.Value.ToString());

            }
        }

        public decimal ConvertToDefault(decimal value)
        {
            try
            {
                var Currency = _currencyRepository.Query().AsNoTracking().Where(x => x.IsDefaultRate).FirstOrDefault();

                value = value * Currency.Rate;
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
            
        }


        private string CurrencyCulture(string CultureCode)
        {
            try
            {

                var CurrencyCode = new RegionInfo(CultureCode).ISOCurrencySymbol;

                //if (CultureCode.Contains("ar"))
                //{
                //    CurrencyCode = new RegionInfo(CultureCode).CurrencyNativeName.Replace("سعودي", "");
                //}

                return CurrencyCode;

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
        public string FormatCurrencyRound(decimal value)
        {
            //long siteId = _WorkContext.GetCurrentSiteId();

            //var decimalPlace = GetDefaultDecimalPlaces(siteId);

            //var RoundDigit = Convert.ToInt32(Convert.ToInt32(GetDefaultRoundDigit()));

            //RoundDigit = Convert.ToInt32(Math.Pow(10, RoundDigit));



            //Check the user culture
            var CurrentUserCurrency = GetCurrentUserCurrencySync();

            if (CurrentUserCurrency != null)
            {
                long CultureId = _WorkContext.GetCurrentCultureId();

                string CultureCode = _cultureSites.Query().Where(x => x.Id == CultureId).Select(x => x.CultureId).FirstOrDefault();

                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);

                value = value * CurrentUserCurrency.Rate;

                var FormattedValue = DcimalRound(value);

                var CurrencyCode = CurrencyCulture(CurrentUserCurrency.CultureCode);

                //var FormattedValue = Convert.ToDecimal(string.Format("{0:N" + decimalPlace + "}", Math.Round(value * RoundDigit, MidpointRounding.AwayFromZero) / RoundDigit));

                return FormattedValue + " " + CurrencyCode;

            }
            else
            {
                var CurrencyCultures = _appSettingRepository.Query().AsNoTracking().Where(x => x.Id == CurrencyCultureSettingId && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();

                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCultures.Value.ToString());

                var CurrencyCode = CurrencyCulture(newCurrencyCulture.Name);

                //var FormattedValue = Convert.ToDecimal(string.Format("{0:N" + decimalPlace + "}", Math.Round(value * RoundDigit, MidpointRounding.AwayFromZero) / RoundDigit));

                var FormattedValue = DcimalRound(value);

                return FormattedValue + " " + CurrencyCode;

            }
        }

        public decimal DcimalRound(decimal value)
        {
            EnsureRoundSettings();
            var RoundValue = Convert.ToDecimal(string.Format("{0:N" + DecimalPlace + "}", Math.Round(value * RoundDigit) / RoundDigit));
            return RoundValue;

        }

        public string FormatRound(string Currency, decimal value)
        {
            try
            {
                EnsureRoundSettings();
                var format = $"F{DecimalPlace}";

                return string.Format("{0:" + format + "}", Math.Round((value) * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Currency;
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public decimal FormatRoundForFile(decimal value)
        {
            EnsureRoundSettings();
            var RoundValue = Convert.ToDecimal((Math.Round(value * RoundDigit) / RoundDigit).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return RoundValue;

        }

        private void EnsureRoundSettings()
        {
            // Load the rounding settings from the DB exactly ONCE per request (CurrencyService is
            // scoped). Previously, when the decimal-place setting was null the '??=' re-issued the DB
            // query on every call -> once per product on a listing page (an N+1 confirmed by profiling).
            // Same values are used; only the redundant per-product queries are removed. No cache.
            if (_roundSettingsLoaded)
                return;

            DecimalPlace = GetDefaultDecimalPlaces();
            RoundDigit = Convert.ToInt32(Math.Pow(10, Convert.ToInt32(Convert.ToInt32(GetDefaultRoundDigit()))));
            _roundSettingsLoaded = true;
        }

        public string GetCurrencyCode(string CountryCode)
        {


            RegionInfo region = new RegionInfo(CountryCode);

            string currencyCode = region.ISOCurrencySymbol;

            return currencyCode;


        }

        public async Task<CurrencyRates> GetUserCurrencyRateOrDefault()
        {
            try
            {
                var siteId = _WorkContext.GetCurrentSiteId();

                var result = await  GetCurrentUserCurrency();

                if(result == null)
                {
                    result = await _currencyRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.IsDefaultRate && x.SiteId == siteId && !x.IsDeleted)
                        .FirstOrDefaultAsync();
                  
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex; 
            }

        }

        public async Task<CurrencyRates> GetCurrencyByCultureId(string CultureCode)
        {
            try
            {

                var siteId = _WorkContext.GetCurrentSiteId();

                var result = await _currencyRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.CultureCode == CultureCode && x.SiteId == siteId && !x.IsDeleted)
                    .FirstOrDefaultAsync();


                return result;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<CurrencyRates> GetCurrencyByName(string Name)
        {
            try
            {

                var siteId = _WorkContext.GetCurrentSiteId();

                var result = await _currencyRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.Name == Name && x.SiteId == siteId && !x.IsDeleted)
                    .FirstOrDefaultAsync();


                return result;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<CurrencyVM> GetDefaultUserCurrency() 
        {
            try
            {

                var result = await GetUserCurrencyRateOrDefault();

                var response = new CurrencyVM
                {
                    Id = result.Id,
                    CultureCode = result.CultureCode,
                    Name = result.Name

                };
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public string FormatByUserCurrency(decimal value)
        {



            var CurrentUserCurrency = GetUserCurrencyRateOrDefaultSync();

            value = value * CurrentUserCurrency.Rate;

            var CurrencyCode = GetCurrencyCulture(CurrentUserCurrency.CultureCode);

            var roundedValue = DcimalRound(value);

            return roundedValue + " " + CurrencyCode;



        }

        public decimal FormatByCurrentCurrency(decimal value)
        {

			var CultureId = _WorkContext.GetCurrentCultureId();

			var Culture = _siteCultureService.GetCultureById(CultureId);

			var CurrentUserCurrency = GetUserCurrencyRateOrDefaultSync();

            value = value * CurrentUserCurrency.Rate;

            var CurrencyCode = GetCurrencyCulture(CurrentUserCurrency.CultureCode);

            var roundedValue = DcimalRound(value);

            return roundedValue;



        }

        public async Task<CurrencyByCultureVM> GetUserCurrencyByCulture()
        {
            try
            {

                var result = await GetUserCurrencyRateOrDefault();
                CurrencyByCultureVM? response = null;
                if (result!=null)
                 response = new CurrencyByCultureVM()
                {
                    Code = GetCurrencyCulture(result?.CultureCode??null),
                    Rate = result?.Rate??0
                };

                return response;

               
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public  decimal RoundToDecimalPlaces(decimal value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places cannot be negative.");

            decimal multiplier = (decimal)Math.Pow(10, decimalPlaces);

            decimal roundedValue = Math.Round(value * multiplier) / multiplier;
            return roundedValue;
        }

        private string GetCurrencyCulture(string CultureCode)
        {
            try
            {

                var cultureId = _WorkContext.GetCurrentCultureId();

                if(cultureId == 0)
                {
                    return new RegionInfo(CultureCode).ISOCurrencySymbol; 
                }
                var siteId = _WorkContext.GetCurrentSiteId();

                var culture = _cultureSites.Query()
                                           .Where(x => x.Id == cultureId && x.SiteId == siteId)
                                           .AsNoTracking()
                                           .FirstOrDefault();

                
                var CurrencyCode = new RegionInfo(CultureCode).ISOCurrencySymbol;





                if (CultureCode.Contains("ar") && culture.CultureId.Contains("ar"))
                {
                    CurrencyCode = new RegionInfo(CultureCode).CurrencyNativeName;
                }
                return CurrencyCode;

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public async Task<CurrencyRates> DefaultSiteCurrencyRate()
        {
            try
            {


                var siteId = _WorkContext.GetCurrentSiteId();
                
                var result = await _currencyRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.IsDefaultRate && x.SiteId == siteId && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<string> FormatCurrencyWithText(decimal number)
        {

           
            var siteId = _WorkContext.GetCurrentSiteId();

            var userCurrency = await GetCurrentUserCurrency();


            if (userCurrency == null)
            {
                userCurrency = await _currencyRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.IsDefaultRate && x.SiteId == siteId && !x.IsDeleted)
                    .FirstOrDefaultAsync();

            }

            var currencyCode = GetCurrencyNativeName(userCurrency.CultureCode);


            var cultureId = _WorkContext.GetCurrentCultureId();


           

            string numberString = number.ToString();

            int decimalIndex = numberString.IndexOf('.');

            var fractionalPart = decimalIndex != -1 ? Convert.ToInt32(numberString.Substring(decimalIndex + 1)) : 0;


            var culture = _cultureSites.Query()
                                       .Where(x => x.Id == cultureId && x.SiteId == siteId)
                                       .Select(x=>x.CultureId)
                                       .FirstOrDefault();



            var and = culture.Contains("ar")  ? "و" : "and";
            var just = culture.Contains("ar") ? "فقط لا غير" : "just only";


            var ifHasDecimalDigits = fractionalPart > 0 ? $"{and} {fractionalPart.ToWords(CultureInfo.GetCultureInfo(culture.Split('-')[0]))}" : "";

            //// var test4 = test.Numbers.Select(x => new { number = x, words = $"{((int)Math.Floor(x)).ToWords(CultureInfo.GetCultureInfo(test.Cutlure))} فقط لا غير" }).ToList();
            var result = $"{((int)Math.Floor(number)).ToWords(CultureInfo.GetCultureInfo(culture.Split('-')[0]))} {currencyCode} {ifHasDecimalDigits} {just}";

            return result;


        }

        private string GetCurrencyNativeName(string CultureCode)
        {
            try
            {

                var cultureId = _WorkContext.GetCurrentCultureId();

                if (cultureId == 0)
                {
                    return new RegionInfo(CultureCode).ISOCurrencySymbol;
                }
                var siteId = _WorkContext.GetCurrentSiteId();

                var culture = _cultureSites.Query().AsNoTracking()
                                           .Where(x => x.Id == cultureId && x.SiteId == siteId)
                                           .FirstOrDefault();


                var CurrencyCode = new RegionInfo(CultureCode).CurrencyNativeName;





                if (CultureCode.Contains("ar") && culture.CultureId.Contains("ar"))
                {
                    CurrencyCode = new RegionInfo(CultureCode).CurrencyNativeName;
                }

                if (CultureCode.Contains("en") && culture.CultureId.Contains("ar"))
                {
                    CultureCode = CultureCode.Replace("en", "ar");

                    CurrencyCode = new RegionInfo(CultureCode).CurrencyNativeName;
                }


                return CurrencyCode;

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public string GetCurrency(string AllowedCurrencies, string ISOCurrency)
        {
            try
            {
                if (string.IsNullOrEmpty(AllowedCurrencies))
                {
                    throw new Exception("Please Add Supported Currencies");
                }
                var SupportedCurrencies = AllowedCurrencies.Split(',');

                if (SupportedCurrencies != null && SupportedCurrencies.Count() > 0)
                {
                    if (SupportedCurrencies.Any(y => ISOCurrency == y))
                    {
                        return ISOCurrency;
                    }
                    else
                    {
                var UserCurrency = DefaultSiteCurrencyRateSync();
                return UserCurrency.Name;
                    }

                }
                else
                {

                    var UserCurrency = DefaultSiteCurrencyRateSync();
                    return UserCurrency.Name;

                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }

}
