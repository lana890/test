using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class CurrencyRatesApiService : ICurrencyRatesApiService
    {

        private readonly IRepository<CurrencyRates> _CurrencyRepository;
        private readonly IRepositoryWithTypedId<Culture, string> _CultureRepository;

        public CurrencyRatesApiService(IRepository<CurrencyRates> CurrencyRepository, IRepositoryWithTypedId<Culture, string> CultureRepository)
        {
            _CurrencyRepository = CurrencyRepository;
            _CultureRepository = CultureRepository;
        }
        public IEnumerable GetAllCurrencies(long SiteId)
        {
            // this function will return currencies from CurrencyRates Table by language
            try
            {
                var currencies = _CurrencyRepository.Query()
                    .AsNoTracking()
                    .Where(c => c.SiteId == SiteId && !c.IsDeleted).ToList();
                return currencies;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public IEnumerable GetAllCurrencies(string Culture)
        {
            try
            {
                string lang = "", contry = "";
                if (!string.IsNullOrEmpty(Culture))
                {
                    lang = Culture.Split("-")[0];
                    contry = Culture.Split("-")[1];

                }

                var currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                         .Select(ci => ci.LCID).Distinct()
                         .Select(id => new RegionInfo(id))
                         .GroupBy(r => r.ISOCurrencySymbol)
                         .Select(g => g.First())
                         .Where(r => GetCultureCode(r.Name).Contains(lang)
                         || string.IsNullOrEmpty(Culture)
                         || GetCultureCode(r.Name).Contains(contry))
                         .Select(r => new CurrencyRatesVM
                         {
                             //Code = r.ISOCurrencySymbol,
                             Name = r.CurrencyNativeName,
                             CultreCode = GetCultureCode(r.Name)
                             //Name_2 = r.CurrencySymbol,
                             //CultureId = GetCultureCode(r.Name)
                         });
                return currencies;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public IEnumerable GetAllCurrencies()
        {
            try
            {
                var currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                         .Select(ci => ci.LCID).Distinct()
                         .Select(id => new RegionInfo(id))
                         //.GroupBy(r => r.ISOCurrencySymbol)
                         //.Select(g => g.First())
                         .Select(r => new CurrencyRatesVM
                         {
                             //Code = r.ISOCurrencySymbol,
                             Name = r.CurrencyEnglishName,
                             CultreCode = GetCultureCode(r.Name),
                             //Name_2 = r.CurrencySymbol,
                             //CultureId = GetCultureCode(r.Name)
                         });
                return currencies;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        private string GetCultureCode(string country)
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var languagesSpoken = allCultures.Where(c =>
            {
                if (c.IsNeutralCulture) return false;
                if (c.LCID == 0x7F) return false; // Ignore Invariant culture
                var region = new RegionInfo(c.LCID);
                return region.TwoLetterISORegionName == country;
            }).Select(c => c.Name).FirstOrDefault();
            return languagesSpoken;
        }
        public SmartGridOutputVM<CurrencyRatesVM> GetCurrencies(SmartSearchTableParam<CurrencyRatesSearchListVM> param, long SiteId)
        {
            CurrencyRatesSearchListVM search = param.Search;
            var CurrencyRatesQuery = _CurrencyRepository.Query().AsNoTracking()
                                      .Where(x =>
                                          x.SiteId == SiteId && !x.IsDeleted &&
                                          (string.IsNullOrEmpty(search.Text) ||
                                          (x.Name.ToUpper().Contains(search.Text.ToUpper()))) &&
                                          (string.IsNullOrEmpty(search.currencyCulture) || x.CultureCode.Contains(search.currencyCulture)))
                                       .ToList();

            SmartGridOutputVM<CurrencyRatesVM> currencies = new SmartGridOutputVM<CurrencyRatesVM>
            {
                Items = CurrencyRatesQuery.Select(x => new CurrencyRatesVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    CultreCode = x.CultureCode,
                    CurrencyCulture = x.CultureCode?.Split('-').LastOrDefault(),
                    Rate = x.Rate,
                    IsDefaultRate = x.IsDefaultRate,
                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).ToList(),
                TotalRecord = CurrencyRatesQuery.Count()
            };

           
            return currencies;
        }
        public async Task<IList<CultureVM>> GeUsedCultures()
        {
            try
            {
                return await _CultureRepository.Query().Select(x => new CultureVM
                {
                    Id = x.Id,
                    Name = x.Name
                }).Distinct().ToListAsync();

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<CurrencyRatesVM> GetCurrencyById(long Id)
        {
            var currency = _CurrencyRepository.Query();
            if (Id == 0)
            {
                return await currency.Where(x => x.IsDefaultRate).Select(x => new CurrencyRatesVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    CultreCode = x.CultureCode,
                    Rate = x.Rate,
                    IsDefaultRate = x.IsDefaultRate,
                }).FirstOrDefaultAsync();
            }
            else
            {
                return await currency.Where(x => x.Id==Id).Select(x => new CurrencyRatesVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    CultreCode = x.CultureCode,
                    Rate = x.Rate,
                    IsDefaultRate = x.IsDefaultRate,
                }).FirstOrDefaultAsync();

            }

        }
        public async Task<bool> Delete(long Id)
        {
            var currency = await _CurrencyRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
            // currency.IsDeleted = true;
            _CurrencyRepository.Remove(currency);
            await _CurrencyRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Create(CurrencyRatesVM Currency, long SiteId)
        {
            CurrencyRates currencyObj = new CurrencyRates();
            string ISOCurrencySymbol = new RegionInfo(Currency.CultreCode).ISOCurrencySymbol;
            CultureInfo CurrencyCultre = new CultureInfo(Currency.CultreCode);

            currencyObj.Name = ISOCurrencySymbol + " - " + CurrencyCultre.NumberFormat.CurrencySymbol;
            currencyObj.Rate = Currency.Rate;
            currencyObj.IsDefaultRate = Currency.IsDefaultRate;
            currencyObj.SiteId = SiteId;
            currencyObj.IsDeleted = false;
            currencyObj.CultureCode = Currency.CultreCode;
            _CurrencyRepository.Add(currencyObj);
            await _CurrencyRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> Edit(CurrencyRatesVM Currency)
        {
            var currencyObj = _CurrencyRepository.Query().FirstOrDefault(x => x.Id == Currency.Id);
            if (currencyObj == null)
            {

                throw new Exception("Currency Not Found");
            }
            string ISOCurrencySymbol = new RegionInfo(Currency.CultreCode).ISOCurrencySymbol;
            CultureInfo CurrencyCultre = new CultureInfo(Currency.CultreCode);

            currencyObj.Name = ISOCurrencySymbol + " - " + CurrencyCultre.NumberFormat.CurrencySymbol; ;
            currencyObj.IsDefaultRate = Currency.IsDefaultRate;
            currencyObj.Rate = Currency.Rate;

            currencyObj.CultureCode = Currency.CultreCode;
            await _CurrencyRepository.SaveChangesAsync();
            return true;
        }


    }
}
