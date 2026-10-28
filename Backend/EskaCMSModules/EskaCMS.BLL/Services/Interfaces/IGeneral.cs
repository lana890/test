
using EskaCMS.Entities.Entity;
using EskaCMS.Menus.Models;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.BLL.Services.Interfaces
{
    public interface IGeneral
    {
        Task<List<SiteSlugsVM>> GetSiteSlugs(long SiteId, long UserId);
        Task<bool> TransferCMSFiles(string FileName, string Description, long CreatedById, long SiteId, EComponentType ComponentType, IFormFileCollection Files);
        // Task<bool> AddNewTypesSettings(TypesSettingsViewModel TypesSettings);
        List<BuildVersions> GetAllVersion();
        // Task<bool> EditTypesSettings(long TypesSettingsId, TypesSettingsViewModel TypesSettings);
        //Task<TypesSettings> GetTypesSettingsByTypesSettingsId(long TypesSettingsId);
        //Task<TypesSettings> GetTypesSettingsByPostId(long PostId);
        //  Task<bool> DeleteTypesSettings(long TypesSettingsId);
        List<string> GetAllCulture();

        Task<List<string>> GetBreadcrumByType(int type, long id, long siteId);

        //Form Builder
        //Task<bool> AddNewFormBuilder(FormDataVM data, IFormFileCollection files);
        Task<string> CheckSiteSlugAsync(long siteId, string slug);

        Task<MenuRolesViewModel> GetPageAssginedRoles(long Id, long SiteId);
        Task<bool> SavePageAssginedRoles(MenuRolesViewModel vm);
        Task<List<SitesHostNamesViewModel>> GetCulturebysiteId(long SiteId);
        public DateTime? HijriToGreg(string Hijri);

        public string GregToHijri(string greg);


    }
}
