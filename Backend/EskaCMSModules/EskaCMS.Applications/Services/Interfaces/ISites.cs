
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Pages.Models;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Model;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;


namespace EskaCMS.Sites.Services.Interfaces
{
    public interface ISites
    {
        Task<SitesDetailsViewModel> GetSiteIdByHostNameSP(string HostName);
        Task<SiteBaseDataViewModel> GetSiteId(string HostName);
        Task<string> GetMetaTages(string Slug);
        Task<long> CompleteAddSite(SitesAddVM Site);
        Task<long> UpsertSiteInfo(SitesAddVM Site);
        Task<List<SitesViewModel>> GetAllSites(long UserId);
        Task<ExpandoObject> GetAssignedUnassignedSites(long UserId);
        Task<SitesDetailsViewModel> GetSiteIdByHostName(string HostName);

        Task<List<HostWithCultrueVM>> GetSiteHostNamesWithCulture(long SiteId);
        Task<PageTemplateVM> GetTemplateSettingsBySlugSP(string Slug);
        Task<bool> EditSite(SitesViewModel Site, long username);
        Task<bool> SetSiteAsDefault(long SiteId, long UserId);
        Task<bool> SetSiteCSS(SitesViewModel Site);
        Task<string> GetSiteCss(long SiteId);
        Task<bool> DeleteSite(long SiteId);
        Task<SitesViewModel> GetSiteDetails(long SiteId);
        Task<string> CheckSiteHosts(long SiteId, string host);
        Task<bool> DeleteSiteHost(long Id);
        Task<long> AddSiteHostName(SitesHostNamesViewModel HostName);
        Task<bool> EditSiteHostName(SitesHostNamesViewModel HostName);
        Task<bool> AddSiteSettings(SiteSettingsVM settings);
        Task<bool> EditSiteSettings(SiteSettingsVM settings);
        Task<bool> DeleteSiteSettings(string name);
        Task<SiteGridOutputVM<SitesViewModel>> GetList(SiteSearchTableParam<SiteListSearchVM> param);
        Task<bool> AddSiteCultures(List<SitesCulturesViewModel> Cultures);
        Task<bool> SetDefaultPage(long SiteId, long UserId, long ParentPageId);

        List<string> GetSiteCultureCodes(long siteId);
        Task<IList<SiteUsedCulture>> GetUsedSiteCultures(long? SiteId);
        Task<IList<StatusSiteCulturesVM>> GetUsedSiteCulturesByStatus(long? SiteId, long? Status);

        Task<IList<SiteUsedCulture>> UpsertHostNamesWithCultures(List<SiteHostNamesWithCulture> model, long UserId);
        Task<bool> AddMetaDataToHostName(long siteId,List<MetaDataViewModel> metaDataViewModel);
        Task<List<MetaDatawithCultureViewModel>> GetMetaDataWithCulture(long siteId);
        Task<bool> AddSiteScripts(long siteId, string script);
        Task<SiteScriptVM> GetSiteScripts(long siteId);
        Task<List<SiteHostNamesWithCultureVM>> GetHostNameBySiteId(long siteId,string Hostname);
        Task<string> GetActiveSiteHostName(long siteId);
        Task<string> GetThemeVariableStyle(long SiteId);
        Task setThemeVariableStyle(string Css, long SiteId);
        Task<bool> UpdateUserCulture( long cultureId);
        Task<HostNamesValidationResultVM> ValidateHostNameExistence(HostNamesValidationRequestVM model, long userId);
        Task<DefaultSlugDto> GetDefaultSlug();
    }
}
