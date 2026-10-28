
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public partial class FormField : EntityBase
    {
        public FormField()
        {
            FormFieldDetails=new List<FormFieldDetails>();    
        }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicCriteriaInputTypes InputType { get; set; }

        public IList<FormFieldDetails> FormFieldDetails { get; set; }
        //[Required(ErrorMessage = "The {0} field is required.")]

        public long? FormItemId { get; set; }
        public FormItem FormItem { get; set; }

        public bool IsGeneral { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }


    }
}
