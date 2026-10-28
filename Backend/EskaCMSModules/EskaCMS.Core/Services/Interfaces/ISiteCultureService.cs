using EskaCMS.Core.Models.SitesCultures;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.Core.Interfaces
{
    public interface ISiteCultureService
    {
        Task<bool> Add(SitesCulturesViewModel culture);
        Task<bool> AddSiteCultures(List<SitesCulturesViewModel> Cultures);
        SitesCulturesGetAllVM GetCultureById(long Id);
        Task<bool> Edit(SitesCulturesViewModel SiteCulture);
        Task<bool> DeleteSiteCulture(long id);
        Task<List<SitesCulturesGetAllVM>> GetSiteCultures(long? SiteId);
        Task<bool> AddCssStyleToSiteCultures(long CultureId, string CssStyle,long SiteId);
        Task<string> GetCssStyle(long CultureId);

        Task<long> GetDefaultSiteCulture(long siteId);
    }
}