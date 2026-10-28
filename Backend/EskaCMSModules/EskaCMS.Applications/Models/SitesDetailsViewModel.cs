using EskaCMS.Entities.Entity;
using EskaCMS.Core.ViewModels;
using System.Collections.Generic;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{

    public class SitesDetailsViewModel
    {

        public SitesDetailsViewModel()
        {
            Site = new SiteVM();
            DefaultTheme = new ThemeVM();

            //SiteSlugs = new List<SiteSlugsVM>();
            //PostSlugs = new List<SiteSlugsVM>();
        }
        public string Prefix { get; set; }
        public SiteVM Site { get; set; }
        public long Culture { get; set; }
        public string CultureName { get; set; }
        //  public string UserCurrencyCulture { get; set; }
        public string Currency { get; set; }
        public long CurrencyId { get; set; }
        public string hostname { get; set; }
        public  ThemeVM DefaultTheme { get; set; }

        public List<SiteSlugsVM> SiteSlugs { get; set; }

        public List<SiteSlugsVM> PostSlugs { get; set; }

        public List<HostWithCultrueVM> SiteHostNames { get; set; }


    }


    public class DefaultPageObjVM
    {
        public long ParentId{ get; set; }
    }

    public class HostWithCultrueVM
    {
        public string HostName { get; set; }
        public string CultureCode { get; set; }


    }

    public class HostMatchViewModel
    {
        public long Id { get; set; }
        public int Score { get; set; }


    }
    public class SiteVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ThumbnailId { get; set; }
       // public string Css { get; set; }
        public long? OrganizationId { get; set; }
        public long? CoreBranchId { get; set; }
        public bool IsADAuthRequired { get; set; }
        public bool IsCoreIntegration { get; set; }
        public long  DefaultParentPageId { get; set; }
        public string DefaultSlug { get; set; }
        public LoginType LoginType { get; set; }

        public string CultureStyleUrl { get; set; }
        public string PrimaryStyleUrl { get; set; }
        public string ThemeStyleUrl { get; set; }

        public string PrimaryScripts { get; set; }

        public string LogoURL { get; set; }
        // public string SiteSettings { get; set; }

        /// <summary>
        /// Public, site-level id of the Guest role (ERoleTypes.Guest). Populated in
        /// GetSiteIdByHostNameSP via the SAME name-based lookup guests are assigned with, so
        /// it always equals the RoleIds claim inside a guest token. Lets the SSR-cached
        /// storefront resolve guest visibility deterministically when a brand-new anonymous
        /// browser has no guest token yet. Not user-specific — safe in shared/SSG cache.
        /// Serialized as "guestRoleId".
        /// </summary>
        public long GuestRoleId { get; set; }

    }
    public class ThemeVM
    {
        public string Name { get; set; }



        public string Colors { get; set; }
        public string Classes { get; set; }
        public string Fonts { get; set; }
        public string StyleUrl { get; set; }
        public string Scripts { get; set; }
        public List<ImportedFilesVM> ImportFiles { get; set; } = new List<ImportedFilesVM>();

    }

    public class SiteHostProc
    {
        public string LogoUrl { get; set; }
        public string SiteId { get; set; }
        public string HostName { get; set; }
        public string SiteName { get; set; }
        public string Css { get; set; }
        public string SiteContentSettings { get; set; }
        public string CultureCodeId { get; set; }
        public string ThemeName { get; set; }
        public string Colors { get; set; }
        public string Classes { get; set; }
        public string Fonts { get; set; }
        public string Styles { get; set; }
        public string Scripts { get; set; }
        public string ImportFiles { get; set; }
        public string PageId { get; set; }
        public string PageName { get; set; }

        public string PageSlug { get; set; }
        public string BlogId { get; set; }

        public string BlogSlug { get; set; }
        public string Currency { get; set; }
        public string Title { get; set; }
        public string SlugsTypes { get; set; }


    }


    public class SiteContentSettingsVM
    {
        public string defaultPage { get; set; }
        public List<SiteModuleSettingsVM> siteModules { get; set; }

    }



    public class SiteModuleSettingsVM
    {
        public string moduleName { get; set; }
        public string path { get; set; }

    }


}
