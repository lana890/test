using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
namespace EskaCMS.Entities.Entity.DynamicForms
{
    public class FormFieldDetails:EntityBase
    {
        public long? FormFieldId { get; set; }
        public FormField FormField { get; set; }

        //[Required(ErrorMessage = "The {0} field is required.")]
        //public long PricingMatrixId { get; set; }
        //public virtual FormItem FormItem { get; set; }//Will Move From PricingMatrixId To DataSource

        [Required(ErrorMessage = "The {0} field is required.")]
        public long CategoryId { get; set; }
        public virtual Category Category { get; set; }
        public string StaticApiLink { get; set; }

        public long? DataSourceId { get; set; }
        public ApiMapper DataSource { get; set; }
        public string ClassesNames { get; set; }
        public string PlaceHolder { get; set; }
        public string Validations { get; set; }
        public string AdditionalInfo { get; set; }
        public long? GroupId { get; set; }
        public FormFieldGroup Group { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
    }
}
