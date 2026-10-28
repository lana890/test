
using EskaCMS.Entities.Entity;
using EskaCMS.Pages.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Page = EskaCMS.Entities.Entity.Pages.Page;

namespace EskaCMS.Pages.Services.Interfaces
{
    public interface IPages 
    {
        Task<bool> UpdatePagesStatus(string PagesIds, int Status, long UserId, long siteId);
        Task<bool> UpdatePagesTemplate(string PagesIds, int TemplateId, long UserId, long siteId);
        Task<bool> DeletePages(string PagesIds, long UserId, long siteId);
        Task<List<PageResultVM>> DublicatePage(long ParentId, long SiteId, long UserId);
        Task<IList<PageVersionVM>> SavePageSettings(SavePageSettingsViewModel SavePageSettingsModel, long siteId);
        Task<PageResultWithTotalPagesVm> GetAllPages(GetAllPagesSearchParamsVM getAllPagesSearchParamsVMl, long SiteId);
        Task<IList<PageResultVM>> GetAllPagesFlat(string PageName = null, long? PageCategory = null, EStatus ? Status=null, long siteId = 0, long cultureId = 0);
        Task<PagesViewModel> GetPageSettings(long pageId);
        Task<bool> DeletePageByID(long PageId, long UserId);
        Task<IList<PageTemplates>> GetAllPageTemplates(long SiteId, EStatus? Status ,ETemplateTypes? Type = null );
        Task<PageTemplateVM> GetPageTemplateSettings(long TemplateId);
        Task<PageTemplates> AddPageTemplates(PageTemplates pageTemplate, long SiteId, long UserId);
        Task<PageTemplates> EditPageTemplates(PageTemplates pageTemplate, long UserId, long PageId);

        Task<bool> DeleteTemplateByID(long ID, long UserId);

        Task<Page> GetPageBySlug(string slug);
        Task<IList<VersionsViewModel>> SaveTemplatesSettings(TemplateSettingsModel templateSettingsModel, long siteId);
        Task<bool> SetTemplate(TemplateSettingsModel template);

        Task<PageTemplates> DuplicateTemplate(PageTemplates pageTemplate, long TemplateId, long UserId);
        Task<Entities.Entity.Pages.Page> GetPageByIdAsync(int id, long siteId);
        Task<List<VersionsViewModel>> GetTemplatesVersions(long TemplateId);
        void UpdatePageVersion(long? pageId, long versionId);
        void UpdateTemplateVersion(long? templateId, long versionId);
        Task<IList<PageVersionVM>> GetAllPageVersions(long pageId);
        int ChangePageAndTemplatesVersions();
        Task<Entities.Entity.Pages.Page> ClonePages(long TargetPageId, long SourcePageId,long SiteId);
        Task<List<PageIdAndCultureIdVM>> AddEditPage(long UserId, long SiteId, List<PagesViewModel> Page);

        Task<bool> SetTemplate(TemplateSettingsModel template, UpdateTemplateEnum Target);
        Task<List<RelatedPageVM>> GetRelatedPages(long ParentId, long SiteId);

        Task<List<PageCultureDetailsVM>> GetPageCultures(long ParentPageId, long SiteId);

        Task<bool> updatePageRoles(long ParentId, List<long> RolesIdList, long SiteId, long UserId);
        Task<bool> DeleteImportedFile(long Id);
        Task<TemplateVersionVM> GetTemplateData(long TemplateId);
        Task<IList<PageSiteMapResultVM>> GetAllPagesBysiteId(long siteId = 0, EStatus Status = EStatus.Active,long? PageCategory = null);
        Task<Dictionary<string, object>> GetComponentsSettings(long PageId, string ComponentId, string Culture,string IdentifierCode,bool? isComponentShared);
        Task<bool> ChangeParentByCultureId(long siteId, long cultureId);
        Task<bool> AddFavouritePageFroUser(long UserId,long PageId);
        Task<List<FavouritePageVM>> GetAllFavouritePage();
        Task<bool> RemoveFavouritePage(long favouritePgaeId);
        Task<bool> SetLoginPage(SetLoginPageVM model);
        Task<string> GetPreviewCode(long pageVersionId);
        Task<PageVersionVM> GetVersionSettings(long verisonId);
        Task<TemplateVersionVM> GetTemplateVersionSettings(long templateVersionId);
        Task<IList<PageVersionVM>> UpdatePageVersionAlias(EditAliasViewModel model);
        Task<IList<VersionsViewModel>> UpdateTemplateVersionAlias(EditAliasViewModel model);
        Task<string> GetComponentSettingsById( string settingsId);
        Task<bool> AddComponentSettings(UpsertComponentSettingsVM model);
        Task<bool> DuplicateComponentSettings(DuplicateComponentSettingsVM model);
        Task<IEnumerable<PageComponentSettings>> FilterComponentsSettings(FilterComponentsSettingsVM model);
        Task<IEnumerable<Page>> GetAllPagesByCultures(long[] ids);
        Task<IEnumerable<string>> GetAllPagesByHostname(GetAllPagesByHostnameVM model);

        Task<string> GetSpecificSettingForPage(string slug, string componentName, string settingKey);
        Task ReplacePageComponent(ReplaceComponentBody model);
    }

}
