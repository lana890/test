using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class FormFieldDetailsVM
    {
        public long? FormFieldId { get; set; }
        public FormField FormField { get; set; }
        public string ClassesNames { get; set; }
        public string PlaceHolder { get; set; }
        public string Validations { get; set; }
        public string AdditionalInfo { get; set; }
        public string Name { get; set; }

        public long? GroupId { get; set; }
        public FormFieldGroup? Group { get; set; }
        public long CultureId { get; set; }
        public SitesCultures? Culture { get; set; }



        public long ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; } = new Category();
        public long? DataSourceId { get; set; }
        public ApiMapper DataSource { get; set; }
        public virtual List<ChildDetailsVM>? ChildrenCategories { get; set; }

        public string StaticApiLink { get; set; }
        public long CategoryId { get; set; }

    }

    public class FormFieldDetailsVMs
    {
        public long? FormFieldId { get; set; }
        public string ClassesNames { get; set; }
        public string PlaceHolder { get; set; }
        public string Validations { get; set; }
        public string AdditionalInfo { get; set; }
        public string Name { get; set; }

        public long? GroupId { get; set; }
        public long CultureId { get; set; }

        public long ParentCategoryId { get; set; }
        public long? DataSourceId { get; set; }
        public string StaticApiLink { get; set; }
        public virtual List<ChildDetailsVM> ChildrenCategories { get; set; }



    }

    public class ChildDetailsVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? SlugInfoId { get; set; }
        public string? Slug { get; set; }
        public long? CultureId { get; set; }
        public long? CategoryTemplateId { get; set; }
        public long CategoryId { get; set; }
    }

    //by haya
    public class ChildDetailsMainInfoVM
    {
        public long Id { get; set; }
        public string Name { get; set; }

    }
    public class FormFieldCultures
    {
        public long SiteId { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public string CultureId { get; set; }

        public string CulturePrefix { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.CultureType CultureType { get; set; }

    }



    public class FormFieldDetailsMainInfoVM
    {
        public long? FormFieldId { get; set; }

        public long CategoryId { get; set; }
        public CategoryVM Category { get; set; }
        public string StaticApiLink { get; set; }

        public long? DataSourceId { get; set; }
        public string AdditionalInfo { get; set; }


    }
}



