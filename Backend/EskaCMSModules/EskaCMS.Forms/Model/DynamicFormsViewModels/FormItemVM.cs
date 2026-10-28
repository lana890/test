using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using System.Dynamic;
using EskaCMS.Media.Models;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class FormItemVM
    {
        public long Id { get; set; }


        public string? ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }

        public IList<FormItemDetailsVM> Cultures { get; set; }

        public string Icon { get; set; }
        public string AdditionalInfo { get; set; }


        public EStatus Status { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long SiteId { get; set; }

        public IList<Classifications> Classifications { get; set; }

        public IList<FormField> FormField { get; set; }
        public IList<string> FormFieldNames { get; set; }


        public IList<FormFieldGroup> FormFieldGroups { get; set; }


        //By Haya
        public IList<FormItemDetailsWithClassiffictionVM> FormItemDetailsWithClassiffictionVM { get; set; }
        public FormItemType Type { get; set; }
    }
    public class RequestFormItemRoleViewModel
    {

        public long FormItemId { get; set; }
        public List<long>? RoleIds { get; set; } = new List<long>();
    }

    public class RequestFormItemWebViewModel
    {

        public List<long>? FormItemIds { get; set; }
        public FormItemType? type { get; set; }
    }

    public class FormItemAssignedRolesVM
    {

        public List<Role> AssingedRoles { get; set; }
        public List<Role> UnassingedRoles { get; set; }
    }
    public class UpsertFormItemVM
    {
        public long Id { get; set; }


        public string? ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }

        public IList<FormItemDetailsVM> Cultures { get; set; }

        public string? Icon { get; set; }
        public EStatus Status { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long SiteId { get; set; }
        public FormItemType Type { get; set; }
    }

    public class UpdateAdditionalInfoVM
    {
        public long Id { get; set; }
        public string AdditionalInfo { get; set; }
    }

    public class FormItemForWebVM
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }


        public string ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }

        public long FormItemId { get; set; }
        public long CultureId { get; set; }
        public IList<FormFieldForWebVM> FormFields { get; set; } = new List<FormFieldForWebVM>();

        public string Icon { get; set; }
        public string AdditionalInfo { get; set; }
        public FormItemType Type { get; set; }
    }


    public class FormFieldForWebVM
    {


        public long Id { get; set; }

        public string Name { get; set; }


        public long? GroupId { get; set; }
        public FormFieldGroup Group { get; set; }


        public long FormItemId { get; set; }

        public long ParentCategoryId { get; set; }
        public virtual Category ParentCategory { get; set; } = new Category();


        public virtual List<ChildCategoriesVM> ChildrenCategories { get; set; }


        public string ClassesNames { get; set; }

        public string PlaceHolder { get; set; }

        public EStatus Status { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }
        public string Validations { get; set; }
        public string AdditionalInfo { get; set; }
        public long? DataSourceId { get; set; }
        public ApiMapper DataSource { get; set; }


        public EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }

        public string StaticApiLink { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }

    }


    public class FormItemWithClassificationsVM
    {

        public dynamic FormItemDetails { get; set; }
        public dynamic Classifications { get; set; }
        public dynamic FormFields { get; set; }
        public FormItemType Type { get; set; }

        public FormItemWithClassificationsVM()
        {
            FormItemDetails = new ExpandoObject();
            Classifications = new ExpandoObject();
            FormFields = new ExpandoObject();
        }

    }






    //By Haya
    public class FormItemDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }
        public FormItemMediaVM Media { get; set; }
        public IList<FormItemDetailsVM> PricingMatrixDetailsVM { get; set; }

        public string Icon { get; set; }


        public EStatus Status { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long SiteId { get; set; }

        public IList<ClassificationVM> Classifications { get; set; }

        public IList<FormFieldVM> FormFields { get; set; }
        public IList<string> FormFieldsNames { get; set; }


        public IList<FormFieldGroupVM> FormFieldGroups { get; set; }
        public IList<FormItemDetailsWithClassiffictionVM> FormItemDetails { get; set; }

        public FormItemType Type { get; set; }

        public FormItemMediaVM Thumbnail { get; set; }
        public FormItemWithClassificationsVM DynamicFormItemWithClassifications { get; set; }


    }

    public class FormItemSummaryVM
    {
        public long Id { get; set; }

        public List<FormItemsCultureViewModel> FormItemDetails { get; set; }
        public string Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }
        public FormItemMediaVM Media { get; set; }
        public int FormItemDetailsNum { get; set; }

        public string Icon { get; set; }


        public EStatus Status { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long SiteId { get; set; }

        public int ClassificationsNum { get; set; }

        public int FormFieldNum { get; set; }
        public List<FormItemsClassificationVM> ClassificationsItems { get; set; }
        public List<FormFieldMainInfoVM> FormFieldsItems { get; set; }
        public string? Name { get; set; }
    }

    public class FormItemsCultureViewModel
    {
        public string Name { get; set; }
        public long CultureId { get; set; }

    }
}
