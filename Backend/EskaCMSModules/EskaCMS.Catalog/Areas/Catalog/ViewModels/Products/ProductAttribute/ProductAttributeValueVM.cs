using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;
namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductAttributeGlobalVM
    {

        public List<ProductAttributeModel> Attribute { get; set; }
        public List<ProductAttributeValueModel> AttributeValue { get; set; }

    }
    public class ProductAttributeValueProductModel
    {
        public long AttributeValueId { get; set; }
        public string Value { get; set; }

        public long CultureId { get; set; }

        public string CultureName { get; set; }

        public long ProductId { get; set; }
        public long AttributeId { get; set; }
    }

    public class ProductAttributeValueModel

    {
        public long AttributeValueId { get; set; }
        public string Value { get; set; }

        public long CultureId { get; set; }

        public string CultureName { get; set; }

        public long ProductId { get; set; }


    }

    public class ProductValueVM
    {
        public long AttributeId { get; set; }

        public long ProductId { get; set; }

        public List<AttributeValueCultureViewModel> AttributeValueCulture { get; set; } = new List<AttributeValueCultureViewModel>();
     

    }

    public class RequestProductValueVM
    {
        public long AttributeId { get; set; }


        public List<AttributeValueCultureViewModel> AttributeValueCulture { get; set; } = new List<AttributeValueCultureViewModel>();


    }

    public class GetProductValueVM
    {
        public long AttributeId { get; set; }
        public string AttributeName { get; set; }
        public long AttributeValueId { get; set; }

        public long ProductId { get; set; }

        public List<AttributeValueCultureViewModel> AttributeValueCulture { get; set; } = new List<AttributeValueCultureViewModel>();


    }



    public class AttributeValueCultureViewModel
    {

        public string Value { get; set; }
        public long CultureId { get; set; }

    }
    public class ProductAttributeModel
    {
        public long AttributeId { get; set; }   
        public string AttributeName { get; set; }
        public long CultureId { get; set; }
           
    }
}
