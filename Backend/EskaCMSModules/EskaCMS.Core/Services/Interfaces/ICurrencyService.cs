using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface ICurrencyService
    {
        public SiteSettings GetDefaultCurrencyCulture();
        Task<CurrencyRates> GetCurrentUserCurrency(long SiteId = 0);
        Task<CurrencyRates> GetDefaultCurrency();
        public string GetDefaultDecimalPlaces(long siteId);
        public string GetDefaultDecimalPlaces();
        public string GetDefaultRoundDigit();

        public string GetUserCurrencyCodeForAdmin();
        public string FormatCurrencyForAdmin(decimal value);

        public string FormatCurrency(decimal value);
        public string FormatCurrency(decimal value, string CurrCulture);

        public string FormatCurrency(decimal value, string decimalPlace, User CurrentUser, string OrderCurrency, SiteSettings CurrencyCulture);
        public string ConvertAndFormateToCurrency(decimal value, CurrencyRates TargetCurrency, string decimalPlace);
        public decimal ConvertToCurrency(decimal value, CurrencyRates TargetCurrency, string decimalPlace);
        decimal ConvertToDefault(decimal value);
        string GetUserCurrencyCode();
        string GetDefaultCurrencyCode();
        string FormatCurrencyRound(decimal value);
        decimal DcimalRound(decimal value);
        string GetCurrency(string AllowedCurrencies, string ISOCurrency);
        string FormatRound(string Currency, decimal value);
        decimal FormatRoundForFile(decimal value);
        public string GetDefaultCurrencyCodeForAdmin();

        string GetCurrencyCode(string CountryCode);
        Task<CurrencyVM> GetDefaultUserCurrency();
        Task<CurrencyRates> GetUserCurrencyRateOrDefault();
        Task<CurrencyRates> GetCurrencyByCultureId(string CultureCode);

        string FormatByUserCurrency(decimal value);

        Task<CurrencyRates> GetCurrencyByName(string Name);
        Task<CurrencyByCultureVM> GetUserCurrencyByCulture();
        decimal RoundToDecimalPlaces(decimal value, int decimalPlaces);
        decimal FormatByCurrentCurrency(decimal value);
        Task<CurrencyRates> DefaultSiteCurrencyRate();
        Task<string> FormatCurrencyWithText(decimal number);
    }
}
