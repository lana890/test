
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.DynamicForms.ViewModels
{

    public class FormFieldVM
    {

        public long Id { get; set; }

        public string Name { get; set; }

        public IList<FormFieldDetailsVM> FormFieldDetailsVMs { get; set; }

        public long? GroupId { get; set; }
        public FormFieldGroup Group { get; set; }

        //public long? PricingSpecificationId { get; set; }
        //public SitesCultures Culture { get; set; }
        public long? FormItemId { get; set; }

        public long? ParentCategoryId { get; set; }
        // public virtual Category ParentCategory { get; set; } = new Category();

        //public virtual IList<Category> ChildrenCategorie { get; set; }
        public IList<Category> ChildrenCategories { get; set; }

        public FormItemType Type { get; set; }

        public string ClassesNames { get; set; }

        public string PlaceHolder { get; set; }

        public EStatus Status { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }
        public string Validations { get; set; }
        public string AdditionalInfo { get; set; }
        public long? DataSourceId { get; set; }
        public ApiMapper DataSource { get; set; }

        public long CultureId { get; set; }
        public EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }

        public string StaticApiLink { get; set; }

        //by haya
        public IList<FormFieldDetailsVMs> FormFields { get; set; }

    }
    public class FormFieldVMs
    {
        public long Id { get; set; }


        public IList<FormFieldDetailsVMs> FormFields { get; set; }

        public EStatus Status { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }

        public bool? IsGeneral { get; set; }
        public EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }
        public long? FormItemId { get; set; }
        public long SiteId { get; set; }
    }
    public class ChildCategoriesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public long? ThumbnailImageId { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

        public int DisplayOrder { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public bool IncludeInMenu { get; set; }

        public long? ParentId { get; set; }

        public IList<Category> Children { get; protected set; }

        public long CategoryTypeId { get; set; }

        public long? SiteId { get; set; }

        public IList<CategoryDetails> CategoryDetails { get; set; }
        public EStatus Status { get; set; }

    }

    //by haya
    public class FromFieldDto
    {
        public long Id { get; set; }
        public FromFieldDto()
        {
            FormFieldDetails = new List<FormFieldDetailsVMs>();
        }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicCriteriaInputTypes InputType { get; set; }

        public IList<FormFieldDetailsVMs> FormFieldDetails { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public long FormItemId { get; set; }
        public FormItemDto FormItem { get; set; }


        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }
        public long SiteId { get; set; }


    }

    public class FormFieldMainInfoVM
    {

        public long Id { get; set; }

        public string Name { get; set; }
        public EDynamicCriteriaInputTypes Type { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }

        public long FormFieldId { get; set; }
        public FormFieldMainInfoVM()
        {
            FormFieldDetails = new List<FormFieldDetailsMainInfoVM>();
        }
        //by haya
        public IList<FormFieldDetailsMainInfoVM> FormFieldDetails { get; set; }

    }
}
