using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Localization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Localization.Services.Interfaces
{
    public interface ILocalizationServices
    {
        Task<bool> UpsertLocalization(List<LocalizationViewModel> model, long siteId, long cultureId);
        Task<List<string>> GetComponentKeys(string ComponentKey);
        Task<Dictionary<string, Dictionary<string, string>>> GetLocalization(string Slug, long SiteId, long CultureId);
        Task<PaginationResult<LocalizationViewModel>> GetAllKeys(SmartSearchTableParam<LocalizationViewModel> param);
        Task<bool> ScanLocalization(string Component, string key, string Group = null, string Slug = null);
        Task<string> GetLocalizationValueByKey(string ResourcesKey);
        Task<bool> EditLocaliztion(LocalizationValueEditVM model);
        Task<bool> UpdateBulkLocaliztionValues(List<LocalizationValueEditVM> model);


    }
}
