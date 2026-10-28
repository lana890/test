using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models.SitesCultures;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{
    public class SiteSettingsVM
    {
        public long? SiteId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Module { get; set; }

    }


    public class SiteHostNamesWithCulture
    {
        public long CultureId{ get; set; }
        public EStatus CultureStatus{ get; set; }
        public string CultureCode{ get; set; }

        public long SiteId { get; set; }

        public List<SitesHostNamesViewModelWithStatus> HostNames { get; set; }
    }

    public class HostNamesValidationRequestVM
    {
        public string HostName { get; set; }
       
    }
    public class HostNamesValidationResultVM
    {
        public bool IsHostNameAvailable { get; set; }

        public string AlternativeHostName { get; set; }

    }
    public class SiteHostNamesWithCultureVM
    {
        public long CultureId { get; set; }
        public EStatus CultureStatus { get; set; }
        public string CultureCode { get; set; }

        public long SiteId { get; set; }

        public string HostNames { get; set; }

        public string Prefix { get; set; }
    }
    public class siteCultureObj{
		public long Id { get; set; }
		public string name { get; set; }
		public string code { get; set; }
	}
    public class SitesViewModel
    {
        public SitesViewModel()
        {
            HostNames = new List<SitesHostNamesViewModel>();
            SiteCulturesList = new List<SitesCulturesGetAllVM>();
            SiteCultures = new List<siteCultureObj>();
            AppSettings = new List<SiteSettingsVM>();
    }
        public long SiteId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
		public string SiteImage { get; set; }
		public string ImageUrl { get; set; }
        public string Css { get; set; }
        public long? OrganizationId { get; set; }
        public long? CoreBranchId { get; set; }
        public bool IsADAuthRequired { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }

        public string CountryId { get; set; }
        public string CountryName { get; set; }
        public bool HasDefaultCountry { get; set; }
        public long CompanyId { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public EStatus Status { get; set; }

        public long DefaultParentPageId { get; set; }
        public string StatusDesc { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public bool IsDefault { get; set; }

        public List<SitesHostNamesViewModel> HostNames { get; set; }
        public List<SitesCulturesGetAllVM> SiteCulturesList { get; set; }
        public List<siteCultureObj> SiteCultures { get; set; }
       // public string Settings { get; set; }
        public List<SiteSettingsVM> AppSettings { get; set; }
    }
    public class SitesAddVM
    {


        public long DefaultParentPageId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
        public string Css { get; set; }
        public long? OrganizationId { get; set; }
        public int? CoreBranchId { get; set; }
        public bool IsADAuthRequired { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }
        public bool IsDefault { get; set; }
        public long? SiteId { get; set; }

        public string CountryId { get; set; }

        public List<SitesHostNamesViewModel> HostNames { get; set; }
        public List<string> SiteCultures { get; set; }
        public string Settings { get; set; }
        public List<SiteSettingsVM> AppSettings { get; set; }
        public long userId { get; set; }
    }
    public class MetaDataViewModel
    {
        public long CultureId { get; set; }
        public string AddtionalMetaTags { get; set; }
        public string PrimaryMetaTags { get; set; }

    }
    public class MetaDatawithCultureViewModel
    {
        public long CultureId { get; set; }
        public string AddtionalMetaTags { get; set; }
        public string CultureName { get; set; }
        public string PrimaryMetaTags { get; set; }

    }
    public class SiteScriptVM
    {
        public string script { get; set; }
    }
    public class ScriptVM
    {
        public long siteId { get; set; }
        public string script { get; set; }
    }

}
