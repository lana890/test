using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.ViewModels
{
    public partial class OrganizationAddVM
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
        public string CountryId { get; set; }
        public long AddressId { get; set; }
        public long CategoryId { get; set; }
        public string ContactEmail { get; set; }
        public string ContactMobileNo { get; set; }
        public int? CoreCompanyId { get; set; }

    }
    public partial class OrganizationUpdateVM
    {

        public long Id { get; set; }
        public EStatus Status { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? ThumbnailIdId { get; set; }
        public string CountryId { get; set; }
        public long AddressId { get; set; }
        public long CategoryId { get; set; }
        public string ContactEmail { get; set; }
        public string ContactMobileNo { get; set; }
        public int? CoreCompanyId { get; set; }

    }
    public partial class OrganizationVM
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
        public string ThumbnailURL { get; set; }
        public string CountryId { get; set; }
        public string CountryName { get; set; }
        public long AddressId { get; set; }
        public string AddressName { get; set; }
        public long CategoryId { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EStatus Status { get; set; }

        public DateTimeOffset StatusDate { get; set; }

        public string ContactEmail { get; set; }
        public string ContactMobileNo { get; set; }
        public int? CoreCompanyId { get; set; }

        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public virtual CategoryOrganzationViewModel Category { get; set; }
        public ICollection<OrganizationsSitesViewModel> Sites { get; set; }

    }
    public partial class OrganizationGetAllVM
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThumbnailURL { get; set; }
        public long? ThumbnailId { get; set; }

    }

    public class CategoryOrganzationViewModel
    {

        public long? Id { get; set; }
        public EStatus? Status { get; set; }

        public IList<CategoryDetailsOrganzationViewModel>? CategoryDetails { get; set; }
    }

    public class CategoryDetailsOrganzationViewModel
    {
        public string Name { get; set; }
        public long? CultureId { get; set; }
    }
    public class OrganizationsSitesViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThumbnailURL { get; set; }
        public long? ThumbnailId { get; set; }
        public long? OrganizationId { get; set; }
    }
    public class OrganizationsSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class OrganizationsGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class OrganizationsListSearchVM
    {
        public string Name { get; set; }
        public string CountryId { get; set; }
        public EStatus? Status { get; set; }
        public DateTimeOffset? CreationDateFrom { get; set; }
        public DateTimeOffset? CreationDateTo { get; set; }
    }
}
