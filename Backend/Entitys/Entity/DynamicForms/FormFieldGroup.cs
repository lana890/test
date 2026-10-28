
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public partial class FormFieldGroup : EntityBase
    {
        public string Name { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public long FormItemId { get; set; }
        public FormItem FormItem { get; set; }

        public IList<FormFieldDetails>  FormFieldDetails { get; set; }
        [NotMapped]
        public long cultureId { get; set; }


    }
}
