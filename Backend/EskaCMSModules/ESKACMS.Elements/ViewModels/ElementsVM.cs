using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EskaCMS.Entities.Entity.Core;

using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Elements.ViewModels
{
    public class ElementsVM
    {

        public ElementsVM()
        {
            this.CategoryId = (long)ECategory.SharedElements;
        }
        public long Id { get; set; }

        public string ElementTypeId { get; set; }

        public long? ElementId { get; set; }
        public string Settings { get; set; }
        public long SiteId { get; set; }
        public long? CultureId { get; set; }
        public long ParentId { get; set; }

        public string Name { get; set; }
        public string DataKey { get; set; }

        public long CategoryId { get; set; }
        public Category Category { get; set; }



        public string IdentifierCode { get; set; }

    }


    public class ElementsCategoriesViewModel
    {
        public CategoryElementViewModel Category { get; set; }
        public IList<ElementsVM> Components { get; set; }
    }

    public class GetElementByCodeVM
    {
        public string IdentifierCode { get; set; }
        public long? CultureId { get; set; }
    }
    public class CategoryElementViewModel
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public bool IncludeInMenu { get; set; }
        public int DisplayOrder { get; set; }
        public ECategoryTypes CategoryTypeId { get; set; }

        public MediaViewModel MediaCategory { get; set; }
        public EStatus Status { get; set; }

        public string Slug { get; set; }
        public long? CategoryTemplateId { get; set; }

        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string Description { get; set; }






        public string CategoryType { get; set; }

        public string CultureCode { get; set; }
        public long? CultureId { get; set; }
    }

    public class CategoryDetailsElementViewModel

    {

        public string Slug { get; set; }
        public long? CategoryTemplateId { get; set; }

        public ECategoryTypes CategoryTypeId { get; set; }
        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string Description { get; set; }






        public string CategoryType { get; set; }

        public string CultureCode { get; set; }
        public long? CultureId { get; set; }
    }

    public class MediaViewModel

    {
        public long? Id { get; set; }
        public string PublicUrl { get; set; }
        public string FileName { get; set; }
    }

}
